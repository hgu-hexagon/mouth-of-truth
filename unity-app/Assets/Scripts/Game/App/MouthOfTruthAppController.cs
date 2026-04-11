using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MouthOfTruth.Game.Analysis;
using MouthOfTruth.Game.Data;
using MouthOfTruth.Game.Input;
using MouthOfTruth.Game.Input.Keyboard;
using MouthOfTruth.Game.Narration;
using MouthOfTruth.Game.Presentation.Runtime;
using MouthOfTruth.Game.Session;
using MouthOfTruth.Game.Voice;
using UnityEngine;

namespace MouthOfTruth.Game.App
{
    [DisallowMultipleComponent]
    public class MouthOfTruthAppController : MonoBehaviour
    {
        private const int PROVISIONAL_FACE_RECOGNITION_COUNT = 6;

        private MouthOfTruthGameView mGameView;
        private MouthOfTruthGameStateMachine mGameStateMachine;
        private IQuestionNarrationService mQuestionNarrationService;
        private IAnswerAnalysisClient mAnswerAnalysisClient;
        private IHandInteractionInputAdapter mHandInteractionInputAdapter;
        private IAnswerCaptureInputAdapter mAnswerCaptureInputAdapter;
        private CancellationTokenSource mLifecycleCancellationTokenSource;

        private bool mIsInitialized;
        private bool mIsTransitionBusy;
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
            mAnswerCaptureInputAdapter = createAnswerCaptureInputAdapter();
            mGameView.SetAnswerTranscriptPlaceholder(mAnswerCaptureInputAdapter.TranscriptPlaceholderText);
            mGameView.SetAnswerTranscriptEditable(mAnswerCaptureInputAdapter.RequiresManualTextEntry);
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

            AnswerCaptureFrameSnapshot frameSnapshot = mAnswerCaptureInputAdapter.Update(Time.deltaTime);
            applyTranscriptUpdate(frameSnapshot.TranscriptText);
            bool shouldFinishAnswer = mGameStateMachine.AdvanceAnswerCollection(
                Time.deltaTime,
                frameSnapshot.IsSpeechDetected);
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
            mGameView.SetAnswerTranscriptEditable(mAnswerCaptureInputAdapter.RequiresManualTextEntry);
            resetAnswerTracking();
            mIsTransitionBusy = false;
        }

        private async Task insertHandAsync()
        {
            mIsTransitionBusy = true;
            bool isResumingAnswer = mGameStateMachine.CurrentState == EGameFlowState.AnswerPaused;

            if (mGameStateMachine.CurrentState == EGameFlowState.AwaitingHandInsertion)
            {
                mGameStateMachine.NotifyHandReachedFrontAnchor();
            }

            await mGameView.AnimateHandInsertionAsync();
            mGameStateMachine.NotifyHandReachedInnerAnchor();
            try
            {
                beginOrResumeAnswerCapture(isResumingAnswer);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Falling back to keyboard answer entry because microphone capture failed.\n"
                    + exception);
                mAnswerCaptureInputAdapter = new KeyboardTranscriptAnswerInputAdapter(mGameView);
                mGameView.SetAnswerTranscriptPlaceholder(mAnswerCaptureInputAdapter.TranscriptPlaceholderText);
                mAnswerCaptureInputAdapter.Reset();
                beginOrResumeAnswerCapture(isResumingAnswer: false);
            }

