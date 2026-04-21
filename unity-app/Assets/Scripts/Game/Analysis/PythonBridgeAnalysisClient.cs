using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MouthOfTruth.Game.App;
using UnityEngine;

namespace MouthOfTruth.Game.Analysis
{
    public class PythonBridgeAnalysisClient : IAnswerAnalysisClient
    {
        private const int DEFAULT_TIMEOUT_MILLISECONDS = 8000;

        public async Task<AnswerAnalysisResult> AnalyzeAsync(
            AnswerAnalysisRequest answerAnalysisRequest,
            CancellationToken cancellationToken)
        {
            if (answerAnalysisRequest == null)
            {
                throw new ArgumentNullException(nameof(answerAnalysisRequest));
            }

            Directory.CreateDirectory(PythonAnalysisBridgePaths.GetBridgeDirectoryPath());

            string requestID = Guid.NewGuid().ToString("N");
            BridgeAnalysisRequestFileData bridgeAnalysisRequestFileData =
                new BridgeAnalysisRequestFileData
                {
                    RequestID = requestID,
                    QuestionID = answerAnalysisRequest.QuestionDefinition.ID,
                    QuestionText = answerAnalysisRequest.QuestionDefinition.Text,
                    AnswerTranscript = answerAnalysisRequest.AnswerTranscript,
                    AnswerAudioFilePath =
                        buildRuntimeRelativePath(answerAnalysisRequest.AnswerAudioFilePath),
                    FaceFramesDirectoryPath =
                        buildRuntimeRelativePath(answerAnalysisRequest.FaceFramesDirectoryPath),
                    FaceFrameCount = answerAnalysisRequest.FaceFrameCount,
                    VoiceSegmentCount = answerAnalysisRequest.VoiceSegmentCount,
                    RequestedAtUtc = DateTime.UtcNow.ToString("O"),
                };

            string requestJson = UnityEngine.JsonUtility.ToJson(bridgeAnalysisRequestFileData, true);
            File.WriteAllText(PythonAnalysisBridgePaths.GetRequestFilePath(), requestJson);
            deletePreviousResultIfPresent();

            await runPythonBridgeProcessAsync(cancellationToken).ConfigureAwait(false);

            if (File.Exists(PythonAnalysisBridgePaths.GetResultFilePath()) == false)
            {
                throw new FileNotFoundException(
                    "Python analysis finished without producing a result file.",
                    PythonAnalysisBridgePaths.GetResultFilePath());
            }

            string resultJson = File.ReadAllText(PythonAnalysisBridgePaths.GetResultFilePath());
            BridgeAnalysisResultFileData bridgeAnalysisResultFileData =
                UnityEngine.JsonUtility.FromJson<BridgeAnalysisResultFileData>(resultJson);

            if (bridgeAnalysisResultFileData == null
                || bridgeAnalysisResultFileData.RequestID != requestID)
            {
                throw new InvalidDataException("Python analysis returned an unexpected request identifier.");
            }

            return new AnswerAnalysisResult(
                parseVerdictKind(bridgeAnalysisResultFileData.Verdict),
                bridgeAnalysisResultFileData.AnswerTranscript,
                bridgeAnalysisResultFileData.ReasonCodes ?? Array.Empty<string>());
        }

        private EVerdictKind parseVerdictKind(string verdictText)
        {
            if (string.Equals(verdictText, "TRUE", StringComparison.OrdinalIgnoreCase))
            {
                return EVerdictKind.True;
            }

            if (string.Equals(verdictText, "FALSE", StringComparison.OrdinalIgnoreCase))
            {
                return EVerdictKind.False;
            }

            return EVerdictKind.Uncertain;
        }

