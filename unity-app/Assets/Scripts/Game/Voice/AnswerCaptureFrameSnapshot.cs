namespace MouthOfTruth.Game.Voice
{
    public class AnswerCaptureFrameSnapshot
    {
        public AnswerCaptureFrameSnapshot(string transcriptText, bool isSpeechDetected)
        {
            TranscriptText = string.IsNullOrEmpty(transcriptText) ? string.Empty : transcriptText;
            IsSpeechDetected = isSpeechDetected;
        }

        public string TranscriptText { get; }

        public bool IsSpeechDetected { get; }
    }
}
