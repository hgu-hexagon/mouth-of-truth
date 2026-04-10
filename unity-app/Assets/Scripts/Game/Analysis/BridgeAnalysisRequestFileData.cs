using System;

namespace NewMouthOfTruth.Game.Analysis
{
    [Serializable]
    public class BridgeAnalysisRequestFileData
    {
        public string RequestID = string.Empty;
        public string QuestionID = string.Empty;
        public string QuestionText = string.Empty;
        public string AnswerTranscript = string.Empty;
        public int FaceRecognitionCount;
        public int VoiceSegmentCount;
        public string RequestedAtUtc = string.Empty;
    }
}
