using System;
using MouthOfTruth.Game.Data;

namespace MouthOfTruth.Game.Analysis
{
    public class AnswerAnalysisRequest
    {
        public AnswerAnalysisRequest(
            QuestionDefinition questionDefinition,
            string answerTranscript,
            int faceRecognitionCount,
            int voiceSegmentCount)
        {
            QuestionDefinition = questionDefinition ?? throw new ArgumentNullException(nameof(questionDefinition));
            AnswerTranscript = answerTranscript ?? string.Empty;
            FaceRecognitionCount = faceRecognitionCount;
            VoiceSegmentCount = voiceSegmentCount;
        }

        public QuestionDefinition QuestionDefinition { get; }

        public string AnswerTranscript { get; }

        public int FaceRecognitionCount { get; }

        public int VoiceSegmentCount { get; }
    }
}
