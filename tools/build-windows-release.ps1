param(
    [string]$UnityEditorPath = ""
)

$ErrorActionPreference = "Stop"

$scriptDirectoryPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRootPath = Resolve-Path (Join-Path $scriptDirectoryPath "..")
$projectPath = Join-Path $repositoryRootPath "unity-app"

if ([string]::IsNullOrWhiteSpace($UnityEditorPath))
{
    $UnityEditorPath = $env:UNITY_EDITOR_PATH
}

if ([string]::IsNullOrWhiteSpace($UnityEditorPath))
{
    throw "Set -UnityEditorPath or UNITY_EDITOR_PATH to the Unity 6000.4.1f1 editor executable."
}

if ((Test-Path $UnityEditorPath) -eq $false)
{
    throw "Unity editor was not found at '$UnityEditorPath'. Set -UnityEditorPath to the Unity 6000.4.1f1 editor executable."
}

& $UnityEditorPath `
    -batchmode `
    -nographics `
    -projectPath $projectPath `
    -quit `
    -executeMethod "MouthOfTruth.Editor.BuildWindowsReleaseEditor.Run"
