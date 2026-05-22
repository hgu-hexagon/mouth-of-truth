using System;
using System.Collections;
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
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MouthOfTruth.Game.App
{
    [DisallowMultipleComponent]
    public class MouthOfTruthAppController : MonoBehaviour
    {
        private const float CARD_SELECTION_DWELL_SECONDS = 2.1f;
        private const float UI_ACTION_DWELL_SECONDS = 1.05f;
        private const float POINTER_REACQUIRE_GUARD_SECONDS = 0.45f;
        private const float POST_CARD_SELECTION_POINTER_SETTLE_SECONDS = 0.0f;
        private const float POINTER_PRESENTATION_FOLLOW_RATE = 9.0f;
        private const float MINIMUM_ANALYSIS_PRESENTATION_SECONDS = 2.5f;
        private const float PRESENTATION_CAPTURE_HAND_INSERTION_EXTRA_DELAY_SECONDS = 1.2f;
        private const string PRESENTATION_CAPTURE_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_PRESENTATION_CAPTURE";
        private const string PRESENTATION_CAPTURE_OUTPUT_DIRECTORY_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_CAPTURE_OUTPUT_DIR";
        private const string PRESENTATION_CAPTURE_ARGUMENT_NAME = "-presentation-capture";

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
        private bool mIsPresentationCaptureRunning;
        private bool mHasShownFirstRunTutorial;
        private bool mWasPointerAvailableLastFrame;
        private float mPointerReacquireGuardRemainingSeconds;
        private float mPointerPresentationOverrideRemainingSeconds;
        private Vector2? mPresentedPointerScreenPosition;
        private string mLastObservedTranscript = string.Empty;
        private EHandAnchorState mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;

        private void Awake()
        {
            Application.runInBackground = true;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;

            if (isPresentationCaptureEnabled())
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            }
        }

        private async void Start()
        {
            try
            {
                Debug.Log($"MouthOfTruthAppController started. Presentation capture enabled: {isPresentationCaptureEnabled()}.");
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

                if (isPresentationCaptureEnabled() == false)
                {
                    await requestCaptureAuthorizationsAsync();
                }

                initializeStateMachine();
                mIsInitialized = true;

                if (isPresentationCaptureEnabled())
                {
                    StartCoroutine(runPresentationCaptureSequenceCoroutine());
                }
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

            Vector2? pointerScreenPosition = tryGetPointerScreenPosition();
            Vector2? presentedPointerScreenPosition = getPresentedPointerScreenPosition(pointerScreenPosition);
            updatePointerPresentation(presentedPointerScreenPosition);

            if (mGameView.ConsumeExitRequested())
            {
                requestApplicationExit();
                return;
            }

            bool canAcceptPointerActivation = mPointerPresentationOverrideRemainingSeconds <= 0.0f
                && updatePointerActivationGuard(presentedPointerScreenPosition);
            Vector2? activatablePointerScreenPosition = canAcceptPointerActivation ? presentedPointerScreenPosition : null;

            if (mIsTransitionBusy || mIsPresentationCaptureRunning)
            {
                if (mGameView.IsFirstRunTutorialVisible)
                {
                    updateUiActionSelection(activatablePointerScreenPosition);
                }

                return;
            }

            if (updateUiActionSelection(activatablePointerScreenPosition))
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
                    updateCardSelection(activatablePointerScreenPosition);
                    break;

                case EGameFlowState.AwaitingHandInsertion:
                case EGameFlowState.AnswerPaused:
                    updateHandInsertion(activatablePointerScreenPosition);
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

            await mGameView.PlayTempleApproachToCardSelectionAsync();
            mGameView.ShowCardSelection(mGameStateMachine.CreateSnapshot().CurrentRoundSelection);
            await mGameView.PlayCardSelectionEntranceAsync();
            mGameStateMachine.MarkCardPresentationCompleted();
            mIsTransitionBusy = false;
        }

        private async Task restartGameAsync()
        {
            mIsTransitionBusy = true;
            mGameStateMachine.TryAgain();
            resetAnswerTracking();
            resetInteractionSelectionState();
            await mGameView.PlayTempleApproachToCardSelectionAsync();
            mGameView.ShowCardSelection(mGameStateMachine.CreateSnapshot().CurrentRoundSelection);
            await mGameView.PlayCardSelectionEntranceAsync();
            beginBottomCenterPointerSettle();
            mGameStateMachine.MarkCardPresentationCompleted();
            mIsTransitionBusy = false;
        }

        private void updateCardSelection(Vector2? pointerScreenPosition)
        {
            EQuestionCardSlot? hoveredQuestionCardSlot = mGameView.GetHoveredQuestionCardSlot(pointerScreenPosition);
            EQuestionCardSlot? confirmedQuestionCardSlot = mGameStateMachine.UpdateCardSelection(hoveredQuestionCardSlot, Time.deltaTime);
            GameSessionSnapshot snapshot = mGameStateMachine.CreateSnapshot();

            float hoverProgress = hoveredQuestionCardSlot == null
                || snapshot.CurrentState != EGameFlowState.AwaitingCardSelection
                ? 0.0f
                : Mathf.Clamp01(snapshot.HoveredCardDwellSeconds / CARD_SELECTION_DWELL_SECONDS);

            mGameView.UpdateCardHoverVisual(hoveredQuestionCardSlot, hoverProgress);

            if (confirmedQuestionCardSlot.HasValue == false)
            {
                return;
            }

            _ = revealQuestionAsync(confirmedQuestionCardSlot.Value, mGameStateMachine.CreateSnapshot().SelectedQuestionDefinition);
        }

        private void updateHandInsertion(Vector2? pointerScreenPosition)
        {
            EHandAnchorState handAnchorState = mGameView.GetHandAnchorState(pointerScreenPosition);

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

        private void updateAnswering(Vector2? pointerScreenPosition)
        {
            EHandAnchorState handAnchorState = mGameView.GetHandAnchorState(pointerScreenPosition);
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

        private bool updateUiActionSelection(Vector2? pointerScreenPosition)
        {
            EUiActionTarget? hoveredUiActionTarget = mGameView.GetHoveredUiActionTarget(pointerScreenPosition);
            hoveredUiActionTarget = isUiActionAllowedForCurrentState(hoveredUiActionTarget) ? hoveredUiActionTarget : null;
            EUiActionTarget? confirmedUiActionTarget = mUiActionDwellSelectionTracker
                .UpdateHoveredTarget(hoveredUiActionTarget, Time.deltaTime);
            float hoverProgress = hoveredUiActionTarget == null
                ? 0.0f
                : Mathf.Clamp01(mUiActionDwellSelectionTracker.HoveredDurationSeconds / UI_ACTION_DWELL_SECONDS);

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

                case EUiActionTarget.ExitGame:
                    requestApplicationExit();
                    return true;

                default:
                    return false;
            }
        }

        private bool isUiActionAllowedForCurrentState(EUiActionTarget? uiActionTarget)
        {
            if (uiActionTarget == null)
            {
                return false;
            }

            return uiActionTarget.Value switch
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
            bool isCinematicTransition = mIsTransitionBusy
                && (mGameStateMachine.CurrentState == EGameFlowState.InsertingHand || mGameStateMachine.CurrentState == EGameFlowState.ShowingResult);
            bool shouldShowPointer = pointerScreenPosition.HasValue
                && isCinematicTransition == false
                && (mGameStateMachine.CurrentState == EGameFlowState.StartScreen || mGameView.IsFirstRunTutorialVisible || mGameStateMachine.CurrentState == EGameFlowState.AwaitingCardSelection || mGameStateMachine.CurrentState == EGameFlowState.ShowingResult || mGameStateMachine.CurrentState == EGameFlowState.AwaitingHandInsertion || mGameStateMachine.CurrentState == EGameFlowState.AnswerPaused || mGameStateMachine.CurrentState == EGameFlowState.Answering);

            mGameView.UpdatePointerVisual(shouldShowPointer, pointerScreenPosition);
        }

        private bool updatePointerActivationGuard(Vector2? pointerScreenPosition)
        {
            if (pointerScreenPosition.HasValue == false)
            {
                mWasPointerAvailableLastFrame = false;
                mPointerReacquireGuardRemainingSeconds = 0.0f;
                resetPointerActivationDwellState();
                return false;
            }

            if (mWasPointerAvailableLastFrame == false)
            {
                mPointerReacquireGuardRemainingSeconds = POINTER_REACQUIRE_GUARD_SECONDS;
                resetPointerActivationDwellState();
            }

            mWasPointerAvailableLastFrame = true;

            if (mPointerReacquireGuardRemainingSeconds <= 0.0f)
            {
                return true;
            }

            mPointerReacquireGuardRemainingSeconds = Mathf.Max(0.0f, mPointerReacquireGuardRemainingSeconds - Time.deltaTime);
            resetPointerActivationDwellState();
            return false;
        }

        private void resetPointerActivationDwellState()
        {
            mUiActionDwellSelectionTracker?.Reset();

            if (mGameStateMachine?.CurrentState == EGameFlowState.AwaitingCardSelection)
            {
                mGameStateMachine.ResetCardSelectionHover();
            }

            mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;
            mGameView?.UpdateCardHoverVisual(null, 0.0f);
            mGameView?.UpdateActionButtonHoverVisual(null, 0.0f);
        }

        private static void applyRuntimeCursorPresentation(bool isFocused)
        {
            Cursor.visible = isFocused == false;
            Cursor.lockState = CursorLockMode.None;
        }

        private static void restoreSystemCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
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
        }

        private void resetInteractionSelectionState()
        {
            mUiActionDwellSelectionTracker?.Reset();
            mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;
            mWasPointerAvailableLastFrame = false;
            mPointerReacquireGuardRemainingSeconds = 0.0f;
            mPointerPresentationOverrideRemainingSeconds = 0.0f;
            mPresentedPointerScreenPosition = null;
            mGameStateMachine?.ResetCardSelectionHover();
            mGameView.UpdateActionButtonHoverVisual(null, 0.0f);
        }

        private Vector2? getPresentedPointerScreenPosition(Vector2? pointerScreenPosition)
        {
            if (mPointerPresentationOverrideRemainingSeconds > 0.0f)
            {
                mPointerPresentationOverrideRemainingSeconds = Mathf.Max(0.0f, mPointerPresentationOverrideRemainingSeconds - Time.deltaTime);
                mPresentedPointerScreenPosition = getBottomCenterPointerScreenPosition();
                return mPresentedPointerScreenPosition;
            }

            if (pointerScreenPosition.HasValue == false)
            {
                mPresentedPointerScreenPosition = null;
                return null;
            }

            if (mPresentedPointerScreenPosition.HasValue == false)
            {
                mPresentedPointerScreenPosition = pointerScreenPosition.Value;
                return mPresentedPointerScreenPosition;
            }

            float followProgress = 1.0f - Mathf.Exp(-POINTER_PRESENTATION_FOLLOW_RATE * Time.deltaTime);
            mPresentedPointerScreenPosition = Vector2.Lerp(mPresentedPointerScreenPosition.Value, pointerScreenPosition.Value, followProgress);
            return mPresentedPointerScreenPosition;
        }

        private void beginBottomCenterPointerSettle(float durationSeconds = POST_CARD_SELECTION_POINTER_SETTLE_SECONDS)
        {
            mPointerPresentationOverrideRemainingSeconds = Mathf.Max(0.0f, durationSeconds);
            mPresentedPointerScreenPosition = getBottomCenterPointerScreenPosition();
            warpSystemPointerToBottomCenter();
            mWasPointerAvailableLastFrame = false;
            mPointerReacquireGuardRemainingSeconds = 0.0f;
            resetPointerActivationDwellState();
        }

        private static Vector2 getBottomCenterPointerScreenPosition()
        {
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.08f);
        }

        private static void warpSystemPointerToBottomCenter()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse currentMouse = Mouse.current;

            if (currentMouse != null)
            {
                currentMouse.WarpCursorPosition(getBottomCenterPointerScreenPosition());
            }
#endif
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
            if (isPresentationCaptureEnabled())
            {
                return new DeterministicAnswerAnalysisClient();
            }

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
            if (isPresentationCaptureEnabled())
            {
                return new KeyboardHandInputAdapter();
            }

            return new CompositeHandInteractionInputAdapter(new LeapHandInputAdapter(), new KeyboardHandInputAdapter());
        }

        private IAnswerCaptureInputAdapter createAnswerCaptureInputAdapter()
        {
            if (isPresentationCaptureEnabled())
            {
                return new PresentationCaptureAnswerInputAdapter();
            }

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
            if (isPresentationCaptureEnabled())
            {
                return new NoOpFaceCaptureInputAdapter();
            }

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

        private bool isPresentationCaptureEnabled()
        {
            string rawValue = Environment.GetEnvironmentVariable(PRESENTATION_CAPTURE_ENVIRONMENT_VARIABLE_NAME);
            bool isEnvironmentEnabled = string.Equals(rawValue, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase);
            return isEnvironmentEnabled
                || Array.Exists(Environment.GetCommandLineArgs(), argument =>
                    string.Equals(argument, PRESENTATION_CAPTURE_ARGUMENT_NAME, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerator runPresentationCaptureSequenceCoroutine()
        {
            mIsPresentationCaptureRunning = true;

            string outputDirectoryPath = Environment.GetEnvironmentVariable(PRESENTATION_CAPTURE_OUTPUT_DIRECTORY_ENVIRONMENT_VARIABLE_NAME);

            if (string.IsNullOrWhiteSpace(outputDirectoryPath))
            {
                outputDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "presentation-captures");
            }

            Directory.CreateDirectory(outputDirectoryPath);
            Debug.Log($"Presentation capture sequence started. Output: {outputDirectoryPath}");

            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "01_start.png");

            mGameStateMachine.StartGame();
            resetInteractionSelectionState();

            Task firstRunTutorialTask = mGameView.PlayFirstRunTutorialAsync();
            yield return waitForRealtimeSecondsCoroutine(1.4f);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "02_first_run_tutorial.png");
            yield return waitForTaskCoroutine(firstRunTutorialTask);

            if (tryLogTaskFailure(firstRunTutorialTask))
            {
                yield return finalizePresentationCaptureCoroutine();
                yield break;
            }

            Task approachToCardSelectionTask = mGameView.PlayTempleApproachToCardSelectionAsync();
            yield return waitForRealtimeSecondsCoroutine(2.8f);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "03_temple_approach.png");
            yield return waitForTaskCoroutine(approachToCardSelectionTask);

            if (tryLogTaskFailure(approachToCardSelectionTask))
            {
                yield return finalizePresentationCaptureCoroutine();
                yield break;
            }

            mGameView.ShowCardSelection(mGameStateMachine.CreateSnapshot().CurrentRoundSelection);
            Task cardEntranceTask = mGameView.PlayCardSelectionEntranceAsync();
            yield return waitForTaskCoroutine(cardEntranceTask);

            if (tryLogTaskFailure(cardEntranceTask))
            {
                yield return finalizePresentationCaptureCoroutine();
                yield break;
            }

            mGameStateMachine.MarkCardPresentationCompleted();
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "04_cards.png");

            Task<EQuestionCardSlot> dwellSelectionTask = dwellSelectPresentationCardAsync(EQuestionCardSlot.CenterCard);
            yield return waitForTaskCoroutine(dwellSelectionTask);

            if (tryLogTaskFailure(dwellSelectionTask))
            {
                yield return finalizePresentationCaptureCoroutine();
                yield break;
            }

            EQuestionCardSlot confirmedQuestionCardSlot = dwellSelectionTask.Result;
            mGameView.PreviewCardSelectionFocus(confirmedQuestionCardSlot);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "05_card_focus.png");

            GameSessionSnapshot snapshot = mGameStateMachine.CreateSnapshot();
            QuestionDefinition selectedQuestionDefinition = snapshot.CurrentRoundSelection.QuestionsBySlot[confirmedQuestionCardSlot];
            Task revealQuestionTask = mGameView.PlayQuestionRevealAsync(confirmedQuestionCardSlot, selectedQuestionDefinition, () => mQuestionNarrationService.SpeakQuestionAsync(selectedQuestionDefinition, mLifecycleCancellationTokenSource.Token));
            yield return waitForRealtimeSecondsCoroutine(2.65f);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "06_card_question.png");
            yield return waitForTaskCoroutine(revealQuestionTask);

            if (tryLogTaskFailure(revealQuestionTask))
            {
                yield return finalizePresentationCaptureCoroutine();
                yield break;
            }

            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "07_card_launch_complete.png");

            Task prepareBackdropTask = mGameView.PrepareTempleGameplayBackdropAsync();
            yield return waitForTaskCoroutine(prepareBackdropTask);

            if (tryLogTaskFailure(prepareBackdropTask))
            {
                yield return finalizePresentationCaptureCoroutine();
                yield break;
            }

            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "08_mouth_arrival.png");

            mGameStateMachine.MarkQuestionRevealCompleted();
            mGameStateMachine.MarkQuestionNarrationCompleted();
            mGameView.ShowAwaitingHandInsertion();
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "09_hand_prompt.png");
            yield return waitForRealtimeSecondsCoroutine(mGameView.HandPromptPanelHoldDurationSeconds + PRESENTATION_CAPTURE_HAND_INSERTION_EXTRA_DELAY_SECONDS);

            Task handInsertionTask = mGameView.AnimateHandInsertionAsync();
            yield return waitForTaskCoroutine(handInsertionTask);

            if (tryLogTaskFailure(handInsertionTask))
            {
                yield return finalizePresentationCaptureCoroutine();
                yield break;
            }

            mGameView.ShowAnswering();
            yield return waitForRealtimeSecondsCoroutine(3.0f);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "10_answering.png");

            mGameView.ShowAnalyzing();
            yield return waitForRealtimeSecondsCoroutine(mGameView.AnalysisFocusRampDurationSeconds + 0.35f);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "11_analyzing.png");

            mGameView.ShowResult(EVerdictKind.True, string.Empty);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "12_result_true.png");

            mGameView.ShowResult(EVerdictKind.False, string.Empty);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "13_result_false.png");

            mGameView.ShowResult(EVerdictKind.Uncertain, string.Empty);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "14_result_uncertain.png");
            Debug.Log("Presentation capture sequence completed.");
            yield return finalizePresentationCaptureCoroutine();
        }

        private IEnumerator waitForTaskCoroutine(Task task)
        {
            while (task.IsCompleted == false)
            {
                yield return null;
            }
        }

        private bool tryLogTaskFailure(Task task)
        {
            if (task.IsFaulted == false)
            {
                return false;
            }

            Exception taskException = task.Exception;
            if (task.Exception != null)
            {
                taskException = task.Exception.GetBaseException();
            }

            Debug.LogException(taskException);
            return true;
        }

        private IEnumerator waitForPresentationFrameCoroutine()
        {
            Debug.Log("Presentation capture waiting for render frames.");

            for (int frameIndex = 0; frameIndex < 24; frameIndex++)
            {
                yield return null;
            }

            Debug.Log("Presentation capture render frame wait completed.");
        }

        private IEnumerator waitForRealtimeSecondsCoroutine(float durationSeconds)
        {
            if (durationSeconds <= 0.0f)
            {
                yield break;
            }

            float targetTimeSeconds = Time.unscaledTime + durationSeconds;

            while (Time.unscaledTime < targetTimeSeconds)
            {
                yield return null;
            }
        }

        private IEnumerator captureScreenshotCoroutine(string outputDirectoryPath, string fileName)
        {
            string screenshotFilePath = Path.Combine(outputDirectoryPath, fileName);
            ScreenCapture.CaptureScreenshot(screenshotFilePath, superSize: 1);
            float timeoutAtSeconds = Time.unscaledTime + 2.0f;

            while (File.Exists(screenshotFilePath) == false && Time.unscaledTime < timeoutAtSeconds)
            {
                yield return null;
            }

            yield return waitForRealtimeSecondsCoroutine(0.2f);
        }

        private IEnumerator finalizePresentationCaptureCoroutine()
        {
            mIsPresentationCaptureRunning = false;
            yield return waitForRealtimeSecondsCoroutine(0.5f);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private async Task<EQuestionCardSlot> dwellSelectPresentationCardAsync(EQuestionCardSlot questionCardSlot)
        {
            Vector2 cardScreenCenter = mGameView.GetQuestionCardScreenCenter(questionCardSlot);
            EQuestionCardSlot? confirmedQuestionCardSlot = null;
            float elapsedSeconds = 0.0f;

            while (elapsedSeconds < CARD_SELECTION_DWELL_SECONDS && confirmedQuestionCardSlot.HasValue == false)
            {
                float deltaSeconds = Mathf.Max(Time.deltaTime, 1.0f / 60.0f);
                elapsedSeconds = Mathf.Min(CARD_SELECTION_DWELL_SECONDS, elapsedSeconds + deltaSeconds);
                mGameView.UpdatePointerVisual(true, cardScreenCenter);
                mGameView.UpdateCardHoverVisual(questionCardSlot, Mathf.Clamp01(elapsedSeconds / CARD_SELECTION_DWELL_SECONDS));
                confirmedQuestionCardSlot = mGameStateMachine.UpdateCardSelection(questionCardSlot, deltaSeconds);
                await Task.Yield();
            }

            if (confirmedQuestionCardSlot.HasValue == false)
            {
                confirmedQuestionCardSlot = mGameStateMachine.UpdateCardSelection(questionCardSlot, 1.0f / 60.0f);
            }

            if (confirmedQuestionCardSlot.HasValue == false)
            {
                throw new InvalidOperationException("Presentation capture could not confirm the center card.");
            }

            mGameView.UpdatePointerVisual(true, cardScreenCenter);
            mGameView.UpdateCardHoverVisual(questionCardSlot, 1.0f);
            return confirmedQuestionCardSlot.Value;
        }

    }
}
