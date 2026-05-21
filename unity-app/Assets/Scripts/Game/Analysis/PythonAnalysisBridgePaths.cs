using System.IO;
using MouthOfTruth.Game.App;
using UnityEngine;

namespace MouthOfTruth.Game.Analysis
{
    public static class PythonAnalysisBridgePaths
    {
        private const string BRIDGE_DIRECTORY_NAME = "bridge";
        private const string REQUEST_FILE_NAME = "analysis_request.json";
        private const string RESULT_FILE_NAME = "analysis_result.json";
        private const string PYTHON_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_PYTHON";
        private const string PYTHON_MODULE_NAME = "mouth_of_truth.runners.bridge_analysis_runner";
        private const string PYTHON_WORKER_MODULE_NAME = "mouth_of_truth.runners.bridge_analysis_worker";
        private static readonly string[] BUNDLED_PYTHON_RELATIVE_PATHS =
        {
            "python-runtime/bin/python",
            "python-runtime/python",
            "python-runtime/python.exe",
        };

        public static string GetProjectRootPath()
        {
            return MouthOfTruthRuntimePaths.GetRuntimeRootPath();
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
            string configuredInterpreterPath = System.Environment.GetEnvironmentVariable(PYTHON_ENVIRONMENT_VARIABLE_NAME);

            if (string.IsNullOrWhiteSpace(configuredInterpreterPath) == false)
            {
                return configuredInterpreterPath;
            }

            string runtimeRootPath = MouthOfTruthRuntimePaths.GetRuntimeRootPath();

            foreach (string bundledPythonRelativePath in BUNDLED_PYTHON_RELATIVE_PATHS)
            {
                string bundledPythonPath = Path.Combine(runtimeRootPath, bundledPythonRelativePath);

                if (File.Exists(bundledPythonPath))
                {
                    return bundledPythonPath;
                }
            }

            return string.Empty;
        }

        public static string GetBridgeLauncherScriptPath()
        {
            string launcherFileName = Application.platform == RuntimePlatform.WindowsEditor
                || Application.platform == RuntimePlatform.WindowsPlayer
                    ? "run_bridge_analysis.bat"
                    : "run_bridge_analysis.sh";

            return Path.Combine(MouthOfTruthRuntimePaths.GetPythonEngineRootPath(), "scripts", launcherFileName);
        }

        public static string GetBridgeWorkerLauncherScriptPath()
        {
            string launcherFileName = Application.platform == RuntimePlatform.WindowsEditor
                || Application.platform == RuntimePlatform.WindowsPlayer
                    ? "run_bridge_analysis_worker.bat"
                    : "run_bridge_analysis_worker.sh";

            return Path.Combine(MouthOfTruthRuntimePaths.GetPythonEngineRootPath(), "scripts", launcherFileName);
        }

        public static string GetPythonModuleRootPath()
        {
            return Path.Combine(MouthOfTruthRuntimePaths.GetPythonEngineRootPath(), "src");
        }

        public static string GetBridgeRunnerModuleName()
        {
            return PYTHON_MODULE_NAME;
        }

        public static string GetBridgeWorkerModuleName()
        {
            return PYTHON_WORKER_MODULE_NAME;
        }
    }
}
