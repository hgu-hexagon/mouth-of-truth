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
    public static class BuildMacReleaseEditor
    {
        private const string MAIN_SCENE_PATH = "Assets/Scenes/Main.unity";
        private const string DISTRIBUTION_ROOT_RELATIVE_PATH = "dist/macos/MouthOfTruth";
        private const string DISTRIBUTION_ARCHIVE_RELATIVE_PATH = "dist/macos/MouthOfTruth-macos.zip";
        private const string APPLICATION_NAME = "MouthOfTruth.app";
        private const string PYTHON_RUNTIME_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_PYTHON_RUNTIME_ROOT";
        private const string PACKAGE_PYTHON_RUNTIME_SCRIPT_RELATIVE_PATH = "python-engine/scripts/package_python_runtime.sh";
        private const string BURST_DEBUG_INFORMATION_DIRECTORY_SUFFIX = "_BurstDebugInformation_DoNotShip";
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

        [MenuItem("Mouth Of Truth/Build Mac Release")]
        public static void Run()
        {
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
                BuildTarget.StandaloneOSX,
                BuildOptions.None);

            if (buildReport.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Mac release build failed with result {buildReport.summary.result}.");
            }

            stageRuntimeSupport(runtimeRootPath, distributionRootPath);
            ReleaseRuntimeValidator.ValidateDistributionRuntimeAssets(distributionRootPath);
            pruneDistributionArtifacts(distributionRootPath);
            writeLauncherScript(distributionRootPath);
            writeDistributionArchive(runtimeRootPath, distributionRootPath);
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

            string configuredPythonRuntimeRootPath = System.Environment.GetEnvironmentVariable(PYTHON_RUNTIME_ENVIRONMENT_VARIABLE_NAME);
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
                    throw new BuildFailedException($"Configured python runtime root does not exist: {configuredPythonRuntimeRootPath}");
                }

                return configuredPythonRuntimeRootPath;
            }

            string bundledPythonRuntimeRootPath = Path.Combine(runtimeRootPath, "python-runtime");

            if (Directory.Exists(bundledPythonRuntimeRootPath))
            {
                return bundledPythonRuntimeRootPath;
            }

            packageBundledPythonRuntime(runtimeRootPath);

            if (Directory.Exists(bundledPythonRuntimeRootPath) == false)
            {
                throw new BuildFailedException("Bundled python runtime could not be prepared for the release build.");
            }

            return bundledPythonRuntimeRootPath;
        }

        private static void packageBundledPythonRuntime(string runtimeRootPath)
        {
            string packageScriptPath = Path.Combine(runtimeRootPath, PACKAGE_PYTHON_RUNTIME_SCRIPT_RELATIVE_PATH);

            if (File.Exists(packageScriptPath) == false)
            {
                throw new BuildFailedException($"Python runtime packaging script is missing: {packageScriptPath}");
            }

            using (Process packageProcess = new Process())
            {
                packageProcess.StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/zsh",
                    Arguments = $"\"{packageScriptPath}\"",
                    WorkingDirectory = runtimeRootPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                if (packageProcess.Start() == false)
                {
                    throw new BuildFailedException("Failed to start the python runtime packaging process.");
                }

                string standardOutput = packageProcess.StandardOutput.ReadToEnd();
                string standardError = packageProcess.StandardError.ReadToEnd();
                packageProcess.WaitForExit();

                if (packageProcess.ExitCode != 0)
                {
                    throw new BuildFailedException(
                        "Python runtime packaging failed.\n"
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
            if (directoryName.EndsWith(BURST_DEBUG_INFORMATION_DIRECTORY_SUFFIX, StringComparison.Ordinal))
            {
                return true;
            }

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
            string launcherScriptPath = Path.Combine(distributionRootPath, "Run Mouth of Truth.command");
            string launcherScriptContents =
                "#!/usr/bin/env zsh\n"
                + "set -euo pipefail\n"
                + "SCRIPT_DIRECTORY_PATH=\"$(cd \"$(dirname \"$0\")\" && pwd)\"\n"
                + "export MOUTH_OF_TRUTH_RUNTIME_ROOT=\"${SCRIPT_DIRECTORY_PATH}\"\n"
                + "open \"${SCRIPT_DIRECTORY_PATH}/MouthOfTruth.app\" --args -screen-fullscreen 1\n";

            File.WriteAllText(launcherScriptPath, launcherScriptContents);
            runProcess("/bin/chmod", $"+x \"{launcherScriptPath}\"", distributionRootPath);
        }

        private static void writeDistributionArchive(string runtimeRootPath, string distributionRootPath)
        {
            string archivePath = Path.Combine(runtimeRootPath, DISTRIBUTION_ARCHIVE_RELATIVE_PATH);

            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            string archiveDirectoryPath = Path.GetDirectoryName(archivePath);
            Directory.CreateDirectory(string.IsNullOrEmpty(archiveDirectoryPath) ? runtimeRootPath : archiveDirectoryPath);
            runProcess("/usr/bin/ditto", $"-c -k --norsrc --noextattr --noqtn --noacl --keepParent \"{distributionRootPath}\" \"{archivePath}\"", runtimeRootPath);
        }

        private static void runProcess(string fileName, string arguments, string workingDirectory)
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                if (process.Start() == false)
                {
                    throw new BuildFailedException($"Failed to start process: {fileName}");
                }

                string standardOutput = process.StandardOutput.ReadToEnd();
                string standardError = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new BuildFailedException(
                        $"{fileName} failed with exit code {process.ExitCode}.\n"
                        + $"stdout:\n{standardOutput}\n"
                        + $"stderr:\n{standardError}");
                }
            }
        }
    }
}
