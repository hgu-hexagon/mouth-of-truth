from __future__ import annotations

import unittest

from mouth_of_truth.contracts.verdict_kind import VerdictKind
from mouth_of_truth.fusion.judgment_policy import build_analysis_result
from mouth_of_truth.fusion.multimodal_fusion import fuse_face_and_voice


class JudgmentPolicyTest(unittest.TestCase):
    def test_build_analysis_result_returns_true_when_both_signals_are_below_false_threshold(self) -> None:
        result = build_analysis_result(
            request_id="request-true",
            answer_transcript="대답",
            face_result={"avg_score": 20.0, "dominant_label": "neutral"},
            voice_result={"avg_score": 10.0, "dominant_label": "neu"},
            face_recognition_count=1,
            voice_segment_count=1,
        )

        self.assertEqual(result.verdict, VerdictKind.TRUE)
        self.assertEqual(result.reason_codes, [])

    def test_build_analysis_result_returns_false_when_fused_score_reaches_false_threshold(self) -> None:
        result = build_analysis_result(
            request_id="request-false",
            answer_transcript="대답",
            face_result={"avg_score": 45.0, "dominant_label": "fear"},
            voice_result={"avg_score": 20.0, "dominant_label": "fru"},
            face_recognition_count=1,
            voice_segment_count=1,
        )

        self.assertEqual(result.verdict, VerdictKind.FALSE)
        self.assertEqual(result.reason_codes, [])

    def test_build_analysis_result_returns_uncertain_when_face_evidence_is_missing(self) -> None:
        result = build_analysis_result(
            request_id="request-no-face",
            answer_transcript="대답",
            face_result={"avg_score": 0.0, "dominant_label": "N/A"},
            voice_result={"avg_score": 80.0, "dominant_label": "ang"},
            face_recognition_count=0,
            voice_segment_count=1,
        )

        self.assertEqual(result.verdict, VerdictKind.UNCERTAIN)
        self.assertEqual(result.reason_codes, ["insufficient_face_data"])

    def test_build_analysis_result_returns_uncertain_when_voice_evidence_is_missing(self) -> None:
        result = build_analysis_result(
            request_id="request-no-voice",
            answer_transcript="대답",
            face_result={"avg_score": 80.0, "dominant_label": "anger"},
            voice_result={"avg_score": 0.0, "dominant_label": "N/A"},
            face_recognition_count=1,
            voice_segment_count=0,
        )

        self.assertEqual(result.verdict, VerdictKind.UNCERTAIN)
        self.assertEqual(result.reason_codes, ["insufficient_voice_data"])

    def test_fuse_face_and_voice_clamps_final_score(self) -> None:
        result = fuse_face_and_voice(
            face_result={"avg_score": 200.0},
            voice_result={"avg_score": 200.0},
        )

        self.assertEqual(result["final_score"], 100.0)
        self.assertEqual(result["verdict"], VerdictKind.FALSE)


if __name__ == "__main__":
    unittest.main()
