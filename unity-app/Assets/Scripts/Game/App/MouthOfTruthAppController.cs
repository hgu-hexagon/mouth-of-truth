using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MouthOfTruth.Game.Analysis;
using MouthOfTruth.Game.Data;
using MouthOfTruth.Game.Input;
using MouthOfTruth.Game.Input.Development;
using MouthOfTruth.Game.Narration;
using MouthOfTruth.Game.Presentation.Runtime;
using MouthOfTruth.Game.Session;
using UnityEngine;

namespace MouthOfTruth.Game.App
{
    [DisallowMultipleComponent]
    public class MouthOfTruthAppController : MonoBehaviour
    {
        private const float TYPING_ACTIVITY_GRACE_SECONDS = 0.75f;

        private MouthOfTruthGameView mGameView;
        private MouthOfTruthGameStateMachine mGameStateMachine;
        private IQuestionNarrationService mQuestionNarrationService;
        private IAnswerAnalysisClient mAnswerAnalysisClient;
        private IHandInteractionInputAdapter mHandInteractionInputAdapter;
        private CancellationTokenSource mLifecycleCancellationTokenSource;

        private bool mIsInitialized;
        private bool mIsTransitionBusy;
        private float mTypingActivityGraceSeconds;
        private string mLastObservedTranscript = string.Empty;

        private async void Start()
        {
            mLifecycleCancellationTokenSource = new CancellationTokenSource();
            mGameView = GetComponent<MouthOfTruthGameView>() ?? gameObject.AddComponent<MouthOfTruthGameView>();
            await mGameView.InitializeAsync();
            initializeStateMachine();
            mIsInitialized = true;
        }

        private void Update()
        {
            if (mIsInitialized == false || mIsTransitionBusy)
            {
                return;
            }

            if (mGameView.ConsumeStartRequested())
            {
                _ = startGameAsync();
                return;
            }

            if (mGameView.ConsumeTryAgainRequested())
            {
                _ = restartGameAsync();
                return;
            }

            if (mGameView.ConsumeBackToTitleRequested()
                || (
                    mGameStateMachine.CurrentState == EGameFlowState.ShowingResult
                    && mHandInteractionInputAdapter.WasReturnToTitleTriggeredThisFrame()))
            {
                mGameStateMachine.ReturnToStart();
                mGameView.ShowStartScreen();
                resetAnswerTracking();
                return;
            }

            switch (mGameStateMachine.CurrentState)
            {
                case EGameFlowState.AwaitingCardSelection:
                    updateCardSelection();
                    break;

                case EGameFlowState.AwaitingHandInsertion:
                case EGameFlowState.AnswerPaused:
                    updateHandInsertion();
                    break;

                case EGameFlowState.Answering:
                    updateAnswering();
                    break;
            }
        }

        private void OnDestroy()
        {
            mLifecycleCancellationTokenSource?.Cancel();
            mLifecycleCancellationTokenSource?.Dispose();
        }

        private void initializeStateMachine()
        {
            string questionPoolFilePath = System.IO.Path.Combine(
                Application.streamingAssetsPath,
                "questions",
                "question_pool.json");
            IReadOnlyList<QuestionDefinition> questionDefinitions =
                QuestionPoolLoader.LoadQuestionDefinitions(questionPoolFilePath);
            QuestionDeckService questionDeckService = new QuestionDeckService(questionDefinitions);
            CardDwellSelectionTracker cardDwellSelectionTracker = new CardDwellSelectionTracker();
            AnswerCollectionPolicy answerCollectionPolicy = new AnswerCollectionPolicy();

            mGameStateMachine = new MouthOfTruthGameStateMachine(
                questionDeckService,
                cardDwellSelectionTracker,
                answerCollectionPolicy);
            mQuestionNarrationService = createNarrationService();
            mAnswerAnalysisClient = createAnalysisClient();
            mHandInteractionInputAdapter = createHandInteractionInputAdapter();
            mGameStateMachine.OpenStartScreen();
        }

        private async Task startGameAsync()
        {
            mIsTransitionBusy = true;
            mGameStateMachine.StartGame();
            mGameView.ShowCardSelection(mGameStateMachine.CreateSnapshot().CurrentRoundSelection);
            await Task.Delay(250, mLifecycleCancellationTokenSource.Token);
            mGameStateMachine.MarkCardPresentationCompleted();
            mIsTransitionBusy = false;
        }

        private async Task restartGameAsync()
        {
            mIsTransitionBusy = true;
            mGameStateMachine.TryAgain();
            resetAnswerTracking();
            mGameView.ShowCardSelection(mGameStateMachine.CreateSnapshot().CurrentRoundSelection);
            await Task.Delay(250, mLifecycleCancellationTokenSource.Token);
            mGameStateMachine.MarkCardPresentationCompleted();
            mIsTransitionBusy = false;
        }

        private void updateCardSelection()
        {
            EQuestionCardSlot? hoveredQuestionCardSlot = mGameView.GetHoveredQuestionCardSlot();
            EQuestionCardSlot? confirmedQuestionCardSlot =
                mGameStateMachine.UpdateCardSelection(hoveredQuestionCardSlot, Time.deltaTime);
            GameSessionSnapshot snapshot = mGameStateMachine.CreateSnapshot();

            float hoverProgress = hoveredQuestionCardSlot == null
                || snapshot.CurrentState != EGameFlowState.AwaitingCardSelection
                ? 0.0f
                : Mathf.Clamp01(snapshot.HoveredCardDwellSeconds / 0.7f);

            mGameView.UpdateCardHoverVisual(hoveredQuestionCardSlot, hoverProgress);

            if (confirmedQuestionCardSlot.HasValue == false)
            {
                return;
            }

            _ = revealQuestionAsync(
                confirmedQuestionCardSlot.Value,
                mGameStateMachine.CreateSnapshot().SelectedQuestionDefinition);
        }

