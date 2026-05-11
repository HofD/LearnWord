$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$EnvFile = Join-Path $ScriptDir "env\local.env"

Push-Location $RootDir
try {
    docker compose --env-file $EnvFile -f deploy/docker-compose.local.yml down
}
finally {
    Pop-Location
}
