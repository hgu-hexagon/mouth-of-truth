from __future__ import annotations

import json
import os
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from mouth_of_truth.contracts.analysis_contracts import AnalysisResult, read_analysis_request, write_analysis_result
from mouth_of_truth.contracts.verdict_kind import VerdictKind


class AnalysisContractsTest(unittest.TestCase):
    def test_read_analysis_request_resolves_runtime_relative_paths(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            runtime_root_path = Path(temporary_directory)
            request_file_path = runtime_root_path / "bridge" / "analysis_request.json"
            request_file_path.parent.mkdir(parents=True)
            request_file_path.write_text(
                json.dumps(
                    {
                        "RequestID": "request-1",
                        "QuestionID": "question-1",
                        "QuestionText": "질문",
                        "AnswerTranscript": "대답",
                        "AnswerAudioFilePath": "python-engine/data/session-workspace/answer.wav",
                        "FaceFramesDirectoryPath": "python-engine/data/session-workspace/face",
                        "FaceFrameCount": 3,
                        "VoiceSegmentCount": 1,
                        "RequestedAtUtc": "2026-06-03T00:00:00Z",
                    }
                ),
                encoding="utf-8",
            )

            with patch.dict(os.environ, {"MOUTH_OF_TRUTH_RUNTIME_ROOT": str(runtime_root_path)}):
                analysis_request = read_analysis_request(request_file_path)

            self.assertEqual(analysis_request.request_id, "request-1")
            self.assertEqual(analysis_request.question_id, "question-1")
            self.assertEqual(analysis_request.question_text, "질문")
            self.assertEqual(analysis_request.answer_transcript, "대답")
            self.assertEqual(analysis_request.face_frame_count, 3)
            self.assertEqual(analysis_request.voice_segment_count, 1)
            self.assertEqual(
                analysis_request.answer_audio_file_path,
                str((runtime_root_path / "python-engine" / "data" / "session-workspace" / "answer.wav").resolve()),
            )
            self.assertEqual(
                analysis_request.face_frames_directory_path,
                str((runtime_root_path / "python-engine" / "data" / "session-workspace" / "face").resolve()),
            )

    def test_write_analysis_result_uses_unity_bridge_keys(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            result_file_path = Path(temporary_directory) / "analysis_result.json"
            analysis_result = AnalysisResult(
                request_id="request-2",
                verdict=VerdictKind.UNCERTAIN,
                answer_transcript="대답",
                reason_codes=["insufficient_face_data"],
            )

            write_analysis_result(result_file_path, analysis_result)

            payload = json.loads(result_file_path.read_text(encoding="utf-8"))
            self.assertEqual(payload["RequestID"], "request-2")
            self.assertEqual(payload["Verdict"], "UNCERTAIN")
            self.assertEqual(payload["AnswerTranscript"], "대답")
            self.assertEqual(payload["ReasonCodes"], ["insufficient_face_data"])
            self.assertIn("CompletedAtUtc", payload)


if __name__ == "__main__":
    unittest.main()
