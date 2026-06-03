using System;
using System.Diagnostics;
using System.IO;
using MouthOfTruth.Game.App;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace MouthOfTruth.Editor
{
    public static class BuildWindowsReleaseEditor
    {
        private const string MAIN_SCENE_PATH = "Assets/Scenes/Main.unity";
        private const string DISTRIBUTION_ROOT_RELATIVE_PATH = "dist/windows/MouthOfTruth";
        private const string APPLICATION_NAME = "MouthOfTruth.exe";
        private const string PYTHON_RUNTIME_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_WINDOWS_PYTHON_RUNTIME_ROOT";
        private const string PACKAGE_PYTHON_RUNTIME_SCRIPT_RELATIVE_PATH = "python-engine/scripts/package_python_runtime.ps1";
        private static readonly string[] DISTRIBUTION_FILE_NAMES_TO_REMOVE =
        {
            ".DS_Store",
            ".gitignore",
            ".gitkeep",
        };

        private static readonly string[] DISTRIBUTION_DIRECTORY_NAMES_TO_REMOVE =
        {
            "__pycache__",
        };

        [MenuItem("Mouth Of Truth/Build Windows Release")]
        public static void Run()
        {
            if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64) == false)
            {
                throw new BuildFailedException("Windows Build Support is not installed for the current Unity editor.");
            }

            BuildMainSceneEditor.Run();

            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                GeneratePresentationBackgroundsEditor.Run();
            }

            string runtimeRootPath = MouthOfTruthRuntimePaths.GetRuntimeRootPath();
            ReleaseRuntimeValidator.ValidateProjectRuntimeAssets(runtimeRootPath);

            string distributionRootPath = Path.Combine(runtimeRootPath, DISTRIBUTION_ROOT_RELATIVE_PATH);
            string applicationPath = Path.Combine(distributionRootPath, APPLICATION_NAME);

            recreateDirectory(distributionRootPath);

            BuildReport buildReport = BuildPipeline.BuildPlayer(
                new[]
                {
                    MAIN_SCENE_PATH,
                },
                applicationPath,
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);

            if (buildReport.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Windows release build failed with result {buildReport.summary.result}.");
            }

            stageRuntimeSupport(runtimeRootPath, distributionRootPath);
            ReleaseRuntimeValidator.ValidateDistributionRuntimeAssets(distributionRootPath);
            pruneDistributionArtifacts(distributionRootPath);
            writeLauncherScript(distributionRootPath);
            AssetDatabase.Refresh();
        }

        private static void stageRuntimeSupport(string runtimeRootPath, string distributionRootPath)
        {
            string distributionPythonEngineRootPath = Path.Combine(distributionRootPath, "python-engine");
            Directory.CreateDirectory(distributionPythonEngineRootPath);
            copyPath(Path.Combine(runtimeRootPath, "python-engine", "src"), Path.Combine(distributionPythonEngineRootPath, "src"));
            copyPath(Path.Combine(runtimeRootPath, "python-engine", "scripts"), Path.Combine(distributionPythonEngineRootPath, "scripts"));
            copyPath(Path.Combine(runtimeRootPath, "python-engine", "models"), Path.Combine(distributionPythonEngineRootPath, "models"));
            copyPath(Path.Combine(runtimeRootPath, "python-engine", "requirements.txt"), Path.Combine(distributionPythonEngineRootPath, "requirements.txt"));
            copyPath(Path.Combine(runtimeRootPath, "python-engine", "environment.yml"), Path.Combine(distributionPythonEngineRootPath, "environment.yml"));
            ensureSessionWorkspaceDirectory(Path.Combine(distributionPythonEngineRootPath, "data", "session-workspace"));
            ensureBridgeDirectory(Path.Combine(distributionRootPath, "bridge"));

            string configuredPythonRuntimeRootPath = Environment.GetEnvironmentVariable(PYTHON_RUNTIME_ENVIRONMENT_VARIABLE_NAME);
            string bundledPythonRuntimeRootPath = resolveBundledPythonRuntimeRootPath(runtimeRootPath, configuredPythonRuntimeRootPath);

            if (Directory.Exists(bundledPythonRuntimeRootPath))
            {
                copyPath(bundledPythonRuntimeRootPath, Path.Combine(distributionRootPath, "python-runtime"));
            }
        }

        private static string resolveBundledPythonRuntimeRootPath(string runtimeRootPath, string configuredPythonRuntimeRootPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPythonRuntimeRootPath) == false)
            {
                if (Directory.Exists(configuredPythonRuntimeRootPath) == false)
                {
                    throw new BuildFailedException($"Configured Windows python runtime root does not exist: {configuredPythonRuntimeRootPath}");
                }

                return configuredPythonRuntimeRootPath;
            }

            string bundledPythonRuntimeRootPath = Path.Combine(runtimeRootPath, "python-runtime-windows");

            if (Directory.Exists(bundledPythonRuntimeRootPath))
            {
                return bundledPythonRuntimeRootPath;
            }

            packageBundledPythonRuntime(runtimeRootPath);

            if (Directory.Exists(bundledPythonRuntimeRootPath) == false)
            {
                throw new BuildFailedException("Bundled Windows python runtime could not be prepared for the release build.");
            }

            return bundledPythonRuntimeRootPath;
        }

        private static void packageBundledPythonRuntime(string runtimeRootPath)
        {
            string packageScriptPath = Path.Combine(runtimeRootPath, PACKAGE_PYTHON_RUNTIME_SCRIPT_RELATIVE_PATH);

            if (File.Exists(packageScriptPath) == false)
            {
                throw new BuildFailedException($"Windows python runtime packaging script is missing: {packageScriptPath}");
            }

            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                throw new BuildFailedException("Automatic Windows python runtime packaging must be run from a Windows Unity editor, " + "or you must set MOUTH_OF_TRUTH_WINDOWS_PYTHON_RUNTIME_ROOT to a prepared runtime folder.");
            }

            using (Process packageProcess = new Process())
            {
                packageProcess.StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{packageScriptPath}\"",
                    WorkingDirectory = runtimeRootPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                if (packageProcess.Start() == false)
                {
                    throw new BuildFailedException("Failed to start the Windows python runtime packaging process.");
                }

                string standardOutput = packageProcess.StandardOutput.ReadToEnd();
                string standardError = packageProcess.StandardError.ReadToEnd();
                packageProcess.WaitForExit();

                if (packageProcess.ExitCode != 0)
                {
                    throw new BuildFailedException(
                        "Windows python runtime packaging failed.\n"
                        + $"stdout:\n{standardOutput}\n"
                        + $"stderr:\n{standardError}");
                }
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
                string destinationDirectoryPath = Path.GetDirectoryName(destinationPath);
                Directory.CreateDirectory(string.IsNullOrEmpty(destinationDirectoryPath) ? destinationPath : destinationDirectoryPath);
                FileUtil.CopyFileOrDirectory(sourcePath, destinationPath);
                return;
            }

            if (Directory.Exists(sourcePath) == false)
            {
                throw new BuildFailedException($"Release source path is missing: {sourcePath}");
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

        private static void pruneDistributionArtifacts(string distributionRootPath)
        {
            foreach (string directoryPath in Directory.GetDirectories(distributionRootPath, "*", SearchOption.AllDirectories))
            {
                string directoryName = Path.GetFileName(directoryPath);

                if (shouldRemoveDistributionDirectory(directoryName))
                {
                    FileUtil.DeleteFileOrDirectory(directoryPath);
                }
            }

            foreach (string filePath in Directory.GetFiles(distributionRootPath, "*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(filePath);

                if (shouldRemoveDistributionFile(fileName))
                {
                    FileUtil.DeleteFileOrDirectory(filePath);
                    continue;
                }

                if (fileName.EndsWith(".pyc", StringComparison.OrdinalIgnoreCase))
                {
                    FileUtil.DeleteFileOrDirectory(filePath);
                }
            }
        }

        private static bool shouldRemoveDistributionDirectory(string directoryName)
        {
            foreach (string candidateName in DISTRIBUTION_DIRECTORY_NAMES_TO_REMOVE)
            {
                if (string.Equals(directoryName, candidateName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool shouldRemoveDistributionFile(string fileName)
        {
            foreach (string candidateName in DISTRIBUTION_FILE_NAMES_TO_REMOVE)
            {
                if (string.Equals(fileName, candidateName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void writeLauncherScript(string distributionRootPath)
        {
            string launcherScriptPath = Path.Combine(distributionRootPath, "Run Mouth of Truth.bat");
            string launcherScriptContents = "@echo off\r\n" + "setlocal EnableExtensions\r\n" + "set \"SCRIPT_DIRECTORY_PATH=%~dp0\"\r\n" + "for %%I in (\"%SCRIPT_DIRECTORY_PATH%.\") do set \"MOUTH_OF_TRUTH_RUNTIME_ROOT=%%~fI\"\r\n" + "start \"Mouth of Truth\" \"%MOUTH_OF_TRUTH_RUNTIME_ROOT%\\MouthOfTruth.exe\" -screen-fullscreen 1\r\n";

            File.WriteAllText(launcherScriptPath, launcherScriptContents);
        }
    }
}
