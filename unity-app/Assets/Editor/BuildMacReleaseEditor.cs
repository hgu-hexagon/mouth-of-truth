using System.Diagnostics;
using System.IO;
using MouthOfTruth.Game.App;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MouthOfTruth.Editor
{
    public static class BuildMacReleaseEditor
    {
        private const string MAIN_SCENE_PATH = "Assets/Scenes/Main.unity";
        private const string DISTRIBUTION_ROOT_RELATIVE_PATH = "dist/macos/MouthOfTruth";
        private const string APPLICATION_NAME = "MouthOfTruth.app";
        private const string PYTHON_RUNTIME_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_PYTHON_RUNTIME_ROOT";

        [MenuItem("Mouth Of Truth/Build Mac Release")]
        public static void Run()
        {
            string runtimeRootPath = MouthOfTruthRuntimePaths.GetRuntimeRootPath();
            string distributionRootPath = Path.Combine(runtimeRootPath, DISTRIBUTION_ROOT_RELATIVE_PATH);
            string applicationPath = Path.Combine(distributionRootPath, APPLICATION_NAME);

            recreateDirectory(distributionRootPath);

            BuildReport buildReport = BuildPipeline.BuildPlayer(
                new[]
                {
                    MAIN_SCENE_PATH,
                },
                applicationPath,
                BuildTarget.StandaloneOSX,
                BuildOptions.None);

            if (buildReport.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Mac release build failed with result {buildReport.summary.result}.");
            }

            stageRuntimeSupport(runtimeRootPath, distributionRootPath);
            writeLauncherScript(distributionRootPath);
            AssetDatabase.Refresh();
        }

        private static void stageRuntimeSupport(string runtimeRootPath, string distributionRootPath)
        {
            string distributionPythonEngineRootPath = Path.Combine(distributionRootPath, "python-engine");
            Directory.CreateDirectory(distributionPythonEngineRootPath);
            copyPath(
                Path.Combine(runtimeRootPath, "python-engine", "src"),
                Path.Combine(distributionPythonEngineRootPath, "src"));
            copyPath(
                Path.Combine(runtimeRootPath, "python-engine", "scripts"),
                Path.Combine(distributionPythonEngineRootPath, "scripts"));
            copyPath(
                Path.Combine(runtimeRootPath, "python-engine", "models"),
                Path.Combine(distributionPythonEngineRootPath, "models"));
            copyPath(
                Path.Combine(runtimeRootPath, "python-engine", "requirements.txt"),
                Path.Combine(distributionPythonEngineRootPath, "requirements.txt"));
            copyPath(
                Path.Combine(runtimeRootPath, "python-engine", "environment.yml"),
                Path.Combine(distributionPythonEngineRootPath, "environment.yml"));
            ensureSessionWorkspaceDirectory(
                Path.Combine(distributionPythonEngineRootPath, "data", "session-workspace"));
            ensureBridgeDirectory(Path.Combine(distributionRootPath, "bridge"));

            string configuredPythonRuntimeRootPath =
                System.Environment.GetEnvironmentVariable(PYTHON_RUNTIME_ENVIRONMENT_VARIABLE_NAME);
            string bundledPythonRuntimeRootPath = string.IsNullOrWhiteSpace(configuredPythonRuntimeRootPath)
                ? Path.Combine(runtimeRootPath, "python-runtime")
                : configuredPythonRuntimeRootPath;

            if (Directory.Exists(bundledPythonRuntimeRootPath))
            {
                copyPath(
                    bundledPythonRuntimeRootPath,
                    Path.Combine(distributionRootPath, "python-runtime"));
            }
        }

        private static void ensureBridgeDirectory(string bridgeDirectoryPath)
        {
            Directory.CreateDirectory(bridgeDirectoryPath);
            string gitKeepFilePath = Path.Combine(bridgeDirectoryPath, ".gitkeep");

            if (File.Exists(gitKeepFilePath) == false)
            {
                File.WriteAllText(gitKeepFilePath, string.Empty);
            }
        }

        private static void ensureSessionWorkspaceDirectory(string sessionWorkspaceDirectoryPath)
        {
            Directory.CreateDirectory(sessionWorkspaceDirectoryPath);
            string gitKeepFilePath = Path.Combine(sessionWorkspaceDirectoryPath, ".gitkeep");

            if (File.Exists(gitKeepFilePath) == false)
            {
                File.WriteAllText(gitKeepFilePath, string.Empty);
            }
        }

        private static void copyPath(string sourcePath, string destinationPath)
        {
            if (File.Exists(sourcePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? destinationPath);
                FileUtil.CopyFileOrDirectory(sourcePath, destinationPath);
                return;
            }

            if (Directory.Exists(sourcePath) == false)
            {
                return;
            }

            if (Directory.Exists(destinationPath))
            {
                FileUtil.DeleteFileOrDirectory(destinationPath);
            }

            FileUtil.CopyFileOrDirectory(sourcePath, destinationPath);
        }

        private static void recreateDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                FileUtil.DeleteFileOrDirectory(directoryPath);
            }

            Directory.CreateDirectory(directoryPath);
        }

        private static void writeLauncherScript(string distributionRootPath)
        {
            string launcherScriptPath = Path.Combine(distributionRootPath, "Run Mouth of Truth.command");
            string launcherScriptContents =
                "#!/usr/bin/env zsh\n"
                + "set -euo pipefail\n"
                + "SCRIPT_DIRECTORY_PATH=\"$(cd \"$(dirname \"$0\")\" && pwd)\"\n"
                + "export MOUTH_OF_TRUTH_RUNTIME_ROOT=\"${SCRIPT_DIRECTORY_PATH}\"\n"
                + "open \"${SCRIPT_DIRECTORY_PATH}/MouthOfTruth.app\"\n";

            File.WriteAllText(launcherScriptPath, launcherScriptContents);
            using Process chmodProcess = new Process();
            chmodProcess.StartInfo = new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x \"{launcherScriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            chmodProcess.Start();
            chmodProcess.WaitForExit();
        }
    }
}
