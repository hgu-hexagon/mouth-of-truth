using System;

namespace NewMouthOfTruth.Game.Analysis
{
    [Serializable]
    public class BridgeAnalysisResultFileData
    {
        public string RequestID = string.Empty;
        public string Verdict = string.Empty;
        public string[] ReasonCodes = Array.Empty<string>();
        public string CompletedAtUtc = string.Empty;
    }
}
