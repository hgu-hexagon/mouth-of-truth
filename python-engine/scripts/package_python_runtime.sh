#!/usr/bin/env zsh

set -euo pipefail

SCRIPT_DIRECTORY_PATH="$(cd "$(dirname "$0")" && pwd)"
PYTHON_ENGINE_ROOT_PATH="$(cd "${SCRIPT_DIRECTORY_PATH}/.." && pwd)"
PROJECT_ROOT_PATH="$(cd "${PYTHON_ENGINE_ROOT_PATH}/.." && pwd)"
CONDA_ENVIRONMENT_NAME="${MOUTH_OF_TRUTH_CONDA_ENV:-mouth-of-truth}"
PYTHON_RUNTIME_ROOT_PATH="${PROJECT_ROOT_PATH}/python-runtime"
ARCHIVE_FILE_PATH="${PROJECT_ROOT_PATH}/python-runtime.tar.gz"

if ! command -v conda >/dev/null 2>&1; then
  echo "conda is required to package the distributable python runtime." >&2
  exit 1
fi

if ! conda env list --json | grep -F "\"${CONDA_ENVIRONMENT_NAME}\"" >/dev/null 2>&1; then
  echo "Conda environment '${CONDA_ENVIRONMENT_NAME}' was not found." >&2
  exit 1
fi

if ! conda run --no-capture-output -n "${CONDA_ENVIRONMENT_NAME}" python -c "import conda_pack" >/dev/null 2>&1; then
  echo "conda-pack is not available in '${CONDA_ENVIRONMENT_NAME}'." >&2
  echo "Install it with: conda install -n ${CONDA_ENVIRONMENT_NAME} -c conda-forge conda-pack" >&2
  exit 1
fi

rm -rf "${PYTHON_RUNTIME_ROOT_PATH}" "${ARCHIVE_FILE_PATH}"
mkdir -p "${PYTHON_RUNTIME_ROOT_PATH}"

conda run --no-capture-output -n "${CONDA_ENVIRONMENT_NAME}" \
  conda-pack \
  -n "${CONDA_ENVIRONMENT_NAME}" \
  --ignore-missing-files \
  -o "${ARCHIVE_FILE_PATH}"

tar -xzf "${ARCHIVE_FILE_PATH}" -C "${PYTHON_RUNTIME_ROOT_PATH}"
rm -f "${ARCHIVE_FILE_PATH}"

if [[ -x "${PYTHON_RUNTIME_ROOT_PATH}/bin/conda-unpack" ]]; then
  "${PYTHON_RUNTIME_ROOT_PATH}/bin/conda-unpack"
fi

echo "Packaged python runtime at ${PYTHON_RUNTIME_ROOT_PATH}"
