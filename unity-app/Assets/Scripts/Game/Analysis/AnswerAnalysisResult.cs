using System;
using System.Collections.Generic;

namespace MouthOfTruth.Game.Analysis
{
    public class AnswerAnalysisResult
    {
        public AnswerAnalysisResult(EVerdictKind verdictKind, string answerTranscript, IReadOnlyList<string> reasonCodes)
        {
            VerdictKind = verdictKind;
            AnswerTranscript = string.IsNullOrEmpty(answerTranscript) ? string.Empty : answerTranscript;
            ReasonCodes = reasonCodes == null ? Array.Empty<string>() : reasonCodes;
        }

        public EVerdictKind VerdictKind { get; }

        public string AnswerTranscript { get; }

        public IReadOnlyList<string> ReasonCodes { get; }
    }
}
