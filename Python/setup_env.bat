@echo off
setlocal
set SCRIPT_DIR=%~dp0
powershell -ExecutionPolicy Bypass -File "%SCRIPT_DIR%setup_env.ps1"
if errorlevel 1 (
  echo [setup_env] Failed.
  exit /b 1
)
echo [setup_env] Success.
exit /b 0
