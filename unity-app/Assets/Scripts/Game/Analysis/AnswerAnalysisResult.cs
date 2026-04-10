using System;
using System.Collections.Generic;

namespace MouthOfTruth.Game.Analysis
{
    public class AnswerAnalysisResult
    {
        public AnswerAnalysisResult(
            EVerdictKind verdictKind,
            IReadOnlyList<string> reasonCodes)
        {
            VerdictKind = verdictKind;
            ReasonCodes = reasonCodes ?? Array.Empty<string>();
        }

        public EVerdictKind VerdictKind { get; }

        public IReadOnlyList<string> ReasonCodes { get; }
    }
}
