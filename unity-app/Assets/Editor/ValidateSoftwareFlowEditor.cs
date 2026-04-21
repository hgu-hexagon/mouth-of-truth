using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MouthOfTruth.Game.Analysis;
using MouthOfTruth.Game.Data;
using MouthOfTruth.Game.Input;
using MouthOfTruth.Game.Presentation.Runtime;
using MouthOfTruth.Game.Session;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MouthOfTruth.Editor
{
    public static class ValidateSoftwareFlowEditor
    {
        private const string QUESTION_POOL_RELATIVE_PATH = "questions/question_pool.json";

        [MenuItem("Mouth Of Truth/Validate Software Flow")]
        public static void Run()
        {
            List<string> errors = new List<string>();

            validateStep("question deck cycling", validateQuestionDeckCycle, errors);
            validateStep("state machine flow", validateStateMachineFlow, errors);
            validateStep("answer timeout behavior", validateAnswerTimeoutFlow, errors);
            validateStep("mouth hand anchor targeting", validateHandAnchorTargeting, errors);
            validateStep("deterministic analysis", validateDeterministicAnalysis, errors);
            validateStep("unity python bridge round trip", validatePythonBridgeRoundTrip, errors);

            if (errors.Count > 0)
            {
                string errorMessage = "Mouth of Truth software flow validation failed:\n- "
                    + string.Join("\n- ", errors);
                throw new BuildFailedException(errorMessage);
            }

            Debug.Log(
                "Mouth of Truth software flow validation succeeded. "
                + "Question selection, answer pause/resume, timeout, "
                + "and deterministic verdict flow are healthy.");
        }

        private static void validateQuestionDeckCycle()
        {
            IReadOnlyList<QuestionDefinition> questionDefinitions = loadQuestionDefinitions();
            int enabledQuestionCount = questionDefinitions.Count(
                questionDefinition => questionDefinition.IsEnabled);
            QuestionDeckService questionDeckService =
                new QuestionDeckService(questionDefinitions, randomSeed: 1234);
            HashSet<string> seenQuestionIDs = new HashSet<string>(StringComparer.Ordinal);
            int expectedUniqueRoundCount = enabledQuestionCount / 3;

            for (int roundIndex = 0; roundIndex < expectedUniqueRoundCount; roundIndex += 1)
            {
                QuestionRoundSelection questionRoundSelection = questionDeckService.DrawNextRound();

                foreach (
                    QuestionDefinition questionDefinition
                    in questionRoundSelection.QuestionsBySlot.Values)
                {
                    if (seenQuestionIDs.Add(questionDefinition.ID) == false)
                    {
                        throw new InvalidOperationException(
                            $"Question {questionDefinition.ID} repeated "
                            + "before the enabled pool was exhausted.");
                    }
                }
            }

            if (seenQuestionIDs.Count != enabledQuestionCount)
            {
                throw new InvalidOperationException(
                    $"Expected to see {enabledQuestionCount} unique questions "
                    + $"before recycling, but saw {seenQuestionIDs.Count}.");
            }

            QuestionRoundSelection recycledRoundSelection = questionDeckService.DrawNextRound();

            if (recycledRoundSelection.QuestionsBySlot.Count != 3)
            {
                throw new InvalidOperationException("A recycled round did not contain three cards.");
            }
        }

        private static void validateStateMachineFlow()
        {
            MouthOfTruthGameStateMachine gameStateMachine = buildStateMachine(randomSeed: 4321);

            gameStateMachine.OpenStartScreen();
            assertState(gameStateMachine, EGameFlowState.StartScreen, "start screen");

            gameStateMachine.StartGame();
            assertState(gameStateMachine, EGameFlowState.PresentingCards, "card presentation");
            assertCondition(
                gameStateMachine.CreateSnapshot().CurrentRoundSelection != null,
                "The first round selection was not created.");

            gameStateMachine.MarkCardPresentationCompleted();
            assertState(
                gameStateMachine,
                EGameFlowState.AwaitingCardSelection,
                "awaiting card selection");

            EQuestionCardSlot? earlySelection = gameStateMachine.UpdateCardSelection(
                EQuestionCardSlot.CenterCard,
                0.35f);

            if (earlySelection != null)
            {
                throw new InvalidOperationException("Card selection confirmed before dwell time was complete.");
            }

            GameSessionSnapshot hoverSnapshot = gameStateMachine.CreateSnapshot();
            assertCondition(
                hoverSnapshot.HoveredCardDwellSeconds > 0.0f
                && hoverSnapshot.HoveredCardDwellSeconds < 0.7f,
                "Card hover dwell did not accumulate correctly.");

            EQuestionCardSlot? confirmedSelection = gameStateMachine.UpdateCardSelection(
                EQuestionCardSlot.CenterCard,
                0.35f);

            if (confirmedSelection != EQuestionCardSlot.CenterCard)
            {
                throw new InvalidOperationException("Card selection did not confirm the centered card.");
            }

            assertState(gameStateMachine, EGameFlowState.RevealingQuestionCard, "question reveal");
            assertCondition(
                gameStateMachine.CreateSnapshot().SelectedQuestionDefinition != null,
                "Selected question definition was not stored.");

            gameStateMachine.MarkQuestionRevealCompleted();
            assertState(gameStateMachine, EGameFlowState.NarratingQuestion, "question narration");

            gameStateMachine.MarkQuestionNarrationCompleted();
            assertState(
                gameStateMachine,
                EGameFlowState.AwaitingHandInsertion,
                "awaiting hand insertion");

            gameStateMachine.NotifyHandReachedFrontAnchor();
            assertState(gameStateMachine, EGameFlowState.InsertingHand, "hand insertion animation");

            gameStateMachine.NotifyHandReachedInnerAnchor();
            assertState(gameStateMachine, EGameFlowState.Answering, "answering");

            gameStateMachine.UpdateAnswerTranscript("spoken answer");
            assertCondition(
                gameStateMachine.CreateSnapshot().CurrentAnswerTranscript == "spoken answer",
                "Answer transcript was not stored.");

            bool shouldFinishAnswer = gameStateMachine.AdvanceAnswerCollection(
                1.0f,
                isSpeechDetected: true);

            if (shouldFinishAnswer)
            {
                throw new InvalidOperationException("Answer finished while speech was still active.");
            }

            GameSessionSnapshot answeringSnapshot = gameStateMachine.CreateSnapshot();
            assertCondition(
                Mathf.Abs(answeringSnapshot.ElapsedAnswerSeconds - 1.0f) < 0.001f,
                "Elapsed answer time did not advance.");
            assertCondition(
                Mathf.Abs(answeringSnapshot.ElapsedSilenceSeconds) < 0.001f,
                "Silence timer did not reset during active speech.");

            gameStateMachine.NotifyHandExitedFrontAnchor();
            assertState(gameStateMachine, EGameFlowState.AnswerPaused, "answer pause");
            assertCondition(
                gameStateMachine.CreateSnapshot().IsAnswerPaused,
                "Pause snapshot flag was not set.");

            gameStateMachine.NotifyHandReachedInnerAnchor();
            assertState(gameStateMachine, EGameFlowState.Answering, "answer resume");

            shouldFinishAnswer = gameStateMachine.AdvanceAnswerCollection(
                1.9f,
                isSpeechDetected: false);

            if (shouldFinishAnswer == false)
            {
                throw new InvalidOperationException("Answer did not finish after silence timeout.");
            }

            assertState(gameStateMachine, EGameFlowState.AnalyzingAnswer, "analysis");

            gameStateMachine.CompleteAnalysis(
                new AnswerAnalysisResult(
                    EVerdictKind.True,
                    "spoken answer",
                    Array.Empty<string>()));
            assertState(gameStateMachine, EGameFlowState.ShowingResult, "result presentation");
            assertCondition(
                gameStateMachine.CreateSnapshot().CurrentVerdictKind == EVerdictKind.True,
                "Verdict kind was not stored in the result snapshot.");

            gameStateMachine.ReturnToStart();
            assertState(gameStateMachine, EGameFlowState.StartScreen, "return to start");
        }

        private static void validateAnswerTimeoutFlow()
        {
            MouthOfTruthGameStateMachine gameStateMachine = buildStateMachine(randomSeed: 9876);

            gameStateMachine.StartGame();
            gameStateMachine.MarkCardPresentationCompleted();
            gameStateMachine.UpdateCardSelection(EQuestionCardSlot.LeftCard, 0.7f);
            gameStateMachine.MarkQuestionRevealCompleted();
            gameStateMachine.MarkQuestionNarrationCompleted();
            gameStateMachine.NotifyHandReachedFrontAnchor();
            gameStateMachine.NotifyHandReachedInnerAnchor();

            bool timedOut = gameStateMachine.AdvanceAnswerCollection(
                8.1f,
                isSpeechDetected: true);

            if (timedOut == false)
            {
                throw new InvalidOperationException("Answer timeout did not trigger after 8 seconds.");
            }

            assertState(gameStateMachine, EGameFlowState.AnalyzingAnswer, "timeout analysis");

            gameStateMachine.CompleteAnalysis(
                new AnswerAnalysisResult(
                    EVerdictKind.False,
                    "timed answer",
                    Array.Empty<string>()));
            assertState(gameStateMachine, EGameFlowState.ShowingResult, "timeout result");

            gameStateMachine.TryAgain();
            assertState(gameStateMachine, EGameFlowState.PresentingCards, "try again");
            assertCondition(
                gameStateMachine.CreateSnapshot().CurrentRoundSelection != null,
                "Try Again did not prepare the next round.");
        }

        private static void validateDeterministicAnalysis()
        {
            DeterministicAnswerAnalysisClient deterministicAnswerAnalysisClient =
                new DeterministicAnswerAnalysisClient();
            QuestionDefinition questionDefinition =
                new QuestionDefinition("QTEST", "질문", "test", 1, true);

            AnswerAnalysisResult insufficientDataResult = runDeterministicAnalysis(
                deterministicAnswerAnalysisClient,
                questionDefinition,
                "typed answer",
                faceFrameCount: 0,
                voiceSegmentCount: 0);

            if (insufficientDataResult.VerdictKind != EVerdictKind.Uncertain)
            {
                throw new InvalidOperationException("Insufficient data should yield UNCERTAIN.");
            }

            if (insufficientDataResult.ReasonCodes.Contains("insufficient_face_data") == false
                || insufficientDataResult.ReasonCodes.Contains("insufficient_voice_data") == false)
            {
                throw new InvalidOperationException("Insufficient data reason codes were incomplete.");
            }

            AnswerAnalysisResult faceOnlyResult = runDeterministicAnalysis(
                deterministicAnswerAnalysisClient,
                questionDefinition,
                "face only answer",
                faceFrameCount: 6,
                voiceSegmentCount: 0);

            if (faceOnlyResult.VerdictKind != EVerdictKind.Uncertain)
            {
                throw new InvalidOperationException("Face-only data should remain UNCERTAIN until voice input is present.");
            }

            if (faceOnlyResult.ReasonCodes.Contains("insufficient_voice_data") == false)
            {
                throw new InvalidOperationException(
                    "Face-only deterministic analysis did not preserve the insufficient_voice_data reason code.");
            }

            AnswerAnalysisResult voiceOnlyResult = runDeterministicAnalysis(
                deterministicAnswerAnalysisClient,
                questionDefinition,
                "voice only answer",
                faceFrameCount: 0,
                voiceSegmentCount: 2);

            if (voiceOnlyResult.VerdictKind != EVerdictKind.Uncertain)
            {
                throw new InvalidOperationException("Voice-only data should remain UNCERTAIN without a face signal.");
            }

            if (voiceOnlyResult.ReasonCodes.Contains("insufficient_face_data") == false)
            {
                throw new InvalidOperationException(
                    "Voice-only deterministic analysis did not preserve the insufficient_face_data reason code.");
            }

            AnswerAnalysisResult transcriptFreeVoiceOnlyResult = runDeterministicAnalysis(
                deterministicAnswerAnalysisClient,
                questionDefinition,
                string.Empty,
                faceFrameCount: 0,
                voiceSegmentCount: 2);

            if (transcriptFreeVoiceOnlyResult.VerdictKind != EVerdictKind.Uncertain)
            {
                throw new InvalidOperationException("Transcript-free voice input should remain UNCERTAIN without a face signal.");
            }

            if (transcriptFreeVoiceOnlyResult.ReasonCodes.Contains(
                    "insufficient_face_data")
                == false)
            {
                throw new InvalidOperationException(
                    "Transcript-free voice input did not preserve the insufficient_face_data reason code.");
            }

            AnswerAnalysisResult firstStableResult = runDeterministicAnalysis(
                deterministicAnswerAnalysisClient,
                questionDefinition,
                "a",
                faceFrameCount: 6,
                voiceSegmentCount: 1);

            AnswerAnalysisResult secondStableResult = runDeterministicAnalysis(
                deterministicAnswerAnalysisClient,
                questionDefinition,
                "b",
                faceFrameCount: 6,
                voiceSegmentCount: 1);

            if (firstStableResult.VerdictKind == EVerdictKind.Uncertain
                || secondStableResult.VerdictKind == EVerdictKind.Uncertain)
            {
                throw new InvalidOperationException("Deterministic verdicts should resolve when counts are sufficient.");
            }

            if (firstStableResult.VerdictKind == secondStableResult.VerdictKind)
            {
                throw new InvalidOperationException("Deterministic verdicts should differ for stable parity-changing transcripts.");
            }
        }

        private static void validateHandAnchorTargeting()
        {
            Vector2 handFrontPosition = new Vector2(0.0f, 0.0f);
            Vector2 handInnerPosition = new Vector2(0.0f, 180.0f);
            const float MOUTH_DIAMETER_PIXELS = 420.0f;

            EHandAnchorState exactFrontState = MouthOfTruthGameView.EvaluateHandAnchorState(
                handFrontPosition,
                handFrontPosition,
                handInnerPosition,
                MOUTH_DIAMETER_PIXELS);
            EHandAnchorState exactInnerState = MouthOfTruthGameView.EvaluateHandAnchorState(
                handInnerPosition,
                handFrontPosition,
                handInnerPosition,
                MOUTH_DIAMETER_PIXELS);
            EHandAnchorState outsideState = MouthOfTruthGameView.EvaluateHandAnchorState(
                new Vector2(92.0f, 86.0f),
                handFrontPosition,
                handInnerPosition,
                MOUTH_DIAMETER_PIXELS);
            EHandAnchorState betweenAnchorsState = MouthOfTruthGameView.EvaluateHandAnchorState(
                new Vector2(0.0f, 84.0f),
                handFrontPosition,
                handInnerPosition,
                MOUTH_DIAMETER_PIXELS);
            bool answerHoldState = MouthOfTruthGameView.EvaluateAnswerHoldState(
                new Vector2(0.0f, 84.0f),
                handFrontPosition,
                handInnerPosition,
                MOUTH_DIAMETER_PIXELS);
            bool wideAnswerHoldState = MouthOfTruthGameView.EvaluateAnswerHoldState(
                new Vector2(92.0f, 86.0f),
                handFrontPosition,
                handInnerPosition,
                MOUTH_DIAMETER_PIXELS);

            if (exactFrontState != EHandAnchorState.AtFrontAnchor)
            {
                throw new InvalidOperationException("Front anchor targeting no longer resolves to AtFrontAnchor.");
            }

            if (exactInnerState != EHandAnchorState.AtInnerAnchor)
            {
                throw new InvalidOperationException("Inner anchor targeting no longer resolves to AtInnerAnchor.");
            }

            if (outsideState != EHandAnchorState.OutsideMouth)
            {
                throw new InvalidOperationException("Wide off-center pointer input is still accepted as a mouth hit.");
            }

            if (betweenAnchorsState != EHandAnchorState.OutsideMouth)
            {
                throw new InvalidOperationException("The corridor between the front and inner anchors is too wide for reliable targeting.");
            }

            if (answerHoldState == false)
            {
                throw new InvalidOperationException("Answer hold sustain no longer accepts a centered mouth-hold position.");
            }

            if (wideAnswerHoldState)
            {
                throw new InvalidOperationException("Answer hold sustain accepted a pointer that is too far off-center.");
            }
        }

        private static void validatePythonBridgeRoundTrip()
        {
            if (File.Exists(PythonAnalysisBridgePaths.GetBridgeLauncherScriptPath()) == false
                || Directory.Exists(PythonAnalysisBridgePaths.GetPythonModuleRootPath()) == false)
            {
                throw new InvalidOperationException("Python bridge runtime prerequisites are missing.");
            }

            PythonBridgeAnalysisClient pythonBridgeAnalysisClient =
                new PythonBridgeAnalysisClient();
            QuestionDefinition questionDefinition =
                new QuestionDefinition("QBRIDGE", "Bridge validation question", "test", 1, true);
            AnswerAnalysisResult bridgeAnalysisResult = runPythonBridgeAnalysis(
                pythonBridgeAnalysisClient,
                questionDefinition,
                "Bridge validation transcript",
                faceFrameCount: 0,
                voiceSegmentCount: 1);

            if (bridgeAnalysisResult.VerdictKind != EVerdictKind.Uncertain)
            {
                throw new InvalidOperationException("Python bridge should keep voice-only input UNCERTAIN without a face signal.");
            }

            if (bridgeAnalysisResult.AnswerTranscript != "Bridge validation transcript")
            {
                throw new InvalidOperationException("Python bridge did not preserve the provided transcript.");
            }

            if (bridgeAnalysisResult.ReasonCodes.Contains("insufficient_face_data") == false)
            {
                throw new InvalidOperationException("Python bridge did not surface the insufficient_face_data reason code.");
            }

            if (bridgeAnalysisResult.ReasonCodes.Contains("insufficient_voice_data") == false)
            {
                throw new InvalidOperationException("Python bridge should treat count-only voice input as insufficient evidence.");
            }

            AnswerAnalysisResult voiceMissingBridgeAnalysisResult = runPythonBridgeAnalysis(
                pythonBridgeAnalysisClient,
                questionDefinition,
                "Bridge validation transcript",
                faceFrameCount: 6,
                voiceSegmentCount: 0);

            if (voiceMissingBridgeAnalysisResult.VerdictKind != EVerdictKind.Uncertain)
            {
                throw new InvalidOperationException(
                    "Python bridge should keep the verdict UNCERTAIN "
                    + "until voice input is present.");
            }

            if (voiceMissingBridgeAnalysisResult.ReasonCodes.Contains("insufficient_voice_data")
                == false)
            {
                throw new InvalidOperationException("Python bridge did not surface the insufficient_voice_data reason code.");
            }

            if (voiceMissingBridgeAnalysisResult.ReasonCodes.Contains("insufficient_face_data")
                == false)
            {
                throw new InvalidOperationException("Python bridge should treat count-only face input as insufficient evidence.");
            }
        }

        private static MouthOfTruthGameStateMachine buildStateMachine(int randomSeed)
        {
            return new MouthOfTruthGameStateMachine(
                new QuestionDeckService(loadQuestionDefinitions(), randomSeed),
                new CardDwellSelectionTracker(),
                new AnswerCollectionPolicy());
        }

        private static IReadOnlyList<QuestionDefinition> loadQuestionDefinitions()
        {
            string questionPoolFilePath = Path.Combine(
                Application.streamingAssetsPath,
                QUESTION_POOL_RELATIVE_PATH);
            return QuestionPoolLoader.LoadQuestionDefinitions(questionPoolFilePath);
        }

        private static void validateStep(
            string stepName,
            Action validationAction,
            List<string> errors)
        {
            try
            {
                Debug.Log($"ValidateSoftwareFlow: starting '{stepName}'.");
                validationAction();
                Debug.Log($"ValidateSoftwareFlow: finished '{stepName}'.");
            }
            catch (Exception exception)
            {
                errors.Add($"{stepName}: {exception.Message}");
            }
        }

        private static AnswerAnalysisResult runDeterministicAnalysis(
            DeterministicAnswerAnalysisClient deterministicAnswerAnalysisClient,
            QuestionDefinition questionDefinition,
            string answerTranscript,
            int faceFrameCount,
            int voiceSegmentCount)
        {
            return deterministicAnswerAnalysisClient.AnalyzeAsync(
                new AnswerAnalysisRequest(
                    questionDefinition,
                    answerTranscript,
                    string.Empty,
                    string.Empty,
                    faceFrameCount,
                    voiceSegmentCount),
                CancellationToken.None).GetAwaiter().GetResult();
        }

        private static AnswerAnalysisResult runPythonBridgeAnalysis(
            PythonBridgeAnalysisClient pythonBridgeAnalysisClient,
            QuestionDefinition questionDefinition,
            string answerTranscript,
            int faceFrameCount,
            int voiceSegmentCount)
        {
            return pythonBridgeAnalysisClient.AnalyzeAsync(
                new AnswerAnalysisRequest(
                    questionDefinition,
                    answerTranscript,
                    string.Empty,
                    string.Empty,
                    faceFrameCount,
                    voiceSegmentCount),
                CancellationToken.None).GetAwaiter().GetResult();
        }

        private static void assertState(
            MouthOfTruthGameStateMachine gameStateMachine,
            EGameFlowState expectedState,
            string stepName)
        {
            assertCondition(
                gameStateMachine.CurrentState == expectedState,
                $"Expected {stepName} to be {expectedState}, "
                + $"but was {gameStateMachine.CurrentState}.");
        }

        private static void assertCondition(bool condition, string message)
        {
            if (condition == false)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
