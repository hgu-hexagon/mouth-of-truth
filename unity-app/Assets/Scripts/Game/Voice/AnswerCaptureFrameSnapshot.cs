namespace MouthOfTruth.Game.Voice
{
    public class AnswerCaptureFrameSnapshot
    {
        public AnswerCaptureFrameSnapshot(string transcriptText, bool isSpeechDetected)
        {
            TranscriptText = transcriptText ?? string.Empty;
            IsSpeechDetected = isSpeechDetected;
        }

        public string TranscriptText { get; }

        public bool IsSpeechDetected { get; }
    }
}
