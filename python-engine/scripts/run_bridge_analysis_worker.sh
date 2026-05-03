#!/usr/bin/env zsh

set -euo pipefail

SCRIPT_DIRECTORY_PATH="$(cd "$(dirname "$0")" && pwd)"
PYTHON_ENGINE_ROOT_PATH="$(cd "${SCRIPT_DIRECTORY_PATH}/.." && pwd)"
PROJECT_ROOT_PATH="$(cd "${PYTHON_ENGINE_ROOT_PATH}/.." && pwd)"
PYTHON_MODULE_ROOT_PATH="${PYTHON_ENGINE_ROOT_PATH}/src"
PYTHON_RUNTIME_ROOT_PATH="${MOUTH_OF_TRUTH_PYTHON_RUNTIME_ROOT:-${PROJECT_ROOT_PATH}/python-runtime}"

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

  printf '%s\n' "mouth-truth"
  printf '%s\n' "mouth-of-truth"
}

buildBundledPythonCandidates() {
  printf '%s\n' "${PYTHON_RUNTIME_ROOT_PATH}/bin/python"
  printf '%s\n' "${PYTHON_RUNTIME_ROOT_PATH}/python"
}

if [[ -n "${MOUTH_OF_TRUTH_PYTHON:-}" ]]; then
  PYTHONPATH="${PYTHON_MODULE_ROOT_PATH}" \
    exec "${MOUTH_OF_TRUTH_PYTHON}" \
    -m mouth_of_truth.runners.bridge_analysis_worker
fi

while IFS= read -r bundledPythonPath; do
  if [[ -n "${bundledPythonPath}" && -x "${bundledPythonPath}" ]]; then
    PYTHONPATH="${PYTHON_MODULE_ROOT_PATH}" \
      exec "${bundledPythonPath}" \
      -m mouth_of_truth.runners.bridge_analysis_worker
  fi
done < <(buildBundledPythonCandidates)

CONDA_CANDIDATE_COMMANDS=()

if [[ -n "${MOUTH_OF_TRUTH_CONDA_EXE:-}" ]]; then
  CONDA_CANDIDATE_COMMANDS+=("${MOUTH_OF_TRUTH_CONDA_EXE}")
fi

for commandName in conda mamba micromamba; do
  if command -v "${commandName}" >/dev/null 2>&1; then
    CONDA_CANDIDATE_COMMANDS+=("$(command -v "${commandName}")")
  fi
done

for condaCandidatePath in "${CONDA_CANDIDATE_COMMANDS[@]}"; do
  if [[ -z "${condaCandidatePath}" || ! -x "${condaCandidatePath}" ]]; then
    continue
  fi

  while IFS= read -r condaEnvironmentName; do
    if condaEnvironmentExists "${condaCandidatePath}" "${condaEnvironmentName}"; then
      PYTHONPATH="${PYTHON_MODULE_ROOT_PATH}" \
        exec "${condaCandidatePath}" run --no-capture-output -n "${condaEnvironmentName}" python \
        -m mouth_of_truth.runners.bridge_analysis_worker
    fi
  done < <(buildCondaEnvironmentCandidates)
done

if command -v python3 >/dev/null 2>&1; then
  PYTHONPATH="${PYTHON_MODULE_ROOT_PATH}" \
    exec python3 \
    -m mouth_of_truth.runners.bridge_analysis_worker
fi

echo "No usable Python runtime was found. Package python-runtime/, set MOUTH_OF_TRUTH_PYTHON, or install the conda environment." >&2
exit 1
