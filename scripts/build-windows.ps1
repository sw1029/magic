param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.com",
    [string]$ProjectPath = "unity/MagicExamHall",
    [string]$BuildPath = "unity/MagicExamHall/Builds/MagicExamHall.exe",
    [string]$LogPath = "unity/MagicExamHall/unity-build.log"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")
$resolvedProjectPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ProjectPath))
$resolvedBuildPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $BuildPath))
$resolvedLogPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $LogPath))

if (-not (Test-Path -LiteralPath $UnityPath)) {
    throw "Unity executable not found at $UnityPath"
}

$buildDirectory = Split-Path -Parent $resolvedBuildPath
if (-not (Test-Path -LiteralPath $buildDirectory)) {
    New-Item -ItemType Directory -Path $buildDirectory | Out-Null
}

$arguments = @(
    "-batchmode",
    "-quit",
    "-projectPath", $resolvedProjectPath,
    "-executeMethod", "MagicExamHall.Editor.MagicExamHallBuildPipeline.BuildWindowsPlayer",
    "-magicExamHallBuildPath", $resolvedBuildPath,
    "-logFile", $resolvedLogPath
)

& $UnityPath @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Unity build failed with exit code $LASTEXITCODE. See $resolvedLogPath"
}

if (-not (Test-Path -LiteralPath $resolvedBuildPath)) {
    throw "Build output was not created at $resolvedBuildPath"
}

Write-Output "Magic Exam Hall Windows build created at $resolvedBuildPath"
