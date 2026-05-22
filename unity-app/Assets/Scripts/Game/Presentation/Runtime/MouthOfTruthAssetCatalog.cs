using System.IO;
using UnityEngine;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public static class MouthOfTruthAssetCatalog
    {
        private const string ART_DIRECTORY_NAME = "art";
        private const string AUDIO_DIRECTORY_NAME = "audio";

        public static string GetStreamingArtPath(string relativePath)
        {
            return Path.Combine(Application.streamingAssetsPath, ART_DIRECTORY_NAME, relativePath);
        }

        public static string GetStreamingAudioPath(string relativePath)
        {
            return Path.Combine(Application.streamingAssetsPath, AUDIO_DIRECTORY_NAME, relativePath);
        }

        public static string TitleBackgroundPath =>
            GetStreamingArtPath("backgrounds/title_background_stone_wall.jpeg");

        public static string CardSelectionBackgroundPath =>
            GetStreamingArtPath("backgrounds/stage_card_selection_generated.png");

        public static string MouthChamberBackgroundPath =>
            GetStreamingArtPath("backgrounds/stage_mouth_chamber_generated.png");

        public static string TitleLogoPath =>
            GetStreamingArtPath("ui/logo_title_main.png");

        public static string TitleVignettePath =>
            GetStreamingArtPath("ui/title_vignette.png");

        public static string QuestionPanelFramePath =>
            GetStreamingArtPath("ui/panel_question.png");

        public static string StatusPanelFramePath =>
            GetStreamingArtPath("ui/panel_status.png");

        public static string ResultPanelFramePath =>
            GetStreamingArtPath("ui/panel_result.png");

        public static string StartButtonPath =>
            GetStreamingArtPath("ui/button_start_game.png");

        public static string TryAgainButtonPath =>
            GetStreamingArtPath("ui/button_try_again.png");

        public static string EndGameButtonPath =>
            GetStreamingArtPath("ui/button_end_game.png");

        public static string ExitIconButtonPath =>
            GetStreamingArtPath("ui/button_exit_icon.png");

        public static string UiFontResourceName => "Fonts/GowunDodum-Regular";

        public static string KoreanFallbackFontResourceName => "Fonts/GowunDodum-Regular";

        public static string FloorRunnerPath =>
            GetStreamingArtPath("environment/floor_red_carpet_runner.png");

        public static string FirstRunTutorialSequencePath =>
            Path.Combine(Application.streamingAssetsPath, "tutorial", "combined_sequence.json");

        public static string QuestionCardBackPath =>
            GetStreamingArtPath("cards/question_card_back.png");

        public static string QuestionCardFrontPath =>
            GetStreamingArtPath("cards/question_card_front.png");

        public static string TruthMouthFacePath =>
            GetStreamingArtPath("mouth/truth_mouth_face.png");

        public static string TrueVerdictPath =>
            GetStreamingArtPath("verdict/verdict_true.png");

        public static string FalseVerdictPath =>
            GetStreamingArtPath("verdict/verdict_false.png");

        public static string UncertainVerdictPath =>
            GetStreamingArtPath("verdict/verdict_uncertain.png");

        public static string PrimaryButtonFramePath =>
            GetStreamingArtPath("ui/button_frame_primary.png");

        public static string HandPointerPath =>
            GetStreamingArtPath("input/hand_pointer_cursor.png");

        public static string RitualHandInsertPath =>
            GetStreamingArtPath("input/ritual_hand_insert.png");

        public static string LeapMotionDevicePath =>
            GetStreamingArtPath("input/leap_motion_device.png");

        public static string CardSelectionGlowPath =>
            GetStreamingArtPath("effects/card_selection_glow.png");

        public static string CardSelectionProgressFillPath =>
            GetStreamingArtPath("effects/card_selection_progress_fill.png");

        public static string TitleAmbiencePath =>
            GetStreamingAudioPath("ambience/title_temple_ambience_loop.wav");

        public static string ButtonConfirmPath =>
            GetStreamingAudioPath("ui/button_confirm.wav");

        public static string CardHoverPath =>
            GetStreamingAudioPath("cards/card_hover.wav");

        public static string CardSelectPath =>
            GetStreamingAudioPath("cards/card_select.wav");

        public static string CardRevealPath =>
            GetStreamingAudioPath("cards/card_reveal.wav");

        public static string HandInsertPath =>
            GetStreamingAudioPath("interaction/hand_insert.wav");

        public static string HandPromptPath =>
            GetStreamingAudioPath("interaction/hand_prompt.wav");

        public static string ResultTruePath =>
            GetStreamingAudioPath("results/result_true.wav");

        public static string ResultFalsePath =>
            GetStreamingAudioPath("results/result_false.wav");

        public static string ResultUncertainPath =>
            GetStreamingAudioPath("results/result_uncertain.wav");
    }
}
