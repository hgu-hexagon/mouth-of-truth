using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MouthOfTruth.Game.Analysis;
using MouthOfTruth.Game.Data;
using MouthOfTruth.Game.Face;
using MouthOfTruth.Game.Input;
using MouthOfTruth.Game.Input.Leap;
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
        private MouthOfTruthGameView mGameView;
        private MouthOfTruthGameStateMachine mGameStateMachine;
        private IQuestionNarrationService mQuestionNarrationService;
        private IAnswerAnalysisClient mAnswerAnalysisClient;
        private IHandInteractionInputAdapter mHandInteractionInputAdapter;
        private IAnswerCaptureInputAdapter mAnswerCaptureInputAdapter;
        private IFaceCaptureInputAdapter mFaceCaptureInputAdapter;
        private CancellationTokenSource mLifecycleCancellationTokenSource;
        private UiActionDwellSelectionTracker mUiActionDwellSelectionTracker;

        private bool mIsInitialized;
        private bool mIsTransitionBusy;
        private string mLastObservedTranscript = string.Empty;
        private EHandAnchorState mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;

        private async void Start()
        {
            mLifecycleCancellationTokenSource = new CancellationTokenSource();
            mGameView = GetComponent<MouthOfTruthGameView>() ?? gameObject.AddComponent<MouthOfTruthGameView>();
            await mGameView.InitializeAsync();
            await requestCaptureAuthorizationsAsync();
            initializeStateMachine();
            mIsInitialized = true;
        }

        private void Update()
        {
            if (mIsInitialized == false || mIsTransitionBusy)
            {
                return;
            }

            Vector2? pointerScreenPosition = tryGetPointerScreenPosition();
            updatePointerPresentation(pointerScreenPosition);

            if (updateUiActionSelection(pointerScreenPosition))
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
                resetInteractionSelectionState();
                return;
            }

            switch (mGameStateMachine.CurrentState)
            {
                case EGameFlowState.AwaitingCardSelection:
                    updateCardSelection(pointerScreenPosition);
                    break;

                case EGameFlowState.AwaitingHandInsertion:
                case EGameFlowState.AnswerPaused:
                    updateHandInsertion(pointerScreenPosition);
                    break;

                case EGameFlowState.Answering:
                    updateAnswering(pointerScreenPosition);
                    break;
            }
        }

        private void OnDestroy()
        {
            mAnswerCaptureInputAdapter?.CancelCollection();
            mFaceCaptureInputAdapter?.CancelCollection();
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
            mUiActionDwellSelectionTracker = new UiActionDwellSelectionTracker();

            mGameStateMachine = new MouthOfTruthGameStateMachine(
                questionDeckService,
                cardDwellSelectionTracker,
                answerCollectionPolicy);
            mQuestionNarrationService = createNarrationService();
            mAnswerAnalysisClient = createAnalysisClient();
            mHandInteractionInputAdapter = createHandInteractionInputAdapter();
            mAnswerCaptureInputAdapter = createAnswerCaptureInputAdapter();
            mFaceCaptureInputAdapter = createFaceCaptureInputAdapter();
            mGameView.SetAnswerTranscriptPlaceholder(mAnswerCaptureInputAdapter.TranscriptPlaceholderText);
            mGameView.SetAnswerTranscriptEditable(mAnswerCaptureInputAdapter.RequiresManualTextEntry);
            resetInteractionSelectionState();
            mGameStateMachine.OpenStartScreen();
        }

        private async Task startGameAsync()
        {
            mIsTransitionBusy = true;
            mGameStateMachine.StartGame();
            resetInteractionSelectionState();
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
            resetInteractionSelectionState();
            mGameView.ShowCardSelection(mGameStateMachine.CreateSnapshot().CurrentRoundSelection);
            await Task.Delay(250, mLifecycleCancellationTokenSource.Token);
            mGameStateMachine.MarkCardPresentationCompleted();
            mIsTransitionBusy = false;
        }

        private void updateCardSelection(Vector2? pointerScreenPosition)
        {
            EQuestionCardSlot? hoveredQuestionCardSlot =
                mGameView.GetHoveredQuestionCardSlot(pointerScreenPosition);
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

        private void updateHandInsertion(Vector2? pointerScreenPosition)
        {
            EHandAnchorState handAnchorState = mGameView.GetHandAnchorState(pointerScreenPosition);

            bool canStartInsertion =
                mLastObservedHandAnchorState == EHandAnchorState.OutsideMouth
                && handAnchorState != EHandAnchorState.OutsideMouth;

            if (canStartInsertion == false)
            {
                mLastObservedHandAnchorState = handAnchorState;
                return;
            }

            mLastObservedHandAnchorState = handAnchorState;
            _ = insertHandAsync();
        }

        private void updateAnswering(Vector2? pointerScreenPosition)
        {
            EHandAnchorState handAnchorState = mGameView.GetHandAnchorState(pointerScreenPosition);

            if (mLastObservedHandAnchorState != EHandAnchorState.OutsideMouth
                && handAnchorState == EHandAnchorState.OutsideMouth)
            {
                mLastObservedHandAnchorState = handAnchorState;
                _ = pauseAnswerAsync();
                return;
            }

            mLastObservedHandAnchorState = handAnchorState;

            AnswerCaptureFrameSnapshot frameSnapshot = mAnswerCaptureInputAdapter.Update(Time.deltaTime);
            mFaceCaptureInputAdapter.Update(Time.deltaTime);
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

        private bool updateUiActionSelection(Vector2? pointerScreenPosition)
        {
            if (mGameStateMachine.CurrentState != EGameFlowState.StartScreen
                && mGameStateMachine.CurrentState != EGameFlowState.ShowingResult)
            {
                mUiActionDwellSelectionTracker?.Reset();
                mGameView.UpdateActionButtonHoverVisual(null, 0.0f);
                return false;
            }

            EUiActionTarget? hoveredUiActionTarget =
                mGameView.GetHoveredUiActionTarget(pointerScreenPosition);
            EUiActionTarget? confirmedUiActionTarget = mUiActionDwellSelectionTracker
                .UpdateHoveredTarget(hoveredUiActionTarget, Time.deltaTime);
            float hoverProgress = hoveredUiActionTarget == null
                ? 0.0f
                : Mathf.Clamp01(mUiActionDwellSelectionTracker.HoveredDurationSeconds / 0.7f);

            mGameView.UpdateActionButtonHoverVisual(hoveredUiActionTarget, hoverProgress);

            if (confirmedUiActionTarget == null)
            {
                return false;
            }

            switch (confirmedUiActionTarget.Value)
            {
                case EUiActionTarget.StartGame:
                    _ = startGameAsync();
                    return true;

                case EUiActionTarget.TryAgain:
                    _ = restartGameAsync();
                    return true;

                case EUiActionTarget.BackToTitle:
                    mGameStateMachine.ReturnToStart();
                    mGameView.ShowStartScreen();
                    resetAnswerTracking();
                    resetInteractionSelectionState();
                    return true;

                default:
                    return false;
            }
        }

        private Vector2? tryGetPointerScreenPosition()
        {
            if (mHandInteractionInputAdapter == null)
            {
                return null;
            }

            return mHandInteractionInputAdapter.TryGetPointerScreenPosition(out Vector2 screenPosition)
                ? screenPosition
                : (Vector2?)null;
        }

        private void updatePointerPresentation(Vector2? pointerScreenPosition)
        {
            bool shouldShowPointer = pointerScreenPosition.HasValue
                && (
                    mGameStateMachine.CurrentState == EGameFlowState.StartScreen
                    || mGameStateMachine.CurrentState == EGameFlowState.AwaitingCardSelection
                    || mGameStateMachine.CurrentState == EGameFlowState.ShowingResult
                    || mGameStateMachine.CurrentState == EGameFlowState.AwaitingHandInsertion
                    || mGameStateMachine.CurrentState == EGameFlowState.AnswerPaused);

            mGameView.UpdatePointerVisual(shouldShowPointer, pointerScreenPosition);
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
            GameSessionSnapshot snapshot = mGameStateMachine.CreateSnapshot();
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
            beginOrResumeFaceCapture(snapshot.SelectedQuestionDefinition?.ID, isResumingAnswer);

            mGameView.ShowAnswering();
            mGameView.SetAnswerTranscriptEditable(mAnswerCaptureInputAdapter.RequiresManualTextEntry);
            mLastObservedHandAnchorState = EHandAnchorState.AtInnerAnchor;
            mIsTransitionBusy = false;
        }

        private async Task pauseAnswerAsync()
        {
            mIsTransitionBusy = true;
            mGameStateMachine.NotifyHandExitedFrontAnchor();
            mAnswerCaptureInputAdapter.PauseCollection();
            mFaceCaptureInputAdapter.PauseCollection();
            await mGameView.AnimateHandRemovalAsync();
            mGameView.ShowAnswerPaused();
            mGameView.SetAnswerTranscriptEditable(false);
            mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;
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
            FaceCaptureResult faceCaptureResult = await mFaceCaptureInputAdapter.CompleteCollectionAsync(
                mLifecycleCancellationTokenSource.Token);

            if (string.IsNullOrWhiteSpace(answerCaptureResult.TranscriptText) == false)
            {
                applyTranscriptUpdate(answerCaptureResult.TranscriptText);
                snapshot = mGameStateMachine.CreateSnapshot();
            }

            AnswerAnalysisRequest answerAnalysisRequest = buildAnalysisRequest(
                snapshot,
                answerCaptureResult,
                faceCaptureResult);
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
            AnswerCaptureResult answerCaptureResult,
            FaceCaptureResult faceCaptureResult)
        {
            string answerTranscript = string.IsNullOrWhiteSpace(answerCaptureResult.TranscriptText)
                ? snapshot.CurrentAnswerTranscript.Trim()
                : answerCaptureResult.TranscriptText.Trim();
            int voiceSegmentCount = answerCaptureResult.VoiceSegmentCount;

            return new AnswerAnalysisRequest(
                snapshot.SelectedQuestionDefinition,
                answerTranscript,
                answerCaptureResult.AudioFilePath,
                faceCaptureResult.FaceFramesDirectoryPath,
                faceCaptureResult.CapturedFrameCount,
                voiceSegmentCount);
        }

        private void resetAnswerTracking()
        {
            mLastObservedTranscript = string.Empty;
            mGameView.ClearAnswerTranscript();
            mAnswerCaptureInputAdapter.Reset();
            mFaceCaptureInputAdapter?.Reset();
            mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;
        }

        private void resetInteractionSelectionState()
        {
            mUiActionDwellSelectionTracker?.Reset();
            mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;
            mGameView.UpdateActionButtonHoverVisual(null, 0.0f);
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

            if (File.Exists(PythonAnalysisBridgePaths.GetBridgeLauncherScriptPath())
                && Directory.Exists(PythonAnalysisBridgePaths.GetPythonModuleRootPath()))
            {
                return new PythonBridgeAnalysisClient();
            }

            return new DeterministicAnswerAnalysisClient();
        }

        private IHandInteractionInputAdapter createHandInteractionInputAdapter()
        {
            return new CompositeHandInteractionInputAdapter(
                new LeapHandInputAdapter(),
                new KeyboardHandInputAdapter());
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

        private IFaceCaptureInputAdapter createFaceCaptureInputAdapter()
        {
            return new WebcamFaceCaptureInputAdapter();
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

        private void beginOrResumeFaceCapture(string questionID, bool isResumingAnswer)
        {
            if (isResumingAnswer == false)
            {
                mFaceCaptureInputAdapter.BeginCollection(questionID);
                return;
            }

            mFaceCaptureInputAdapter.ResumeCollection();
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

        private async Task requestCaptureAuthorizationsAsync()
        {
            await requestAuthorizationIfNeededAsync(UserAuthorization.Microphone);
            await requestAuthorizationIfNeededAsync(UserAuthorization.WebCam);
        }

        private async Task requestAuthorizationIfNeededAsync(UserAuthorization userAuthorization)
        {
            if (Application.HasUserAuthorization(userAuthorization))
            {
                return;
            }

            AsyncOperation requestOperation = Application.RequestUserAuthorization(userAuthorization);

            while (requestOperation.isDone == false)
            {
                await Task.Yield();
            }
        }
    }
}
