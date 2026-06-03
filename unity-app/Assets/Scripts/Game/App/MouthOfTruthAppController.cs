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
    public partial class MouthOfTruthAppController : MonoBehaviour
    {
        private const float CARD_SELECTION_DWELL_SECONDS = 2.1f;
        private const float UI_ACTION_DWELL_SECONDS = 1.05f;
        private const float MINIMUM_ANALYSIS_PRESENTATION_SECONDS = 2.5f;
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
        private bool mHasShownFirstRunTutorial;
        private string mLastObservedTranscript = string.Empty;

        private void Awake()
        {
            Application.runInBackground = true;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
        }

        private void Start()
        {
            _ = startAsync();
        }

        private async Task startAsync()
        {
            try
            {
                Debug.Log("MouthOfTruthAppController started.");
                mLifecycleCancellationTokenSource = new CancellationTokenSource();
                mAnswerAnalysisClient = createAnalysisClient();
                _ = warmUpAnalysisClientAsync();
                mGameView = GetComponent<MouthOfTruthGameView>();
                if (mGameView == null)
                {
                    mGameView = gameObject.AddComponent<MouthOfTruthGameView>();
                }

                await mGameView.InitializeAsync();
                Debug.Log("MouthOfTruthGameView initialized.");
                applyRuntimeCursorPresentation(isFocused: true);

                await requestCaptureAuthorizationsAsync();

                initializeStateMachine();
                mIsInitialized = true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void Update()
        {
            if (mIsInitialized == false)
            {
                return;
            }

            Vector2? pointerScreenPositionOrNull = tryGetPointerScreenPositionOrNull();
            Vector2? presentedPointerScreenPositionOrNull = getPresentedPointerScreenPositionOrNull(pointerScreenPositionOrNull);
            updatePointerPresentation(presentedPointerScreenPositionOrNull);

            if (mGameView.ConsumeExitRequested())
            {
                requestApplicationExit();
                return;
            }

            bool canAcceptPointerActivation = mPointerPresentationOverrideRemainingSeconds <= 0.0f
                && updatePointerActivationGuard(presentedPointerScreenPositionOrNull);
            Vector2? activatablePointerScreenPositionOrNull = canAcceptPointerActivation ? presentedPointerScreenPositionOrNull : null;

            if (mIsTransitionBusy)
            {
                if (mGameView.IsFirstRunTutorialVisible)
                {
                    updateUiActionSelection(activatablePointerScreenPositionOrNull);
                }

                return;
            }

            if (updateUiActionSelection(activatablePointerScreenPositionOrNull))
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

            if (mGameView.ConsumeBackToTitleRequested() || (mGameStateMachine.CurrentState == EGameFlowState.ShowingResult && mHandInteractionInputAdapter.WasReturnToTitleTriggeredThisFrame()))
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
                    updateCardSelection(activatablePointerScreenPositionOrNull);
                    break;

                case EGameFlowState.AwaitingHandInsertion:
                    updateHandPromptDismissal(presentedPointerScreenPositionOrNull);
                    updateHandInsertion(activatablePointerScreenPositionOrNull);
                    break;

                case EGameFlowState.AnswerPaused:
                    updateHandInsertion(activatablePointerScreenPositionOrNull);
                    break;

                case EGameFlowState.Answering:
                    updateAnswering(pointerScreenPositionOrNull);
                    break;

                default:
                    break;
            }
        }

        private void OnDestroy()
        {
            mAnswerCaptureInputAdapter?.CancelCollection();
            mFaceCaptureInputAdapter?.CancelCollection();
            (mAnswerAnalysisClient as IDisposable)?.Dispose();
            mLifecycleCancellationTokenSource?.Cancel();
            mLifecycleCancellationTokenSource?.Dispose();
            restoreSystemCursor();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                applyRuntimeCursorPresentation(isFocused: true);
                return;
            }

            restoreSystemCursor();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                restoreSystemCursor();
                return;
            }

            applyRuntimeCursorPresentation(isFocused: true);
        }

        private void initializeStateMachine()
        {
            string questionPoolFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, "questions", "question_pool.json");
            IReadOnlyList<QuestionDefinition> questionDefinitions = QuestionPoolLoader.LoadQuestionDefinitions(questionPoolFilePath);
            QuestionDeckService questionDeckService = new QuestionDeckService(questionDefinitions);
            CardDwellSelectionTracker cardDwellSelectionTracker = new CardDwellSelectionTracker(CARD_SELECTION_DWELL_SECONDS);
            AnswerCollectionPolicy answerCollectionPolicy = new AnswerCollectionPolicy();
            mUiActionDwellSelectionTracker = new UiActionDwellSelectionTracker(UI_ACTION_DWELL_SECONDS);

            mGameStateMachine = new MouthOfTruthGameStateMachine(questionDeckService, cardDwellSelectionTracker, answerCollectionPolicy);
            mQuestionNarrationService = createNarrationService();
            if (mAnswerAnalysisClient == null)
            {
                mAnswerAnalysisClient = createAnalysisClient();
            }

            mHandInteractionInputAdapter = createHandInteractionInputAdapter();
            mAnswerCaptureInputAdapter = createAnswerCaptureInputAdapter();
            prepareAnswerAudioSession();
            mFaceCaptureInputAdapter = createFaceCaptureInputAdapter();
            mGameView.SetAnswerTranscriptPlaceholder(mAnswerCaptureInputAdapter.TranscriptPlaceholderText);
            mGameView.SetAnswerTranscriptEditable(mAnswerCaptureInputAdapter.RequiresManualTextEntry);
            resetInteractionSelectionState();
            mGameStateMachine.OpenStartScreen();
        }

        private async Task startGameAsync()
        {
            mIsTransitionBusy = true;
            bool shouldShowFirstRunTutorial = mHasShownFirstRunTutorial == false;
            mHasShownFirstRunTutorial = true;
            mGameStateMachine.StartGame();
            resetInteractionSelectionState();

            if (shouldShowFirstRunTutorial)
            {
                await mGameView.PlayFirstRunTutorialAsync();
            }

            await playCardSelectionPresentationAsync();
            mGameStateMachine.MarkCardPresentationCompleted();
            mIsTransitionBusy = false;
        }

        private async Task restartGameAsync()
        {
            mIsTransitionBusy = true;
            mGameStateMachine.TryAgain();
            resetAnswerTracking();
            resetInteractionSelectionState();
            await playCardSelectionPresentationAsync();
            beginBottomCenterPointerSettle();
            mGameStateMachine.MarkCardPresentationCompleted();
            mIsTransitionBusy = false;
        }

        private async Task playCardSelectionPresentationAsync()
        {
            await mGameView.PlayTempleApproachToCardSelectionAsync();
            mGameView.ShowCardSelection(mGameStateMachine.CreateSnapshot().CurrentRoundSelection);
            await mGameView.PlayCardSelectionEntranceAsync();
        }

        private void updateCardSelection(Vector2? pointerScreenPositionOrNull)
        {
            EQuestionCardSlot? hoveredQuestionCardSlotOrNull = mGameView.GetHoveredQuestionCardSlotOrNull(pointerScreenPositionOrNull);
            EQuestionCardSlot? confirmedQuestionCardSlotOrNull = mGameStateMachine.UpdateCardSelectionOrNull(hoveredQuestionCardSlotOrNull, Time.deltaTime);
            GameSessionSnapshot snapshot = mGameStateMachine.CreateSnapshot();

            float hoverProgress = hoveredQuestionCardSlotOrNull == null
                || snapshot.CurrentState != EGameFlowState.AwaitingCardSelection
                ? 0.0f
                : Mathf.Clamp01(snapshot.HoveredCardDwellSeconds / CARD_SELECTION_DWELL_SECONDS);

            mGameView.UpdateCardHoverVisual(hoveredQuestionCardSlotOrNull, hoverProgress);

            if (confirmedQuestionCardSlotOrNull.HasValue == false)
            {
                return;
            }

            _ = revealQuestionAsync(confirmedQuestionCardSlotOrNull.Value, mGameStateMachine.CreateSnapshot().SelectedQuestionDefinition);
        }

        private void updateHandInsertion(Vector2? pointerScreenPositionOrNull)
        {
            EHandAnchorState handAnchorState = mGameView.GetHandAnchorState(pointerScreenPositionOrNull);

            bool canStartInsertion = mLastObservedHandAnchorState == EHandAnchorState.OutsideMouth
                && handAnchorState != EHandAnchorState.OutsideMouth;

            if (canStartInsertion == false)
            {
                mLastObservedHandAnchorState = handAnchorState;
                return;
            }

            mLastObservedHandAnchorState = handAnchorState;
            _ = insertHandAsync();
        }

        private void updateAnswering(Vector2? pointerScreenPositionOrNull)
        {
            EHandAnchorState handAnchorState = mGameView.GetHandAnchorState(pointerScreenPositionOrNull);
            mLastObservedHandAnchorState = handAnchorState == EHandAnchorState.OutsideMouth
                ? EHandAnchorState.AtFrontAnchor
                : handAnchorState;

            AnswerCaptureFrameSnapshot frameSnapshot = mAnswerCaptureInputAdapter.Update(Time.deltaTime);
            mFaceCaptureInputAdapter.Update(Time.deltaTime);
            applyTranscriptUpdate(frameSnapshot.TranscriptText);
            bool shouldFinishAnswer = mGameStateMachine.AdvanceAnswerCollection(Time.deltaTime, frameSnapshot.IsSpeechDetected);
            GameSessionSnapshot snapshot = mGameStateMachine.CreateSnapshot();
            mGameView.UpdateAnswerMetrics(snapshot.ElapsedAnswerSeconds, snapshot.ElapsedSilenceSeconds);

            if (shouldFinishAnswer)
            {
                _ = analyzeAnswerAsync();
            }
        }

        private bool updateUiActionSelection(Vector2? pointerScreenPositionOrNull)
        {
            EUiActionTarget? hoveredUiActionTargetOrNull = mGameView.GetHoveredUiActionTargetOrNull(pointerScreenPositionOrNull);
            hoveredUiActionTargetOrNull = isUiActionAllowedForCurrentState(hoveredUiActionTargetOrNull) ? hoveredUiActionTargetOrNull : null;
            EUiActionTarget? confirmedUiActionTargetOrNull = mUiActionDwellSelectionTracker
                .UpdateHoveredTargetOrNull(hoveredUiActionTargetOrNull, Time.deltaTime);
            float hoverProgress = hoveredUiActionTargetOrNull == null
                ? 0.0f
                : Mathf.Clamp01(mUiActionDwellSelectionTracker.HoveredDurationSeconds / UI_ACTION_DWELL_SECONDS);

            mGameView.UpdateActionButtonHoverVisual(hoveredUiActionTargetOrNull, hoverProgress);

            if (confirmedUiActionTargetOrNull == null)
            {
                return false;
            }

            switch (confirmedUiActionTargetOrNull.Value)
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

                case EUiActionTarget.ExitGame:
                    requestApplicationExit();
                    return true;

                default:
                    return false;
            }
        }

        private bool isUiActionAllowedForCurrentState(EUiActionTarget? uiActionTargetOrNull)
        {
            if (uiActionTargetOrNull == null)
            {
                return false;
            }

            return uiActionTargetOrNull.Value switch
            {
                EUiActionTarget.ExitGame => true,
                EUiActionTarget.StartGame => mGameStateMachine.CurrentState == EGameFlowState.StartScreen,
                EUiActionTarget.TryAgain => mGameStateMachine.CurrentState == EGameFlowState.ShowingResult,
                EUiActionTarget.BackToTitle => mGameStateMachine.CurrentState == EGameFlowState.ShowingResult,
                _ => false,
            };
        }

        private void requestApplicationExit()
        {
            mAnswerCaptureInputAdapter?.CancelCollection();
            mFaceCaptureInputAdapter?.CancelCollection();
            restoreSystemCursor();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private async Task revealQuestionAsync(EQuestionCardSlot selectedQuestionCardSlot, QuestionDefinition selectedQuestionDefinition)
        {
            mIsTransitionBusy = true;
            mGameView.UpdatePointerVisual(false, null);
            mGameView.UpdateActionButtonHoverVisual(null, 0.0f);
            await mGameView.PlayQuestionRevealAsync(selectedQuestionCardSlot, selectedQuestionDefinition, () => mQuestionNarrationService.SpeakQuestionAsync(selectedQuestionDefinition, mLifecycleCancellationTokenSource.Token));
            await mGameView.PrepareTempleGameplayBackdropAsync();
            mGameStateMachine.MarkQuestionRevealCompleted();
            mGameStateMachine.MarkQuestionNarrationCompleted();
            mGameView.ShowAwaitingHandInsertion();
            beginBottomCenterPointerSettle(mGameView.HandPromptPanelHoldDurationSeconds);
            mGameView.SetAnswerTranscriptEditable(mAnswerCaptureInputAdapter.RequiresManualTextEntry);
            resetAnswerTracking();
            mIsTransitionBusy = false;
        }

        private async Task insertHandAsync()
        {
            mIsTransitionBusy = true;
            resetPointerPresentationRebase();
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
                Debug.LogWarning("Falling back to keyboard answer entry because microphone capture failed.\n" + exception);
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

        private async Task analyzeAnswerAsync()
        {
            mIsTransitionBusy = true;
            mGameView.ShowAnalyzing();
            float analysisPresentationStartedAtSeconds = Time.unscaledTime + mGameView.AnalysisFocusRampDurationSeconds;
            GameSessionSnapshot snapshot = mGameStateMachine.CreateSnapshot();
            System.Diagnostics.Stopwatch captureStopwatch = System.Diagnostics.Stopwatch.StartNew();
            Task<AnswerCaptureResult> answerCaptureTask = mAnswerCaptureInputAdapter.CompleteCollectionAsync(snapshot.SelectedQuestionDefinition?.ID, mLifecycleCancellationTokenSource.Token);
            Task<FaceCaptureResult> faceCaptureTask = mFaceCaptureInputAdapter.CompleteCollectionAsync(mLifecycleCancellationTokenSource.Token);
            await Task.WhenAll(answerCaptureTask, faceCaptureTask);
            captureStopwatch.Stop();
            Debug.Log($"Answer capture finalization completed in {captureStopwatch.ElapsedMilliseconds} ms.");
            AnswerCaptureResult answerCaptureResult = answerCaptureTask.Result;
            FaceCaptureResult faceCaptureResult = faceCaptureTask.Result;

            if (string.IsNullOrWhiteSpace(answerCaptureResult.TranscriptText) == false)
            {
                applyTranscriptUpdate(answerCaptureResult.TranscriptText);
                snapshot = mGameStateMachine.CreateSnapshot();
            }

            AnswerAnalysisRequest answerAnalysisRequest = buildAnalysisRequest(snapshot, answerCaptureResult, faceCaptureResult);
            AnswerAnalysisResult answerAnalysisResult;

            try
            {
                System.Diagnostics.Stopwatch analysisStopwatch = System.Diagnostics.Stopwatch.StartNew();
                answerAnalysisResult = await mAnswerAnalysisClient.AnalyzeAsync(answerAnalysisRequest, mLifecycleCancellationTokenSource.Token);
                analysisStopwatch.Stop();
                Debug.Log($"Answer analysis completed in {analysisStopwatch.ElapsedMilliseconds} ms.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Primary answer analysis failed. Falling back to deterministic analysis.\n" + exception);
                answerAnalysisResult = await new DeterministicAnswerAnalysisClient().AnalyzeAsync(answerAnalysisRequest, mLifecycleCancellationTokenSource.Token);
            }

            float elapsedAnalysisPresentationSeconds = Time.unscaledTime - analysisPresentationStartedAtSeconds;
            await waitForRealtimeSecondsAsync(MINIMUM_ANALYSIS_PRESENTATION_SECONDS - elapsedAnalysisPresentationSeconds, mLifecycleCancellationTokenSource.Token);
            await mGameView.PlayAnalysisCompleteTransitionAsync();
            applyTranscriptUpdate(answerAnalysisResult.AnswerTranscript);
            snapshot = mGameStateMachine.CreateSnapshot();
            mGameStateMachine.CompleteAnalysis(answerAnalysisResult);
            mGameView.ShowResult(answerAnalysisResult.VerdictKind, snapshot.CurrentAnswerTranscript);
            await mGameView.PlayResultRevealAnimationAsync(answerAnalysisResult.VerdictKind);
            mIsTransitionBusy = false;
        }

        private static async Task waitForRealtimeSecondsAsync(float durationSeconds, CancellationToken cancellationToken)
        {
            if (durationSeconds <= 0.0f)
            {
                return;
            }

            float targetTimeSeconds = Time.unscaledTime + durationSeconds;

            while (Time.unscaledTime < targetTimeSeconds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        private AnswerAnalysisRequest buildAnalysisRequest(GameSessionSnapshot snapshot, AnswerCaptureResult answerCaptureResult, FaceCaptureResult faceCaptureResult)
        {
            string answerTranscript = string.IsNullOrWhiteSpace(answerCaptureResult.TranscriptText)
                ? snapshot.CurrentAnswerTranscript.Trim()
                : answerCaptureResult.TranscriptText.Trim();
            int voiceSegmentCount = answerCaptureResult.VoiceSegmentCount;

            return new AnswerAnalysisRequest(snapshot.SelectedQuestionDefinition, answerTranscript, answerCaptureResult.AudioFilePath, faceCaptureResult.FaceFramesDirectoryPath, faceCaptureResult.CapturedFrameCount, voiceSegmentCount);
        }

        private void resetAnswerTracking()
        {
            mLastObservedTranscript = string.Empty;
            mGameView.ClearAnswerTranscript();
            mAnswerCaptureInputAdapter.Reset();
            mFaceCaptureInputAdapter?.Reset();
            mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;
            resetHandPromptDismissalTracking();
        }

        private IQuestionNarrationService createNarrationService()
        {
            IQuestionNarrationService fallbackNarrationService = Application.platform == RuntimePlatform.OSXEditor
                || Application.platform == RuntimePlatform.OSXPlayer
                ? new MacOsQuestionNarrationService()
                : new SilentQuestionNarrationService();

            string questionAudioDirectoryPath = Path.Combine(Application.streamingAssetsPath, "audio", "questions");

            return new PrerecordedQuestionNarrationService(questionAudioDirectoryPath, fallbackNarrationService);
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

            if (File.Exists(PythonAnalysisBridgePaths.GetBridgeLauncherScriptPath()) && Directory.Exists(PythonAnalysisBridgePaths.GetPythonModuleRootPath()))
            {
                return new PythonBridgeAnalysisClient();
            }

            return new DeterministicAnswerAnalysisClient();
        }

        private async Task warmUpAnalysisClientAsync()
        {
            try
            {
                System.Diagnostics.Stopwatch warmUpStopwatch = System.Diagnostics.Stopwatch.StartNew();
                await mAnswerAnalysisClient.WarmUpAsync(mLifecycleCancellationTokenSource.Token);
                warmUpStopwatch.Stop();
                Debug.Log($"Answer analysis engine warmed up in {warmUpStopwatch.ElapsedMilliseconds} ms.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Answer analysis engine warm-up did not finish before gameplay. " + "The first verdict may wait for startup.\n" + exception);
            }
        }

        private IHandInteractionInputAdapter createHandInteractionInputAdapter()
        {
            return new CompositeHandInteractionInputAdapter(new LeapHandInputAdapter(), new KeyboardHandInputAdapter());
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

        private void prepareAnswerAudioSession()
        {
            MicrophoneAnswerInputAdapter microphoneAnswerInputAdapter = mAnswerCaptureInputAdapter as MicrophoneAnswerInputAdapter;

            if (microphoneAnswerInputAdapter == null)
            {
                return;
            }

            try
            {
                microphoneAnswerInputAdapter.PrepareAudioSession();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Microphone audio session prewarm failed. Capture will retry when answering.\n" + exception);
            }
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
            string normalizedTranscriptText = string.IsNullOrEmpty(transcriptText) ? string.Empty : transcriptText;

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
