using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewMouthOfTruth.Game.Analysis
{
    public class SimulatedAnswerAnalysisClient : IAnswerAnalysisClient
    {
        private const int MINIMUM_FACE_RECOGNITION_COUNT = 5;
        private const int MINIMUM_VOICE_SEGMENT_COUNT = 1;

        public Task<AnswerAnalysisResult> AnalyzeAsync(
            AnswerAnalysisRequest answerAnalysisRequest,
            CancellationToken cancellationToken)
        {
            if (answerAnalysisRequest == null)
            {
                throw new ArgumentNullException(nameof(answerAnalysisRequest));
            }

            List<string> reasonCodes = new List<string>();

            if (answerAnalysisRequest.FaceRecognitionCount < MINIMUM_FACE_RECOGNITION_COUNT)
            {
                reasonCodes.Add("insufficient_face_data");
            }

            if (answerAnalysisRequest.VoiceSegmentCount < MINIMUM_VOICE_SEGMENT_COUNT)
            {
                reasonCodes.Add("insufficient_voice_data");
            }

            if (reasonCodes.Count > 0)
            {
                return Task.FromResult(
                    new AnswerAnalysisResult(EVerdictKind.Uncertain, reasonCodes));
            }

            int paritySeed = (
                answerAnalysisRequest.QuestionDefinition.ID
                + "|"
                + answerAnalysisRequest.AnswerTranscript.Trim()).GetHashCode();

            EVerdictKind verdictKind =
                Math.Abs(paritySeed) % 2 == 0
                    ? EVerdictKind.True
                    : EVerdictKind.False;

            return Task.FromResult(
                new AnswerAnalysisResult(verdictKind, Array.Empty<string>()));
        }
    }
}
