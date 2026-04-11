using System.IO;
using UnityEngine;

namespace MouthOfTruth.Game.Analysis
{
    public static class PythonAnalysisBridgePaths
    {
        private const string BRIDGE_DIRECTORY_NAME = "bridge";
        private const string REQUEST_FILE_NAME = "analysis_request.json";
        private const string RESULT_FILE_NAME = "analysis_result.json";
        private const string DEFAULT_PYTHON_INTERPRETER_PATH =
            "/Users/potterlim/Developments/Packages/miniforge/envs/mouth-truth/bin/python";
        private const string PYTHON_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_PYTHON";
        private const string PYTHON_MODULE_NAME = "mouth_of_truth.runners.bridge_analysis_runner";

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

        public static string GetPythonInterpreterPath()
        {
            string configuredInterpreterPath =
                System.Environment.GetEnvironmentVariable(PYTHON_ENVIRONMENT_VARIABLE_NAME);

            if (string.IsNullOrWhiteSpace(configuredInterpreterPath) == false)
            {
                return configuredInterpreterPath;
            }

            return DEFAULT_PYTHON_INTERPRETER_PATH;
        }

        public static string GetPythonModuleRootPath()
        {
            return Path.Combine(GetProjectRootPath(), "python-engine", "src");
        }

        public static string GetBridgeRunnerModuleName()
        {
            return PYTHON_MODULE_NAME;
        }
    }
}
