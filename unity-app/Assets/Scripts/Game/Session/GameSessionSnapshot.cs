using NewMouthOfTruth.Game.Analysis;
using NewMouthOfTruth.Game.Data;

namespace NewMouthOfTruth.Game.Session
{
    public class GameSessionSnapshot
    {
        public EGameFlowState CurrentState { get; init; }

        public QuestionRoundSelection CurrentRoundSelection { get; init; }

        public EQuestionCardSlot? SelectedQuestionCardSlot { get; init; }

        public QuestionDefinition SelectedQuestionDefinition { get; init; }

        public EVerdictKind? CurrentVerdictKind { get; init; }

        public string CurrentAnswerTranscript { get; init; }

        public float ElapsedAnswerSeconds { get; init; }

        public float ElapsedSilenceSeconds { get; init; }

        public bool IsAnswerPaused => CurrentState == EGameFlowState.AnswerPaused;
    }
}
