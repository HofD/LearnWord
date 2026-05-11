$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$DeployEnv = if ($env:DEPLOY_ENV) { $env:DEPLOY_ENV } else { Join-Path $ScriptDir "env\deploy.env" }

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

$DistDir = Join-Path $ScriptDir "dist"
$ImageArchive = Join-Path $DistDir "learnword-images-$ImageTag.tar"
$Remote = "$($env:LW_SERVER_USER)@$($env:LW_SERVER)"
$RemoteArchive = "$ServerDir/learnword-images-$ImageTag.tar"

New-Item -ItemType Directory -Force -Path $DistDir | Out-Null

Write-Host "Building LearnWord images with tag $ImageTag"
Push-Location $RootDir
try {
    $env:LW_IMAGE_PREFIX = $ImagePrefix
    $env:LW_IMAGE_TAG = $ImageTag
    $env:LW_PLATFORM = $Platform
    docker compose -f deploy/docker-compose.build.yml build
}
finally {
    Pop-Location
}

Write-Host "Saving images to $ImageArchive"
docker save `
    "$ImagePrefix/learnword-webapi:$ImageTag" `
    "$ImagePrefix/learnword-identity:$ImageTag" `
    "$ImagePrefix/identityservice:$ImageTag" `
    "$ImagePrefix/gateway:$ImageTag" `
    "$ImagePrefix/webapp:$ImageTag" `
    -o "$ImageArchive"

Write-Host "Preparing remote directory $ServerDir"
ssh -p $ServerPort $Remote "mkdir -p '$ServerDir'"

Write-Host "Copying compose file and image archive"
scp -P $ServerPort (Join-Path $ScriptDir "docker-compose.prod.yml") "${Remote}:$ServerDir/docker-compose.yml"
scp -P $ServerPort $ImageArchive "${Remote}:$RemoteArchive"

if ($env:LW_REMOTE_ENV_FILE) {
    Write-Host "Uploading production .env from $($env:LW_REMOTE_ENV_FILE)"
    scp -P $ServerPort $env:LW_REMOTE_ENV_FILE "${Remote}:$ServerDir/.env"
}

$RemoteCommand = "cd '$ServerDir' && test -f .env && docker load -i 'learnword-images-$ImageTag.tar' && sed -i.bak 's|^LW_PLATFORM=.*|LW_PLATFORM=$Platform|' .env && if ! grep -q '^LW_PLATFORM=' .env; then echo 'LW_PLATFORM=$Platform' >> .env; fi && sed -i.bak 's|^LW_IMAGE_TAG=.*|LW_IMAGE_TAG=$ImageTag|' .env && if ! grep -q '^LW_IMAGE_TAG=' .env; then echo 'LW_IMAGE_TAG=$ImageTag' >> .env; fi && docker compose --env-file .env -f docker-compose.yml up -d --remove-orphans"

Write-Host "Loading images and restarting services"
ssh -p $ServerPort $Remote $RemoteCommand

Write-Host "Deployed LearnWord $ImageTag to $ServerDir"
