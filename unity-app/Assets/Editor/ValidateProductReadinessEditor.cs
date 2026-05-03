using System.Collections.Generic;
using System.IO;
using MouthOfTruth.Game.Analysis;
using MouthOfTruth.Game.App;
using MouthOfTruth.Game.Data;
using MouthOfTruth.Game.Presentation;
using MouthOfTruth.Game.Presentation.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MouthOfTruth.Editor
{
    public static class ValidateProductReadinessEditor
    {
        private const string MAIN_SCENE_PATH = "Assets/Scenes/Main.unity";
        private const string DUNGEON_ROOT_PATH = "Assets/ThirdParty/Environment/DungeonModularPack";
        private const string CARPET_ROOT_PATH = "Assets/ThirdParty/Environment/PersianCarpetUrp";
        private const string UI_FONT_PATH = "Assets/Resources/Fonts/JuliusSansOne-Regular.ttf";
        private const string KOREAN_FALLBACK_FONT_PATH = "Assets/Resources/Fonts/NotoSansCJKkr-Regular.otf";

        [MenuItem("Mouth Of Truth/Validate Product Readiness")]
        public static void Run()
        {
            BuildMainSceneEditor.Run();

            if (shouldRegeneratePresentationBackgrounds())
            {
                GeneratePresentationBackgroundsEditor.Run();
            }
            else
            {
                Debug.Log(
                    "Skipping presentation background regeneration because the current Unity process "
                    + "is running with a null graphics device.");
            }

            List<string> errors = new List<string>();

            validateAssetPath(MAIN_SCENE_PATH, errors);
            validateAssetPath(DUNGEON_ROOT_PATH, errors);
            validateAssetPath(CARPET_ROOT_PATH, errors);
            validateAssetPath(UI_FONT_PATH, errors);
            validateAssetPath(KOREAN_FALLBACK_FONT_PATH, errors);
            validateStreamingAssets(errors);
            validateQuestionPool(errors);
            validatePythonBridge(errors);
            validateRenderPipeline(errors);
            validateMainScene(errors);

            if (errors.Count > 0)
            {
                string errorMessage = "Mouth of Truth product readiness validation failed:\n- "
                    + string.Join("\n- ", errors);
                throw new BuildFailedException(errorMessage);
            }

            Debug.Log(
                "Mouth of Truth product readiness validation succeeded. "
                + "Scene, assets, bridge paths, and presentation anchors are all available.");
        }

        private static bool shouldRegeneratePresentationBackgrounds()
        {
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
        }

        private static void validateAssetPath(string assetPath, List<string> errors)
        {
            if (AssetDatabase.IsValidFolder(assetPath) || AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                return;
            }

            errors.Add($"Required asset path is missing: {assetPath}");
        }

        private static void validateStreamingAssets(List<string> errors)
        {
            List<string> requiredStreamingAssetPaths = new List<string>
            {
                MouthOfTruthAssetCatalog.TitleBackgroundPath,
                MouthOfTruthAssetCatalog.CardSelectionBackgroundPath,
                MouthOfTruthAssetCatalog.MouthChamberBackgroundPath,
                MouthOfTruthAssetCatalog.TitleLogoPath,
                MouthOfTruthAssetCatalog.TitleVignettePath,
                MouthOfTruthAssetCatalog.QuestionPanelFramePath,
                MouthOfTruthAssetCatalog.StatusPanelFramePath,
                MouthOfTruthAssetCatalog.ResultPanelFramePath,
                MouthOfTruthAssetCatalog.StartButtonPath,
                MouthOfTruthAssetCatalog.TryAgainButtonPath,
                MouthOfTruthAssetCatalog.EndGameButtonPath,
                MouthOfTruthAssetCatalog.ExitIconButtonPath,
                MouthOfTruthAssetCatalog.FloorRunnerPath,
                MouthOfTruthAssetCatalog.QuestionCardBackPath,
                MouthOfTruthAssetCatalog.QuestionCardFrontPath,
                MouthOfTruthAssetCatalog.TruthMouthFacePath,
                MouthOfTruthAssetCatalog.TrueVerdictPath,
                MouthOfTruthAssetCatalog.FalseVerdictPath,
                MouthOfTruthAssetCatalog.UncertainVerdictPath,
                MouthOfTruthAssetCatalog.PrimaryButtonFramePath,
                MouthOfTruthAssetCatalog.HandPointerPath,
                MouthOfTruthAssetCatalog.CardSelectionGlowPath,
                MouthOfTruthAssetCatalog.CardSelectionProgressFillPath,
                MouthOfTruthAssetCatalog.TitleAmbiencePath,
                MouthOfTruthAssetCatalog.ButtonConfirmPath,
                MouthOfTruthAssetCatalog.CardHoverPath,
                MouthOfTruthAssetCatalog.CardSelectPath,
                MouthOfTruthAssetCatalog.CardRevealPath,
                MouthOfTruthAssetCatalog.HandInsertPath,
                MouthOfTruthAssetCatalog.HandPausePath,
                MouthOfTruthAssetCatalog.ResultTruePath,
                MouthOfTruthAssetCatalog.ResultFalsePath,
                MouthOfTruthAssetCatalog.ResultUncertainPath,
            };

            foreach (string assetPath in requiredStreamingAssetPaths)
            {
                if (File.Exists(assetPath) == false)
                {
                    errors.Add($"Required streaming asset is missing: {assetPath}");
                }
            }
        }

        private static void validateQuestionPool(List<string> errors)
        {
            string questionPoolFilePath = Path.Combine(
                Application.streamingAssetsPath,
                "questions",
                "question_pool.json");

            if (File.Exists(questionPoolFilePath) == false)
            {
                errors.Add($"Question pool file is missing: {questionPoolFilePath}");
                return;
            }

            IReadOnlyList<QuestionDefinition> questionDefinitions = QuestionPoolLoader.LoadQuestionDefinitions(questionPoolFilePath);

            if (questionDefinitions.Count < 3)
            {
                errors.Add(
                    $"Question pool must contain at least three enabled questions. Current count: {questionDefinitions.Count}");
            }
        }

        private static void validatePythonBridge(List<string> errors)
        {
            string pythonInterpreterPath = PythonAnalysisBridgePaths.GetPythonInterpreterPath();
            string bridgeLauncherScriptPath = PythonAnalysisBridgePaths.GetBridgeLauncherScriptPath();
            string pythonModuleRootPath = PythonAnalysisBridgePaths.GetPythonModuleRootPath();

            if (File.Exists(bridgeLauncherScriptPath) == false)
            {
                errors.Add($"Python bridge launcher script is missing: {bridgeLauncherScriptPath}");
            }

            if (string.IsNullOrWhiteSpace(pythonInterpreterPath) == false && File.Exists(pythonInterpreterPath) == false)
            {
                errors.Add($"Python interpreter path is missing: {pythonInterpreterPath}");
            }

            if (Directory.Exists(pythonModuleRootPath) == false)
            {
                errors.Add($"Python module root path is missing: {pythonModuleRootPath}");
            }
        }

        private static void validateRenderPipeline(List<string> errors)
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                errors.Add("Project Graphics settings are missing a default render pipeline asset.");
            }

            if (QualitySettings.renderPipeline == null)
            {
                errors.Add("Current quality level is missing a render pipeline asset.");
            }
        }

        private static void validateMainScene(List<string> errors)
        {
            EditorSceneManager.OpenScene(MAIN_SCENE_PATH, OpenSceneMode.Single);

            GameObject appRoot = GameObject.Find("MouthOfTruthApp");

            if (appRoot == null)
            {
                errors.Add("Main scene is missing MouthOfTruthApp.");
            }
            else
            {
                if (appRoot.GetComponent<MouthOfTruthGameView>() == null)
                {
                    errors.Add("MouthOfTruthApp is missing MouthOfTruthGameView.");
                }

                if (appRoot.GetComponent<MouthOfTruthAppController>() == null)
                {
                    errors.Add("MouthOfTruthApp is missing MouthOfTruthAppController.");
                }
            }

            if (GameObject.Find("Main Camera") == null)
            {
                errors.Add("Main scene is missing Main Camera.");
            }

            if (GameObject.Find("EventSystem") == null)
            {
                errors.Add("Main scene is missing EventSystem.");
            }

            CardPresentationAnchorSet cardPresentationAnchorSet = Object.FindAnyObjectByType<CardPresentationAnchorSet>();

            if (cardPresentationAnchorSet == null || cardPresentationAnchorSet.HasRequiredAnchors() == false)
            {
                errors.Add("CardPresentationAnchorSet is missing or incomplete.");
            }

            MouthAnchorSet mouthAnchorSet = Object.FindAnyObjectByType<MouthAnchorSet>();

            if (mouthAnchorSet == null || mouthAnchorSet.HasRequiredAnchors() == false)
            {
                errors.Add("MouthAnchorSet is missing or incomplete.");
            }

            if (GameObject.Find("Models") == null)
            {
                errors.Add("Imported dungeon environment root 'Models' is missing from Main scene.");
            }

            if (GameObject.Find("MouthOfTruthStage") == null)
            {
                errors.Add("Main scene is missing MouthOfTruthStage.");
            }
        }
    }
}
