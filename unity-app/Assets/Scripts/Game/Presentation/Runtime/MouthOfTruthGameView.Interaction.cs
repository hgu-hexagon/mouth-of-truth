using System.Collections.Generic;
using MouthOfTruth.Game.Data;
using MouthOfTruth.Game.Input;
using UnityEngine;
using UnityEngine.UI;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public partial class MouthOfTruthGameView
    {
        public void UpdateAnswerMetrics(float elapsedAnswerSeconds, float elapsedSilenceSeconds)
        {
            _ = elapsedAnswerSeconds;
            _ = elapsedSilenceSeconds;
        }

        public EQuestionCardSlot? GetHoveredQuestionCardSlotOrNull(Vector2? pointerScreenPositionOrNull)
        {
            if (pointerScreenPositionOrNull.HasValue)
            {
                foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
                {
                    if (pair.Value.gameObject.activeInHierarchy == false)
                    {
                        continue;
                    }

                    if (isScreenPointOverRectTransform(pair.Value.RectTransform, pointerScreenPositionOrNull.Value))
                    {
                        return pair.Key;
                    }
                }

                return EvaluateQuestionCardIntentSlotOrNull(pointerScreenPositionOrNull.Value, Screen.width, Screen.height);
            }

            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                if (pair.Value.IsHovered)
                {
                    return pair.Key;
                }
            }

            return null;
        }

        public EUiActionTarget? GetHoveredUiActionTargetOrNull(Vector2? pointerScreenPositionOrNull)
        {
            if (pointerScreenPositionOrNull.HasValue == false)
            {
                return null;
            }

            Vector2 screenPosition = pointerScreenPositionOrNull.Value;

            if (isScreenPointOverButton(mStartButton, screenPosition, BUTTON_INTENT_EXPANSION_PIXELS))
            {
                return EUiActionTarget.StartGame;
            }

            if (isScreenPointOverButton(mTryAgainButton, screenPosition, BUTTON_INTENT_EXPANSION_PIXELS))
            {
                return EUiActionTarget.TryAgain;
            }

            if (isScreenPointOverButton(mExitButton, screenPosition, EXIT_BUTTON_INTENT_EXPANSION_PIXELS))
            {
                return EUiActionTarget.ExitGame;
            }

            if (isScreenPointOverButton(mBackToTitleButton, screenPosition, BUTTON_INTENT_EXPANSION_PIXELS))
            {
                return EUiActionTarget.BackToTitle;
            }

            return null;
        }

        public void UpdateActionButtonHoverVisual(EUiActionTarget? hoveredUiActionTargetOrNull, float hoverProgress)
        {
            if (hoveredUiActionTargetOrNull != mLastHoveredUiActionTargetOrNull)
            {
                mLastHoveredUiActionTargetOrNull = hoveredUiActionTargetOrNull;
            }

            updateButtonVisual(mStartButton, hoveredUiActionTargetOrNull == EUiActionTarget.StartGame, hoverProgress);
            updateButtonVisual(mTryAgainButton, hoveredUiActionTargetOrNull == EUiActionTarget.TryAgain, hoverProgress);
            updateButtonVisual(mExitButton, hoveredUiActionTargetOrNull == EUiActionTarget.ExitGame, hoverProgress);
            updateButtonVisual(mBackToTitleButton, hoveredUiActionTargetOrNull == EUiActionTarget.BackToTitle, hoverProgress);
        }

        public EHandAnchorState GetHandAnchorState(Vector2? pointerScreenPositionOrNull)
        {
            if (pointerScreenPositionOrNull.HasValue == false)
            {
                return EHandAnchorState.OutsideMouth;
            }

            Vector2 pointerCanvasPosition;
            if (tryConvertScreenPointToCanvasPosition(pointerScreenPositionOrNull.Value, out pointerCanvasPosition) == false)
            {
                return EHandAnchorState.OutsideMouth;
            }

            if (isTempleApproachSceneActive())
            {
                syncTempleStageMouthOverlay(0.0f);
            }

            float mouthDiameterPixels = Mathf.Max(1.0f, Mathf.Min(mMouthImage.rectTransform.rect.width, mMouthImage.rectTransform.rect.height));
            Vector2 handDetectionOffset = getHandDetectionOffset(mouthDiameterPixels);
            Vector2 handFrontPosition = getHandFrontPosition() + handDetectionOffset;
            Vector2 handInnerPosition = getHandInnerPosition() + handDetectionOffset;
            EHandAnchorState exactAnchorState = EvaluateHandAnchorState(pointerCanvasPosition, handFrontPosition, handInnerPosition, mouthDiameterPixels);

            if (exactAnchorState != EHandAnchorState.OutsideMouth)
            {
                return exactAnchorState;
            }

            return EvaluateMouthIntentAnchorState(pointerCanvasPosition, handFrontPosition, handInnerPosition, mouthDiameterPixels);
        }

        public static EQuestionCardSlot? EvaluateQuestionCardIntentSlotOrNull(Vector2 screenPosition, float screenWidth, float screenHeight)
        {
            if (screenWidth <= 0.0f || screenHeight <= 0.0f)
            {
                return null;
            }

            float normalizedX = Mathf.Clamp01(screenPosition.x / screenWidth);
            float normalizedY = Mathf.Clamp01(screenPosition.y / screenHeight);

            if (normalizedY < CARD_INTENT_MIN_NORMALIZED_Y || normalizedY > CARD_INTENT_MAX_NORMALIZED_Y)
            {
                return null;
            }

            if (normalizedX < CARD_INTENT_LEFT_MAX_NORMALIZED_X)
            {
                return EQuestionCardSlot.LeftCard;
            }

            if (normalizedX > CARD_INTENT_RIGHT_MIN_NORMALIZED_X)
            {
                return EQuestionCardSlot.RightCard;
            }

            return EQuestionCardSlot.CenterCard;
        }

        public static EHandAnchorState EvaluateHandAnchorState(Vector2 pointerCanvasPosition, Vector2 handFrontPosition, Vector2 handInnerPosition, float mouthDiameterPixels)
        {
            float clampedMouthDiameterPixels = Mathf.Max(1.0f, mouthDiameterPixels);
            float frontAnchorRadiusPixels = clampedMouthDiameterPixels * FRONT_ANCHOR_RADIUS_FACTOR;
            float innerAnchorRadiusPixels = clampedMouthDiameterPixels * INNER_ANCHOR_RADIUS_FACTOR;
            float distanceToInnerAnchor = Vector2.Distance(pointerCanvasPosition, handInnerPosition);
            Vector2 innerAnchorOffset = pointerCanvasPosition - handInnerPosition;

            if (distanceToInnerAnchor <= innerAnchorRadiusPixels || isInsideAnchorWindow(innerAnchorOffset, clampedMouthDiameterPixels * INNER_ENTRY_HALF_WIDTH_FACTOR, clampedMouthDiameterPixels * INNER_ENTRY_HALF_HEIGHT_FACTOR))
            {
                return EHandAnchorState.AtInnerAnchor;
            }

            float distanceToFrontAnchor = Vector2.Distance(pointerCanvasPosition, handFrontPosition);
            Vector2 frontAnchorOffset = pointerCanvasPosition - handFrontPosition;

            if (distanceToFrontAnchor <= frontAnchorRadiusPixels || isInsideAnchorWindow(frontAnchorOffset, clampedMouthDiameterPixels * FRONT_ENTRY_HALF_WIDTH_FACTOR, clampedMouthDiameterPixels * FRONT_ENTRY_HALF_HEIGHT_FACTOR))
            {
                return EHandAnchorState.AtFrontAnchor;
            }

            return EHandAnchorState.OutsideMouth;
        }

        public static EHandAnchorState EvaluateMouthIntentAnchorState(Vector2 pointerCanvasPosition, Vector2 handFrontPosition, Vector2 handInnerPosition, float mouthDiameterPixels)
        {
            float clampedMouthDiameterPixels = Mathf.Max(1.0f, mouthDiameterPixels);
            float intentLeftWidth = clampedMouthDiameterPixels * MOUTH_INTENT_LEFT_WIDTH_FACTOR;
            float intentRightWidth = clampedMouthDiameterPixels * MOUTH_INTENT_RIGHT_WIDTH_FACTOR;
            float minimumY = Mathf.Min(handFrontPosition.y, handInnerPosition.y)
                - (clampedMouthDiameterPixels * MOUTH_INTENT_LOWER_MARGIN_FACTOR);
            float maximumY = Mathf.Max(handFrontPosition.y, handInnerPosition.y)
                + (clampedMouthDiameterPixels * MOUTH_INTENT_UPPER_MARGIN_FACTOR);
            float centerX = Mathf.Lerp(handFrontPosition.x, handInnerPosition.x, 0.5f);
            float minimumX = centerX - intentLeftWidth;
            float maximumX = centerX + intentRightWidth;

            if (pointerCanvasPosition.x < minimumX || pointerCanvasPosition.x > maximumX || pointerCanvasPosition.y < minimumY || pointerCanvasPosition.y > maximumY)
            {
                return EHandAnchorState.OutsideMouth;
            }

            float innerSwitchY = Mathf.Lerp(handFrontPosition.y, handInnerPosition.y, MOUTH_INTENT_INNER_SWITCH_FACTOR);
            return pointerCanvasPosition.y >= innerSwitchY
                ? EHandAnchorState.AtInnerAnchor
                : EHandAnchorState.AtFrontAnchor;
        }

        private static Vector2 getHandDetectionOffset(float mouthDiameterPixels)
        {
            return new Vector2(0.0f, Mathf.Max(1.0f, mouthDiameterPixels) * HAND_DETECTION_VERTICAL_OFFSET_FACTOR);
        }

        public void UpdatePointerVisual(bool isVisible, Vector2? pointerScreenPositionOrNull)
        {
            if (mPointerImage == null)
            {
                return;
            }

            if (isVisible == false || pointerScreenPositionOrNull.HasValue == false)
            {
                setObjectActive(mPointerImage, false);
                return;
            }

            Vector2 anchoredPosition;
            if (tryConvertScreenPointToCanvasPosition(pointerScreenPositionOrNull.Value, out anchoredPosition) == false)
            {
                setObjectActive(mPointerImage, false);
                return;
            }

            setObjectActive(mPointerImage, true);
            mPointerImage.transform.SetAsLastSibling();
            RectTransform pointerRectTransform = mPointerImage.rectTransform;
            pointerRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            pointerRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            pointerRectTransform.sizeDelta = POINTER_CURSOR_SIZE_PIXELS;
            pointerRectTransform.anchoredPosition = anchoredPosition;
        }

        public string GetAnswerTranscript()
        {
            if (mAnswerInputField == null)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(mAnswerInputField.text) ? string.Empty : mAnswerInputField.text;
        }

        public void SetAnswerTranscriptText(string transcriptText)
        {
            if (mAnswerInputField == null)
            {
                return;
            }

            string safeTranscriptText = string.IsNullOrEmpty(transcriptText) ? string.Empty : transcriptText;
            mAnswerInputField.SetTextWithoutNotify(safeTranscriptText);
            setText(mAnswerInputField.textComponent, safeTranscriptText);
        }

        public void ClearAnswerTranscript()
        {
            SetAnswerTranscriptText(string.Empty);
        }

        public void SetAnswerTranscriptPlaceholder(string placeholderText)
        {
            if (mAnswerInputField == null)
            {
                return;
            }

            if (mAnswerInputField.placeholder is Text placeholderLabel)
            {
                string safePlaceholderText = string.IsNullOrEmpty(placeholderText) ? string.Empty : placeholderText;
                setText(placeholderLabel, safePlaceholderText);
            }
        }

        public void SetAnswerTranscriptEditable(bool isEditable)
        {
            if (mAnswerInputField == null)
            {
                return;
            }

            setObjectActive(mAnswerInputField, isEditable);
            mAnswerInputField.interactable = isEditable;

            if (isEditable)
            {
                mAnswerInputField.ActivateInputField();
            }
        }

        public bool ConsumeStartRequested()
        {
            bool wasRequested = mStartRequested;
            mStartRequested = false;
            return wasRequested;
        }

        public bool ConsumeTryAgainRequested()
        {
            bool wasRequested = mTryAgainRequested;
            mTryAgainRequested = false;
            return wasRequested;
        }

        public bool ConsumeBackToTitleRequested()
        {
            bool wasRequested = mBackToTitleRequested;
            mBackToTitleRequested = false;
            return wasRequested;
        }

        public bool ConsumeExitRequested()
        {
            bool wasRequested = mExitRequested;
            mExitRequested = false;
            return wasRequested;
        }
    }
}
