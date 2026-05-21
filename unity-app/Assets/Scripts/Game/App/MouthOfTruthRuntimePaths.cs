using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MouthOfTruth.Game.App
{
    public static class MouthOfTruthRuntimePaths
    {
        private const string RUNTIME_ROOT_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_RUNTIME_ROOT";
        private const string PYTHON_ENGINE_DIRECTORY_NAME = "python-engine";
        private const string BRIDGE_DIRECTORY_NAME = "bridge";
        private static readonly string[] BRIDGE_LAUNCHER_RELATIVE_PATHS =
        {
            "python-engine/scripts/run_bridge_analysis.sh",
            "python-engine/scripts/run_bridge_analysis.bat",
        };

        public static string GetRuntimeRootPath()
        {
            string configuredRuntimeRootPath = Environment.GetEnvironmentVariable(RUNTIME_ROOT_ENVIRONMENT_VARIABLE_NAME);

            if (string.IsNullOrWhiteSpace(configuredRuntimeRootPath) == false && Directory.Exists(configuredRuntimeRootPath))
            {
                return configuredRuntimeRootPath;
            }

            foreach (string candidateRuntimeRootPath in enumerateCandidateRuntimeRoots())
            {
                if (isRuntimeRoot(candidateRuntimeRootPath))
                {
                    return candidateRuntimeRootPath;
                }
            }

            DirectoryInfo unityProjectDirectoryInfo = Directory.GetParent(Application.dataPath);
            if (unityProjectDirectoryInfo == null)
            {
                unityProjectDirectoryInfo = new DirectoryInfo(Application.dataPath);
            }

            if (unityProjectDirectoryInfo.Parent != null)
            {
                return unityProjectDirectoryInfo.Parent.FullName;
            }

            return unityProjectDirectoryInfo.FullName;
        }

        public static string GetBridgeDirectoryPath()
        {
            return Path.Combine(GetRuntimeRootPath(), BRIDGE_DIRECTORY_NAME);
        }

        public static string GetPythonEngineRootPath()
        {
            return Path.Combine(GetRuntimeRootPath(), PYTHON_ENGINE_DIRECTORY_NAME);
        }

        private static IEnumerable<string> enumerateCandidateRuntimeRoots()
        {
            DirectoryInfo currentDirectoryInfo = new DirectoryInfo(Application.dataPath);

            while (currentDirectoryInfo != null)
            {
                yield return currentDirectoryInfo.FullName;

                if (currentDirectoryInfo.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase) && currentDirectoryInfo.Parent != null)
                {
                    yield return currentDirectoryInfo.Parent.FullName;
                }

                currentDirectoryInfo = currentDirectoryInfo.Parent;
            }
        }

        private static bool isRuntimeRoot(string candidateRuntimeRootPath)
        {
            if (string.IsNullOrWhiteSpace(candidateRuntimeRootPath))
            {
                return false;
            }

            string bridgeDirectoryPath = Path.Combine(candidateRuntimeRootPath, BRIDGE_DIRECTORY_NAME);

            if (Directory.Exists(bridgeDirectoryPath) == false)
            {
                return false;
            }

            foreach (string bridgeLauncherRelativePath in BRIDGE_LAUNCHER_RELATIVE_PATHS)
            {
                string bridgeLauncherScriptPath = Path.Combine(candidateRuntimeRootPath, bridgeLauncherRelativePath);

                if (File.Exists(bridgeLauncherScriptPath))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
