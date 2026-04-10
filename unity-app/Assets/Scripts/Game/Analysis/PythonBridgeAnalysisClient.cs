using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MouthOfTruth.Game.Analysis
{
    public class PythonBridgeAnalysisClient : IAnswerAnalysisClient
    {
        private const int DEFAULT_TIMEOUT_MILLISECONDS = 5000;
        private const int DEFAULT_POLL_INTERVAL_MILLISECONDS = 100;

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
                    FaceRecognitionCount = answerAnalysisRequest.FaceRecognitionCount,
                    VoiceSegmentCount = answerAnalysisRequest.VoiceSegmentCount,
                    RequestedAtUtc = DateTime.UtcNow.ToString("O"),
                };

            string requestJson = UnityEngine.JsonUtility.ToJson(bridgeAnalysisRequestFileData, true);
            File.WriteAllText(PythonAnalysisBridgePaths.GetRequestFilePath(), requestJson);

            using CancellationTokenSource timeoutCancellationTokenSource =
                new CancellationTokenSource(DEFAULT_TIMEOUT_MILLISECONDS);
            using CancellationTokenSource linkedCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutCancellationTokenSource.Token);

            while (linkedCancellationTokenSource.IsCancellationRequested == false)
            {
                if (File.Exists(PythonAnalysisBridgePaths.GetResultFilePath()))
                {
                    string resultJson = File.ReadAllText(PythonAnalysisBridgePaths.GetResultFilePath());
                    BridgeAnalysisResultFileData bridgeAnalysisResultFileData =
                        UnityEngine.JsonUtility.FromJson<BridgeAnalysisResultFileData>(resultJson);

                    if (bridgeAnalysisResultFileData != null
                        && bridgeAnalysisResultFileData.RequestID == requestID)
                    {
                        return new AnswerAnalysisResult(
                            parseVerdictKind(bridgeAnalysisResultFileData.Verdict),
                            bridgeAnalysisResultFileData.ReasonCodes ?? Array.Empty<string>());
                    }
                }

                await Task.Delay(
                    DEFAULT_POLL_INTERVAL_MILLISECONDS,
                    linkedCancellationTokenSource.Token);
            }

            throw new TimeoutException("Timed out while waiting for a Python analysis result.");
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
    }
}
