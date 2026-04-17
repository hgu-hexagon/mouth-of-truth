using MouthOfTruth.Game.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public class QuestionCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HOVER_MAX_SCALE = 1.06f;

        private Image mCardImage;
        private Image mGlowImage;
        private Image mProgressImage;
        private Text mQuestionText;
        private CanvasGroup mCanvasGroup;
        private RectTransform mRectTransform;
        private Vector2 mDefaultAnchoredPosition;

        public EQuestionCardSlot QuestionCardSlot { get; private set; }

        public bool IsHovered { get; private set; }

        public RectTransform RectTransform => mRectTransform;

        public void Initialize(
            EQuestionCardSlot questionCardSlot,
            Transform parentTransform,
            Sprite cardBackSprite)
        {
            QuestionCardSlot = questionCardSlot;
            transform.SetParent(parentTransform, false);
            gameObject.name = questionCardSlot.ToString();

            mRectTransform = gameObject.AddComponent<RectTransform>();
            mRectTransform.sizeDelta = new Vector2(280.0f, 420.0f);

            mCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            mCardImage = gameObject.AddComponent<Image>();
            mCardImage.sprite = cardBackSprite;
            mCardImage.type = Image.Type.Sliced;
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
            textRectTransform.anchorMin = new Vector2(0.08f, 0.18f);
            textRectTransform.anchorMax = new Vector2(0.92f, 0.84f);
            textRectTransform.offsetMin = Vector2.zero;
            textRectTransform.offsetMax = Vector2.zero;
            mQuestionText = textObject.AddComponent<Text>();
            mQuestionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mQuestionText.alignment = TextAnchor.MiddleCenter;
            mQuestionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            mQuestionText.verticalOverflow = VerticalWrapMode.Overflow;
            mQuestionText.color = new Color(0.14f, 0.09f, 0.04f, 1.0f);
            mQuestionText.fontSize = 28;
            mQuestionText.raycastTarget = false;
            mQuestionText.text = string.Empty;
            Shadow questionTextShadow = textObject.AddComponent<Shadow>();
            questionTextShadow.effectColor = new Color(0.98f, 0.95f, 0.89f, 0.38f);
            questionTextShadow.effectDistance = new Vector2(1.0f, -1.0f);
        }

        public void SetBack(Sprite cardBackSprite)
        {
            mCardImage.sprite = cardBackSprite;
            mCardImage.color = Color.white;
            mQuestionText.text = string.Empty;
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

            mCardImage.color = new Color(0.96f, 0.93f, 0.88f, 1.0f);
            mQuestionText.enabled = true;
            mQuestionText.text = questionText;
        }

        public void SetVisualState(
            bool isDimmed,
            bool isSelected,
            float hoverProgress)
        {
            float targetScale = isSelected
                ? 1.12f
                : Mathf.Lerp(1.0f, HOVER_MAX_SCALE, hoverProgress);

            mRectTransform.localScale = Vector3.one * targetScale;
            mCanvasGroup.alpha = isDimmed ? 0.22f : 1.0f;
            mGlowImage.color = new Color(
                0.90f,
                0.72f,
                0.25f,
                Mathf.Clamp01(hoverProgress) * 0.7f + (isSelected ? 0.2f : 0.0f));

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
    }
}
