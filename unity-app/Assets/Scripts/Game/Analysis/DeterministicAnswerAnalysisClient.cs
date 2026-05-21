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
        private const string INSUFFICIENT_FACE_DATA_REASON_CODE = "insufficient_face_data";
        private const string INSUFFICIENT_VOICE_DATA_REASON_CODE = "insufficient_voice_data";

        public Task WarmUpAsync(CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<AnswerAnalysisResult> AnalyzeAsync(AnswerAnalysisRequest answerAnalysisRequest, CancellationToken cancellationToken)
        {
            if (answerAnalysisRequest == null)
            {
                throw new ArgumentNullException(nameof(answerAnalysisRequest));
            }

            _ = cancellationToken;

            bool hasFaceSignal = answerAnalysisRequest.FaceFrameCount >= MINIMUM_FACE_RECOGNITION_COUNT;
            bool hasVoiceSignal = answerAnalysisRequest.VoiceSegmentCount >= MINIMUM_VOICE_SEGMENT_COUNT;
            List<string> reasonCodes = new List<string>();

            if (hasFaceSignal == false)
            {
                reasonCodes.Add(INSUFFICIENT_FACE_DATA_REASON_CODE);
            }

            if (hasVoiceSignal == false)
            {
                reasonCodes.Add(INSUFFICIENT_VOICE_DATA_REASON_CODE);
            }

            if (hasFaceSignal == false || hasVoiceSignal == false)
            {
                return Task.FromResult(new AnswerAnalysisResult(EVerdictKind.Uncertain, answerAnalysisRequest.AnswerTranscript, reasonCodes));
            }

            int paritySeed = calculateStableParitySeed(answerAnalysisRequest.QuestionDefinition.ID, answerAnalysisRequest.AnswerTranscript);

            EVerdictKind verdictKind = paritySeed % 2 == 0 ? EVerdictKind.True : EVerdictKind.False;

            return Task.FromResult(new AnswerAnalysisResult(verdictKind, answerAnalysisRequest.AnswerTranscript, reasonCodes));
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
