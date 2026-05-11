#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
ENV_FILE="${SCRIPT_DIR}/env/local.env"

if [[ ! -f "${ENV_FILE}" ]]; then
  cp "${SCRIPT_DIR}/env/local.env.example" "${ENV_FILE}"
fi

cd "${ROOT_DIR}"
docker compose --env-file "${ENV_FILE}" -f deploy/docker-compose.local.yml up -d --build
