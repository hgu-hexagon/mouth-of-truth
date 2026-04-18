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

        public static string FloorRunnerPath =>
            GetStreamingArtPath("environment/floor_red_carpet_runner.jpeg");

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

        public static string CardSelectionGlowPath =>
            GetStreamingArtPath("effects/card_selection_glow.png");

        public static string CardSelectionProgressFillPath =>
            GetStreamingArtPath("effects/card_selection_progress_fill.png");

        public static string TitleAmbiencePath =>
            GetStreamingAudioPath("ambience/title_temple_ambience_loop.ogg");

        public static string ButtonConfirmPath =>
            GetStreamingAudioPath("ui/button_confirm.ogg");

        public static string CardHoverPath =>
            GetStreamingAudioPath("cards/card_hover.ogg");

        public static string CardSelectPath =>
            GetStreamingAudioPath("cards/card_select.ogg");

        public static string CardRevealPath =>
            GetStreamingAudioPath("cards/card_reveal.ogg");

        public static string HandInsertPath =>
            GetStreamingAudioPath("interaction/hand_insert.ogg");

        public static string HandPausePath =>
            GetStreamingAudioPath("interaction/hand_pause.ogg");

        public static string ResultTruePath =>
            GetStreamingAudioPath("results/result_true.ogg");

        public static string ResultFalsePath =>
            GetStreamingAudioPath("results/result_false.ogg");

        public static string ResultUncertainPath =>
            GetStreamingAudioPath("results/result_uncertain.ogg");
    }
}
