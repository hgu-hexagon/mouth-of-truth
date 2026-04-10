using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public static class RuntimeSpriteLoader
    {
        public static async Task<Sprite> LoadSpriteAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            if (File.Exists(filePath) == false)
            {
                return null;
            }

            byte[] imageBytes = await File.ReadAllBytesAsync(filePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (texture.LoadImage(imageBytes) == false)
            {
                Object.Destroy(texture);
                return null;
            }

            texture.name = Path.GetFileNameWithoutExtension(filePath);

            return Sprite.Create(
                texture,
                new Rect(0.0f, 0.0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100.0f);
        }

        public static Sprite CreateSolidSprite(Color color, int size = 8)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0.0f, 0.0f, size, size),
                new Vector2(0.5f, 0.5f),
                100.0f);
        }
    }
}
