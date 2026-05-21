$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$DeployEnv = if ($env:DEPLOY_ENV) { $env:DEPLOY_ENV } else { Join-Path $ScriptDir "env\deploy.env" }
$StandardImages = @(
    "mcr.microsoft.com/dotnet/sdk:8.0",
    "mcr.microsoft.com/dotnet/aspnet:8.0",
    "node:20-alpine",
    "nginx:1.27-alpine"
)

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }
}

function Pull-StandardImages {
    param(
        [string]$PullMode
    )

    if ($PullMode -eq "never") {
        Write-Host "Skipping standard image pull."
        return
    }

    $missingImages = @()
    foreach ($image in $StandardImages) {
        $exists = $false
        if ($PullMode -ne "always") {
            docker image inspect $image *> $null
            $exists = $LASTEXITCODE -eq 0
        }

        if ($PullMode -eq "always" -or -not $exists) {
            $missingImages += $image
        }
    }

    if ($missingImages.Count -eq 0) {
        Write-Host "Standard images are already present; skipping pull."
        return
    }

    Write-Host "Pulling standard images: $($missingImages -join ' ')"
    foreach ($image in $missingImages) {
        Invoke-Native docker pull $image
    }
}

if (Test-Path $DeployEnv) {
    Get-Content $DeployEnv | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#")) {
            $parts = $line.Split("=", 2)
            if ($parts.Length -eq 2) {
                [Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), "Process")
            }
        }
    }
}

if (-not $env:LW_SERVER) { throw "Set LW_SERVER in $DeployEnv or environment." }
if (-not $env:LW_SERVER_USER) { throw "Set LW_SERVER_USER in $DeployEnv or environment." }

$ServerPort = if ($env:LW_SERVER_PORT) { $env:LW_SERVER_PORT } else { "22" }
$ServerDir = if ($env:LW_SERVER_DIR) { $env:LW_SERVER_DIR } else { "/opt/lw" }
$ImagePrefix = if ($env:LW_IMAGE_PREFIX) { $env:LW_IMAGE_PREFIX } else { "learnword" }
$ImageTag = if ($env:LW_IMAGE_TAG) { $env:LW_IMAGE_TAG } else { Get-Date -Format "yyyyMMddHHmmss" }
$Platform = if ($env:LW_PLATFORM) { $env:LW_PLATFORM } else { "linux/amd64" }
$StandardImagePull = if ($env:LW_STANDARD_IMAGE_PULL) { $env:LW_STANDARD_IMAGE_PULL } else { "missing" }

$DistDir = Join-Path $ScriptDir "dist"
$ImageArchive = Join-Path $DistDir "learnword-images-$ImageTag.tar"
$Remote = "$($env:LW_SERVER_USER)@$($env:LW_SERVER)"
$RemoteArchive = "$ServerDir/learnword-images-$ImageTag.tar"
$RemoteArchiveUploaded = $false

try {
    Write-Host "Running LearnWord tests"
    $PowerShellExe = [System.Diagnostics.Process]::GetCurrentProcess().Path
    Invoke-Native $PowerShellExe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RootDir "LearnWord\tests\run-all-tests.ps1")

    New-Item -ItemType Directory -Force -Path $DistDir | Out-Null
    Pull-StandardImages -PullMode $StandardImagePull

    Write-Host "Building LearnWord images with tag $ImageTag"
    Push-Location $RootDir
    try {
        $env:LW_IMAGE_PREFIX = $ImagePrefix
        $env:LW_IMAGE_TAG = $ImageTag
        $env:LW_PLATFORM = $Platform
        Invoke-Native docker compose -f deploy/docker-compose.build.yml build
    }
    finally {
        Pop-Location
    }

    Write-Host "Saving images to $ImageArchive"
    Invoke-Native docker save `
        "$ImagePrefix/migrations:$ImageTag" `
        "$ImagePrefix/learnword-webapi:$ImageTag" `
        "$ImagePrefix/learnword-identity:$ImageTag" `
        "$ImagePrefix/identityservice:$ImageTag" `
        "$ImagePrefix/gateway:$ImageTag" `
        "$ImagePrefix/webapp:$ImageTag" `
        -o "$ImageArchive"

    Write-Host "Preparing remote directory $ServerDir"
    Invoke-Native ssh -p $ServerPort $Remote "mkdir -p '$ServerDir'"

    Write-Host "Copying compose file and image archive"
    Invoke-Native scp -P $ServerPort (Join-Path $ScriptDir "docker-compose.prod.yml") "${Remote}:$ServerDir/docker-compose.yml"
    Invoke-Native scp -P $ServerPort $ImageArchive "${Remote}:$RemoteArchive"
    $RemoteArchiveUploaded = $true

    if ($env:LW_REMOTE_ENV_FILE) {
        Write-Host "Uploading production .env from $($env:LW_REMOTE_ENV_FILE)"
        Invoke-Native scp -P $ServerPort $env:LW_REMOTE_ENV_FILE "${Remote}:$ServerDir/.env"
    }

    $RemoteArchiveName = "learnword-images-$ImageTag.tar"
    $RemoteCommand = "cd '$ServerDir' && test -f .env && docker load -i '$RemoteArchiveName' && sed -i.bak 's|^LW_PLATFORM=.*|LW_PLATFORM=$Platform|' .env && if ! grep -q '^LW_PLATFORM=' .env; then echo 'LW_PLATFORM=$Platform' >> .env; fi && sed -i.bak 's|^LW_IMAGE_TAG=.*|LW_IMAGE_TAG=$ImageTag|' .env && if ! grep -q '^LW_IMAGE_TAG=' .env; then echo 'LW_IMAGE_TAG=$ImageTag' >> .env; fi && docker compose --env-file .env -f docker-compose.yml up -d --remove-orphans && rm -f '$RemoteArchiveName'"

    Write-Host "Loading images and restarting services"
    Invoke-Native ssh -p $ServerPort $Remote $RemoteCommand

    Write-Host "Removing local image archive $ImageArchive"
    Remove-Item -Force $ImageArchive
    $RemoteArchiveUploaded = $false

    Write-Host "Deployed LearnWord $ImageTag to $ServerDir"
}
catch {
    if (Test-Path $ImageArchive) {
        Write-Host "Removing local image archive $ImageArchive"
        Remove-Item -Force $ImageArchive
    }

    if ($RemoteArchiveUploaded) {
        Write-Host "Removing remote image archive $RemoteArchive"
        & ssh -p $ServerPort $Remote "rm -f '$RemoteArchive'" | Out-Null
    }

    throw
}
