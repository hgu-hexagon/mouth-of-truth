#!/bin/zsh

set -euo pipefail

SCRIPT_DIRECTORY_PATH="$(cd "$(dirname "$0")" && pwd)"
REPOSITORY_ROOT_PATH="$(cd "${SCRIPT_DIRECTORY_PATH}/.." && pwd)"
DISTRIBUTION_DIRECTORY_PATH="${REPOSITORY_ROOT_PATH}/dist/model-assets"
REQUIRED_ARCHIVE_PATH="${DISTRIBUTION_DIRECTORY_PATH}/mouth-of-truth-models-required.tar.gz"
OPTIONAL_WHISPER_ARCHIVE_PATH="${DISTRIBUTION_DIRECTORY_PATH}/mouth-of-truth-models-whisper-cache.tar.gz"
INCLUDE_WHISPER_CACHE="${MOUTH_OF_TRUTH_INCLUDE_WHISPER_CACHE:-0}"

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

validate_required_model_files() {
  for (( file_index = 1; file_index <= ${#REQUIRED_MODEL_FILES[@]}; file_index++ )); do
    relative_file_path="${REQUIRED_MODEL_FILES[$file_index]}"
    expected_sha256_hash="${REQUIRED_MODEL_SHA256_HASHES[$file_index]}"
    absolute_file_path="${REPOSITORY_ROOT_PATH}/${relative_file_path}"

    if [[ ! -f "${absolute_file_path}" ]]; then
      echo "Required model file is missing: ${relative_file_path}" >&2
      exit 1
    fi

    actual_sha256_hash="$(shasum -a 256 "${absolute_file_path}" | awk '{print $1}')"

    if [[ "${actual_sha256_hash}" != "${expected_sha256_hash}" ]]; then
      echo "Required model SHA-256 mismatch: ${relative_file_path}" >&2
      echo "expected: ${expected_sha256_hash}" >&2
      echo "actual:   ${actual_sha256_hash}" >&2
      exit 1
    fi
  done
}

write_archive_sha256_file() {
  archive_path="$1"
  archive_directory_path="$(dirname "${archive_path}")"
  archive_file_name="$(basename "${archive_path}")"

  (
    cd "${archive_directory_path}"
    shasum -a 256 "${archive_file_name}" > "${archive_file_name}.sha256"
  )
}

validate_required_model_files
mkdir -p "${DISTRIBUTION_DIRECTORY_PATH}"

tar -czf "${REQUIRED_ARCHIVE_PATH}" -C "${REPOSITORY_ROOT_PATH}" "${REQUIRED_MODEL_FILES[@]}"
write_archive_sha256_file "${REQUIRED_ARCHIVE_PATH}"

echo "Created required model bundle:"
echo "${REQUIRED_ARCHIVE_PATH}"
echo "${REQUIRED_ARCHIVE_PATH}.sha256"

if [[ "${INCLUDE_WHISPER_CACHE}" == "1" ]]; then
  WHISPER_CACHE_PATH="${REPOSITORY_ROOT_PATH}/python-engine/models/whisper/models--openai--whisper-tiny"

  if [[ ! -d "${WHISPER_CACHE_PATH}" ]]; then
    echo "Whisper cache directory is missing: python-engine/models/whisper/models--openai--whisper-tiny" >&2
    exit 1
  fi

  tar -czf "${OPTIONAL_WHISPER_ARCHIVE_PATH}" -C "${REPOSITORY_ROOT_PATH}" "python-engine/models/whisper/models--openai--whisper-tiny"
  write_archive_sha256_file "${OPTIONAL_WHISPER_ARCHIVE_PATH}"

  echo "Created optional Whisper cache bundle:"
  echo "${OPTIONAL_WHISPER_ARCHIVE_PATH}"
  echo "${OPTIONAL_WHISPER_ARCHIVE_PATH}.sha256"
fi
