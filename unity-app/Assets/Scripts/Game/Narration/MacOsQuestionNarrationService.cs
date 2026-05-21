using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MouthOfTruth.Game.Data;
using UnityEngine;

namespace MouthOfTruth.Game.Narration
{
    public class MacOsQuestionNarrationService : IQuestionNarrationService
    {
        private const int DEFAULT_SPEECH_RATE = 145;
        private const string DEFAULT_VOICE_NAME = "Grandpa (한국어(한국))";
        private const string VOICE_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_TTS_VOICE";

        public async Task SpeakQuestionAsync(QuestionDefinition questionDefinition, CancellationToken cancellationToken)
        {
            string questionText = questionDefinition == null ? string.Empty : questionDefinition.Text;

            if (string.IsNullOrWhiteSpace(questionText))
            {
                return;
            }

            if (Application.platform != RuntimePlatform.OSXEditor && Application.platform != RuntimePlatform.OSXPlayer)
            {
                await Task.Delay(estimateFallbackDelayMilliseconds(questionText), cancellationToken);
                return;
            }

            using (Process speechProcess = new Process())
            {
                speechProcess.StartInfo.FileName = "/usr/bin/say";
                speechProcess.StartInfo.Arguments = $"--voice \"{escapeArgument(getVoiceName())}\" --rate {DEFAULT_SPEECH_RATE} " + $"\"{escapeArgument(questionText)}\"";
                speechProcess.StartInfo.UseShellExecute = false;
                speechProcess.StartInfo.CreateNoWindow = true;

                speechProcess.Start();

                while (speechProcess.HasExited == false)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(100, cancellationToken);
                }
            }
        }

        private string getVoiceName()
        {
            string configuredVoiceName = Environment.GetEnvironmentVariable(VOICE_ENVIRONMENT_VARIABLE_NAME);
            return string.IsNullOrWhiteSpace(configuredVoiceName)
                ? DEFAULT_VOICE_NAME
                : configuredVoiceName.Trim();
        }

        private int estimateFallbackDelayMilliseconds(string questionText)
        {
            int wordCount = questionText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            return Mathf.Max(1200, wordCount * 350);
        }

        private string escapeArgument(string rawText)
        {
            return rawText.Replace("\"", "\\\"");
        }
    }
}
