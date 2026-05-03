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
        private const float ANSWER_HOLD_LOSS_GRACE_SECONDS = 0.35f;
        private const string PRESENTATION_CAPTURE_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_PRESENTATION_CAPTURE";
        private const string PRESENTATION_CAPTURE_OUTPUT_DIRECTORY_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_CAPTURE_OUTPUT_DIR";

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
        private float mAnswerHoldLossElapsedSeconds;
        private string mLastObservedTranscript = string.Empty;
        private EHandAnchorState mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;

        private async void Start()
        {
            mLifecycleCancellationTokenSource = new CancellationTokenSource();
            mGameView = GetComponent<MouthOfTruthGameView>() ?? gameObject.AddComponent<MouthOfTruthGameView>();
            await mGameView.InitializeAsync();
            applyRuntimeCursorPresentation(isFocused: true);

            if (isPresentationCaptureEnabled() == false)
            {
                await requestCaptureAuthorizationsAsync();
            }

            initializeStateMachine();
            mIsInitialized = true;

            if (isPresentationCaptureEnabled())
            {
                _ = runPresentationCaptureSequenceAsync();
            }
        }

        private void Update()
        {
            if (mIsInitialized == false || mIsTransitionBusy || mIsPresentationCaptureRunning)
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

            if (mGameView.ConsumeExitRequested())
            {
                requestApplicationExit();
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
            string questionPoolFilePath = System.IO.Path.Combine(
                Application.streamingAssetsPath,
                "questions",
                "question_pool.json");
            IReadOnlyList<QuestionDefinition> questionDefinitions = QuestionPoolLoader.LoadQuestionDefinitions(questionPoolFilePath);
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
            EQuestionCardSlot? hoveredQuestionCardSlot = mGameView.GetHoveredQuestionCardSlot(pointerScreenPosition);
            EQuestionCardSlot? confirmedQuestionCardSlot = mGameStateMachine.UpdateCardSelection(
                hoveredQuestionCardSlot,
                Time.deltaTime);
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
            bool isAnswerHoldMaintained = mGameView.IsAnswerHoldMaintained(pointerScreenPosition);

            if (isAnswerHoldMaintained == false)
            {
                mAnswerHoldLossElapsedSeconds += Time.deltaTime;

                if (mAnswerHoldLossElapsedSeconds >= ANSWER_HOLD_LOSS_GRACE_SECONDS)
                {
                    mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;
                    _ = pauseAnswerAsync();
                    return;
                }
            }
            else
            {
                mAnswerHoldLossElapsedSeconds = 0.0f;
            }

            mLastObservedHandAnchorState = handAnchorState == EHandAnchorState.OutsideMouth
                ? EHandAnchorState.AtFrontAnchor
                : handAnchorState;

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
                && mGameStateMachine.CurrentState != EGameFlowState.AwaitingCardSelection
                && mGameStateMachine.CurrentState != EGameFlowState.ShowingResult)
            {
                mUiActionDwellSelectionTracker?.Reset();
                mGameView.UpdateActionButtonHoverVisual(null, 0.0f);
                return false;
            }

            EUiActionTarget? hoveredUiActionTarget = mGameView.GetHoveredUiActionTarget(pointerScreenPosition);
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

                case EUiActionTarget.ExitGame:
                    requestApplicationExit();
                    return true;

                default:
                    return false;
            }
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
            bool shouldShowPointer = pointerScreenPosition.HasValue
                && (
                    mGameStateMachine.CurrentState == EGameFlowState.StartScreen
                    || mGameStateMachine.CurrentState == EGameFlowState.AwaitingCardSelection
                    || mGameStateMachine.CurrentState == EGameFlowState.ShowingResult
                    || mGameStateMachine.CurrentState == EGameFlowState.AwaitingHandInsertion
                    || mGameStateMachine.CurrentState == EGameFlowState.AnswerPaused);

            mGameView.UpdatePointerVisual(shouldShowPointer, pointerScreenPosition);
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

        private async Task revealQuestionAsync(
            EQuestionCardSlot selectedQuestionCardSlot,
            QuestionDefinition selectedQuestionDefinition)
        {
            mIsTransitionBusy = true;
            mGameView.UpdatePointerVisual(false, null);
            mGameView.UpdateActionButtonHoverVisual(null, 0.0f);
            await mGameView.PlayQuestionRevealAsync(selectedQuestionCardSlot, selectedQuestionDefinition);
            mGameStateMachine.MarkQuestionRevealCompleted();
            mGameView.ShowNarratingQuestion(selectedQuestionDefinition.Text);
            await mQuestionNarrationService.SpeakQuestionAsync(
                selectedQuestionDefinition,
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
            mGameView.UpdatePointerVisual(false, null);
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
            mAnswerHoldLossElapsedSeconds = 0.0f;
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
            mAnswerHoldLossElapsedSeconds = 0.0f;
            mIsTransitionBusy = false;
        }

        private async Task analyzeAnswerAsync()
        {
            mIsTransitionBusy = true;
            mGameView.ShowAnalyzing();
            GameSessionSnapshot snapshot = mGameStateMachine.CreateSnapshot();
            Task<AnswerCaptureResult> answerCaptureTask = mAnswerCaptureInputAdapter.CompleteCollectionAsync(
                snapshot.SelectedQuestionDefinition?.ID,
                mLifecycleCancellationTokenSource.Token);
            Task<FaceCaptureResult> faceCaptureTask = mFaceCaptureInputAdapter.CompleteCollectionAsync(
                mLifecycleCancellationTokenSource.Token);
            await Task.WhenAll(answerCaptureTask, faceCaptureTask);
            AnswerCaptureResult answerCaptureResult = answerCaptureTask.Result;
            FaceCaptureResult faceCaptureResult = faceCaptureTask.Result;

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
            mAnswerHoldLossElapsedSeconds = 0.0f;
        }

        private void resetInteractionSelectionState()
        {
            mUiActionDwellSelectionTracker?.Reset();
            mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;
            mAnswerHoldLossElapsedSeconds = 0.0f;
            mGameView.UpdateActionButtonHoverVisual(null, 0.0f);
        }

        private IQuestionNarrationService createNarrationService()
        {
            IQuestionNarrationService fallbackNarrationService = Application.platform == RuntimePlatform.OSXEditor
                || Application.platform == RuntimePlatform.OSXPlayer
                ? new MacOsQuestionNarrationService()
                : new SilentQuestionNarrationService();

            string questionAudioDirectoryPath = Path.Combine(
                Application.streamingAssetsPath,
                "audio",
                "questions");

            return new PrerecordedQuestionNarrationService(
                questionAudioDirectoryPath,
                fallbackNarrationService);
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

        private bool isPresentationCaptureEnabled()
        {
            string rawValue = Environment.GetEnvironmentVariable(
                PRESENTATION_CAPTURE_ENVIRONMENT_VARIABLE_NAME);
            return string.Equals(rawValue, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase);
        }

        private async Task runPresentationCaptureSequenceAsync()
        {
            mIsPresentationCaptureRunning = true;

            try
            {
                string outputDirectoryPath =
                    Environment.GetEnvironmentVariable(PRESENTATION_CAPTURE_OUTPUT_DIRECTORY_ENVIRONMENT_VARIABLE_NAME);

                if (string.IsNullOrWhiteSpace(outputDirectoryPath))
                {
                    outputDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "presentation-captures");
                }

                Directory.CreateDirectory(outputDirectoryPath);

                await waitForPresentationFrameAsync();
                await captureScreenshotAsync(outputDirectoryPath, "01_start.png");

                await startGameAsync();
                await waitForPresentationFrameAsync();
                await captureScreenshotAsync(outputDirectoryPath, "02_cards.png");

                mGameView.PreviewCardSelectionFocus(EQuestionCardSlot.CenterCard);
                await waitForPresentationFrameAsync();
                await captureScreenshotAsync(outputDirectoryPath, "03_card_focus.png");

                GameSessionSnapshot snapshot = mGameStateMachine.CreateSnapshot();
                QuestionDefinition selectedQuestionDefinition =
                    snapshot.CurrentRoundSelection.QuestionsBySlot[EQuestionCardSlot.CenterCard];
                await mGameView.PlayQuestionRevealAsync(EQuestionCardSlot.CenterCard, selectedQuestionDefinition);
                await waitForPresentationFrameAsync();
                await captureScreenshotAsync(outputDirectoryPath, "04_card_launch.png");

                mGameStateMachine.UpdateCardSelection(EQuestionCardSlot.CenterCard, 0.7f);
                mGameStateMachine.MarkQuestionRevealCompleted();
                mGameView.ShowNarratingQuestion(selectedQuestionDefinition.Text);
                await waitForPresentationFrameAsync();
                await captureScreenshotAsync(outputDirectoryPath, "05_question_narration.png");

                mGameStateMachine.MarkQuestionNarrationCompleted();
                mGameView.ShowAwaitingHandInsertion();
                await waitForPresentationFrameAsync();
                await captureScreenshotAsync(outputDirectoryPath, "06_hand_prompt.png");

                await mGameView.AnimateHandInsertionAsync();
                mGameView.ShowAnswering();
                await waitForPresentationFrameAsync();
                await captureScreenshotAsync(outputDirectoryPath, "07_answering.png");

                mGameView.ShowResult(EVerdictKind.True, string.Empty);
                await waitForPresentationFrameAsync();
                await captureScreenshotAsync(outputDirectoryPath, "08_result_true.png");

                mGameView.ShowResult(EVerdictKind.False, string.Empty);
                await waitForPresentationFrameAsync();
                await captureScreenshotAsync(outputDirectoryPath, "09_result_false.png");

                mGameView.ShowResult(EVerdictKind.Uncertain, string.Empty);
                await waitForPresentationFrameAsync();
                await captureScreenshotAsync(outputDirectoryPath, "10_result_uncertain.png");
            }
            finally
            {
                mIsPresentationCaptureRunning = false;
                await Task.Delay(500);

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        private async Task waitForPresentationFrameAsync()
        {
            await Task.Yield();
            await Task.Delay(420);
        }

        private async Task captureScreenshotAsync(string outputDirectoryPath, string fileName)
        {
            string screenshotFilePath = Path.Combine(outputDirectoryPath, fileName);
            ScreenCapture.CaptureScreenshot(screenshotFilePath, superSize: 1);
            await Task.Delay(520);
        }
    }
}
