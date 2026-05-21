namespace MouthOfTruth.Game.Voice
{
    public class AnswerCaptureResult
    {
        public AnswerCaptureResult(string transcriptText, string audioFilePath, int voiceSegmentCount)
        {
            TranscriptText = string.IsNullOrEmpty(transcriptText) ? string.Empty : transcriptText;
            AudioFilePath = string.IsNullOrEmpty(audioFilePath) ? string.Empty : audioFilePath;
            VoiceSegmentCount = voiceSegmentCount;
        }

        public string TranscriptText { get; }

        public string AudioFilePath { get; }

        public int VoiceSegmentCount { get; }
    }
}
