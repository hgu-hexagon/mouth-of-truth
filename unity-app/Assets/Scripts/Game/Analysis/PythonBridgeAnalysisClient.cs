using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MouthOfTruth.Game.App;
using UnityEngine;

namespace MouthOfTruth.Game.Analysis
{
    public class PythonBridgeAnalysisClient : IAnswerAnalysisClient, IDisposable
    {
        private const int DEFAULT_TIMEOUT_MILLISECONDS = 15000;
        private const int WORKER_STARTUP_TIMEOUT_MILLISECONDS = 30000;
        private const int WORKER_SHUTDOWN_TIMEOUT_MILLISECONDS = 1000;

        private readonly object mWorkerReadyLock = new object();
        private readonly SemaphoreSlim mAnalysisSemaphore = new SemaphoreSlim(1, 1);
        private Process mWorkerProcess;
        private Task mWorkerReadyTask;
        private bool mIsWorkerReady;

        public PythonBridgeAnalysisClient()
        {
            tryStartWorkerProcess();
        }

        public Task WarmUpAsync(CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return ensureWorkerReadyAsync();
        }

        public async Task<AnswerAnalysisResult> AnalyzeAsync(AnswerAnalysisRequest answerAnalysisRequest, CancellationToken cancellationToken)
        {
            if (answerAnalysisRequest == null)
            {
                throw new ArgumentNullException(nameof(answerAnalysisRequest));
            }

            await mAnalysisSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                Directory.CreateDirectory(PythonAnalysisBridgePaths.GetBridgeDirectoryPath());

                string requestID = Guid.NewGuid().ToString("N");
                BridgeAnalysisRequestFileData bridgeAnalysisRequestFileData =
                    new BridgeAnalysisRequestFileData
                    {
                        RequestID = requestID,
                        QuestionID = answerAnalysisRequest.QuestionDefinition.ID,
                        QuestionText = answerAnalysisRequest.QuestionDefinition.Text,
                        AnswerTranscript = answerAnalysisRequest.AnswerTranscript,
                        AnswerAudioFilePath = buildRuntimeRelativePath(answerAnalysisRequest.AnswerAudioFilePath),
                        FaceFramesDirectoryPath = buildRuntimeRelativePath(answerAnalysisRequest.FaceFramesDirectoryPath),
                        FaceFrameCount = answerAnalysisRequest.FaceFrameCount,
                        VoiceSegmentCount = answerAnalysisRequest.VoiceSegmentCount,
                        RequestedAtUtc = DateTime.UtcNow.ToString("O"),
                    };

                string requestJson = UnityEngine.JsonUtility.ToJson(bridgeAnalysisRequestFileData, true);
                File.WriteAllText(PythonAnalysisBridgePaths.GetRequestFilePath(), requestJson);
                deletePreviousResultIfPresent();

                await runPythonAnalysisAsync(cancellationToken).ConfigureAwait(false);

                if (File.Exists(PythonAnalysisBridgePaths.GetResultFilePath()) == false)
                {
                    throw new FileNotFoundException("Python analysis finished without producing a result file.", PythonAnalysisBridgePaths.GetResultFilePath());
                }

                string resultJson = File.ReadAllText(PythonAnalysisBridgePaths.GetResultFilePath());
                BridgeAnalysisResultFileData bridgeAnalysisResultFileData = UnityEngine.JsonUtility.FromJson<BridgeAnalysisResultFileData>(resultJson);

                if (bridgeAnalysisResultFileData == null || bridgeAnalysisResultFileData.RequestID != requestID)
                {
                    throw new InvalidDataException("Python analysis returned an unexpected request identifier.");
                }

                string[] reasonCodes = bridgeAnalysisResultFileData.ReasonCodes;
                if (reasonCodes == null)
                {
                    reasonCodes = Array.Empty<string>();
                }

                return new AnswerAnalysisResult(parseVerdictKind(bridgeAnalysisResultFileData.Verdict), bridgeAnalysisResultFileData.AnswerTranscript, reasonCodes);
            }
            finally
            {
                mAnalysisSemaphore.Release();
            }
        }

