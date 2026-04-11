using System;
using MouthOfTruth.Game.Data;

namespace MouthOfTruth.Game.Analysis
{
    public class AnswerAnalysisRequest
    {
        public AnswerAnalysisRequest(
            QuestionDefinition questionDefinition,
            string answerTranscript,
            string answerAudioFilePath,
            int faceRecognitionCount,
            int voiceSegmentCount)
        {
            QuestionDefinition = questionDefinition ?? throw new ArgumentNullException(nameof(questionDefinition));
            AnswerTranscript = answerTranscript ?? string.Empty;
            AnswerAudioFilePath = answerAudioFilePath ?? string.Empty;
            FaceRecognitionCount = faceRecognitionCount;
            VoiceSegmentCount = voiceSegmentCount;
        }

        public QuestionDefinition QuestionDefinition { get; }

        public string AnswerTranscript { get; }

        public string AnswerAudioFilePath { get; }

        public int FaceRecognitionCount { get; }

        public int VoiceSegmentCount { get; }
    }
}
