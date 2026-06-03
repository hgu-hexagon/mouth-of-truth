using System;
using MouthOfTruth.Game.Analysis;
using MouthOfTruth.Game.Data;
using MouthOfTruth.Game.Input;

namespace MouthOfTruth.Game.Session
{
    public class MouthOfTruthGameStateMachine
    {
        private readonly QuestionDeckService mQuestionDeckService;
        private readonly CardDwellSelectionTracker mCardDwellSelectionTracker;
        private readonly AnswerCollectionPolicy mAnswerCollectionPolicy;

        private QuestionRoundSelection mCurrentRoundSelection;
        private EQuestionCardSlot? mSelectedQuestionCardSlotOrNull;
        private QuestionDefinition mSelectedQuestionDefinition;
        private EVerdictKind? mCurrentVerdictKindOrNull;
        private string mCurrentAnswerTranscript = string.Empty;
        private float mElapsedAnswerSeconds;
        private float mElapsedSilenceSeconds;

        public MouthOfTruthGameStateMachine(QuestionDeckService questionDeckService, CardDwellSelectionTracker cardDwellSelectionTracker, AnswerCollectionPolicy answerCollectionPolicy)
        {
            if (questionDeckService == null)
            {
                throw new ArgumentNullException(nameof(questionDeckService));
            }

            if (cardDwellSelectionTracker == null)
            {
                throw new ArgumentNullException(nameof(cardDwellSelectionTracker));
            }

            if (answerCollectionPolicy == null)
            {
                throw new ArgumentNullException(nameof(answerCollectionPolicy));
            }

            mQuestionDeckService = questionDeckService;
            mCardDwellSelectionTracker = cardDwellSelectionTracker;
            mAnswerCollectionPolicy = answerCollectionPolicy;
            CurrentState = EGameFlowState.StartScreen;
        }

        public EGameFlowState CurrentState { get; private set; }

        public void OpenStartScreen()
        {
            resetTransientRoundState();
            CurrentState = EGameFlowState.StartScreen;
        }

        public void StartGame()
        {
            mCurrentRoundSelection = mQuestionDeckService.DrawNextRound();
            mSelectedQuestionCardSlotOrNull = null;
            mSelectedQuestionDefinition = null;
            mCurrentVerdictKindOrNull = null;
            mCurrentAnswerTranscript = string.Empty;
            mElapsedAnswerSeconds = 0.0f;
            mElapsedSilenceSeconds = 0.0f;
            CurrentState = EGameFlowState.PresentingCards;
        }

        public void MarkCardPresentationCompleted()
        {
            ensureCurrentState(EGameFlowState.PresentingCards);
            CurrentState = EGameFlowState.AwaitingCardSelection;
        }

        public EQuestionCardSlot? UpdateCardSelectionOrNull(EQuestionCardSlot? hoveredQuestionCardSlotOrNull, float deltaTimeSeconds)
        {
            ensureCurrentState(EGameFlowState.AwaitingCardSelection);

            EQuestionCardSlot? confirmedQuestionCardSlotOrNull = mCardDwellSelectionTracker
                .UpdateHoveredCardOrNull(hoveredQuestionCardSlotOrNull, deltaTimeSeconds);

            if (confirmedQuestionCardSlotOrNull == null)
            {
                return null;
            }

            mSelectedQuestionCardSlotOrNull = confirmedQuestionCardSlotOrNull;
            mSelectedQuestionDefinition = mCurrentRoundSelection.GetQuestionBySlot(confirmedQuestionCardSlotOrNull.Value);
            CurrentState = EGameFlowState.RevealingQuestionCard;
            return confirmedQuestionCardSlotOrNull;
        }

        public void MarkQuestionRevealCompleted()
        {
            ensureCurrentState(EGameFlowState.RevealingQuestionCard);
            CurrentState = EGameFlowState.NarratingQuestion;
        }

        public void MarkQuestionNarrationCompleted()
        {
            ensureCurrentState(EGameFlowState.NarratingQuestion);
            CurrentState = EGameFlowState.AwaitingHandInsertion;
        }

