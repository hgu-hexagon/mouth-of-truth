using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MouthOfTruth.Game.Analysis
{
    public class DeterministicAnswerAnalysisClient : IAnswerAnalysisClient
    {
        private const int MINIMUM_FACE_RECOGNITION_COUNT = 4;
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

            if (answerAnalysisRequest.FaceFrameCount < MINIMUM_FACE_RECOGNITION_COUNT)
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
                    new AnswerAnalysisResult(
                        EVerdictKind.Uncertain,
                        answerAnalysisRequest.AnswerTranscript,
                        reasonCodes));
            }

            int paritySeed = calculateStableParitySeed(
                answerAnalysisRequest.QuestionDefinition.ID,
                answerAnalysisRequest.AnswerTranscript);

            EVerdictKind verdictKind =
                paritySeed % 2 == 0
                    ? EVerdictKind.True
                    : EVerdictKind.False;

            return Task.FromResult(
                new AnswerAnalysisResult(
                    verdictKind,
                    answerAnalysisRequest.AnswerTranscript,
                    Array.Empty<string>()));
        }

        private int calculateStableParitySeed(string questionID, string answerTranscript)
        {
            string combinedText = $"{questionID}|{answerTranscript.Trim()}";
            int checksum = 0;

            foreach (char character in combinedText)
            {
                checksum += character;
            }

            return checksum;
        }
    }
}
