using System;

namespace MouthOfTruth.Game.Session
{
    public class AnswerCollectionPolicy
    {
        public AnswerCollectionPolicy(float initialSilenceGraceSeconds = 2.6f, float silenceTimeoutSeconds = 1.2f, float maximumAnswerDurationSeconds = 8.0f)
        {
            if (initialSilenceGraceSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(initialSilenceGraceSeconds));
            }

            if (silenceTimeoutSeconds <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(silenceTimeoutSeconds));
            }

            if (maximumAnswerDurationSeconds <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumAnswerDurationSeconds));
            }

            InitialSilenceGraceSeconds = initialSilenceGraceSeconds;
            SilenceTimeoutSeconds = silenceTimeoutSeconds;
            MaximumAnswerDurationSeconds = maximumAnswerDurationSeconds;
        }

        public float InitialSilenceGraceSeconds { get; }

        public float SilenceTimeoutSeconds { get; }

        public float MaximumAnswerDurationSeconds { get; }

        public AnswerCollectionTickResult Advance(float elapsedAnswerSeconds, float elapsedSilenceSeconds, float deltaTimeSeconds, bool isSpeechDetected)
        {
            if (deltaTimeSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));
            }

            float nextElapsedAnswerSeconds = elapsedAnswerSeconds + deltaTimeSeconds;
            float nextElapsedSilenceSeconds = isSpeechDetected
                ? 0.0f
                : elapsedSilenceSeconds + deltaTimeSeconds;

            bool shouldFinishForSilence = nextElapsedAnswerSeconds >= InitialSilenceGraceSeconds && nextElapsedSilenceSeconds >= SilenceTimeoutSeconds;
            bool shouldFinishForTimeout = nextElapsedAnswerSeconds >= MaximumAnswerDurationSeconds;

            return new AnswerCollectionTickResult(nextElapsedAnswerSeconds, nextElapsedSilenceSeconds, shouldFinishForSilence, shouldFinishForTimeout);
        }
    }

    public readonly struct AnswerCollectionTickResult
    {
        public AnswerCollectionTickResult(float elapsedAnswerSeconds, float elapsedSilenceSeconds, bool shouldFinishForSilence, bool shouldFinishForTimeout)
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
