#!/bin/zsh

set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo "Usage: run_bridge_analysis.sh <request-file-path> <result-file-path>" >&2
  exit 2
fi

SCRIPT_DIRECTORY_PATH="$(cd "$(dirname "$0")" && pwd)"
PYTHON_ENGINE_ROOT_PATH="$(cd "${SCRIPT_DIRECTORY_PATH}/.." && pwd)"
PROJECT_ROOT_PATH="$(cd "${PYTHON_ENGINE_ROOT_PATH}/.." && pwd)"
REQUEST_FILE_PATH="$1"
RESULT_FILE_PATH="$2"
PYTHON_MODULE_ROOT_PATH="${PYTHON_ENGINE_ROOT_PATH}/src"
condaEnvironmentExists() {
  local condaExecutablePath="$1"
  local condaEnvironmentName="$2"

  "${condaExecutablePath}" env list --json 2>/dev/null | grep -F "\"${condaEnvironmentName}\"" >/dev/null 2>&1
}

buildCondaEnvironmentCandidates() {
  if [[ -n "${MOUTH_OF_TRUTH_CONDA_ENV:-}" ]]; then
    printf '%s\n' "${MOUTH_OF_TRUTH_CONDA_ENV}"
    return
  fi

  printf '%s\n' "mouth-of-truth"
  printf '%s\n' "mouth-truth"
}

if [[ -n "${MOUTH_OF_TRUTH_PYTHON:-}" ]]; then
  PYTHONPATH="${PYTHON_MODULE_ROOT_PATH}" \
    exec "${MOUTH_OF_TRUTH_PYTHON}" \
    -m mouth_of_truth.runners.bridge_analysis_runner \
    "${REQUEST_FILE_PATH}" \
    "${RESULT_FILE_PATH}"
fi

CONDA_CANDIDATE_PATHS=(
  "${MOUTH_OF_TRUTH_CONDA_EXE:-}"
  "${HOME}/miniforge3/bin/conda"
  "${HOME}/mambaforge/bin/conda"
  "/opt/homebrew/Caskroom/miniforge/base/bin/conda"
  "/usr/local/Caskroom/miniforge/base/bin/conda"
)

for condaCandidatePath in "${CONDA_CANDIDATE_PATHS[@]}"; do
  if [[ -z "${condaCandidatePath}" || ! -x "${condaCandidatePath}" ]]; then
    continue
  fi

  while IFS= read -r condaEnvironmentName; do
    if condaEnvironmentExists "${condaCandidatePath}" "${condaEnvironmentName}"; then
      PYTHONPATH="${PYTHON_MODULE_ROOT_PATH}" \
        exec "${condaCandidatePath}" run --no-capture-output -n "${condaEnvironmentName}" python \
        -m mouth_of_truth.runners.bridge_analysis_runner \
        "${REQUEST_FILE_PATH}" \
        "${RESULT_FILE_PATH}"
    fi
  done < <(buildCondaEnvironmentCandidates)
done

if command -v conda >/dev/null 2>&1; then
  while IFS= read -r condaEnvironmentName; do
    if condaEnvironmentExists "$(command -v conda)" "${condaEnvironmentName}"; then
      PYTHONPATH="${PYTHON_MODULE_ROOT_PATH}" \
        exec conda run --no-capture-output -n "${condaEnvironmentName}" python \
        -m mouth_of_truth.runners.bridge_analysis_runner \
        "${REQUEST_FILE_PATH}" \
        "${RESULT_FILE_PATH}"
    fi
  done < <(buildCondaEnvironmentCandidates)
fi

if command -v python3 >/dev/null 2>&1; then
  PYTHONPATH="${PYTHON_MODULE_ROOT_PATH}" \
    exec python3 \
    -m mouth_of_truth.runners.bridge_analysis_runner \
    "${REQUEST_FILE_PATH}" \
    "${RESULT_FILE_PATH}"
fi

echo "No usable Python runtime was found. Set MOUTH_OF_TRUTH_PYTHON or install the conda environment." >&2
exit 1
