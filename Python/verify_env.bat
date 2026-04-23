@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "VENV_PY=%SCRIPT_DIR%.venv\Scripts\python.exe"
set "REQ=%SCRIPT_DIR%requirements.txt"
set "SCRIPT1=%SCRIPT_DIR%hand_control_socket.py"
set "SCRIPT2=%SCRIPT_DIR%..\Assets\StreamingAssets\Python\hand_control_socket.py"
set "OUT_TXT=%TEMP%\verify_env_out.txt"
set "VERIFY_PY=%TEMP%\verify_env_imports.py"

echo [verify_env] Checking Python hand-control runtime...
echo.

if not exist "%VENV_PY%" (
  echo [FAIL] Missing venv python: "%VENV_PY%"
  echo        Run: "%SCRIPT_DIR%setup_env.bat"
  exit /b 1
)
echo [PASS] venv python found.

for /f "usebackq delims=" %%v in (`"%VENV_PY%" --version 2^>^&1`) do set "PYVER=%%v"
echo [INFO] %PYVER%
echo %PYVER% | findstr /r /c:"Python 3\.11\." /c:"Python 3\.10\." >nul
if errorlevel 1 (
  echo [FAIL] Unsupported Python version in venv. Expected 3.11.x or 3.10.x.
  exit /b 1
)
echo [PASS] Python version is supported.

if not exist "%REQ%" (
  echo [FAIL] Missing requirements.txt: "%REQ%"
  exit /b 1
)
echo [PASS] requirements.txt found.

if exist "%VERIFY_PY%" del /q "%VERIFY_PY%" >nul 2>nul
if exist "%OUT_TXT%" del /q "%OUT_TXT%" >nul 2>nul

echo import cv2, mediapipe, numpy>"%VERIFY_PY%"
echo print("[PASS] Imports OK")>>"%VERIFY_PY%"
echo print("[INFO] cv2", cv2.__version__)>>"%VERIFY_PY%"
echo print("[INFO] mediapipe", mediapipe.__version__)>>"%VERIFY_PY%"
echo print("[INFO] numpy", numpy.__version__)>>"%VERIFY_PY%"

"%VENV_PY%" "%VERIFY_PY%" > "%OUT_TXT%" 2>&1
if errorlevel 1 (
  echo [FAIL] Cannot import required packages: opencv-python, mediapipe, numpy.
  echo ----- python output -----
  if exist "%OUT_TXT%" type "%OUT_TXT%"
  echo -------------------------
  if exist "%VERIFY_PY%" del /q "%VERIFY_PY%" >nul 2>nul
  if exist "%OUT_TXT%" del /q "%OUT_TXT%" >nul 2>nul
  exit /b 1
)
type "%OUT_TXT%"
if exist "%VERIFY_PY%" del /q "%VERIFY_PY%" >nul 2>nul
if exist "%OUT_TXT%" del /q "%OUT_TXT%" >nul 2>nul

if exist "%SCRIPT1%" (
  echo [PASS] Python script found: "%SCRIPT1%"
) else (
  echo [FAIL] Missing script: "%SCRIPT1%"
  exit /b 1
)

if exist "%SCRIPT2%" (
  echo [PASS] StreamingAssets script found: "%SCRIPT2%"
) else (
  echo [WARN] StreamingAssets script not found: "%SCRIPT2%"
  echo        If you run from Unity Editor, this may still work via Python folder.
)

echo.
echo [verify_env] ALL CORE CHECKS PASSED.
echo [verify_env] You can now run Unity and enable Python hand control.
exit /b 0
