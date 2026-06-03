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

        public QuestionDeckService(IReadOnlyList<QuestionDefinition> questionDefinitions, int? randomSeedOrNull = null)
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

            mRandom = randomSeedOrNull.HasValue
                ? new Random(randomSeedOrNull.Value)
                : new Random();
            mRemainingQuestionDefinitions = new Queue<QuestionDefinition>();
            refillDeckExcluding(Array.Empty<QuestionDefinition>());
        }

        public QuestionRoundSelection DrawNextRound()
        {
            List<QuestionDefinition> roundQuestionDefinitions = new List<QuestionDefinition>();

            while (roundQuestionDefinitions.Count < DEFAULT_ROUND_SIZE)
            {
                if (mRemainingQuestionDefinitions.Count == 0)
                {
                    refillDeckExcluding(roundQuestionDefinitions);
                }

                roundQuestionDefinitions.Add(mRemainingQuestionDefinitions.Dequeue());
            }

            Dictionary<EQuestionCardSlot, QuestionDefinition> questionsBySlot =
                new Dictionary<EQuestionCardSlot, QuestionDefinition>
                {
                    { EQuestionCardSlot.LeftCard, roundQuestionDefinitions[0] },
                    { EQuestionCardSlot.CenterCard, roundQuestionDefinitions[1] },
                    { EQuestionCardSlot.RightCard, roundQuestionDefinitions[2] },
                };

            return new QuestionRoundSelection(questionsBySlot);
        }

        private void refillDeckExcluding(IReadOnlyList<QuestionDefinition> currentRoundQuestionDefinitions)
        {
            HashSet<string> currentRoundQuestionIDs = new HashSet<string>(
                currentRoundQuestionDefinitions.Select(questionDefinition => questionDefinition.ID),
                StringComparer.Ordinal);

            List<QuestionDefinition> shuffledQuestionDefinitions = mAllEnabledQuestionDefinitions
                .Where(questionDefinition => currentRoundQuestionIDs.Contains(questionDefinition.ID) == false)
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
