using UnityEngine;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public partial class MouthOfTruthGameView
    {
        private static Sprite createPointerCursorSprite()
        {
            Texture2D texture = new Texture2D(POINTER_CURSOR_TEXTURE_SIZE, POINTER_CURSOR_TEXTURE_SIZE, TextureFormat.RGBA32, mipChain: false);
            texture.hideFlags = HideFlags.DontSave;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            float center = (POINTER_CURSOR_TEXTURE_SIZE - 1.0f) * 0.5f;
            float fillRadius = POINTER_CURSOR_TEXTURE_SIZE * 0.24f;
            float ringInnerRadius = POINTER_CURSOR_TEXTURE_SIZE * 0.31f;
            float ringOuterRadius = POINTER_CURSOR_TEXTURE_SIZE * 0.39f;
            Color[] pixels = new Color[POINTER_CURSOR_TEXTURE_SIZE * POINTER_CURSOR_TEXTURE_SIZE];

            for (int y = 0; y < POINTER_CURSOR_TEXTURE_SIZE; y += 1)
            {
                for (int x = 0; x < POINTER_CURSOR_TEXTURE_SIZE; x += 1)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    Color pixelColor = Color.clear;

                    if (distance <= fillRadius)
                    {
                        pixelColor = POINTER_CURSOR_FILL_COLOR;
                    }
                    else if (distance >= ringInnerRadius && distance <= ringOuterRadius)
                    {
                        pixelColor = POINTER_CURSOR_RING_COLOR;
                    }

                    pixels[(y * POINTER_CURSOR_TEXTURE_SIZE) + x] = pixelColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return Sprite.Create(texture, new Rect(0.0f, 0.0f, POINTER_CURSOR_TEXTURE_SIZE, POINTER_CURSOR_TEXTURE_SIZE), new Vector2(0.5f, 0.5f), POINTER_CURSOR_TEXTURE_SIZE);
        }

        private static Sprite createRadialGlowSprite()
        {
            const int TEXTURE_SIZE = 256;
            Texture2D texture = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, mipChain: false);
            texture.hideFlags = HideFlags.DontSave;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            float center = (TEXTURE_SIZE - 1.0f) * 0.5f;
            float radius = TEXTURE_SIZE * 0.48f;

            for (int y = 0; y < TEXTURE_SIZE; y += 1)
            {
                for (int x = 0; x < TEXTURE_SIZE; x += 1)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float normalizedDistance = Mathf.Clamp01(distance / radius);
                    float alpha = Mathf.Pow(1.0f - normalizedDistance, 2.2f);
                    pixels[(y * TEXTURE_SIZE) + x] = new Color(1.0f, 1.0f, 1.0f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return Sprite.Create(texture, new Rect(0.0f, 0.0f, TEXTURE_SIZE, TEXTURE_SIZE), new Vector2(0.5f, 0.5f), TEXTURE_SIZE);
        }

        private static Sprite createRingGlowSprite()
        {
            const int TEXTURE_SIZE = 256;
            Texture2D texture = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, mipChain: false);
            texture.hideFlags = HideFlags.DontSave;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
            float center = (TEXTURE_SIZE - 1.0f) * 0.5f;
            float ringRadius = TEXTURE_SIZE * 0.36f;
            float ringThickness = TEXTURE_SIZE * 0.12f;

            for (int y = 0; y < TEXTURE_SIZE; y += 1)
            {
                for (int x = 0; x < TEXTURE_SIZE; x += 1)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float ringDistance = Mathf.Abs(distance - ringRadius) / ringThickness;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1.0f - ringDistance), 1.6f);
                    float coreGlow = Mathf.Pow(Mathf.Clamp01(1.0f - (distance / (TEXTURE_SIZE * 0.52f))), 3.0f) * 0.45f;
                    pixels[(y * TEXTURE_SIZE) + x] = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(alpha + coreGlow));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return Sprite.Create(texture, new Rect(0.0f, 0.0f, TEXTURE_SIZE, TEXTURE_SIZE), new Vector2(0.5f, 0.5f), TEXTURE_SIZE);
        }

        private static Sprite createEyeBeamSprite(bool isSourceOnRight)
        {
            const int TEXTURE_WIDTH = 192;
            const int TEXTURE_HEIGHT = 256;
            Texture2D texture = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT, TextureFormat.RGBA32, mipChain: false);
            texture.hideFlags = HideFlags.DontSave;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[TEXTURE_WIDTH * TEXTURE_HEIGHT];
            float sourceCenterX = isSourceOnRight ? 0.52f : 0.48f;

            for (int y = 0; y < TEXTURE_HEIGHT; y += 1)
            {
                for (int x = 0; x < TEXTURE_WIDTH; x += 1)
                {
                    float normalizedX = x / (TEXTURE_WIDTH - 1.0f);
                    float downProgress = 1.0f - (y / (TEXTURE_HEIGHT - 1.0f));
                    float halfWidth = Mathf.Lerp(0.018f, 0.50f, Mathf.Pow(downProgress, 0.88f));
                    float horizontalDistance = Mathf.Abs(normalizedX - sourceCenterX) / halfWidth;
                    float edgeFade = Mathf.Pow(Mathf.Clamp01(1.0f - horizontalDistance), 0.72f);
                    float verticalFade = Mathf.Lerp(1.0f, 0.76f, downProgress);
                    float sourceFlare = Mathf.Pow(Mathf.Clamp01(1.0f - (downProgress * 7.5f)), 2.0f);
                    float alpha = horizontalDistance <= 1.0f ? (edgeFade * verticalFade) + (sourceFlare * 0.62f) : 0.0f;
                    pixels[(y * TEXTURE_WIDTH) + x] = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(alpha));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return Sprite.Create(texture, new Rect(0.0f, 0.0f, TEXTURE_WIDTH, TEXTURE_HEIGHT), new Vector2(0.5f, 0.5f), TEXTURE_WIDTH);
        }
    }
}
