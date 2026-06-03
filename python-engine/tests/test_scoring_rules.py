from __future__ import annotations

import unittest

from mouth_of_truth.face.face_score_logic import calculate_base_score, calculate_suspicion_score, summarize_session
from mouth_of_truth.voice.voice_score_logic import calculate_voice_base_score, calculate_voice_suspicion_score, summarize_voice_session


class ScoringRulesTest(unittest.TestCase):
    def test_face_base_score_treats_tense_expression_as_more_suspicious_than_neutral(self) -> None:
        calm_score = calculate_base_score({"neutral": 0.90, "happiness": 0.10})
        tense_score = calculate_base_score({"fear": 0.70, "anger": 0.20, "neutral": 0.10})

        self.assertGreater(tense_score, calm_score)

    def test_face_suspicion_score_clamps_to_supported_range(self) -> None:
        self.assertEqual(calculate_suspicion_score(200.0, 200.0), 100.0)
        self.assertEqual(calculate_suspicion_score(-20.0, -20.0), 0.0)

    def test_voice_base_score_treats_tense_labels_as_more_suspicious_than_stable_labels(self) -> None:
        stable_score = calculate_voice_base_score({"neu": 0.80, "hap": 0.20})
        tense_score = calculate_voice_base_score({"ang": 0.55, "fru": 0.35, "neu": 0.10})

        self.assertGreater(tense_score, stable_score)

    def test_voice_suspicion_score_clamps_to_supported_range(self) -> None:
        self.assertEqual(calculate_voice_suspicion_score(200.0, 200.0), 100.0)
        self.assertEqual(calculate_voice_suspicion_score(-20.0, -20.0), 0.0)

    def test_empty_modality_summaries_are_marked_as_no_data(self) -> None:
        self.assertEqual(summarize_session([])["dominant_label"], "N/A")
        self.assertEqual(summarize_voice_session([])["dominant_label"], "N/A")


if __name__ == "__main__":
    unittest.main()