        public void Dispose()
        {
            stopWorkerProcess();
            mAnalysisSemaphore.Dispose();
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

        private async Task runPythonAnalysisAsync(CancellationToken cancellationToken)
        {
            if (isWorkerAvailable())
            {
                try
                {
                    await runPythonWorkerAnalysisAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogWarning("Persistent Python analysis worker failed. Falling back to one-shot analysis.\n" + exception);
                    stopWorkerProcess();
                    deletePreviousResultIfPresent();
                }
            }

            await runPythonBridgeProcessAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task runPythonWorkerAnalysisAsync(CancellationToken cancellationToken)
        {
            await ensureWorkerReadyAsync().ConfigureAwait(false);

            if (mIsWorkerReady == false)
            {
                throw new InvalidOperationException("Python analysis worker is not ready.");
            }

            BridgeWorkerCommandFileData bridgeWorkerCommandFileData = new BridgeWorkerCommandFileData
            {
                Command = "analyze",
                RequestFilePath = PythonAnalysisBridgePaths.GetRequestFilePath(),
                ResultFilePath = PythonAnalysisBridgePaths.GetResultFilePath(),
            };
            string workerCommandJson = UnityEngine.JsonUtility.ToJson(bridgeWorkerCommandFileData, false);
            await mWorkerProcess.StandardInput.WriteLineAsync(workerCommandJson).ConfigureAwait(false);
            await mWorkerProcess.StandardInput.FlushAsync().ConfigureAwait(false);

            BridgeWorkerResponseFileData response = await readWorkerResponseAsync(DEFAULT_TIMEOUT_MILLISECONDS, cancellationToken).ConfigureAwait(false);

            if (string.Equals(response.Status, "done", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new InvalidOperationException("Python analysis worker failed.\n" + response.ErrorMessage);
        }

        private Task ensureWorkerReadyAsync()
        {
            if (mIsWorkerReady || isWorkerAvailable() == false)
            {
                return Task.CompletedTask;
            }

            lock (mWorkerReadyLock)
            {
                if (mWorkerReadyTask == null)
                {
                    mWorkerReadyTask = readWorkerReadyAsync();
                }

                return mWorkerReadyTask;
            }
        }

        private async Task readWorkerReadyAsync()
        {
            BridgeWorkerResponseFileData readyResponse = await readWorkerResponseAsync(WORKER_STARTUP_TIMEOUT_MILLISECONDS, CancellationToken.None).ConfigureAwait(false);

            if (string.Equals(readyResponse.Status, "ready", StringComparison.OrdinalIgnoreCase) == false)
            {
                throw new InvalidOperationException("Python analysis worker returned an unexpected startup status: " + readyResponse.Status);
            }

            mIsWorkerReady = true;
        }

        private async Task<BridgeWorkerResponseFileData> readWorkerResponseAsync(int timeoutMilliseconds, CancellationToken cancellationToken)
        {
            if (isWorkerAvailable() == false)
            {
                throw new InvalidOperationException("Python analysis worker is not running.");
            }

            Task<string> readLineTask = mWorkerProcess.StandardOutput.ReadLineAsync();
            Task timeoutTask = Task.Delay(timeoutMilliseconds, cancellationToken);
            Task completedTask = await Task.WhenAny(readLineTask, timeoutTask).ConfigureAwait(false);

            if (completedTask != readLineTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException("Timed out while waiting for the Python analysis worker.");
            }

            string responseJson = await readLineTask.ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(responseJson))
            {
                throw new EndOfStreamException("Python analysis worker stopped without returning a response.");
            }

            BridgeWorkerResponseFileData response = UnityEngine.JsonUtility.FromJson<BridgeWorkerResponseFileData>(responseJson);

            if (response == null || string.IsNullOrWhiteSpace(response.Status))
            {
                throw new InvalidDataException("Python analysis worker returned malformed JSON: " + responseJson);
            }

            return response;
        }

        private void tryStartWorkerProcess()
        {
            try
            {
                string bridgeWorkerLauncherScriptPath = PythonAnalysisBridgePaths.GetBridgeWorkerLauncherScriptPath();

                if (File.Exists(bridgeWorkerLauncherScriptPath) == false)
                {
                    return;
                }

                mWorkerProcess = buildPythonProcess(bridgeWorkerLauncherScriptPath, string.Empty);

                if (mWorkerProcess.Start() == false)
                {
                    mWorkerProcess = null;
                    return;
                }

                mWorkerProcess.ErrorDataReceived += (_, eventArguments) =>
                {
                    if (string.IsNullOrWhiteSpace(eventArguments.Data) == false)
                    {
                        UnityEngine.Debug.Log(eventArguments.Data);
                    }
                };
                mWorkerProcess.BeginErrorReadLine();
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning("Could not start the persistent Python analysis worker. One-shot analysis will be used.\n" + exception);
                stopWorkerProcess();
            }
        }

        private bool isWorkerAvailable()
        {
            return mWorkerProcess != null && mWorkerProcess.HasExited == false;
        }

        private void stopWorkerProcess()
        {
            if (mWorkerProcess == null)
            {
                return;
            }

            try
            {
                if (mWorkerProcess.HasExited == false)
                {
                    BridgeWorkerCommandFileData shutdownCommandFileData = new BridgeWorkerCommandFileData
                    {
                        Command = "shutdown",
                    };
                    string shutdownCommandJson = UnityEngine.JsonUtility.ToJson(shutdownCommandFileData, false);
                    mWorkerProcess.StandardInput.WriteLine(shutdownCommandJson);
                    mWorkerProcess.StandardInput.Flush();

                    if (mWorkerProcess.WaitForExit(WORKER_SHUTDOWN_TIMEOUT_MILLISECONDS) == false)
                    {
                        mWorkerProcess.Kill();
                    }
                }
            }
            catch (Exception)
            {
                try
                {
                    if (mWorkerProcess.HasExited == false)
                    {
                        mWorkerProcess.Kill();
                    }
                }
                catch (Exception)
                {
                }
            }
            finally
            {
                mWorkerProcess.Dispose();
                mWorkerProcess = null;
                mWorkerReadyTask = null;
                mIsWorkerReady = false;
            }
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

            using (Process process = buildPythonProcess(bridgeLauncherScriptPath, buildBridgeLauncherArguments(requestFilePath, resultFilePath)))
            {
                if (process.Start() == false)
                {
                    throw new InvalidOperationException("Failed to start the Python analysis process.");
                }

                Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

                bool exitedWithinTimeout = await Task.Run(() => process.WaitForExit(DEFAULT_TIMEOUT_MILLISECONDS), cancellationToken).ConfigureAwait(false);

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
                    throw new InvalidOperationException("Python analysis failed.\n" + $"stdout:\n{standardOutput}\n" + $"stderr:\n{standardError}");
                }
            }
        }

        private Process buildPythonProcess(string launcherScriptPath, string launcherArguments)
        {
            string pythonInterpreterPath = PythonAnalysisBridgePaths.GetPythonInterpreterPath();
            bool useWindowsCommandShell = Application.platform == RuntimePlatform.WindowsEditor
                || Application.platform == RuntimePlatform.WindowsPlayer;
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = useWindowsCommandShell ? "cmd.exe" : launcherScriptPath,
                    Arguments = buildCommandShellArguments(useWindowsCommandShell, launcherScriptPath, launcherArguments),
                    WorkingDirectory = PythonAnalysisBridgePaths.GetProjectRootPath(),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };
            process.StartInfo.Environment["PYTHONPATH"] = PythonAnalysisBridgePaths.GetPythonModuleRootPath();

            if (string.IsNullOrWhiteSpace(pythonInterpreterPath) == false)
            {
                process.StartInfo.Environment["MOUTH_OF_TRUTH_PYTHON"] = pythonInterpreterPath;
            }

            return process;
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

            if (normalizedPath.StartsWith(runtimeRootWithSeparator, StringComparison.OrdinalIgnoreCase) || string.Equals(normalizedPath, runtimeRootPath, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetRelativePath(runtimeRootPath, normalizedPath)
                    .Replace(Path.DirectorySeparatorChar, '/');
            }

            return normalizedPath;
        }

        private string buildBridgeLauncherArguments(string requestFilePath, string resultFilePath)
        {
            return $"\"{requestFilePath}\" \"{resultFilePath}\"";
        }

        private string buildCommandShellArguments(bool useWindowsCommandShell, string launcherScriptPath, string launcherArguments)
        {
            if (useWindowsCommandShell)
            {
                return string.IsNullOrWhiteSpace(launcherArguments)
                    ? $"/c \"\"{launcherScriptPath}\"\""
                    : $"/c \"\"{launcherScriptPath}\" {launcherArguments}\"";
            }

            return launcherArguments;
        }

        [Serializable]
        private sealed class BridgeWorkerCommandFileData
        {
            // JsonUtility maps fields by the Python worker protocol keys.
            public string Command = string.Empty;
            public string RequestFilePath = string.Empty;
            public string ResultFilePath = string.Empty;
        }

        [Serializable]
        private sealed class BridgeWorkerResponseFileData
        {
            // JsonUtility maps fields by the Python worker protocol keys.
            public string Status = string.Empty;
            public string ErrorMessage = string.Empty;
        }
    }
}
