using System;
using MouthOfTruth.Game.Data;

namespace MouthOfTruth.Game.Analysis
{
    public class AnswerAnalysisRequest
    {
        public AnswerAnalysisRequest(QuestionDefinition questionDefinition, string answerTranscript, string answerAudioFilePath, string faceFramesDirectoryPath, int faceFrameCount, int voiceSegmentCount)
        {
            if (questionDefinition == null)
            {
                throw new ArgumentNullException(nameof(questionDefinition));
            }

            QuestionDefinition = questionDefinition;
            AnswerTranscript = string.IsNullOrEmpty(answerTranscript) ? string.Empty : answerTranscript;
            AnswerAudioFilePath = string.IsNullOrEmpty(answerAudioFilePath) ? string.Empty : answerAudioFilePath;
            FaceFramesDirectoryPath = string.IsNullOrEmpty(faceFramesDirectoryPath) ? string.Empty : faceFramesDirectoryPath;
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
