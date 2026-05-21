using System;
using System.IO;
using MouthOfTruth.Game.App;
using UnityEngine;

namespace MouthOfTruth.Game.Face
{
    public static class FaceFrameWorkspacePaths
    {
        private const string WORKSPACE_DIRECTORY_NAME = "session-workspace";
        private const string FACE_DIRECTORY_NAME = "face-frames";

        public static string GetWorkspaceDirectoryPath()
        {
            return Path.Combine(MouthOfTruthRuntimePaths.GetPythonEngineRootPath(), "data", WORKSPACE_DIRECTORY_NAME);
        }

        public static string GetFaceFramesDirectoryPath()
        {
            return Path.Combine(GetWorkspaceDirectoryPath(), FACE_DIRECTORY_NAME);
        }

        public static string BuildCaptureDirectoryPath(string questionID)
        {
            string sanitizedQuestionID = string.IsNullOrWhiteSpace(questionID)
                ? "question"
                : questionID.Trim().Replace(" ", "_");
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            return Path.Combine(GetFaceFramesDirectoryPath(), $"{sanitizedQuestionID}_{timestamp}");
        }
    }
}
