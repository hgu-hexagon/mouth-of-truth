using MouthOfTruth.Game.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public class QuestionCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HOVER_MAX_SCALE = 1.06f;
        private const int QUESTION_TEXT_MAXIMUM_FONT_SIZE = 26;
        private const int QUESTION_TEXT_MINIMUM_FONT_SIZE = 20;
        private const float QUESTION_TEXT_LINE_SPACING = 1.2f;
        private static readonly Color QUESTION_TEXT_INK_COLOR = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        private static readonly Color QUESTION_TEXT_INK_BOOST_COLOR = new Color(0.0f, 0.0f, 0.0f, 0.82f);
        private static readonly Vector2 QUESTION_TEXT_INK_BOOST_OFFSET = new Vector2(0.18f, -0.18f);

        private Image mCardImage;
        private Image mGlowImage;
        private Image mProgressImage;
        private Text mQuestionText;
        private Shadow mQuestionTextInkBoost;
        private CanvasGroup mCanvasGroup;
        private RectTransform mRectTransform;
        private Vector2 mDefaultAnchoredPosition;
        private Font mPrimaryUiFont;
        private Font mKoreanFallbackFont;

        public EQuestionCardSlot QuestionCardSlot { get; private set; }

        public bool IsHovered { get; private set; }

        public RectTransform RectTransform => mRectTransform;

        public void Initialize(EQuestionCardSlot questionCardSlot, Transform parentTransform, Sprite cardBackSprite, Font primaryUiFont, Font koreanFallbackFont)
        {
            QuestionCardSlot = questionCardSlot;
            mPrimaryUiFont = primaryUiFont == null
                ? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                : primaryUiFont;
            mKoreanFallbackFont = koreanFallbackFont == null ? mPrimaryUiFont : koreanFallbackFont;
            transform.SetParent(parentTransform, false);
            gameObject.name = questionCardSlot.ToString();

            mRectTransform = gameObject.AddComponent<RectTransform>();
            mRectTransform.sizeDelta = new Vector2(320.0f, 480.0f);

            mCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            mCardImage = gameObject.AddComponent<Image>();
            mCardImage.sprite = cardBackSprite;
            mCardImage.type = Image.Type.Simple;
            mCardImage.preserveAspect = true;
            mCardImage.raycastTarget = true;

            GameObject glowObject = new GameObject("Glow");
            glowObject.transform.SetParent(transform, false);
            RectTransform glowRectTransform = glowObject.AddComponent<RectTransform>();
            glowRectTransform.anchorMin = Vector2.zero;
            glowRectTransform.anchorMax = Vector2.one;
            glowRectTransform.offsetMin = new Vector2(-12.0f, -12.0f);
            glowRectTransform.offsetMax = new Vector2(12.0f, 12.0f);
            mGlowImage = glowObject.AddComponent<Image>();
            mGlowImage.color = new Color(0.90f, 0.72f, 0.25f, 0.0f);
            mGlowImage.raycastTarget = false;

            GameObject progressObject = new GameObject("Progress");
            progressObject.transform.SetParent(transform, false);
            RectTransform progressRectTransform = progressObject.AddComponent<RectTransform>();
            progressRectTransform.anchorMin = new Vector2(0.1f, 0.03f);
            progressRectTransform.anchorMax = new Vector2(0.9f, 0.08f);
            progressRectTransform.offsetMin = Vector2.zero;
            progressRectTransform.offsetMax = Vector2.zero;
            Image progressBackgroundImage = progressObject.AddComponent<Image>();
            progressBackgroundImage.color = new Color(0.0f, 0.0f, 0.0f, 0.45f);
            progressBackgroundImage.raycastTarget = false;

            GameObject progressFillObject = new GameObject("ProgressFill");
            progressFillObject.transform.SetParent(progressObject.transform, false);
            RectTransform progressFillRectTransform = progressFillObject.AddComponent<RectTransform>();
            progressFillRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            progressFillRectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            progressFillRectTransform.offsetMin = Vector2.zero;
            progressFillRectTransform.offsetMax = Vector2.zero;
            mProgressImage = progressFillObject.AddComponent<Image>();
            mProgressImage.color = new Color(0.95f, 0.82f, 0.33f, 0.95f);
            mProgressImage.raycastTarget = false;

            GameObject textObject = new GameObject("QuestionText");
            textObject.transform.SetParent(transform, false);
            RectTransform textRectTransform = textObject.AddComponent<RectTransform>();
            textRectTransform.anchorMin = new Vector2(0.18f, 0.24f);
            textRectTransform.anchorMax = new Vector2(0.82f, 0.76f);
            textRectTransform.offsetMin = Vector2.zero;
            textRectTransform.offsetMax = Vector2.zero;
            mQuestionText = textObject.AddComponent<Text>();
            mQuestionText.font = mPrimaryUiFont;
            mQuestionText.alignment = TextAnchor.MiddleCenter;
            mQuestionText.horizontalOverflow = HorizontalWrapMode.Overflow;
            mQuestionText.verticalOverflow = VerticalWrapMode.Truncate;
            mQuestionText.color = QUESTION_TEXT_INK_COLOR;
            mQuestionText.fontSize = QUESTION_TEXT_MAXIMUM_FONT_SIZE;
            mQuestionText.fontStyle = FontStyle.Normal;
            mQuestionText.lineSpacing = QUESTION_TEXT_LINE_SPACING;
            mQuestionText.resizeTextForBestFit = false;
            mQuestionText.resizeTextMinSize = QUESTION_TEXT_MINIMUM_FONT_SIZE;
            mQuestionText.resizeTextMaxSize = QUESTION_TEXT_MAXIMUM_FONT_SIZE;
            mQuestionText.raycastTarget = false;
            mQuestionText.material = Graphic.defaultGraphicMaterial;
            mQuestionTextInkBoost = textObject.AddComponent<Shadow>();
            mQuestionTextInkBoost.effectColor = QUESTION_TEXT_INK_BOOST_COLOR;
            mQuestionTextInkBoost.effectDistance = QUESTION_TEXT_INK_BOOST_OFFSET;
            mQuestionTextInkBoost.useGraphicAlpha = false;
            setQuestionText(string.Empty);
        }

        public void SetBack(Sprite cardBackSprite)
        {
            mCardImage.sprite = cardBackSprite;
            mCardImage.type = Image.Type.Simple;
            mCardImage.color = Color.white;
            setQuestionText(string.Empty);
            mQuestionText.enabled = false;
        }

        public void SetDecorSprites(Sprite glowSprite, Sprite progressFillSprite)
        {
            mGlowImage.sprite = glowSprite;
            mGlowImage.type = Image.Type.Sliced;
            mProgressImage.sprite = progressFillSprite;
            mProgressImage.type = Image.Type.Sliced;
        }

        public void SetFront(Sprite cardFrontSprite, string questionText)
        {
            if (cardFrontSprite != null)
            {
                mCardImage.sprite = cardFrontSprite;
            }

            mCardImage.type = Image.Type.Simple;
            mCardImage.color = Color.white;
            mQuestionText.enabled = true;
            mQuestionText.color = QUESTION_TEXT_INK_COLOR;
            mQuestionText.material = Graphic.defaultGraphicMaterial;
            mCanvasGroup.alpha = 1.0f;
            setQuestionText(questionText);
            applyQuestionTextLayout(questionText);
        }

        public void SetVisualState(bool isDimmed, bool isSelected, float hoverProgress)
        {
            float targetScale = isSelected
                ? 1.12f
                : Mathf.Lerp(1.0f, HOVER_MAX_SCALE, hoverProgress);

            mRectTransform.localScale = Vector3.one * targetScale;
            mCanvasGroup.alpha = isDimmed ? 0.22f : 1.0f;
            mGlowImage.color = new Color(0.90f, 0.72f, 0.25f, Mathf.Clamp01(hoverProgress) * 0.7f + (isSelected ? 0.2f : 0.0f));

            float progressWidth = Mathf.Clamp01(hoverProgress);
            mProgressImage.rectTransform.anchorMax = new Vector2(progressWidth, 1.0f);
            mProgressImage.enabled = progressWidth > 0.0f && isSelected == false;
        }

        public void SetAnchoredPosition(Vector2 anchoredPosition)
        {
            mDefaultAnchoredPosition = anchoredPosition;
            mRectTransform.anchoredPosition = anchoredPosition;
        }

        public void ResetTransformState()
        {
            mRectTransform.anchoredPosition = mDefaultAnchoredPosition;
            mRectTransform.localScale = Vector3.one;
            mCanvasGroup.alpha = 1.0f;
        }

        public void SetAlpha(float alpha)
        {
            mCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        public void SetScale(float scale)
        {
            mRectTransform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        }

        public void SetScale(float horizontalScale, float verticalScale)
        {
            mRectTransform.localScale = new Vector3(Mathf.Max(0.01f, horizontalScale), Mathf.Max(0.01f, verticalScale), 1.0f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
        }

        public void ResetHoverState()
        {
            IsHovered = false;
        }

        private void setQuestionText(string questionText)
        {
            mQuestionText.font = containsHangul(questionText) ? mKoreanFallbackFont : mPrimaryUiFont;
            mQuestionText.color = QUESTION_TEXT_INK_COLOR;
            mQuestionText.material = Graphic.defaultGraphicMaterial;
            mQuestionText.fontStyle = FontStyle.Normal;
            mQuestionText.text = questionText;

            if (mQuestionTextInkBoost != null)
            {
                mQuestionTextInkBoost.effectColor = QUESTION_TEXT_INK_BOOST_COLOR;
                mQuestionTextInkBoost.effectDistance = QUESTION_TEXT_INK_BOOST_OFFSET;
            }
        }

        private void applyQuestionTextLayout(string questionText)
        {
            mQuestionText.horizontalOverflow = HorizontalWrapMode.Overflow;
            mQuestionText.verticalOverflow = VerticalWrapMode.Truncate;
            mQuestionText.lineSpacing = QUESTION_TEXT_LINE_SPACING;
            mQuestionText.resizeTextForBestFit = false;
            mQuestionText.fontSize = calculateFittingQuestionTextFontSize(questionText);
        }

        private int calculateFittingQuestionTextFontSize(string questionText)
        {
            if (string.IsNullOrWhiteSpace(questionText))
            {
                return QUESTION_TEXT_MAXIMUM_FONT_SIZE;
            }

            RectTransform textRectTransform = mQuestionText.rectTransform;
            Vector2 availableTextSize = textRectTransform.rect.size;

            if (availableTextSize.x <= 0.0f || availableTextSize.y <= 0.0f)
            {
                Vector2 anchorRange = textRectTransform.anchorMax - textRectTransform.anchorMin;
                availableTextSize = new Vector2(mRectTransform.rect.width * anchorRange.x, mRectTransform.rect.height * anchorRange.y);
            }

            if (availableTextSize.x <= 0.0f || availableTextSize.y <= 0.0f)
            {
                return QUESTION_TEXT_MAXIMUM_FONT_SIZE;
            }

            for (int fontSize = QUESTION_TEXT_MAXIMUM_FONT_SIZE; fontSize >= QUESTION_TEXT_MINIMUM_FONT_SIZE; fontSize -= 1)
            {
                if (canQuestionTextFitAtFontSize(questionText, availableTextSize, fontSize))
                {
                    return fontSize;
                }
            }

            return QUESTION_TEXT_MINIMUM_FONT_SIZE;
        }

        private bool canQuestionTextFitAtFontSize(string questionText, Vector2 availableTextSize, int fontSize)
        {
            mQuestionText.fontSize = fontSize;
            TextGenerationSettings textGenerationSettings = mQuestionText.GetGenerationSettings(availableTextSize);

            foreach (string questionLine in questionText.Split('\n'))
            {
                float lineWidth = mQuestionText.cachedTextGeneratorForLayout.GetPreferredWidth(questionLine, textGenerationSettings) / mQuestionText.pixelsPerUnit;

                if (lineWidth > availableTextSize.x)
                {
                    return false;
                }
            }

            float textHeight = mQuestionText.cachedTextGeneratorForLayout.GetPreferredHeight(questionText, textGenerationSettings) / mQuestionText.pixelsPerUnit;
            return textHeight <= availableTextSize.y;
        }

        private static bool containsHangul(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            foreach (char character in text)
            {
                if (character >= '\uac00' && character <= '\ud7a3')
                {
                    return true;
                }
            }

            return false;
        }
    }
}
