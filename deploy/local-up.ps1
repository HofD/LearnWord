$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$EnvFile = Join-Path $ScriptDir "env\local.env"
$ExampleFile = Join-Path $ScriptDir "env\local.env.example"

if (-not (Test-Path $EnvFile)) {
    Copy-Item $ExampleFile $EnvFile
}

Push-Location $RootDir
try {
    docker compose --env-file $EnvFile -f deploy/docker-compose.local.yml up -d --build
}
finally {
    Pop-Location
}
