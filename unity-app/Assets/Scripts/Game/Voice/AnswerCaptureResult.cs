namespace MouthOfTruth.Game.Voice
{
    public class AnswerCaptureResult
    {
        public AnswerCaptureResult(
            string transcriptText,
            string audioFilePath,
            int voiceSegmentCount)
        {
            TranscriptText = transcriptText ?? string.Empty;
            AudioFilePath = audioFilePath ?? string.Empty;
            VoiceSegmentCount = voiceSegmentCount;
        }

        public string TranscriptText { get; }

        public string AudioFilePath { get; }

        public int VoiceSegmentCount { get; }
    }
}