        public void NotifyHandReachedFrontAnchor()
        {
            ensureCurrentState(EGameFlowState.AwaitingHandInsertion);
            CurrentState = EGameFlowState.InsertingHand;
        }

        public void NotifyHandReachedInnerAnchor()
        {
            if (CurrentState == EGameFlowState.InsertingHand)
            {
                mElapsedAnswerSeconds = 0.0f;
                mElapsedSilenceSeconds = 0.0f;
                CurrentState = EGameFlowState.Answering;
                return;
            }

            if (CurrentState == EGameFlowState.AnswerPaused)
            {
                CurrentState = EGameFlowState.Answering;
                return;
            }

            throw new InvalidOperationException($"Cannot move to the inner anchor from state: {CurrentState}");
        }

        public void NotifyHandExitedFrontAnchor()
        {
            if (CurrentState != EGameFlowState.Answering)
            {
                return;
            }

            // Hand insertion is a start trigger. Once answering starts, temporary hand loss does not pause collection.
        }

        public bool AdvanceAnswerCollection(float deltaTimeSeconds, bool isSpeechDetected)
        {
            ensureCurrentState(EGameFlowState.Answering);

            AnswerCollectionTickResult answerCollectionTickResult = mAnswerCollectionPolicy.Advance(mElapsedAnswerSeconds, mElapsedSilenceSeconds, deltaTimeSeconds, isSpeechDetected);

            mElapsedAnswerSeconds = answerCollectionTickResult.ElapsedAnswerSeconds;
            mElapsedSilenceSeconds = answerCollectionTickResult.ElapsedSilenceSeconds;

            if (answerCollectionTickResult.ShouldFinishForSilence || answerCollectionTickResult.ShouldFinishForTimeout)
            {
                CurrentState = EGameFlowState.AnalyzingAnswer;
                return true;
            }

            return false;
        }

        public void UpdateAnswerTranscript(string answerTranscript)
        {
            mCurrentAnswerTranscript = string.IsNullOrEmpty(answerTranscript) ? string.Empty : answerTranscript;
        }

        public void ResetCardSelectionHover()
        {
            mCardDwellSelectionTracker.Reset();
        }

        public void CompleteAnalysis(AnswerAnalysisResult answerAnalysisResult)
        {
            ensureCurrentState(EGameFlowState.AnalyzingAnswer);

            if (answerAnalysisResult == null)
            {
                throw new ArgumentNullException(nameof(answerAnalysisResult));
            }

            mCurrentVerdictKindOrNull = answerAnalysisResult.VerdictKind;
            CurrentState = EGameFlowState.ShowingResult;
        }

        public void TryAgain()
        {
            ensureCurrentState(EGameFlowState.ShowingResult);
            StartGame();
        }

        public void ReturnToStart()
        {
            ensureCurrentState(EGameFlowState.ShowingResult);
            OpenStartScreen();
        }

        public GameSessionSnapshot CreateSnapshot()
        {
            return new GameSessionSnapshot(CurrentState, mCurrentRoundSelection, mSelectedQuestionCardSlotOrNull, mSelectedQuestionDefinition, mCurrentVerdictKindOrNull, mCurrentAnswerTranscript, mCardDwellSelectionTracker.HoveredDurationSeconds, mElapsedAnswerSeconds, mElapsedSilenceSeconds);
        }

        private void ensureCurrentState(EGameFlowState expectedGameFlowState)
        {
            if (CurrentState != expectedGameFlowState)
            {
                throw new InvalidOperationException($"Expected state {expectedGameFlowState}, but was {CurrentState}.");
            }
        }

        private void resetTransientRoundState()
        {
            mCardDwellSelectionTracker.Reset();
            mCurrentRoundSelection = null;
            mSelectedQuestionCardSlotOrNull = null;
            mSelectedQuestionDefinition = null;
            mCurrentVerdictKindOrNull = null;
            mCurrentAnswerTranscript = string.Empty;
            mElapsedAnswerSeconds = 0.0f;
            mElapsedSilenceSeconds = 0.0f;
        }
    }
}
