using System.Threading.Tasks;
using MouthOfTruth.Game.Analysis;
using UnityEngine;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public partial class MouthOfTruthGameView
    {
        private async Task loadAudioClipsAsync()
        {
            mTitleAmbienceClip = await RuntimeAudioClipLoader.LoadClipOrNullAsync(MouthOfTruthAssetCatalog.TitleAmbiencePath);
            mButtonConfirmClip = await RuntimeAudioClipLoader.LoadClipOrNullAsync(MouthOfTruthAssetCatalog.ButtonConfirmPath);
            mCardHoverClip = await RuntimeAudioClipLoader.LoadClipOrNullAsync(MouthOfTruthAssetCatalog.CardHoverPath);
            mCardSelectClip = await RuntimeAudioClipLoader.LoadClipOrNullAsync(MouthOfTruthAssetCatalog.CardSelectPath);
            mCardRevealClip = await RuntimeAudioClipLoader.LoadClipOrNullAsync(MouthOfTruthAssetCatalog.CardRevealPath);
            mHandInsertClip = await RuntimeAudioClipLoader.LoadClipOrNullAsync(MouthOfTruthAssetCatalog.HandInsertPath);
            mHandPromptClip = await RuntimeAudioClipLoader.LoadClipOrNullAsync(MouthOfTruthAssetCatalog.HandPromptPath);
            mResultTrueClip = await RuntimeAudioClipLoader.LoadClipOrNullAsync(MouthOfTruthAssetCatalog.ResultTruePath);
            mResultFalseClip = await RuntimeAudioClipLoader.LoadClipOrNullAsync(MouthOfTruthAssetCatalog.ResultFalsePath);
            mResultUncertainClip = await RuntimeAudioClipLoader.LoadClipOrNullAsync(MouthOfTruthAssetCatalog.ResultUncertainPath);
        }

        private void buildAudioSources()
        {
            mAmbienceAudioSource = gameObject.AddComponent<AudioSource>();
            mAmbienceAudioSource.loop = true;
            mAmbienceAudioSource.playOnAwake = false;
            mAmbienceAudioSource.volume = AMBIENCE_AUDIO_VOLUME;
            mAmbienceAudioSource.spatialBlend = 0.0f;
            mAmbienceAudioSource.priority = 0;
            mAmbienceAudioSource.dopplerLevel = 0.0f;
            mAmbienceAudioSource.ignoreListenerPause = true;
            mAmbienceAudioSource.ignoreListenerVolume = true;

            mInterfaceAudioSource = gameObject.AddComponent<AudioSource>();
            mInterfaceAudioSource.loop = false;
            mInterfaceAudioSource.playOnAwake = false;
            mInterfaceAudioSource.volume = INTERFACE_AUDIO_VOLUME;
            mInterfaceAudioSource.spatialBlend = 0.0f;
            mInterfaceAudioSource.priority = 16;
            mInterfaceAudioSource.dopplerLevel = 0.0f;
            mInterfaceAudioSource.ignoreListenerPause = true;
            mInterfaceAudioSource.ignoreListenerVolume = true;
        }

        private void ensureAmbiencePlayback()
        {
            if (mAmbienceAudioSource == null || mTitleAmbienceClip == null || mAmbienceAudioSource.isPlaying)
            {
                return;
            }

            mAmbienceAudioSource.clip = mTitleAmbienceClip;
            mAmbienceAudioSource.Play();
        }

        private void stabilizeAudioSourceLevels()
        {
            if (mAmbienceAudioSource != null)
            {
                mAmbienceAudioSource.volume = AMBIENCE_AUDIO_VOLUME;
            }

            if (mInterfaceAudioSource != null)
            {
                mInterfaceAudioSource.volume = INTERFACE_AUDIO_VOLUME;
            }
        }

        private void playInterfaceCue(AudioClip audioClip, float volumeScale)
        {
            if (mInterfaceAudioSource == null || audioClip == null)
            {
                return;
            }

            float safeVolumeScale = Mathf.Clamp(volumeScale, 0.0f, INTERFACE_AUDIO_MAX_VOLUME_SCALE);
            if (mInterfaceAudioSource.isPlaying)
            {
                safeVolumeScale *= INTERFACE_AUDIO_OVERLAP_DUCK_SCALE;
            }

            mInterfaceAudioSource.PlayOneShot(audioClip, safeVolumeScale);
        }

        private void playInterfaceCueClean(AudioClip audioClip, float volumeScale)
        {
            if (mInterfaceAudioSource == null || audioClip == null)
            {
                return;
            }

            mInterfaceAudioSource.Stop();
            float safeVolumeScale = Mathf.Clamp(volumeScale, 0.0f, INTERFACE_AUDIO_MAX_VOLUME_SCALE);
            mInterfaceAudioSource.PlayOneShot(audioClip, safeVolumeScale);
        }

        private void playVerdictCue(EVerdictKind verdictKind)
        {
            AudioClip verdictClip = mResultUncertainClip;
            if (verdictKind == EVerdictKind.True)
            {
                verdictClip = mResultTrueClip;
            }
            else if (verdictKind == EVerdictKind.False)
            {
                verdictClip = mResultFalseClip;
            }

            playInterfaceCue(verdictClip, 0.62f);
        }
    }
}