        private void updateHandInsertion()
        {
            if (mHandInteractionInputAdapter.WasInsertPressedThisFrame() == false)
            {
                return;
            }

            _ = insertHandAsync();
        }

        private void updateAnswering()
        {
            if (mHandInteractionInputAdapter.WasInsertReleasedThisFrame())
            {
                _ = pauseAnswerAsync();
                return;
            }

            string currentTranscript = mGameView.GetAnswerTranscript();

            if (string.Equals(currentTranscript, mLastObservedTranscript, StringComparison.Ordinal) == false)
            {
                mLastObservedTranscript = currentTranscript;
                mTypingActivityGraceSeconds = TYPING_ACTIVITY_GRACE_SECONDS;
                mGameStateMachine.UpdateAnswerTranscript(currentTranscript);
            }

            if (mTypingActivityGraceSeconds > 0.0f)
            {
                mTypingActivityGraceSeconds -= Time.deltaTime;
            }

            bool isSpeechDetected = mTypingActivityGraceSeconds > 0.0f;
            bool shouldFinishAnswer = mGameStateMachine.AdvanceAnswerCollection(Time.deltaTime, isSpeechDetected);
            GameSessionSnapshot snapshot = mGameStateMachine.CreateSnapshot();
            mGameView.UpdateAnswerMetrics(snapshot.ElapsedAnswerSeconds, snapshot.ElapsedSilenceSeconds);

            if (shouldFinishAnswer)
            {
                _ = analyzeAnswerAsync();
            }
        }

        private async Task revealQuestionAsync(
            EQuestionCardSlot selectedQuestionCardSlot,
            QuestionDefinition selectedQuestionDefinition)
        {
            mIsTransitionBusy = true;
            await mGameView.PlayQuestionRevealAsync(selectedQuestionCardSlot, selectedQuestionDefinition);
            mGameStateMachine.MarkQuestionRevealCompleted();
            mGameView.ShowNarratingQuestion(selectedQuestionDefinition.Text);
            await mQuestionNarrationService.SpeakQuestionAsync(
                selectedQuestionDefinition.Text,
                mLifecycleCancellationTokenSource.Token);
            mGameStateMachine.MarkQuestionNarrationCompleted();
            mGameView.ShowAwaitingHandInsertion();
            resetAnswerTracking();
            mIsTransitionBusy = false;
        }

        private async Task insertHandAsync()
        {
            mIsTransitionBusy = true;

            if (mGameStateMachine.CurrentState == EGameFlowState.AwaitingHandInsertion)
            {
                mGameStateMachine.NotifyHandReachedFrontAnchor();
            }

            await mGameView.AnimateHandInsertionAsync();
            mGameStateMachine.NotifyHandReachedInnerAnchor();
            mGameView.ShowAnswering();
            mIsTransitionBusy = false;
        }

        private async Task pauseAnswerAsync()
        {
            mIsTransitionBusy = true;
            mGameStateMachine.NotifyHandExitedFrontAnchor();
            await mGameView.AnimateHandRemovalAsync();
            mGameView.ShowAnswerPaused();
            mIsTransitionBusy = false;
        }

        private async Task analyzeAnswerAsync()
        {
            mIsTransitionBusy = true;
            mGameView.ShowAnalyzing();
            GameSessionSnapshot snapshot = mGameStateMachine.CreateSnapshot();
            AnswerAnalysisRequest answerAnalysisRequest = buildAnalysisRequest(snapshot);
            AnswerAnalysisResult answerAnalysisResult = await mAnswerAnalysisClient.AnalyzeAsync(
                answerAnalysisRequest,
                mLifecycleCancellationTokenSource.Token);
            mGameStateMachine.CompleteAnalysis(answerAnalysisResult);
            mGameView.ShowResult(answerAnalysisResult.VerdictKind, snapshot.CurrentAnswerTranscript);
            mIsTransitionBusy = false;
        }

        private AnswerAnalysisRequest buildAnalysisRequest(GameSessionSnapshot snapshot)
        {
            string answerTranscript = snapshot.CurrentAnswerTranscript.Trim();
            string[] words = answerTranscript.Split(
                new[] { ' ', '\n', '\t' },
                StringSplitOptions.RemoveEmptyEntries);
            int voiceSegmentCount = words.Length >= 2 ? 1 : 0;
            int faceRecognitionCount = words.Length >= 4 ? 6 : Mathf.Min(4, words.Length);

            return new AnswerAnalysisRequest(
                snapshot.SelectedQuestionDefinition,
                answerTranscript,
                faceRecognitionCount,
                voiceSegmentCount);
        }

        private void resetAnswerTracking()
        {
            mTypingActivityGraceSeconds = 0.0f;
            mLastObservedTranscript = string.Empty;
        }

        private IQuestionNarrationService createNarrationService()
        {
            return Application.platform == RuntimePlatform.OSXEditor
                || Application.platform == RuntimePlatform.OSXPlayer
                ? new MacOsQuestionNarrationService()
                : new SilentQuestionNarrationService();
        }

        private IAnswerAnalysisClient createAnalysisClient()
        {
            string analysisMode = Environment.GetEnvironmentVariable("MOUTH_OF_TRUTH_ANALYSIS_MODE");

            if (string.Equals(analysisMode, "python", StringComparison.OrdinalIgnoreCase))
            {
                return new PythonBridgeAnalysisClient();
            }

            return new SimulatedAnswerAnalysisClient();
        }

        private IHandInteractionInputAdapter createHandInteractionInputAdapter()
        {
            return new DebugHandInputAdapter();
        }
    }
}
