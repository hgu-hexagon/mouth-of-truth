using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public static class RuntimeSpriteLoader
    {
        private const int DEFAULT_SOLID_SPRITE_SIZE = 8;

        public static Task<Sprite> LoadSpriteOrNullAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult<Sprite>(null);
            }

            if (File.Exists(filePath) == false)
            {
                return Task.FromResult<Sprite>(null);
            }

            byte[] imageBytes = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (texture.LoadImage(imageBytes) == false)
            {
                Object.Destroy(texture);
                return Task.FromResult<Sprite>(null);
            }

            texture.name = Path.GetFileNameWithoutExtension(filePath);

            Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f, 0u, SpriteMeshType.FullRect, getImplicitBorder(filePath, texture.width, texture.height));

            return Task.FromResult(sprite);
        }

        public static Sprite CreateSolidSprite(Color color)
        {
            return CreateSolidSprite(color, DEFAULT_SOLID_SPRITE_SIZE);
        }

        public static Sprite CreateSolidSprite(Color color, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0.0f, 0.0f, size, size), new Vector2(0.5f, 0.5f), 100.0f);
        }

        private static Vector4 getImplicitBorder(string filePath, int width, int height)
        {
            string safeFilePath = string.IsNullOrEmpty(filePath) ? string.Empty : filePath;
            string normalizedFilePath = safeFilePath
                .Replace('\\', '/')
                .ToLowerInvariant();

            if (normalizedFilePath.Contains("/ui/panel_"))
            {
                return createBorder(width, height, 0.045f, 0.22f, 18.0f, 18.0f);
            }

            if (normalizedFilePath.Contains("button_frame_primary"))
            {
                return createBorder(width, height, 0.12f, 0.32f, 18.0f, 18.0f);
            }

            if (normalizedFilePath.Contains("question_card_back") || normalizedFilePath.Contains("question_card_front"))
            {
                return createBorder(width, height, 0.11f, 0.08f, 24.0f, 24.0f);
            }

            if (normalizedFilePath.Contains("card_selection_glow"))
            {
                return createBorder(width, height, 0.18f, 0.18f, 24.0f, 24.0f);
            }

            if (normalizedFilePath.Contains("card_selection_progress_fill"))
            {
                return createBorder(width, height, 0.06f, 0.18f, 12.0f, 12.0f);
            }

            return Vector4.zero;
        }

        private static Vector4 createBorder(int width, int height, float horizontalRatio, float verticalRatio, float minimumHorizontalBorderPixels, float minimumVerticalBorderPixels)
        {
            float horizontalBorderPixels = Mathf.Max(minimumHorizontalBorderPixels, width * horizontalRatio);
            float verticalBorderPixels = Mathf.Max(minimumVerticalBorderPixels, height * verticalRatio);
            return new Vector4(horizontalBorderPixels, verticalBorderPixels, horizontalBorderPixels, verticalBorderPixels);
        }
    }
}
