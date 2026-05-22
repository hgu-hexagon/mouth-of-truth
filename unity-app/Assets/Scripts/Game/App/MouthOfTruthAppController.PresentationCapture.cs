using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using MouthOfTruth.Game.Analysis;
using MouthOfTruth.Game.Data;
using MouthOfTruth.Game.Session;
using UnityEngine;

namespace MouthOfTruth.Game.App
{
    public partial class MouthOfTruthAppController
    {
        private const float PRESENTATION_CAPTURE_CARD_ABSORPTION_PROGRESS = 0.82f;
        private const float PRESENTATION_CAPTURE_HAND_INSERTION_EXTRA_DELAY_SECONDS = 1.2f;
        private const float PRESENTATION_CAPTURE_HAND_INSERTION_SECONDS = 0.92f;
        private const string PRESENTATION_CAPTURE_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_PRESENTATION_CAPTURE";
        private const string PRESENTATION_CAPTURE_OUTPUT_DIRECTORY_ENVIRONMENT_VARIABLE_NAME = "MOUTH_OF_TRUTH_CAPTURE_OUTPUT_DIR";
        private const string PRESENTATION_CAPTURE_ARGUMENT_NAME = "-presentation-capture";

        private bool isPresentationCaptureEnabled()
        {
            string rawValue = Environment.GetEnvironmentVariable(PRESENTATION_CAPTURE_ENVIRONMENT_VARIABLE_NAME);
            bool isEnvironmentEnabled = string.Equals(rawValue, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase);
            return isEnvironmentEnabled || Array.Exists(Environment.GetCommandLineArgs(), argument => string.Equals(argument, PRESENTATION_CAPTURE_ARGUMENT_NAME, StringComparison.OrdinalIgnoreCase));
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
            yield return waitForCardAbsorptionProgressCoroutine(revealQuestionTask, PRESENTATION_CAPTURE_CARD_ABSORPTION_PROGRESS);

            if (tryLogTaskFailure(revealQuestionTask))
            {
                yield return finalizePresentationCaptureCoroutine();
                yield break;
            }

            yield return captureScreenshotCoroutine(outputDirectoryPath, "07_card_absorption.png");
            yield return waitForTaskCoroutine(revealQuestionTask);

            if (tryLogTaskFailure(revealQuestionTask))
            {
                yield return finalizePresentationCaptureCoroutine();
                yield break;
            }

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
            yield return waitForRealtimeSecondsCoroutine(PRESENTATION_CAPTURE_HAND_INSERTION_SECONDS);
            yield return captureScreenshotCoroutine(outputDirectoryPath, "10_hand_insertion.png");
            yield return waitForTaskCoroutine(handInsertionTask);

            if (tryLogTaskFailure(handInsertionTask))
            {
                yield return finalizePresentationCaptureCoroutine();
                yield break;
            }

            mGameView.ShowAnswering();
            yield return waitForRealtimeSecondsCoroutine(mGameView.AnswerBeamSweepCycleDurationSeconds);
            yield return captureScreenshotCoroutine(outputDirectoryPath, "11_answering.png");

            mGameView.ShowAnalyzing();
            yield return waitForRealtimeSecondsCoroutine(mGameView.AnalysisFocusRampDurationSeconds + 0.35f);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "12_analyzing.png");

            mGameView.ShowResult(EVerdictKind.True, string.Empty);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "13_result_true.png");

            mGameView.ShowResult(EVerdictKind.False, string.Empty);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "14_result_false.png");

            mGameView.ShowResult(EVerdictKind.Uncertain, string.Empty);
            yield return waitForPresentationFrameCoroutine();
            yield return captureScreenshotCoroutine(outputDirectoryPath, "15_result_uncertain.png");
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

        private IEnumerator waitForCardAbsorptionProgressCoroutine(Task revealQuestionTask, float targetProgress)
        {
            while (revealQuestionTask.IsCompleted == false)
            {
                if (mGameView.IsCardAbsorptionPresentationActive && mGameView.CardAbsorptionPresentationProgress >= targetProgress)
                {
                    yield break;
                }

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
            EQuestionCardSlot? confirmedQuestionCardSlot = null;
            float elapsedSeconds = 0.0f;

            while (elapsedSeconds < CARD_SELECTION_DWELL_SECONDS && confirmedQuestionCardSlot.HasValue == false)
            {
                float deltaSeconds = Mathf.Max(Time.deltaTime, 1.0f / 60.0f);
                elapsedSeconds = Mathf.Min(CARD_SELECTION_DWELL_SECONDS, elapsedSeconds + deltaSeconds);
                mGameView.UpdatePointerVisual(false, null);
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

            mGameView.UpdatePointerVisual(false, null);
            mGameView.UpdateCardHoverVisual(questionCardSlot, 1.0f);
            return confirmedQuestionCardSlot.Value;
        }
    }
}
