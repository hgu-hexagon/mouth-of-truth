#!/bin/zsh

set -euo pipefail

SCRIPT_DIRECTORY_PATH="$(cd "$(dirname "$0")" && pwd)"
REPOSITORY_ROOT_PATH="$(cd "${SCRIPT_DIRECTORY_PATH}/.." && pwd)"
UNITY_EDITOR_PATH="${UNITY_EDITOR_PATH:-}"
PROJECT_PATH="${REPOSITORY_ROOT_PATH}/unity-app"

if [[ -z "${UNITY_EDITOR_PATH}" ]]; then
  echo "UNITY_EDITOR_PATH is not set." >&2
  echo "Set UNITY_EDITOR_PATH to the Unity 6000.4.1f1 executable and try again." >&2
  exit 1
fi

if [[ ! -x "${UNITY_EDITOR_PATH}" ]]; then
  echo "Unity editor was not found at: ${UNITY_EDITOR_PATH}" >&2
  echo "Set UNITY_EDITOR_PATH to the Unity 6000.4.1f1 executable and try again." >&2
  exit 1
fi

"${UNITY_EDITOR_PATH}" \
  -batchmode \
  -nographics \
  -projectPath "${PROJECT_PATH}" \
  -quit \
  -executeMethod MouthOfTruth.Editor.BuildMacReleaseEditor.Run
