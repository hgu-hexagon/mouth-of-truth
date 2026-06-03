using System.IO;
using MouthOfTruth.Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MouthOfTruth.Editor
{
    public static class GeneratePresentationBackgroundsEditor
    {
        private const string MAIN_SCENE_PATH = "Assets/Scenes/Main.unity";
        private const string OUTPUT_DIRECTORY_PATH = "Assets/StreamingAssets/art/backgrounds";
        private const string CARD_SELECTION_BACKGROUND_FILE_NAME = "stage_card_selection_generated.png";
        private const string MOUTH_CHAMBER_BACKGROUND_FILE_NAME = "stage_mouth_chamber_generated.png";

        [MenuItem("Mouth Of Truth/Generate Presentation Backgrounds")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(MAIN_SCENE_PATH, OpenSceneMode.Single);

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = Object.FindAnyObjectByType<Camera>();
            }

            CardPresentationAnchorSet cardPresentationAnchorSet = Object.FindAnyObjectByType<CardPresentationAnchorSet>();
            MouthAnchorSet mouthAnchorSet = Object.FindAnyObjectByType<MouthAnchorSet>();

            if (mainCamera == null)
            {
                throw new FileNotFoundException("Main scene does not contain a camera for background generation.");
            }

            if (cardPresentationAnchorSet == null || cardPresentationAnchorSet.HasRequiredAnchors() == false)
            {
                throw new FileNotFoundException("CardPresentationAnchorSet is missing or incomplete.");
            }

            if (mouthAnchorSet == null || mouthAnchorSet.HasRequiredAnchors() == false)
            {
                throw new FileNotFoundException("MouthAnchorSet is missing or incomplete.");
            }

            Directory.CreateDirectory(OUTPUT_DIRECTORY_PATH);
            renderCameraToPng(mainCamera, Path.Combine(OUTPUT_DIRECTORY_PATH, CARD_SELECTION_BACKGROUND_FILE_NAME), mainCamera.transform.position, mainCamera.transform.rotation, mainCamera.fieldOfView);

            Vector3 stageForward = (mouthAnchorSet.TruthMouth.position - cardPresentationAnchorSet.CenterCard.position).normalized;
            Vector3 mouthChamberLookTarget = mouthAnchorSet.TruthMouth.position + (Vector3.up * 0.35f);
            Vector3 mouthChamberCameraPosition = mouthChamberLookTarget - (stageForward * 5.35f) + (Vector3.up * 0.55f);
            Quaternion mouthChamberRotation = Quaternion.LookRotation((mouthChamberLookTarget - mouthChamberCameraPosition).normalized);

            renderCameraToPng(mainCamera, Path.Combine(OUTPUT_DIRECTORY_PATH, MOUTH_CHAMBER_BACKGROUND_FILE_NAME), mouthChamberCameraPosition, mouthChamberRotation, 26.0f);

            AssetDatabase.Refresh();
            Debug.Log("Generated presentation background images:\n" + $"- {Path.Combine(OUTPUT_DIRECTORY_PATH, CARD_SELECTION_BACKGROUND_FILE_NAME)}\n" + $"- {Path.Combine(OUTPUT_DIRECTORY_PATH, MOUTH_CHAMBER_BACKGROUND_FILE_NAME)}");
        }

        private static void renderCameraToPng(Camera sourceCamera, string outputFilePath, Vector3 position, Quaternion rotation, float fieldOfView)
        {
            const int IMAGE_WIDTH = 1920;
            const int IMAGE_HEIGHT = 1080;

            Vector3 originalPosition = sourceCamera.transform.position;
            Quaternion originalRotation = sourceCamera.transform.rotation;
            float originalFieldOfView = sourceCamera.fieldOfView;
            RenderTexture originalTargetTexture = sourceCamera.targetTexture;
            RenderTexture previousActiveRenderTexture = RenderTexture.active;

            RenderTexture renderTexture = new RenderTexture(IMAGE_WIDTH, IMAGE_HEIGHT, 24);
            Texture2D texture = new Texture2D(IMAGE_WIDTH, IMAGE_HEIGHT, TextureFormat.RGB24, false);

            try
            {
                sourceCamera.transform.position = position;
                sourceCamera.transform.rotation = rotation;
                sourceCamera.fieldOfView = fieldOfView;
                sourceCamera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;

                sourceCamera.Render();
                texture.ReadPixels(new Rect(0.0f, 0.0f, IMAGE_WIDTH, IMAGE_HEIGHT), 0, 0);
                texture.Apply();

                File.WriteAllBytes(outputFilePath, texture.EncodeToPNG());
            }
            finally
            {
                sourceCamera.transform.position = originalPosition;
                sourceCamera.transform.rotation = originalRotation;
                sourceCamera.fieldOfView = originalFieldOfView;
                sourceCamera.targetTexture = originalTargetTexture;
                RenderTexture.active = previousActiveRenderTexture;

                Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(texture);
            }
        }
    }
}
