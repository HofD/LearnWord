# LearnWord deployment

This directory contains the combined Docker deployment for the two repositories:

- backend: `../LearnWord`
- frontend: `../../LearnWordWebApp/lw-app`

Production uses PostgreSQL on the internal server `192.168.0.6`; the production compose file does not start a PostgreSQL container. Local development starts PostgreSQL in Docker.

## Local run

```bash
cp deploy/env/local.env.example deploy/env/local.env
docker compose --env-file deploy/env/local.env -f deploy/docker-compose.local.yml up -d --build
```

Or use the helper scripts:

```bash
./deploy/local-up.sh
```

```powershell
.\deploy\local-up.ps1
```

Frontend: `http://localhost:8088`

Gateway: `http://localhost:5100`

Mailpit inbox: `http://localhost:8025`

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

By default deploy builds `linux/amd64` images, which is the usual Linux server platform. Override `LW_PLATFORM` in `deploy/env/deploy.env` only if the server uses another architecture.

If port `80` is already used on the server, either stop the process that owns it or set another frontend port in the production `.env`, for example:

```env
LW_WEBAPP_PORT=8088
```

To find what owns port `80` on the server:

```bash
ss -ltnp 'sport = :80'
```
