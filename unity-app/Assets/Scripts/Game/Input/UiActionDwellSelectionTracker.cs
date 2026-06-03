using System;

namespace MouthOfTruth.Game.Input
{
    public class UiActionDwellSelectionTracker
    {
        private readonly float mRequiredDwellSeconds;

        private EUiActionTarget? mHoveredUiActionTargetOrNull;
        private float mHoveredDurationSeconds;

        public UiActionDwellSelectionTracker(float requiredDwellSeconds = 0.7f)
        {
            if (requiredDwellSeconds <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredDwellSeconds));
            }

            mRequiredDwellSeconds = requiredDwellSeconds;
        }

        public float HoveredDurationSeconds => mHoveredDurationSeconds;

        public EUiActionTarget? UpdateHoveredTargetOrNull(EUiActionTarget? hoveredUiActionTargetOrNull, float deltaTimeSeconds)
        {
            if (deltaTimeSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));
            }

            if (hoveredUiActionTargetOrNull == null)
            {
                Reset();
                return null;
            }

            if (mHoveredUiActionTargetOrNull != hoveredUiActionTargetOrNull)
            {
                mHoveredUiActionTargetOrNull = hoveredUiActionTargetOrNull;
                mHoveredDurationSeconds = 0.0f;
            }

            mHoveredDurationSeconds += deltaTimeSeconds;

            if (mHoveredDurationSeconds < mRequiredDwellSeconds)
            {
                return null;
            }

            EUiActionTarget confirmedUiActionTarget = hoveredUiActionTargetOrNull.Value;
            Reset();
            return confirmedUiActionTarget;
        }

        public void Reset()
        {
            mHoveredUiActionTargetOrNull = null;
            mHoveredDurationSeconds = 0.0f;
        }
    }
}
