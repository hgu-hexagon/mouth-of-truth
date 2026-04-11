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
            string faceFramesDirectoryPath,
            int faceFrameCount,
            int voiceSegmentCount)
        {
            QuestionDefinition = questionDefinition ?? throw new ArgumentNullException(nameof(questionDefinition));
            AnswerTranscript = answerTranscript ?? string.Empty;
            AnswerAudioFilePath = answerAudioFilePath ?? string.Empty;
            FaceFramesDirectoryPath = faceFramesDirectoryPath ?? string.Empty;
            FaceFrameCount = faceFrameCount;
            VoiceSegmentCount = voiceSegmentCount;
        }

        public QuestionDefinition QuestionDefinition { get; }

        public string AnswerTranscript { get; }

        public string AnswerAudioFilePath { get; }

        public string FaceFramesDirectoryPath { get; }

        public int FaceFrameCount { get; }

        public int VoiceSegmentCount { get; }
    }
}
