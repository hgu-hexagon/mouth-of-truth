using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MouthOfTruth.Game.Narration
{
    public class MacOsQuestionNarrationService : IQuestionNarrationService
    {
        public async Task SpeakQuestionAsync(string questionText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(questionText))
            {
                return;
            }

            if (Application.platform != RuntimePlatform.OSXEditor
                && Application.platform != RuntimePlatform.OSXPlayer)
            {
                await Task.Delay(estimateFallbackDelayMilliseconds(questionText), cancellationToken);
                return;
            }

            using Process speechProcess = new Process();
            speechProcess.StartInfo.FileName = "/usr/bin/say";
            speechProcess.StartInfo.Arguments = $"--voice Yuna \"{escapeArgument(questionText)}\"";
            speechProcess.StartInfo.UseShellExecute = false;
            speechProcess.StartInfo.CreateNoWindow = true;

            speechProcess.Start();

            while (speechProcess.HasExited == false)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100, cancellationToken);
            }
        }

        private int estimateFallbackDelayMilliseconds(string questionText)
        {
            int wordCount = questionText.Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries).Length;
            return Mathf.Max(1200, wordCount * 350);
        }

        private string escapeArgument(string rawText)
        {
            return rawText.Replace("\"", "\\\"");
        }
    }
}
