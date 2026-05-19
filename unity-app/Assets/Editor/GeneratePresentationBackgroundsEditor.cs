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

            Camera mainCamera = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            CardPresentationAnchorSet cardPresentationAnchorSet =
                Object.FindAnyObjectByType<CardPresentationAnchorSet>();
            MouthAnchorSet mouthAnchorSet = Object.FindAnyObjectByType<MouthAnchorSet>();

            if (mainCamera == null)
            {
                throw new FileNotFoundException("Main scene does not contain a camera for presentation capture.");
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
            renderCameraToPng(
                mainCamera,
                Path.Combine(OUTPUT_DIRECTORY_PATH, CARD_SELECTION_BACKGROUND_FILE_NAME),
                mainCamera.transform.position,
                mainCamera.transform.rotation,
                mainCamera.fieldOfView);

            Vector3 stageForward =
                (mouthAnchorSet.TruthMouth.position - cardPresentationAnchorSet.CenterCard.position).normalized;
            Vector3 mouthChamberLookTarget = mouthAnchorSet.TruthMouth.position + (Vector3.up * 0.35f);
            Vector3 mouthChamberCameraPosition =
                mouthChamberLookTarget
                - (stageForward * 5.35f)
                + (Vector3.up * 0.55f);
            Quaternion mouthChamberRotation =
                Quaternion.LookRotation((mouthChamberLookTarget - mouthChamberCameraPosition).normalized);

            renderCameraToPng(
                mainCamera,
                Path.Combine(OUTPUT_DIRECTORY_PATH, MOUTH_CHAMBER_BACKGROUND_FILE_NAME),
                mouthChamberCameraPosition,
                mouthChamberRotation,
                26.0f);

            AssetDatabase.Refresh();
            Debug.Log(
                "Generated presentation background images:\n"
                + $"- {Path.Combine(OUTPUT_DIRECTORY_PATH, CARD_SELECTION_BACKGROUND_FILE_NAME)}\n"
                + $"- {Path.Combine(OUTPUT_DIRECTORY_PATH, MOUTH_CHAMBER_BACKGROUND_FILE_NAME)}");
        }

        private static void renderCameraToPng(
            Camera sourceCamera,
            string outputFilePath,
            Vector3 position,
            Quaternion rotation,
            float fieldOfView)
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
                balanceDungeonPresentationTone(texture);

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

        private static void balanceDungeonPresentationTone(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();

            for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex += 1)
            {
                Color color = pixels[pixelIndex];
                float luminance = (color.r * 0.2126f) + (color.g * 0.7152f) + (color.b * 0.0722f);

                color = liftDungeonShadows(color, luminance);
                color = compressTorchHighlights(color, luminance);
                color = applyWarmDungeonGrade(color);
                pixels[pixelIndex] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        private static Color liftDungeonShadows(Color color, float luminance)
        {
            const float SHADOW_LIMIT = 0.32f;

            if (luminance >= SHADOW_LIMIT)
            {
                return color;
            }

            float liftAmount = Mathf.InverseLerp(SHADOW_LIMIT, 0.0f, luminance) * 0.18f;
            return new Color(
                Mathf.Clamp01(color.r + (liftAmount * 1.00f)),
                Mathf.Clamp01(color.g + (liftAmount * 0.82f)),
                Mathf.Clamp01(color.b + (liftAmount * 0.62f)),
                color.a);
        }

        private static Color compressTorchHighlights(Color color, float luminance)
        {
            const float HIGHLIGHT_START = 0.52f;

            if (luminance <= HIGHLIGHT_START)
            {
                return color;
            }

            float compressionAmount = Mathf.InverseLerp(HIGHLIGHT_START, 1.0f, luminance);
            Color warmHighlightColor = new Color(0.78f, 0.48f, 0.24f, color.a);
            Color compressedColor = Color.Lerp(color, warmHighlightColor, compressionAmount * 0.70f);
            float targetLuminance = Mathf.Lerp(luminance, 0.64f, compressionAmount * 0.74f);
            float scale = luminance > 0.0f ? targetLuminance / luminance : 1.0f;

            return new Color(
                Mathf.Clamp01(compressedColor.r * scale),
                Mathf.Clamp01(compressedColor.g * scale),
                Mathf.Clamp01(compressedColor.b * scale),
                color.a);
        }

        private static Color applyWarmDungeonGrade(Color color)
        {
            return new Color(
                Mathf.Clamp01(color.r * 1.03f),
                Mathf.Clamp01(color.g * 1.00f),
                Mathf.Clamp01(color.b * 0.95f),
                color.a);
        }
    }
}
