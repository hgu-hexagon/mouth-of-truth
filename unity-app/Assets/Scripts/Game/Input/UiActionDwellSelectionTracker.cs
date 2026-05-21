using System;

namespace MouthOfTruth.Game.Input
{
    public class UiActionDwellSelectionTracker
    {
        private readonly float mRequiredDwellSeconds;

        private EUiActionTarget? mHoveredUiActionTarget;
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

        public EUiActionTarget? UpdateHoveredTarget(EUiActionTarget? hoveredUiActionTarget, float deltaTimeSeconds)
        {
            if (deltaTimeSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));
            }

            if (hoveredUiActionTarget == null)
            {
                Reset();
                return null;
            }

            if (mHoveredUiActionTarget != hoveredUiActionTarget)
            {
                mHoveredUiActionTarget = hoveredUiActionTarget;
                mHoveredDurationSeconds = 0.0f;
            }

            mHoveredDurationSeconds += deltaTimeSeconds;

            if (mHoveredDurationSeconds < mRequiredDwellSeconds)
            {
                return null;
            }

            EUiActionTarget confirmedUiActionTarget = hoveredUiActionTarget.Value;
            Reset();
            return confirmedUiActionTarget;
        }

        public void Reset()
        {
            mHoveredUiActionTarget = null;
            mHoveredDurationSeconds = 0.0f;
        }
    }
}
