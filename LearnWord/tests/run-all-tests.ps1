$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Projects = Get-ChildItem -Path $ScriptDir -Directory |
    ForEach-Object { Get-ChildItem -Path $_.FullName -Filter "*Tests.csproj" -File } |
    Sort-Object FullName

if ($Projects.Count -eq 0) {
    Write-Host "No test projects found under $ScriptDir"
    exit 1
}

$Results = @()
$OverallStatus = 0

Write-Host "Found $($Projects.Count) test project(s)."
Write-Host

foreach ($Project in $Projects) {
    $Name = Split-Path -Leaf (Split-Path -Parent $Project.FullName)

    Write-Host "========================================"
    Write-Host "Running $Name"
    Write-Host "Project: $($Project.FullName)"
    Write-Host "========================================"

    $StartedAt = Get-Date
    & dotnet test $Project.FullName --logger "console;verbosity=minimal"
    $ExitCode = $LASTEXITCODE
    $Duration = [int]((Get-Date) - $StartedAt).TotalSeconds

    if ($ExitCode -eq 0) {
        $Status = "PASSED"
    }
    else {
        $Status = "FAILED"
        $OverallStatus = 1
    }

    $Results += [pscustomobject]@{
        Name = $Name
        Status = $Status
        Duration = "${Duration}s"
    }

    Write-Host
}

Write-Host "========================================"
Write-Host "Test summary"
Write-Host "========================================"

foreach ($Result in $Results) {
    "{0,-40} {1,-8} {2}" -f $Result.Name, $Result.Status, $Result.Duration
}

Write-Host

if ($OverallStatus -eq 0) {
    Write-Host "All test projects passed."
}
else {
    Write-Host "One or more test projects failed."
}

exit $OverallStatus
