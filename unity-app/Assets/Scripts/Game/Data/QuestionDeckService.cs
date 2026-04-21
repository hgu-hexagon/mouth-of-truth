using System;
using System.Collections.Generic;
using System.Linq;

namespace MouthOfTruth.Game.Data
{
    public class QuestionDeckService
    {
        private const int DEFAULT_ROUND_SIZE = 3;

        private readonly Random mRandom;
        private readonly List<QuestionDefinition> mAllEnabledQuestionDefinitions;
        private readonly Queue<QuestionDefinition> mRemainingQuestionDefinitions;

        public QuestionDeckService(
            IReadOnlyList<QuestionDefinition> questionDefinitions,
            int? randomSeed = null)
        {
            if (questionDefinitions == null)
            {
                throw new ArgumentNullException(nameof(questionDefinitions));
            }

            mAllEnabledQuestionDefinitions = questionDefinitions
                .Where(questionDefinition => questionDefinition.IsEnabled)
                .ToList();

            if (mAllEnabledQuestionDefinitions.Count < DEFAULT_ROUND_SIZE)
            {
                throw new InvalidOperationException("At least three enabled questions are required to start the game.");
            }

            mRandom = randomSeed.HasValue
                ? new Random(randomSeed.Value)
                : new Random();
            mRemainingQuestionDefinitions = new Queue<QuestionDefinition>();
            refillDeckIfNeeded(DEFAULT_ROUND_SIZE);
        }

        public QuestionRoundSelection DrawNextRound()
        {
            refillDeckIfNeeded(DEFAULT_ROUND_SIZE);

            Dictionary<EQuestionCardSlot, QuestionDefinition> questionsBySlot =
                new Dictionary<EQuestionCardSlot, QuestionDefinition>
                {
                    { EQuestionCardSlot.LeftCard, mRemainingQuestionDefinitions.Dequeue() },
                    { EQuestionCardSlot.CenterCard, mRemainingQuestionDefinitions.Dequeue() },
                    { EQuestionCardSlot.RightCard, mRemainingQuestionDefinitions.Dequeue() },
                };

            return new QuestionRoundSelection(questionsBySlot);
        }

        private void refillDeckIfNeeded(int minimumQuestionCount)
        {
            if (mRemainingQuestionDefinitions.Count >= minimumQuestionCount)
            {
                return;
            }

            List<QuestionDefinition> shuffledQuestionDefinitions = mAllEnabledQuestionDefinitions
                .OrderBy(_ => mRandom.Next())
                .ToList();

            mRemainingQuestionDefinitions.Clear();

            foreach (QuestionDefinition questionDefinition in shuffledQuestionDefinitions)
            {
                mRemainingQuestionDefinitions.Enqueue(questionDefinition);
            }
        }
    }
}
