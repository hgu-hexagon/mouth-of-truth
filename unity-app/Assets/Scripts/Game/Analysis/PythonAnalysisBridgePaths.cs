using System.IO;
using UnityEngine;

namespace MouthOfTruth.Game.Analysis
{
    public static class PythonAnalysisBridgePaths
    {
        private const string BRIDGE_DIRECTORY_NAME = "bridge";
        private const string REQUEST_FILE_NAME = "analysis_request.json";
        private const string RESULT_FILE_NAME = "analysis_result.json";

        public static string GetProjectRootPath()
        {
            return Directory.GetParent(Application.dataPath)?.Parent?.FullName
                ?? Directory.GetParent(Application.dataPath)?.FullName
                ?? Application.dataPath;
        }

        public static string GetBridgeDirectoryPath()
        {
            return Path.Combine(GetProjectRootPath(), BRIDGE_DIRECTORY_NAME);
        }

        public static string GetRequestFilePath()
        {
            return Path.Combine(GetBridgeDirectoryPath(), REQUEST_FILE_NAME);
        }

        public static string GetResultFilePath()
        {
            return Path.Combine(GetBridgeDirectoryPath(), RESULT_FILE_NAME);
        }
    }
}
