using MouthOfTruth.Game.Analysis;
using MouthOfTruth.Game.Data;

namespace MouthOfTruth.Game.Session
{
    public class GameSessionSnapshot
    {
        public GameSessionSnapshot(EGameFlowState currentState, QuestionRoundSelection currentRoundSelection, EQuestionCardSlot? selectedQuestionCardSlot, QuestionDefinition selectedQuestionDefinition, EVerdictKind? currentVerdictKind, string currentAnswerTranscript, float hoveredCardDwellSeconds, float elapsedAnswerSeconds, float elapsedSilenceSeconds)
        {
            CurrentState = currentState;
            CurrentRoundSelection = currentRoundSelection;
            SelectedQuestionCardSlot = selectedQuestionCardSlot;
            SelectedQuestionDefinition = selectedQuestionDefinition;
            CurrentVerdictKind = currentVerdictKind;
            CurrentAnswerTranscript = currentAnswerTranscript;
            HoveredCardDwellSeconds = hoveredCardDwellSeconds;
            ElapsedAnswerSeconds = elapsedAnswerSeconds;
            ElapsedSilenceSeconds = elapsedSilenceSeconds;
        }

        public EGameFlowState CurrentState { get; }

        public QuestionRoundSelection CurrentRoundSelection { get; }

        public EQuestionCardSlot? SelectedQuestionCardSlot { get; }

        public QuestionDefinition SelectedQuestionDefinition { get; }

        public EVerdictKind? CurrentVerdictKind { get; }

        public string CurrentAnswerTranscript { get; }

        public float HoveredCardDwellSeconds { get; }

        public float ElapsedAnswerSeconds { get; }

        public float ElapsedSilenceSeconds { get; }

        public bool IsAnswerPaused => CurrentState == EGameFlowState.AnswerPaused;
    }
}
