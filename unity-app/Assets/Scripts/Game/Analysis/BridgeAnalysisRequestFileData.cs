using System;

namespace MouthOfTruth.Game.Analysis
{
    [Serializable]
    internal sealed class BridgeAnalysisRequestFileData
    {
        // JsonUtility maps fields by the Python bridge protocol keys.
        public string RequestID = string.Empty;
        public string QuestionID = string.Empty;
        public string QuestionText = string.Empty;
        public string AnswerTranscript = string.Empty;
        public string AnswerAudioFilePath = string.Empty;
        public string FaceFramesDirectoryPath = string.Empty;
        public int FaceFrameCount = 0;
        public int VoiceSegmentCount = 0;
        public string RequestedAtUtc = string.Empty;
    }
}
