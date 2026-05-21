using System;
using MouthOfTruth.Game.Data;

namespace MouthOfTruth.Game.Input
{
    public class CardDwellSelectionTracker
    {
        private readonly float mRequiredDwellSeconds;

        private EQuestionCardSlot? mHoveredQuestionCardSlot;
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

        public EQuestionCardSlot? UpdateHoveredCard(EQuestionCardSlot? hoveredQuestionCardSlot, float deltaTimeSeconds)
        {
            if (deltaTimeSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));
            }

            if (hoveredQuestionCardSlot == null)
            {
                Reset();
                return null;
            }

            if (mHoveredQuestionCardSlot != hoveredQuestionCardSlot)
            {
                mHoveredQuestionCardSlot = hoveredQuestionCardSlot;
                mHoveredDurationSeconds = 0.0f;
            }

            mHoveredDurationSeconds += deltaTimeSeconds;

            if (mHoveredDurationSeconds < mRequiredDwellSeconds)
            {
                return null;
            }

            EQuestionCardSlot confirmedQuestionCardSlot = hoveredQuestionCardSlot.Value;
            Reset();
            return confirmedQuestionCardSlot;
        }

        public void Reset()
        {
            mHoveredQuestionCardSlot = null;
            mHoveredDurationSeconds = 0.0f;
        }
    }
}
