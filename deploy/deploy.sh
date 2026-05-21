#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
DEPLOY_ENV="${DEPLOY_ENV:-${SCRIPT_DIR}/env/deploy.env}"
STANDARD_IMAGE_PULL="${LW_STANDARD_IMAGE_PULL:-missing}"
STANDARD_IMAGES=(
  "mcr.microsoft.com/dotnet/sdk:8.0"
  "mcr.microsoft.com/dotnet/aspnet:8.0"
  "node:20-alpine"
  "nginx:1.27-alpine"
)

pull_missing_standard_images() {
  local missing_images=()
  local image

  if [[ "${STANDARD_IMAGE_PULL}" == "never" ]]; then
    echo "Skipping standard image pull."
    return
  fi

  for image in "${STANDARD_IMAGES[@]}"; do
    if [[ "${STANDARD_IMAGE_PULL}" == "always" ]] || ! docker image inspect "${image}" >/dev/null 2>&1; then
      missing_images+=("${image}")
    fi
  done

  if [[ "${#missing_images[@]}" -gt 0 ]]; then
    echo "Pulling standard images: ${missing_images[*]}"
    for image in "${missing_images[@]}"; do
      docker pull "${image}"
    done
  else
    echo "Standard images are already present; skipping pull."
  fi
}

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
REMOTE_ARCHIVE_UPLOADED=0

cleanup_on_error() {
  local exit_code=$?

  if [[ -f "${IMAGE_ARCHIVE}" ]]; then
    echo "Removing local image archive ${IMAGE_ARCHIVE}"
    rm -f "${IMAGE_ARCHIVE}"
  fi

  if [[ "${REMOTE_ARCHIVE_UPLOADED}" -eq 1 ]]; then
    echo "Removing remote image archive ${REMOTE_ARCHIVE}"
    ssh -p "${LW_SERVER_PORT}" "${REMOTE}" "rm -f '${REMOTE_ARCHIVE}'" || true
  fi

  exit "${exit_code}"
}

trap cleanup_on_error ERR

echo "Running LearnWord tests"
(
  cd "${ROOT_DIR}/LearnWord"
  ./tests/run-all-tests.sh
)

mkdir -p "${DIST_DIR}"
pull_missing_standard_images

echo "Building LearnWord images with tag ${LW_IMAGE_TAG}"
(
  cd "${ROOT_DIR}"
  LW_IMAGE_PREFIX="${LW_IMAGE_PREFIX}" LW_IMAGE_TAG="${LW_IMAGE_TAG}" LW_PLATFORM="${LW_PLATFORM}" \
    docker compose -f deploy/docker-compose.build.yml build
)

echo "Saving images to ${IMAGE_ARCHIVE}"
docker save \
  "${LW_IMAGE_PREFIX}/migrations:${LW_IMAGE_TAG}" \
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
REMOTE_ARCHIVE_UPLOADED=1

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
REMOTE_ARCHIVE_UPLOADED=0
trap - ERR

echo "Deployed LearnWord ${LW_IMAGE_TAG} to ${LW_SERVER_DIR}"
