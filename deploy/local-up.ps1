$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$EnvFile = Join-Path $ScriptDir "env\local.env"
$ExampleFile = Join-Path $ScriptDir "env\local.env.example"
$StandardImagePull = if ($env:LW_STANDARD_IMAGE_PULL) { $env:LW_STANDARD_IMAGE_PULL } else { "missing" }
$StandardImages = @(
    "postgres:16-alpine",
    "axllent/mailpit:v1.27",
    "mcr.microsoft.com/dotnet/sdk:8.0",
    "mcr.microsoft.com/dotnet/aspnet:8.0",
    "node:20-alpine",
    "nginx:1.27-alpine"
)

function Pull-StandardImages {
    if ($StandardImagePull -eq "never") {
        Write-Host "Skipping standard image pull."
        return
    }

    $missingImages = @()
    foreach ($image in $StandardImages) {
        $exists = $false
        if ($StandardImagePull -ne "always") {
            docker image inspect $image *> $null
            $exists = $LASTEXITCODE -eq 0
        }

        if ($StandardImagePull -eq "always" -or -not $exists) {
            $missingImages += $image
        }
    }

    if ($missingImages.Count -eq 0) {
        Write-Host "Standard images are already present; skipping pull."
        return
    }

    Write-Host "Pulling standard images: $($missingImages -join ' ')"
    foreach ($image in $missingImages) {
        docker pull $image
        if ($LASTEXITCODE -ne 0) {
            throw "docker pull $image failed with exit code $LASTEXITCODE."
        }
    }
}

if (-not (Test-Path $EnvFile)) {
    Copy-Item $ExampleFile $EnvFile
}

Pull-StandardImages

Push-Location $RootDir
try {
    docker compose --env-file $EnvFile -f deploy/docker-compose.local.yml up -d --build --pull never
}
finally {
    Pop-Location
}
