#!/usr/bin/env zsh

set -euo pipefail

SCRIPT_DIRECTORY_PATH="$(cd "$(dirname "$0")" && pwd)"
PYTHON_ENGINE_ROOT_PATH="$(cd "${SCRIPT_DIRECTORY_PATH}/.." && pwd)"
PROJECT_ROOT_PATH="$(cd "${PYTHON_ENGINE_ROOT_PATH}/.." && pwd)"
VALIDATION_ROOT_PATH="${PYTHON_ENGINE_ROOT_PATH}/data/validation"
REQUEST_FILE_PATH="${VALIDATION_ROOT_PATH}/bridge_request.json"
RESULT_FILE_PATH="${VALIDATION_ROOT_PATH}/bridge_result.json"
AIFF_FILE_PATH="${VALIDATION_ROOT_PATH}/bridge_prompt.aiff"
WAV_FILE_PATH="${VALIDATION_ROOT_PATH}/bridge_prompt.wav"

mkdir -p "${VALIDATION_ROOT_PATH}"
rm -f "${REQUEST_FILE_PATH}" "${RESULT_FILE_PATH}" "${AIFF_FILE_PATH}" "${WAV_FILE_PATH}"

if ! command -v say >/dev/null 2>&1; then
  echo "macOS 'say' command is required for bridge runtime validation." >&2
  exit 1
fi

if ! command -v afconvert >/dev/null 2>&1; then
  echo "macOS 'afconvert' command is required for bridge runtime validation." >&2
  exit 1
fi

say \
  -o "${AIFF_FILE_PATH}" \
  "I had grilled salmon and rice for lunch today."

afconvert \
  -f WAVE \
  -d LEI16 \
  "${AIFF_FILE_PATH}" \
  "${WAV_FILE_PATH}"

cat > "${REQUEST_FILE_PATH}" <<JSON
{
  "RequestID": "bridge-runtime-validation",
  "QuestionID": "QTEST",
  "QuestionText": "What did you have for lunch today?",
  "AnswerTranscript": "",
  "AnswerAudioFilePath": "${WAV_FILE_PATH}",
  "FaceFramesDirectoryPath": "",
  "FaceFrameCount": 0,
  "VoiceSegmentCount": 0,
  "RequestedAtUtc": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
}
JSON

"${SCRIPT_DIRECTORY_PATH}/run_bridge_analysis.sh" \
  "${REQUEST_FILE_PATH}" \
  "${RESULT_FILE_PATH}"

if [[ ! -f "${RESULT_FILE_PATH}" ]]; then
  echo "Bridge analysis result file was not produced." >&2
  exit 1
fi

if ! grep -q '"Verdict"[[:space:]]*:[[:space:]]*"UNCERTAIN"' "${RESULT_FILE_PATH}"; then
  echo "Expected UNCERTAIN verdict in bridge validation result." >&2
  cat "${RESULT_FILE_PATH}" >&2
  exit 1
fi

if ! grep -q '"insufficient_face_data"' "${RESULT_FILE_PATH}"; then
  echo "Expected insufficient_face_data reason code in bridge validation result." >&2
  cat "${RESULT_FILE_PATH}" >&2
  exit 1
fi

if ! grep -Eq '"AnswerTranscript"[[:space:]]*:[[:space:]]*"[^"]+' "${RESULT_FILE_PATH}"; then
  echo "Expected non-empty AnswerTranscript in bridge validation result." >&2
  cat "${RESULT_FILE_PATH}" >&2
  exit 1
fi

echo "Bridge runtime validation succeeded."
echo "Request: ${REQUEST_FILE_PATH}"
echo "Result: ${RESULT_FILE_PATH}"
