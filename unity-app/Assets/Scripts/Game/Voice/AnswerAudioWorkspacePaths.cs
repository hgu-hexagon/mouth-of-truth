using System;
using System.IO;
using MouthOfTruth.Game.App;
using UnityEngine;

namespace MouthOfTruth.Game.Voice
{
    public static class AnswerAudioWorkspacePaths
    {
        private const string WORKSPACE_DIRECTORY_NAME = "session-workspace";
        private const string AUDIO_DIRECTORY_NAME = "answer-audio";

        public static string GetWorkspaceDirectoryPath()
        {
            return Path.Combine(MouthOfTruthRuntimePaths.GetPythonEngineRootPath(), "data", WORKSPACE_DIRECTORY_NAME);
        }

        public static string GetAudioDirectoryPath()
        {
            return Path.Combine(GetWorkspaceDirectoryPath(), AUDIO_DIRECTORY_NAME);
        }

        public static string BuildAudioFilePath(string questionID)
        {
            string sanitizedQuestionID = string.IsNullOrWhiteSpace(questionID)
                ? "question"
                : questionID.Trim().Replace(" ", "_");
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            return Path.Combine(GetAudioDirectoryPath(), $"{sanitizedQuestionID}_{timestamp}.wav");
        }
    }
}
