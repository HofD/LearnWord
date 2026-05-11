#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
ENV_FILE="${SCRIPT_DIR}/env/local.env"

cd "${ROOT_DIR}"
docker compose --env-file "${ENV_FILE}" -f deploy/docker-compose.local.yml down
