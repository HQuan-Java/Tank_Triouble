$ErrorActionPreference = "Stop"

$pythonDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$venvDir = Join-Path $pythonDir ".venv"
$requirements = Join-Path $pythonDir "requirements.txt"
$preferredVersions = @("3.11", "3.10")

Write-Host "[setup_env] Folder: $pythonDir"

if (!(Test-Path $requirements)) {
    throw "[setup_env] Missing requirements.txt at $requirements"
}

$launcher = Get-Command py -ErrorAction SilentlyContinue
if ($null -eq $launcher) {
    throw "[setup_env] Python Launcher 'py' was not found. Install Python 3.11 (or 3.10) and enable py launcher."
}

$chosenVersion = $null
foreach ($v in $preferredVersions) {
    & py -$v --version *> $null
    if ($LASTEXITCODE -eq 0) {
        $chosenVersion = $v
        break
    }
}

if ($null -eq $chosenVersion) {
    throw "[setup_env] No supported Python found. Install Python 3.11 or 3.10. (Detected versions like 3.14 are not supported by this project)."
}

Write-Host "[setup_env] Creating virtual environment with Python $chosenVersion..."
& py -$chosenVersion -m venv $venvDir

$venvPython = Join-Path $venvDir "Scripts\python.exe"
if (!(Test-Path $venvPython)) {
    throw "[setup_env] venv python not found: $venvPython"
}

Write-Host "[setup_env] Installing pinned dependencies..."
& $venvPython -m pip install --upgrade pip
& $venvPython -m pip install -r $requirements

Write-Host ""
Write-Host "[setup_env] Done."
Write-Host "[setup_env] Runtime pinned in: $venvPython"
Write-Host "[setup_env] ControlModePanel will auto-prioritize this runtime."
