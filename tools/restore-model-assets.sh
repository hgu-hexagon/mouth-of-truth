#!/bin/zsh

set -euo pipefail

SCRIPT_DIRECTORY_PATH="$(cd "$(dirname "$0")" && pwd)"
REPOSITORY_ROOT_PATH="$(cd "${SCRIPT_DIRECTORY_PATH}/.." && pwd)"
MODEL_BUNDLE_PATH="${1:-}"

REQUIRED_MODEL_FILES=(
  "python-engine/models/face/yolo26x_rafdb_best.pt"
  "python-engine/models/voice/best_wav2vec2_iemocap/config.json"
  "python-engine/models/voice/best_wav2vec2_iemocap/model.safetensors"
  "python-engine/models/voice/best_wav2vec2_iemocap/preprocessor_config.json"
)

REQUIRED_MODEL_SHA256_HASHES=(
  "48e47f019b8214b4c6869af87a3ab8a23fa34a0e891a6d4caf7fd25f7492e35a"
  "e80a86c0d4e859cd46cc852d4f5864f3de78be8e64f47c1f79b31b687099f5be"
  "699c55de39fddb538eee49a24afc1008a20bb78918b7a50429b63b59dc62f5c3"
  "8cdfd65ff4115423185a1512bdae100e2e0cd744f5b322417429944aaafd0827"
)

if [[ -z "${MODEL_BUNDLE_PATH}" ]]; then
  echo "Usage: tools/restore-model-assets.sh <model-bundle.tar.gz>" >&2
  exit 1
fi

if [[ ! -f "${MODEL_BUNDLE_PATH}" ]]; then
  echo "Model bundle was not found: ${MODEL_BUNDLE_PATH}" >&2
  exit 1
fi

tar -xzf "${MODEL_BUNDLE_PATH}" -C "${REPOSITORY_ROOT_PATH}"

for (( file_index = 1; file_index <= ${#REQUIRED_MODEL_FILES[@]}; file_index++ )); do
  relative_file_path="${REQUIRED_MODEL_FILES[$file_index]}"
  expected_sha256_hash="${REQUIRED_MODEL_SHA256_HASHES[$file_index]}"
  absolute_file_path="${REPOSITORY_ROOT_PATH}/${relative_file_path}"

  if [[ ! -f "${absolute_file_path}" ]]; then
    echo "Required model file is missing after restore: ${relative_file_path}" >&2
    exit 1
  fi

  actual_sha256_hash="$(shasum -a 256 "${absolute_file_path}" | awk '{print $1}')"

  if [[ "${actual_sha256_hash}" != "${expected_sha256_hash}" ]]; then
    echo "Required model SHA-256 mismatch after restore: ${relative_file_path}" >&2
    echo "expected: ${expected_sha256_hash}" >&2
    echo "actual:   ${actual_sha256_hash}" >&2
    exit 1
  fi
done

echo "Model assets restored and verified."
