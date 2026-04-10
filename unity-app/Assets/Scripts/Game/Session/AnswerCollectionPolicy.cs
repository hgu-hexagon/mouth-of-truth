using System;

namespace NewMouthOfTruth.Game.Session
{
    public class AnswerCollectionPolicy
    {
        public AnswerCollectionPolicy(
            float silenceTimeoutSeconds = 3.0f,
            float maximumAnswerDurationSeconds = 15.0f)
        {
            if (silenceTimeoutSeconds <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(silenceTimeoutSeconds));
            }

            if (maximumAnswerDurationSeconds <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumAnswerDurationSeconds));
            }

            SilenceTimeoutSeconds = silenceTimeoutSeconds;
            MaximumAnswerDurationSeconds = maximumAnswerDurationSeconds;
        }

        public float SilenceTimeoutSeconds { get; }

        public float MaximumAnswerDurationSeconds { get; }

        public AnswerCollectionTickResult Advance(
            float elapsedAnswerSeconds,
            float elapsedSilenceSeconds,
            float deltaTimeSeconds,
            bool isSpeechDetected)
        {
            if (deltaTimeSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));
            }

            float nextElapsedAnswerSeconds = elapsedAnswerSeconds + deltaTimeSeconds;
            float nextElapsedSilenceSeconds = isSpeechDetected
                ? 0.0f
                : elapsedSilenceSeconds + deltaTimeSeconds;

            bool shouldFinishForSilence = nextElapsedSilenceSeconds >= SilenceTimeoutSeconds;
            bool shouldFinishForTimeout = nextElapsedAnswerSeconds >= MaximumAnswerDurationSeconds;

            return new AnswerCollectionTickResult(
                nextElapsedAnswerSeconds,
                nextElapsedSilenceSeconds,
                shouldFinishForSilence,
                shouldFinishForTimeout);
        }
    }

    public readonly struct AnswerCollectionTickResult
    {
        public AnswerCollectionTickResult(
            float elapsedAnswerSeconds,
            float elapsedSilenceSeconds,
            bool shouldFinishForSilence,
            bool shouldFinishForTimeout)
        {
            ElapsedAnswerSeconds = elapsedAnswerSeconds;
            ElapsedSilenceSeconds = elapsedSilenceSeconds;
            ShouldFinishForSilence = shouldFinishForSilence;
            ShouldFinishForTimeout = shouldFinishForTimeout;
        }

        public float ElapsedAnswerSeconds { get; }

        public float ElapsedSilenceSeconds { get; }

        public bool ShouldFinishForSilence { get; }

        public bool ShouldFinishForTimeout { get; }
    }
}
