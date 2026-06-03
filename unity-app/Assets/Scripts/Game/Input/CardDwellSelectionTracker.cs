using System;
using MouthOfTruth.Game.Data;

namespace MouthOfTruth.Game.Input
{
    public class CardDwellSelectionTracker
    {
        private readonly float mRequiredDwellSeconds;

        private EQuestionCardSlot? mHoveredQuestionCardSlotOrNull;
        private float mHoveredDurationSeconds;

        public CardDwellSelectionTracker(float requiredDwellSeconds = 0.7f)
        {
            if (requiredDwellSeconds <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredDwellSeconds));
            }

            mRequiredDwellSeconds = requiredDwellSeconds;
        }

        public float HoveredDurationSeconds => mHoveredDurationSeconds;

        public EQuestionCardSlot? UpdateHoveredCardOrNull(EQuestionCardSlot? hoveredQuestionCardSlotOrNull, float deltaTimeSeconds)
        {
            if (deltaTimeSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));
            }

            if (hoveredQuestionCardSlotOrNull == null)
            {
                Reset();
                return null;
            }

            if (mHoveredQuestionCardSlotOrNull != hoveredQuestionCardSlotOrNull)
            {
                mHoveredQuestionCardSlotOrNull = hoveredQuestionCardSlotOrNull;
                mHoveredDurationSeconds = 0.0f;
            }

            mHoveredDurationSeconds += deltaTimeSeconds;

            if (mHoveredDurationSeconds < mRequiredDwellSeconds)
            {
                return null;
            }

            EQuestionCardSlot confirmedQuestionCardSlot = hoveredQuestionCardSlotOrNull.Value;
            Reset();
            return confirmedQuestionCardSlot;
        }

        public void Reset()
        {
            mHoveredQuestionCardSlotOrNull = null;
            mHoveredDurationSeconds = 0.0f;
        }
    }
}
