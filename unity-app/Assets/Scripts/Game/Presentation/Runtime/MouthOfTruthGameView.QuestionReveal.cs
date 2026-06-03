using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MouthOfTruth.Game.Data;
using UnityEngine;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public partial class MouthOfTruthGameView
    {
        public async Task PlayQuestionRevealAsync(EQuestionCardSlot selectedQuestionCardSlot, QuestionDefinition questionDefinition, Func<Task> questionNarrationTaskFactoryOrNull = null)
        {
            resetHandPromptPanelAlpha();
            mIsCardAbsorptionPresentationActive = false;
            mCardAbsorptionPresentationProgress = 0.0f;
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mQuestionText, false);
            setObjectActive(mSceneOverlayImage, true);
            setMouthEffectImagesActive(false, false);
            setOverlayAlpha(0.12f);
            mLastAudibleHoveredCardSlotOrNull = null;
            mLastCardHoverCueTimeSeconds = Time.unscaledTime;
            playInterfaceCueClean(mCardSelectClip, 0.58f);
            await animateOverTimeAsync(CARD_SELECTION_CUE_SETTLE_SECONDS, _ => { });

            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                bool isSelected = pair.Key == selectedQuestionCardSlot;
                pair.Value.SetVisualState(isDimmed: isSelected == false, isSelected, 0.0f);
                pair.Value.gameObject.SetActive(isSelected);
            }

            QuestionCardView selectedCardView = mCardViews[selectedQuestionCardSlot];
            Vector2 startPosition = selectedCardView.RectTransform.anchoredPosition;
            Vector2 endPosition = getCenteredCardRevealPosition();

            await animateOverTimeAsync(
                0.75f,
                progress =>
                {
                    float easedProgress = easeOut(progress);
                    selectedCardView.RectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedProgress);
                    selectedCardView.RectTransform.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.22f, easedProgress);
                });

            await animateOverTimeAsync(
                CARD_FLIP_CLOSE_SECONDS,
                progress =>
                {
                    float easedProgress = easeInOut(progress);
                    float verticalScale = 1.22f + (Mathf.Sin(progress * Mathf.PI) * 0.02f);
                    selectedCardView.SetScale(Mathf.Lerp(1.22f, 0.08f, easedProgress), verticalScale);
                });

            selectedCardView.SetFront(mCardFrontSprite, questionDefinition.Text);
            playInterfaceCueClean(mCardRevealClip, 0.58f);

            await animateOverTimeAsync(
                CARD_FLIP_OPEN_SECONDS + CARD_REVEAL_CUE_SETTLE_SECONDS,
                progress =>
                {
                    float easedProgress = easeOut(progress);
                    float settlePulse = Mathf.Sin(progress * Mathf.PI) * 0.012f;
                    selectedCardView.SetScale(Mathf.Lerp(0.08f, 1.26f, easedProgress), Mathf.Lerp(1.24f, 1.26f, easedProgress) + settlePulse);
                });

            await animateOverTimeAsync(
                CARD_FRONT_FOCUS_BEFORE_NARRATION_SECONDS,
                progress =>
                {
                    float pulse = Mathf.Sin(progress * Mathf.PI) * 0.006f;
                    selectedCardView.SetScale(1.26f + pulse);
                });

            Task questionNarrationTask = Task.CompletedTask;
            if (questionNarrationTaskFactoryOrNull != null)
            {
                questionNarrationTask = questionNarrationTaskFactoryOrNull.Invoke();
            }

            float cardFrontReadHoldDurationSeconds = getCardFrontReadHoldDurationSeconds(questionDefinition.Text);
            float elapsedFrontReadHoldSeconds = 0.0f;

            while (elapsedFrontReadHoldSeconds < cardFrontReadHoldDurationSeconds
                || questionNarrationTask.IsCompleted == false)
            {
                elapsedFrontReadHoldSeconds += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedFrontReadHoldSeconds / cardFrontReadHoldDurationSeconds);
                float pulse = Mathf.Sin(progress * Mathf.PI) * 0.012f;
                selectedCardView.SetScale(1.26f + pulse);
                await Task.Yield();
            }

            await questionNarrationTask;
            await animateOverTimeAsync(
                CARD_FRONT_AFTER_NARRATION_HOLD_SECONDS,
                progress =>
                {
                    float pulse = Mathf.Sin(progress * Mathf.PI) * 0.008f;
                    selectedCardView.SetScale(1.26f + pulse);
                });

            bool isTempleApproachSceneVisible = mTempleApproachCameraObject != null;
            prepareCardLaunchPresentation(isTempleApproachSceneVisible);
            Vector2 launchStartPosition = selectedCardView.RectTransform.anchoredPosition;
            TempleCameraTransition templeCameraTransition = default;
            if (isTempleApproachSceneVisible)
            {
                templeCameraTransition = captureTempleCameraTransition(TEMPLE_APPROACH_END_SCALE, TEMPLE_MOUTH_FOCUS_CENTER);
                setTempleApproachMouthAlpha(CARD_SELECTION_DIM_MOUTH_ALPHA);
            }
            else
            {
                setTempleApproachMouthAlpha(1.0f);
            }

            mIsCardAbsorptionPresentationActive = true;
            mCardAbsorptionPresentationProgress = 0.0f;

            await animateOverTimeAsync(
                CARD_TO_MOUTH_ABSORPTION_SECONDS,
                progress =>
                {
                    mCardAbsorptionPresentationProgress = progress;
                    float cameraProgress = easeInOut(progress);
                    float suctionProgress = easeIn(Mathf.Clamp01(progress * 1.04f));
                    float absorptionProgress = easeIn(Mathf.Clamp01((progress - 0.48f) / 0.52f));
                    Vector2 launchTargetPosition;

                    if (isTempleApproachSceneVisible)
                    {
                        float cameraScale = templeCameraTransition.GetScale(TEMPLE_APPROACH_END_SCALE, cameraProgress);
                        Vector2 cameraPosition = templeCameraTransition.GetPosition(cameraProgress);
                        float inhaleBob = Mathf.Sin(progress * Mathf.PI * 2.0f) * (1.0f - cameraProgress) * 1.2f;
                        setTempleCameraPose(cameraScale, cameraPosition.y + inhaleBob, cameraPosition.x);
                        float mouthRevealProgress = easeOut(Mathf.Clamp01(progress / 0.42f));
                        setTempleApproachMouthAlpha(Mathf.Lerp(CARD_SELECTION_DIM_MOUTH_ALPHA, 1.0f, mouthRevealProgress));
                        launchTargetPosition = getTempleApproachMouthCanvasPosition() + new Vector2(0.0f, -20.0f);
                    }
                    else
                    {
                        launchTargetPosition = getMouthAnchorPosition() + new Vector2(0.0f, -24.0f);
                        mMouthImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1.04f, Mathf.Sin(progress * Mathf.PI));
                    }

                    Vector2 basePosition = Vector2.Lerp(launchStartPosition, launchTargetPosition, suctionProgress);
                    Vector2 inhaleOffset = new Vector2(Mathf.Sin(progress * Mathf.PI * 3.0f) * (1.0f - absorptionProgress) * 16.0f, Mathf.Sin(progress * Mathf.PI) * 34.0f * (1.0f - absorptionProgress));
                    selectedCardView.RectTransform.anchoredPosition = basePosition + inhaleOffset;
                    selectedCardView.SetScale(Mathf.Lerp(1.26f, 0.18f, absorptionProgress));
                    selectedCardView.SetAlpha(Mathf.Lerp(1.0f, 0.0f, absorptionProgress));
                });

            mIsCardAbsorptionPresentationActive = false;
            mCardAbsorptionPresentationProgress = 1.0f;
            setCardsVisible(false);
            if (isTempleApproachSceneVisible)
            {
                setTempleCameraPoseCenteredOnMouth(TEMPLE_APPROACH_END_SCALE, TEMPLE_MOUTH_FOCUS_CENTER);
                setTempleApproachMouthAlpha(1.0f);
            }

            selectedCardView.SetAlpha(1.0f);
            selectedCardView.ResetTransformState();
            await animateOverTimeAsync(HAND_PROMPT_AFTER_CARD_LAUNCH_DELAY_SECONDS, _ => { });
        }

        private static float getCardFrontReadHoldDurationSeconds(string questionText)
        {
            int questionLength = string.IsNullOrWhiteSpace(questionText) ? 0 : questionText.Trim().Length;
            float weightedDuration = questionLength * CARD_FRONT_READ_HOLD_PER_CHARACTER_SECONDS;
            return Mathf.Clamp(CARD_FRONT_READ_HOLD_MINIMUM_SECONDS + weightedDuration, CARD_FRONT_READ_HOLD_MINIMUM_SECONDS, CARD_FRONT_READ_HOLD_MAXIMUM_SECONDS);
        }
    }
}
