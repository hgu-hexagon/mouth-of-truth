using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MouthOfTruth.Game.Data;
using MouthOfTruth.Game.Presentation.Runtime;
using UnityEngine;

namespace MouthOfTruth.Game.Narration
{
    public class PrerecordedQuestionNarrationService : IQuestionNarrationService
    {
        private static readonly string[] SUPPORTED_AUDIO_EXTENSIONS =
        {
            ".mp3",
            ".wav",
            ".ogg",
        };

        private readonly AudioSource mAudioSource;
        private readonly IQuestionNarrationService mFallbackNarrationService;
        private readonly string mQuestionAudioDirectoryPath;

        public PrerecordedQuestionNarrationService(
            string questionAudioDirectoryPath,
            IQuestionNarrationService fallbackNarrationService)
        {
            mQuestionAudioDirectoryPath = questionAudioDirectoryPath ?? string.Empty;
            mFallbackNarrationService = fallbackNarrationService ?? new SilentQuestionNarrationService();
            GameObject audioSourceObject = new GameObject("QuestionNarrationAudioSource");
            Object.DontDestroyOnLoad(audioSourceObject);
            mAudioSource = audioSourceObject.AddComponent<AudioSource>();
            mAudioSource.playOnAwake = false;
            mAudioSource.loop = false;
            mAudioSource.spatialBlend = 0.0f;
        }

        public async Task SpeakQuestionAsync(QuestionDefinition questionDefinition, CancellationToken cancellationToken)
        {
            string audioFilePath = getQuestionAudioFilePath(questionDefinition);

            if (string.IsNullOrWhiteSpace(audioFilePath))
            {
                await mFallbackNarrationService.SpeakQuestionAsync(questionDefinition, cancellationToken);
                return;
            }

            AudioClip audioClip = await RuntimeAudioClipLoader.LoadClipAsync(audioFilePath);

            if (audioClip == null)
            {
                await mFallbackNarrationService.SpeakQuestionAsync(questionDefinition, cancellationToken);
                return;
            }

            await playClipAsync(audioClip, cancellationToken);
        }

        private async Task playClipAsync(AudioClip audioClip, CancellationToken cancellationToken)
        {
            mAudioSource.Stop();
            mAudioSource.clip = audioClip;
            mAudioSource.Play();

            try
            {
                while (mAudioSource.isPlaying)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(50, cancellationToken);
                }
            }
            finally
            {
                mAudioSource.Stop();
                mAudioSource.clip = null;
            }
        }

        private string getQuestionAudioFilePath(QuestionDefinition questionDefinition)
        {
            if (questionDefinition == null || string.IsNullOrWhiteSpace(questionDefinition.ID))
            {
                return string.Empty;
            }

            foreach (string audioExtension in SUPPORTED_AUDIO_EXTENSIONS)
            {
                string candidateFilePath = Path.Combine(
                    mQuestionAudioDirectoryPath,
                    $"{questionDefinition.ID}{audioExtension}");

                if (File.Exists(candidateFilePath))
                {
                    return candidateFilePath;
                }
            }

            return string.Empty;
        }
    }
}
