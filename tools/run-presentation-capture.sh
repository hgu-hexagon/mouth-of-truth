#!/usr/bin/env zsh
set -euo pipefail

SCRIPT_DIRECTORY_PATH="${0:A:h}"
REPOSITORY_ROOT_PATH="${SCRIPT_DIRECTORY_PATH:h}"
APP_BUNDLE_PATH="${REPOSITORY_ROOT_PATH}/dist/macos/MouthOfTruth/MouthOfTruth.app"
APP_EXECUTABLE_PATH="${APP_BUNDLE_PATH}/Contents/MacOS/MouthOfTruth"
DEFAULT_CAPTURE_OUTPUT_DIRECTORY_PATH="${REPOSITORY_ROOT_PATH}/presentation-captures/latest"
CAPTURE_OUTPUT_DIRECTORY_PATH="${MOUTH_OF_TRUTH_CAPTURE_OUTPUT_DIR:-${DEFAULT_CAPTURE_OUTPUT_DIRECTORY_PATH}}"
CAPTURE_LOG_PATH="${CAPTURE_OUTPUT_DIRECTORY_PATH}/capture.log"
CAPTURE_STDOUT_PATH="${CAPTURE_OUTPUT_DIRECTORY_PATH}/capture.stdout.log"
CAPTURE_STDERR_PATH="${CAPTURE_OUTPUT_DIRECTORY_PATH}/capture.stderr.log"

if [[ ! -x "${APP_EXECUTABLE_PATH}" ]]; then
    echo "MouthOfTruth app executable was not found."
    echo "Run ./tools/build-macos-release.sh first, then retry this capture runner."
    echo "Expected executable: ${APP_EXECUTABLE_PATH}"
    exit 1
fi

rm -rf "${CAPTURE_OUTPUT_DIRECTORY_PATH}"
mkdir -p "${CAPTURE_OUTPUT_DIRECTORY_PATH}"

open -n -W \
    --env MOUTH_OF_TRUTH_PRESENTATION_CAPTURE=1 \
    --env MOUTH_OF_TRUTH_CAPTURE_OUTPUT_DIR="${CAPTURE_OUTPUT_DIRECTORY_PATH}" \
    --stdout "${CAPTURE_STDOUT_PATH}" \
    --stderr "${CAPTURE_STDERR_PATH}" \
    "${APP_BUNDLE_PATH}" \
    --args -presentation-capture -screen-width 1280 -screen-height 720 -logFile "${CAPTURE_LOG_PATH}"

echo "Presentation captures written to:"
echo "${CAPTURE_OUTPUT_DIRECTORY_PATH}"
ls -1 "${CAPTURE_OUTPUT_DIRECTORY_PATH}"
