#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
DEPLOY_ENV="${DEPLOY_ENV:-${SCRIPT_DIR}/env/deploy.env}"

if [[ -f "${DEPLOY_ENV}" ]]; then
  # shellcheck disable=SC1090
  set -a
  source "${DEPLOY_ENV}"
  set +a
fi

: "${LW_SERVER:?Set LW_SERVER in ${DEPLOY_ENV} or environment}"
: "${LW_SERVER_USER:?Set LW_SERVER_USER in ${DEPLOY_ENV} or environment}"

LW_SERVER_PORT="${LW_SERVER_PORT:-22}"
LW_SERVER_DIR="${LW_SERVER_DIR:-/opt/lw}"
LW_IMAGE_PREFIX="${LW_IMAGE_PREFIX:-learnword}"
LW_IMAGE_TAG="${LW_IMAGE_TAG:-$(date +%Y%m%d%H%M%S)}"
LW_PLATFORM="${LW_PLATFORM:-linux/amd64}"

DIST_DIR="${SCRIPT_DIR}/dist"
IMAGE_ARCHIVE="${DIST_DIR}/learnword-images-${LW_IMAGE_TAG}.tar"
REMOTE="${LW_SERVER_USER}@${LW_SERVER}"
REMOTE_COMPOSE="${LW_SERVER_DIR}/docker-compose.yml"
REMOTE_ARCHIVE="${LW_SERVER_DIR}/$(basename "${IMAGE_ARCHIVE}")"

mkdir -p "${DIST_DIR}"

echo "Building LearnWord images with tag ${LW_IMAGE_TAG}"
(
  cd "${ROOT_DIR}"
  LW_IMAGE_PREFIX="${LW_IMAGE_PREFIX}" LW_IMAGE_TAG="${LW_IMAGE_TAG}" LW_PLATFORM="${LW_PLATFORM}" \
    docker compose -f deploy/docker-compose.build.yml build
)

echo "Saving images to ${IMAGE_ARCHIVE}"
docker save \
  "${LW_IMAGE_PREFIX}/learnword-webapi:${LW_IMAGE_TAG}" \
  "${LW_IMAGE_PREFIX}/learnword-identity:${LW_IMAGE_TAG}" \
  "${LW_IMAGE_PREFIX}/identityservice:${LW_IMAGE_TAG}" \
  "${LW_IMAGE_PREFIX}/gateway:${LW_IMAGE_TAG}" \
  "${LW_IMAGE_PREFIX}/webapp:${LW_IMAGE_TAG}" \
  -o "${IMAGE_ARCHIVE}"

echo "Preparing remote directory ${LW_SERVER_DIR}"
ssh -p "${LW_SERVER_PORT}" "${REMOTE}" "mkdir -p '${LW_SERVER_DIR}'"

echo "Copying compose file and image archive"
scp -P "${LW_SERVER_PORT}" "${SCRIPT_DIR}/docker-compose.prod.yml" "${REMOTE}:${REMOTE_COMPOSE}"
scp -P "${LW_SERVER_PORT}" "${IMAGE_ARCHIVE}" "${REMOTE}:${REMOTE_ARCHIVE}"

if [[ -n "${LW_REMOTE_ENV_FILE:-}" ]]; then
  echo "Uploading production .env from ${LW_REMOTE_ENV_FILE}"
  scp -P "${LW_SERVER_PORT}" "${LW_REMOTE_ENV_FILE}" "${REMOTE}:${LW_SERVER_DIR}/.env"
fi

echo "Loading images and restarting services"
ssh -p "${LW_SERVER_PORT}" "${REMOTE}" "\
  cd '${LW_SERVER_DIR}' && \
  test -f .env && \
  docker load -i '$(basename "${IMAGE_ARCHIVE}")' && \
  sed -i.bak 's|^LW_PLATFORM=.*|LW_PLATFORM=${LW_PLATFORM}|' .env && \
  if ! grep -q '^LW_PLATFORM=' .env; then echo 'LW_PLATFORM=${LW_PLATFORM}' >> .env; fi && \
  sed -i.bak 's|^LW_IMAGE_TAG=.*|LW_IMAGE_TAG=${LW_IMAGE_TAG}|' .env && \
  if ! grep -q '^LW_IMAGE_TAG=' .env; then echo 'LW_IMAGE_TAG=${LW_IMAGE_TAG}' >> .env; fi && \
  docker compose --env-file .env -f docker-compose.yml up -d --remove-orphans && \
  rm -f '$(basename "${IMAGE_ARCHIVE}")'"

echo "Removing local image archive ${IMAGE_ARCHIVE}"
rm -f "${IMAGE_ARCHIVE}"

echo "Deployed LearnWord ${LW_IMAGE_TAG} to ${LW_SERVER_DIR}"
