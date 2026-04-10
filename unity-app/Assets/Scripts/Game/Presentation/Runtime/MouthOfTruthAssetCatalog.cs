using System.IO;
using UnityEngine;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public static class MouthOfTruthAssetCatalog
    {
        private const string ART_DIRECTORY_NAME = "art";

        public static string GetStreamingArtPath(string relativePath)
        {
            return Path.Combine(Application.streamingAssetsPath, ART_DIRECTORY_NAME, relativePath);
        }

        public static string TitleBackgroundPath =>
            GetStreamingArtPath("backgrounds/title_background_stone_wall.jpeg");

        public static string TitleLogoPath =>
            GetStreamingArtPath("ui/logo_title_main.jpeg");

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
            GetStreamingArtPath("cards/question_card_back.jpeg");

        public static string TruthMouthFacePath =>
            GetStreamingArtPath("mouth/truth_mouth_face.jpeg");

        public static string TrueVerdictPath =>
            GetStreamingArtPath("verdict/verdict_true.jpeg");

        public static string FalseVerdictPath =>
            GetStreamingArtPath("verdict/verdict_false.jpeg");

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
    }
}