        private async Task runPythonBridgeProcessAsync(CancellationToken cancellationToken)
        {
            string pythonInterpreterPath = PythonAnalysisBridgePaths.GetPythonInterpreterPath();
            string bridgeLauncherScriptPath = PythonAnalysisBridgePaths.GetBridgeLauncherScriptPath();
            string requestFilePath = PythonAnalysisBridgePaths.GetRequestFilePath();
            string resultFilePath = PythonAnalysisBridgePaths.GetResultFilePath();

            if (string.IsNullOrWhiteSpace(pythonInterpreterPath) == false && File.Exists(pythonInterpreterPath) == false)
            {
                throw new FileNotFoundException("The configured Python interpreter was not found.", pythonInterpreterPath);
            }

            if (File.Exists(bridgeLauncherScriptPath) == false)
            {
                throw new FileNotFoundException("The Python bridge launcher script was not found.", bridgeLauncherScriptPath);
            }

            using Process process = new Process();
            bool useWindowsCommandShell = Application.platform == RuntimePlatform.WindowsEditor
                || Application.platform == RuntimePlatform.WindowsPlayer;
            process.StartInfo = new ProcessStartInfo
            {
                FileName = useWindowsCommandShell ? "cmd.exe" : bridgeLauncherScriptPath,
                Arguments = buildBridgeLauncherArguments(
                    useWindowsCommandShell,
                    bridgeLauncherScriptPath,
                    requestFilePath,
                    resultFilePath),
                WorkingDirectory = PythonAnalysisBridgePaths.GetProjectRootPath(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            process.StartInfo.Environment["PYTHONPATH"] = PythonAnalysisBridgePaths.GetPythonModuleRootPath();

            if (string.IsNullOrWhiteSpace(pythonInterpreterPath) == false)
            {
                process.StartInfo.Environment["MOUTH_OF_TRUTH_PYTHON"] = pythonInterpreterPath;
            }

            if (process.Start() == false)
            {
                throw new InvalidOperationException("Failed to start the Python analysis process.");
            }

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

            bool exitedWithinTimeout = await Task.Run(
                () => process.WaitForExit(DEFAULT_TIMEOUT_MILLISECONDS),
                cancellationToken).ConfigureAwait(false);

            if (exitedWithinTimeout == false)
            {
                try
                {
                    process.Kill();
                }
                catch (InvalidOperationException)
                {
                }

                throw new TimeoutException("Timed out while waiting for the Python analysis process.");
            }

            string standardOutput = await standardOutputTask.ConfigureAwait(false);
            string standardError = await standardErrorTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    "Python analysis failed.\n"
                    + $"stdout:\n{standardOutput}\n"
                    + $"stderr:\n{standardError}");
            }
        }

        private void deletePreviousResultIfPresent()
        {
            string resultFilePath = PythonAnalysisBridgePaths.GetResultFilePath();

            if (File.Exists(resultFilePath))
            {
                File.Delete(resultFilePath);
            }
        }

        private string buildRuntimeRelativePath(string originalPath)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
            {
                return string.Empty;
            }

            string normalizedPath = Path.GetFullPath(originalPath);
            string runtimeRootPath = Path.GetFullPath(MouthOfTruthRuntimePaths.GetRuntimeRootPath());
            string runtimeRootWithSeparator = runtimeRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (normalizedPath.StartsWith(
                    runtimeRootWithSeparator,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    normalizedPath,
                    runtimeRootPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetRelativePath(runtimeRootPath, normalizedPath)
                    .Replace(Path.DirectorySeparatorChar, '/');
            }

            return normalizedPath;
        }

        private string buildBridgeLauncherArguments(
            bool useWindowsCommandShell,
            string bridgeLauncherScriptPath,
            string requestFilePath,
            string resultFilePath)
        {
            if (useWindowsCommandShell)
            {
                return $"/c \"\"{bridgeLauncherScriptPath}\" "
                    + $"\"{requestFilePath}\" \"{resultFilePath}\"\"";
            }

            return $"\"{requestFilePath}\" \"{resultFilePath}\"";
        }
    }
}
