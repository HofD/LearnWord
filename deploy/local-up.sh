#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
ENV_FILE="${SCRIPT_DIR}/env/local.env"
STANDARD_IMAGE_PULL="${LW_STANDARD_IMAGE_PULL:-missing}"
STANDARD_IMAGES=(
  "postgres:16-alpine"
  "axllent/mailpit:v1.27"
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

if [[ ! -f "${ENV_FILE}" ]]; then
  cp "${SCRIPT_DIR}/env/local.env.example" "${ENV_FILE}"
fi

pull_missing_standard_images

cd "${ROOT_DIR}"
docker compose --env-file "${ENV_FILE}" -f deploy/docker-compose.local.yml up -d --build --pull never
