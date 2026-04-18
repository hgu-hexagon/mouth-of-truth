@echo off
setlocal EnableExtensions

if "%~2"=="" (
  echo Usage: run_bridge_analysis.bat ^<request-file-path^> ^<result-file-path^> 1>&2
  exit /b 2
)

set "SCRIPT_DIRECTORY_PATH=%~dp0"
for %%I in ("%SCRIPT_DIRECTORY_PATH%..") do set "PYTHON_ENGINE_ROOT_PATH=%%~fI"
for %%I in ("%PYTHON_ENGINE_ROOT_PATH%\..") do set "PROJECT_ROOT_PATH=%%~fI"

set "REQUEST_FILE_PATH=%~1"
set "RESULT_FILE_PATH=%~2"
set "PYTHON_MODULE_ROOT_PATH=%PYTHON_ENGINE_ROOT_PATH%\src"

if defined MOUTH_OF_TRUTH_PYTHON_RUNTIME_ROOT (
  set "PYTHON_RUNTIME_ROOT_PATH=%MOUTH_OF_TRUTH_PYTHON_RUNTIME_ROOT%"
) else (
  set "PYTHON_RUNTIME_ROOT_PATH=%PROJECT_ROOT_PATH%\python-runtime"
)

if defined MOUTH_OF_TRUTH_PYTHON (
  set "PYTHONPATH=%PYTHON_MODULE_ROOT_PATH%"
  "%MOUTH_OF_TRUTH_PYTHON%" -m mouth_of_truth.runners.bridge_analysis_runner "%REQUEST_FILE_PATH%" "%RESULT_FILE_PATH%"
  exit /b %ERRORLEVEL%
)

if exist "%PYTHON_RUNTIME_ROOT_PATH%\python.exe" (
  set "PYTHONPATH=%PYTHON_MODULE_ROOT_PATH%"
  "%PYTHON_RUNTIME_ROOT_PATH%\python.exe" -m mouth_of_truth.runners.bridge_analysis_runner "%REQUEST_FILE_PATH%" "%RESULT_FILE_PATH%"
  exit /b %ERRORLEVEL%
)

if exist "%PYTHON_RUNTIME_ROOT_PATH%\Scripts\python.exe" (
  set "PYTHONPATH=%PYTHON_MODULE_ROOT_PATH%"
  "%PYTHON_RUNTIME_ROOT_PATH%\Scripts\python.exe" -m mouth_of_truth.runners.bridge_analysis_runner "%REQUEST_FILE_PATH%" "%RESULT_FILE_PATH%"
  exit /b %ERRORLEVEL%
)

if defined MOUTH_OF_TRUTH_CONDA_EXE (
  call :tryCondaRuntime "%MOUTH_OF_TRUTH_CONDA_EXE%"
  if not errorlevel 1 (
    exit /b 0
  )
)

for %%C in (conda.exe mamba.exe micromamba.exe) do (
  for %%P in ("%%~$PATH:C") do (
    if exist "%%~fP" (
      call :tryCondaRuntime "%%~fP"
      if not errorlevel 1 (
        exit /b 0
      )
    )
  )
)

if exist "%SystemRoot%\py.exe" (
  set "PYTHONPATH=%PYTHON_MODULE_ROOT_PATH%"
  "%SystemRoot%\py.exe" -3 -m mouth_of_truth.runners.bridge_analysis_runner "%REQUEST_FILE_PATH%" "%RESULT_FILE_PATH%"
  exit /b %ERRORLEVEL%
)

for %%P in ("python.exe") do (
  if exist "%%~$PATH:P" (
    set "PYTHONPATH=%PYTHON_MODULE_ROOT_PATH%"
    "%%~$PATH:P" -m mouth_of_truth.runners.bridge_analysis_runner "%REQUEST_FILE_PATH%" "%RESULT_FILE_PATH%"
    exit /b %ERRORLEVEL%
  )
)

echo No usable Python runtime was found. Package python-runtime/, set MOUTH_OF_TRUTH_PYTHON, or install the conda environment. 1>&2
exit /b 1

:tryCondaRuntime
set "CONDA_EXECUTABLE_PATH=%~1"

if not exist "%CONDA_EXECUTABLE_PATH%" (
  exit /b 1
)

if defined MOUTH_OF_TRUTH_CONDA_ENV (
  call :runCondaEnvironment "%CONDA_EXECUTABLE_PATH%" "%MOUTH_OF_TRUTH_CONDA_ENV%"
  exit /b %ERRORLEVEL%
)

call :runCondaEnvironment "%CONDA_EXECUTABLE_PATH%" "mouth-truth"
if not errorlevel 1 (
  exit /b 0
)

call :runCondaEnvironment "%CONDA_EXECUTABLE_PATH%" "mouth-of-truth"
if not errorlevel 1 (
  exit /b 0
)

exit /b 1

:runCondaEnvironment
set "CONDA_EXECUTABLE_PATH=%~1"
set "CONDA_ENVIRONMENT_NAME=%~2"

"%CONDA_EXECUTABLE_PATH%" env list --json | findstr /C:"\"%CONDA_ENVIRONMENT_NAME%\"" >nul 2>nul
if errorlevel 1 (
  exit /b 1
)

set "PYTHONPATH=%PYTHON_MODULE_ROOT_PATH%"
"%CONDA_EXECUTABLE_PATH%" run --no-capture-output -n "%CONDA_ENVIRONMENT_NAME%" python -m mouth_of_truth.runners.bridge_analysis_runner "%REQUEST_FILE_PATH%" "%RESULT_FILE_PATH%"
exit /b %ERRORLEVEL%
