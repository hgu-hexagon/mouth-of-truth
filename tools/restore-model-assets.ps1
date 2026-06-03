param(
    [Parameter(Mandatory = $true)]
    [string]$ModelBundlePath
)

$ErrorActionPreference = "Stop"

$scriptDirectoryPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRootPath = (Resolve-Path (Join-Path $scriptDirectoryPath "..")).Path

$requiredModelFiles = @(
    @{
        Path = "python-engine/models/face/yolo26x_rafdb_best.pt"
        Sha256 = "48e47f019b8214b4c6869af87a3ab8a23fa34a0e891a6d4caf7fd25f7492e35a"
    },
    @{
        Path = "python-engine/models/voice/best_wav2vec2_iemocap/config.json"
        Sha256 = "e80a86c0d4e859cd46cc852d4f5864f3de78be8e64f47c1f79b31b687099f5be"
    },
    @{
        Path = "python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors"
        Sha256 = "699c55de39fddb538eee49a24afc1008a20bb78918b7a50429b63b59dc62f5c3"
    },
    @{
        Path = "python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json"
        Sha256 = "8cdfd65ff4115423185a1512bdae100e2e0cd744f5b322417429944aaafd0827"
    }
)

if ((Test-Path $ModelBundlePath) -eq $false)
{
    throw "Model bundle was not found: $ModelBundlePath"
}

& tar -xzf $ModelBundlePath -C $repositoryRootPath

if ($LASTEXITCODE -ne 0)
{
    throw "Failed to extract model bundle: $ModelBundlePath"
}

foreach ($requiredModelFile in $requiredModelFiles)
{
    $modelFilePath = Join-Path $repositoryRootPath ($requiredModelFile.Path -replace "/", [System.IO.Path]::DirectorySeparatorChar)

    if ((Test-Path $modelFilePath) -eq $false)
    {
        throw "Required model file is missing after restore: $($requiredModelFile.Path)"
    }

    $actualSha256Hash = (Get-FileHash -Algorithm SHA256 $modelFilePath).Hash.ToLowerInvariant()

    if ($actualSha256Hash -ne $requiredModelFile.Sha256)
    {
        throw "Required model SHA-256 mismatch after restore: $($requiredModelFile.Path)`nexpected: $($requiredModelFile.Sha256)`nactual:   $actualSha256Hash"
    }
}

Write-Host "Model assets restored and verified."