            mGameView.ShowAnswering();
            mGameView.SetAnswerTranscriptEditable(mAnswerCaptureInputAdapter.RequiresManualTextEntry);
            mIsTransitionBusy = false;
        }

        private async Task pauseAnswerAsync()
        {
            mIsTransitionBusy = true;
            mGameStateMachine.NotifyHandExitedFrontAnchor();
            mAnswerCaptureInputAdapter.PauseCollection();
            await mGameView.AnimateHandRemovalAsync();
            mGameView.ShowAnswerPaused();
            mGameView.SetAnswerTranscriptEditable(false);
            mIsTransitionBusy = false;
        }

        private async Task analyzeAnswerAsync()
        {
            mIsTransitionBusy = true;
            mGameView.ShowAnalyzing();
            GameSessionSnapshot snapshot = mGameStateMachine.CreateSnapshot();
            AnswerCaptureResult answerCaptureResult = await mAnswerCaptureInputAdapter.CompleteCollectionAsync(
                snapshot.SelectedQuestionDefinition?.ID,
                mLifecycleCancellationTokenSource.Token);

            if (string.IsNullOrWhiteSpace(answerCaptureResult.TranscriptText) == false)
            {
                applyTranscriptUpdate(answerCaptureResult.TranscriptText);
                snapshot = mGameStateMachine.CreateSnapshot();
            }

            AnswerAnalysisRequest answerAnalysisRequest = buildAnalysisRequest(snapshot, answerCaptureResult);
            AnswerAnalysisResult answerAnalysisResult;

            try
            {
                answerAnalysisResult = await mAnswerAnalysisClient.AnalyzeAsync(
                    answerAnalysisRequest,
                    mLifecycleCancellationTokenSource.Token);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Primary answer analysis failed. Falling back to deterministic analysis.\n"
                    + exception);
                answerAnalysisResult = await new DeterministicAnswerAnalysisClient().AnalyzeAsync(
                    answerAnalysisRequest,
                    mLifecycleCancellationTokenSource.Token);
            }

            applyTranscriptUpdate(answerAnalysisResult.AnswerTranscript);
            snapshot = mGameStateMachine.CreateSnapshot();
            mGameStateMachine.CompleteAnalysis(answerAnalysisResult);
            mGameView.ShowResult(answerAnalysisResult.VerdictKind, snapshot.CurrentAnswerTranscript);
            mIsTransitionBusy = false;
        }

        private AnswerAnalysisRequest buildAnalysisRequest(
            GameSessionSnapshot snapshot,
            AnswerCaptureResult answerCaptureResult)
        {
            string answerTranscript = string.IsNullOrWhiteSpace(answerCaptureResult.TranscriptText)
                ? snapshot.CurrentAnswerTranscript.Trim()
                : answerCaptureResult.TranscriptText.Trim();
            int voiceSegmentCount = answerCaptureResult.VoiceSegmentCount;
            int faceRecognitionCount = string.IsNullOrWhiteSpace(answerTranscript)
                ? 0
                : PROVISIONAL_FACE_RECOGNITION_COUNT;

            return new AnswerAnalysisRequest(
                snapshot.SelectedQuestionDefinition,
                answerTranscript,
                answerCaptureResult.AudioFilePath,
                faceRecognitionCount,
                voiceSegmentCount);
        }

        private void resetAnswerTracking()
        {
            mLastObservedTranscript = string.Empty;
            mGameView.ClearAnswerTranscript();
            mAnswerCaptureInputAdapter.Reset();
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

            if (string.Equals(analysisMode, "deterministic", StringComparison.OrdinalIgnoreCase))
            {
                return new DeterministicAnswerAnalysisClient();
            }

            if (File.Exists(PythonAnalysisBridgePaths.GetPythonInterpreterPath()))
            {
                return new PythonBridgeAnalysisClient();
            }

            return new DeterministicAnswerAnalysisClient();
        }

        private IHandInteractionInputAdapter createHandInteractionInputAdapter()
        {
            return new KeyboardHandInputAdapter();
        }

        private IAnswerCaptureInputAdapter createAnswerCaptureInputAdapter()
        {
            MicrophoneAnswerInputAdapter microphoneAnswerInputAdapter = new MicrophoneAnswerInputAdapter();

            if (microphoneAnswerInputAdapter.HasAvailableDevice())
            {
                return microphoneAnswerInputAdapter;
            }

            return new KeyboardTranscriptAnswerInputAdapter(mGameView);
        }

        private void beginOrResumeAnswerCapture(bool isResumingAnswer)
        {
            if (isResumingAnswer == false)
            {
                mAnswerCaptureInputAdapter.BeginCollection();
                return;
            }

            mAnswerCaptureInputAdapter.ResumeCollection();
        }

        private void applyTranscriptUpdate(string transcriptText)
        {
            string normalizedTranscriptText = transcriptText ?? string.Empty;

            if (string.Equals(normalizedTranscriptText, mLastObservedTranscript, StringComparison.Ordinal))
            {
                return;
            }

            mLastObservedTranscript = normalizedTranscriptText;
            mGameStateMachine.UpdateAnswerTranscript(normalizedTranscriptText);
            mGameView.SetAnswerTranscriptText(normalizedTranscriptText);
        }
    }
}
