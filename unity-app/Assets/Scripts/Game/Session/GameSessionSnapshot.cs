using MouthOfTruth.Game.Analysis;
using MouthOfTruth.Game.Data;

namespace MouthOfTruth.Game.Session
{
    public class GameSessionSnapshot
    {
        public GameSessionSnapshot(EGameFlowState currentState, QuestionRoundSelection currentRoundSelection, EQuestionCardSlot? selectedQuestionCardSlotOrNull, QuestionDefinition selectedQuestionDefinition, EVerdictKind? currentVerdictKindOrNull, string currentAnswerTranscript, float hoveredCardDwellSeconds, float elapsedAnswerSeconds, float elapsedSilenceSeconds)
        {
            CurrentState = currentState;
            CurrentRoundSelection = currentRoundSelection;
            SelectedQuestionCardSlotOrNull = selectedQuestionCardSlotOrNull;
            SelectedQuestionDefinition = selectedQuestionDefinition;
            CurrentVerdictKindOrNull = currentVerdictKindOrNull;
            CurrentAnswerTranscript = currentAnswerTranscript;
            HoveredCardDwellSeconds = hoveredCardDwellSeconds;
            ElapsedAnswerSeconds = elapsedAnswerSeconds;
            ElapsedSilenceSeconds = elapsedSilenceSeconds;
        }

        public EGameFlowState CurrentState { get; }

        public QuestionRoundSelection CurrentRoundSelection { get; }

        public EQuestionCardSlot? SelectedQuestionCardSlotOrNull { get; }

        public QuestionDefinition SelectedQuestionDefinition { get; }

        public EVerdictKind? CurrentVerdictKindOrNull { get; }

        public string CurrentAnswerTranscript { get; }

        public float HoveredCardDwellSeconds { get; }

        public float ElapsedAnswerSeconds { get; }

        public float ElapsedSilenceSeconds { get; }

        public bool IsAnswerPaused => CurrentState == EGameFlowState.AnswerPaused;
    }
}
