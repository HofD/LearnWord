# LearnWord deployment

This directory contains the combined Docker deployment for the two repositories:

- backend: `../LearnWord`
- frontend: `../../LearnWordWebApp/lw-app`

Production uses PostgreSQL on the internal server `192.168.0.6`; the production compose file does not start a PostgreSQL container. Local development starts PostgreSQL in Docker.

## Local run

```bash
cp deploy/env/local.env.example deploy/env/local.env
./deploy/local-up.sh
```

Or from Windows PowerShell:

```powershell
.\deploy\local-up.ps1
```

The helper scripts avoid re-pulling standard images on every run. They check these images first:

- `postgres:16-alpine`
- `axllent/mailpit:v1.27`
- `mcr.microsoft.com/dotnet/sdk:8.0`
- `mcr.microsoft.com/dotnet/aspnet:8.0`
- `node:20-alpine`
- `nginx:1.27-alpine`

By default `LW_STANDARD_IMAGE_PULL=missing`, so only absent standard images are pulled. The local compose startup then uses `--pull never`, which prevents Compose from checking `postgres` and `mailpit` again during `up`.

Use these modes when needed:

```bash
LW_STANDARD_IMAGE_PULL=never ./deploy/local-up.sh
LW_STANDARD_IMAGE_PULL=always ./deploy/local-up.sh
```

```powershell
$env:LW_STANDARD_IMAGE_PULL = "never"; .\deploy\local-up.ps1
$env:LW_STANDARD_IMAGE_PULL = "always"; .\deploy\local-up.ps1
```

Frontend: `http://localhost:8088`

Gateway: `http://localhost:5100`

Mailpit inbox: `http://localhost:8025`

The local AI card generator uses the fake provider by default:

```env
LW_AI_PROVIDER=Fake
LW_AI_OPENROUTER_MODEL=google/gemma-4-26b-a4b-it:free
```

To test OpenRouter locally, set these values in `deploy/env/local.env` before starting the stack:

```env
LW_AI_PROVIDER=OpenRouter
LW_AI_OPENROUTER_API_KEY=<secret>
LW_AI_OPENROUTER_MODEL=google/gemma-4-26b-a4b-it:free
```

Do not put OpenRouter keys in frontend code or committed files.

Stop local services:

```bash
docker compose --env-file deploy/env/local.env -f deploy/docker-compose.local.yml down
```

## First production setup

Create `${LW_SERVER_DIR}/.env` on the server from `deploy/env/prod.env.example`.

At minimum set:

- `LW_IMAGE_TAG`
- `LW_DB_USER`
- `LW_DB_PASSWORD`
- `LW_JWT_KEY`
- `LW_SMTP_USERNAME`
- `LW_SMTP_PASSWORD`

AI generation can stay disabled from real LLM calls by keeping:

```env
LW_AI_PROVIDER=Fake
```

To enable OpenRouter in production, set:

```env
LW_AI_PROVIDER=OpenRouter
LW_AI_OPENROUTER_API_KEY=<secret>
LW_AI_OPENROUTER_MODEL=google/gemma-4-26b-a4b-it:free
```

The default DB host is `192.168.0.6`.

## Deploy from macOS or Linux

```bash
cp deploy/env/deploy.env.example deploy/env/deploy.env
./deploy/deploy.sh
```

## Deploy from Windows PowerShell

```powershell
Copy-Item deploy\env\deploy.env.example deploy\env\deploy.env
.\deploy\deploy.ps1
```

Both scripts build all images locally, save them to a tar archive, copy the archive and production compose file to `${LW_SERVER_DIR}` on the server, run `docker load`, and then run:

```bash
docker compose --env-file .env -f docker-compose.yml up -d --remove-orphans
```

Before any images are built or copied, both scripts run the backend test suite: `LearnWord/tests/run-all-tests.sh` on macOS/Linux and `LearnWord\tests\run-all-tests.ps1` on Windows. If tests or any later deploy step fail, deployment stops and any created image archive is removed locally; if the archive was already copied, the remote copy is removed too.

Before building application images, deploy scripts check standard build/runtime base images:

- `mcr.microsoft.com/dotnet/sdk:8.0`
- `mcr.microsoft.com/dotnet/aspnet:8.0`
- `node:20-alpine`
- `nginx:1.27-alpine`

`LW_STANDARD_IMAGE_PULL=missing` is the default and pulls only absent standard images. Set `LW_STANDARD_IMAGE_PULL=never` to rely only on the local Docker cache, or `LW_STANDARD_IMAGE_PULL=always` to force-refresh standard images before building.

After a successful restart, the copied tar archive is removed from the server and from local `deploy/dist`.

By default deploy builds `linux/amd64` images, which is the usual Linux server platform. Override `LW_PLATFORM` in `deploy/env/deploy.env` only if the server uses another architecture.

The production compose is intended to sit behind host nginx. By default it binds only to localhost:

```env
LW_WEBAPP_PORT=8080
LW_GATEWAY_PORT=5000
```

With the current nginx config, `/` proxies to `http://127.0.0.1:8080/` and `/api` proxies to `http://127.0.0.1:5000`, so keep these values unless nginx changes.

Only `webapp` and `gateway` publish ports in production, and both are hard-bound to `127.0.0.1` for the host nginx proxy. Internal services (`learnword.webapi`, `learnword.identity`, `identityservice`) are available only on the Docker network through `expose: 8080`; do not add `ports` for them in production.

To find what owns a port on the server:

```bash
ss -ltnp 'sport = :8080'
```
