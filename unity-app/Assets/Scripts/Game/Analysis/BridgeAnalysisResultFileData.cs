using System;

namespace MouthOfTruth.Game.Analysis
{
    [Serializable]
    internal sealed class BridgeAnalysisResultFileData
    {
        // JsonUtility maps fields by the Python bridge protocol keys.
        public string RequestID = string.Empty;
        public string Verdict = string.Empty;
        public string AnswerTranscript = string.Empty;
        public string[] ReasonCodes = Array.Empty<string>();
        public string CompletedAtUtc = string.Empty;
    }
}
