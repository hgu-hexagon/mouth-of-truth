Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectoryPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonEngineRootPath = Split-Path -Parent $scriptDirectoryPath
$projectRootPath = Split-Path -Parent $pythonEngineRootPath
$pythonRuntimeRootPath = Join-Path $projectRootPath "python-runtime-windows"
$archiveFilePath = Join-Path $projectRootPath "python-runtime-windows.zip"

function Get-CondaEnvironmentCandidates {
    if ($env:MOUTH_OF_TRUTH_CONDA_ENV) {
        return @($env:MOUTH_OF_TRUTH_CONDA_ENV)
    }

    return @("mouth-truth", "mouth-of-truth")
}

function Resolve-CondaExecutablePath {
    if ($env:MOUTH_OF_TRUTH_CONDA_EXE) {
        if (Test-Path $env:MOUTH_OF_TRUTH_CONDA_EXE) {
            return $env:MOUTH_OF_TRUTH_CONDA_EXE
        }
    }

    foreach ($candidateName in @("conda.exe", "mamba.exe", "micromamba.exe")) {
        $command = Get-Command $candidateName -ErrorAction SilentlyContinue

        if ($command) {
            return $command.Source
        }
    }

    throw "A conda-compatible executable is required to package the Windows python runtime."
}

function Resolve-CondaEnvironmentName {
    param(
        [string]$CondaExecutablePath
    )

    $environmentListJson = & $CondaExecutablePath env list --json

    foreach ($candidateEnvironmentName in Get-CondaEnvironmentCandidates) {
        if ($environmentListJson -match [Regex]::Escape("""$candidateEnvironmentName""")) {
            return $candidateEnvironmentName
        }
    }

    throw "No supported conda environment was found. Expected one of: mouth-truth, mouth-of-truth."
}

$condaExecutablePath = Resolve-CondaExecutablePath
$condaEnvironmentName = Resolve-CondaEnvironmentName -CondaExecutablePath $condaExecutablePath

& $condaExecutablePath run --no-capture-output -n $condaEnvironmentName python -c "import conda_pack" | Out-Null

if (Test-Path $pythonRuntimeRootPath) {
    Remove-Item -Recurse -Force $pythonRuntimeRootPath
}

if (Test-Path $archiveFilePath) {
    Remove-Item -Force $archiveFilePath
}

New-Item -ItemType Directory -Force -Path $pythonRuntimeRootPath | Out-Null

& $condaExecutablePath run --no-capture-output -n $condaEnvironmentName `
    conda-pack `
    -n $condaEnvironmentName `
    --ignore-missing-files `
    -o $archiveFilePath

Expand-Archive -Path $archiveFilePath -DestinationPath $pythonRuntimeRootPath -Force
Remove-Item -Force $archiveFilePath

$condaUnpackExecutablePath = Join-Path $pythonRuntimeRootPath "Scripts\conda-unpack.exe"

if (Test-Path $condaUnpackExecutablePath) {
    & $condaUnpackExecutablePath
}

Write-Host "Packaged Windows python runtime at $pythonRuntimeRootPath"
