using System;
using System.Collections.Generic;

namespace MouthOfTruth.Game.Data
{
    public class QuestionRoundSelection
    {
        public QuestionRoundSelection(IReadOnlyDictionary<EQuestionCardSlot, QuestionDefinition> questionsBySlot)
        {
            QuestionsBySlot = questionsBySlot;
        }

        public IReadOnlyDictionary<EQuestionCardSlot, QuestionDefinition> QuestionsBySlot { get; }

        public QuestionDefinition GetQuestionBySlot(EQuestionCardSlot questionCardSlot)
        {
            return QuestionsBySlot[questionCardSlot];
        }
    }
}
