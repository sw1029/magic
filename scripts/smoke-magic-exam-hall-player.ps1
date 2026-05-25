param(
    [string]$BuildPath = "unity/MagicExamHall/Builds/MagicExamHall.exe",
    [string]$LogPath = "unity/MagicExamHall/player-smoke.log",
    [int]$TimeoutSeconds = 8
)

$ErrorActionPreference = "Stop"

$resolvedBuildPath = Resolve-Path -LiteralPath $BuildPath
$absoluteLogPath = [System.IO.Path]::GetFullPath($LogPath)
$absoluteLogDirectory = Split-Path -Parent $absoluteLogPath

if ($TimeoutSeconds -lt 3) {
    throw "TimeoutSeconds must be at least 3 so the player has time to load the scene."
}

if (-not (Test-Path -LiteralPath $absoluteLogDirectory)) {
    New-Item -ItemType Directory -Path $absoluteLogDirectory | Out-Null
}

if (Test-Path -LiteralPath $absoluteLogPath) {
    Remove-Item -LiteralPath $absoluteLogPath -Force
}

$arguments = @("-batchmode", "-nographics", "-logFile", $absoluteLogPath)
$process = Start-Process -FilePath $resolvedBuildPath.Path -ArgumentList $arguments -PassThru -WindowStyle Hidden

try {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        if ($process.HasExited) {
            throw "Magic Exam Hall player exited during smoke test with exit code $($process.ExitCode)."
        }
    }
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
        $process.WaitForExit()
    }
}

if (-not (Test-Path -LiteralPath $absoluteLogPath)) {
    throw "Magic Exam Hall player did not create a smoke log at $absoluteLogPath."
}

$logText = Get-Content -LiteralPath $absoluteLogPath -Raw
$requiredPatterns = @(
    "Initialize engine version",
    "UnloadTime"
)

foreach ($pattern in $requiredPatterns) {
    if ($logText -notmatch [regex]::Escape($pattern)) {
        throw "Magic Exam Hall player smoke log is missing expected startup marker: $pattern"
    }
}

$fatalPattern = "(?i)(NullReferenceException|MissingMethodException|DllNotFoundException|Fatal|Crash|Could not load scene|Failed to load)"
if ($logText -match $fatalPattern) {
    throw "Magic Exam Hall player smoke log contains a fatal startup pattern: $($Matches[0])"
}

Write-Output "Magic Exam Hall player smoke passed: process stayed alive for $TimeoutSeconds seconds and startup log markers were present."
