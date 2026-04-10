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

        public static string StartBackgroundPath =>
            GetStreamingArtPath("backgrounds/start_stone_wall.jpeg");

        public static string LogoTitlePath =>
            GetStreamingArtPath("ui/logo_title.jpeg");

        public static string TitleVignettePath =>
            GetStreamingArtPath("ui/title_vignette.png");

        public static string QuestionPanelPath =>
            GetStreamingArtPath("ui/question_panel.png");

        public static string StatusPanelPath =>
            GetStreamingArtPath("ui/status_panel.png");

        public static string ResultPanelPath =>
            GetStreamingArtPath("ui/result_panel.png");

        public static string RedCarpetPath =>
            GetStreamingArtPath("environment/red_carpet_runner.jpeg");

        public static string CardBackPath =>
            GetStreamingArtPath("cards/card_back.jpeg");

        public static string TruthMouthPath =>
            GetStreamingArtPath("mouth/truth_mouth.jpeg");

        public static string VerdictTruePath =>
            GetStreamingArtPath("verdict/true.jpeg");

        public static string VerdictFalsePath =>
            GetStreamingArtPath("verdict/false.jpeg");

        public static string VerdictUncertainPath =>
            GetStreamingArtPath("verdict/uncertain.png");

        public static string ButtonFramePath =>
            GetStreamingArtPath("ui/button_frame.png");

        public static string HandCursorPath =>
            GetStreamingArtPath("input/hand_cursor.png");

        public static string CardGlowPath =>
            GetStreamingArtPath("effects/card_glow.png");

        public static string DwellFillPath =>
            GetStreamingArtPath("effects/dwell_fill.png");
    }
}
