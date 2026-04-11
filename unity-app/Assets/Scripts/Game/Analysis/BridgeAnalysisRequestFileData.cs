using System;

namespace MouthOfTruth.Game.Analysis
{
    [Serializable]
    public class BridgeAnalysisRequestFileData
    {
        public string RequestID = string.Empty;
        public string QuestionID = string.Empty;
        public string QuestionText = string.Empty;
        public string AnswerTranscript = string.Empty;
        public string AnswerAudioFilePath = string.Empty;
        public string FaceFramesDirectoryPath = string.Empty;
        public int FaceFrameCount;
        public int VoiceSegmentCount;
        public string RequestedAtUtc = string.Empty;
    }
}
