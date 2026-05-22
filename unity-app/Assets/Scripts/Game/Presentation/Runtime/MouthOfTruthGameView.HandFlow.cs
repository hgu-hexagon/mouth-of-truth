using System.Threading.Tasks;
using UnityEngine;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    public partial class MouthOfTruthGameView
    {
        public void ShowAwaitingHandInsertion()
        {
            resetHandPromptPanelAlpha();
            disableAnsweringPresentation();
            disableAnalyzingPresentation();
            bool isTempleSceneActive = isTempleApproachSceneActive();

            if (isTempleSceneActive)
            {
                applyTempleStageBackgroundPresentation(0.26f);
                applyHandPromptPanelLayout();
            }
            else
            {
                applyAwaitingHandInsertionLayout();
                mBackgroundImage.sprite = mMouthChamberBackgroundSprite;
                setBackgroundTint(STAGE_BACKGROUND_TINT);
                setObjectActive(mBackgroundImage, true);
                setObjectActive(mCarpetImage, false);
            }

            setObjectActive(mSceneOverlayImage, true);
            setGameplayOverlayAlpha(0.26f);
            configureExitButtonAsTopLeftIcon();
            setObjectActive(mExitButton, true);
            setObjectActive(mMouthImage, true);
            setMouthEffectImagesActive(false, false);
            setEyeBeamImagesActive(false);
            setObjectActive(mHandImage, false);
            setObjectActive(mRitualHandImage, false);
            setObjectActive(mPointerImage, false);
            setObjectActive(mAnswerInputField, false);
            setObjectActive(mAnswerTimerText, false);
            setObjectActive(mQuestionPanelImage, true);
            setObjectActive(mQuestionText, true);
            setObjectActive(mStatusPanelImage, false);
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mResultPanelImage, false);
            mAnswerInputField.text = string.Empty;
            mAnswerInputField.interactable = false;
            setText(mQuestionText, "“손을 내밀고, 진실을 답하라.”");
            setHandPromptPanelAlpha(1.0f);
            if (isTempleSceneActive == false)
            {
                applyMouthAnchoredLayout();
            }

            setHandVisual(0.0f);
            playInterfaceCueClean(mHandPromptClip, 0.74f);
        }

        public void BeginHandPromptDismissal()
        {
            bool isPromptVisible = mQuestionPanelImage != null && mQuestionText != null && (mQuestionPanelImage.gameObject.activeSelf || mQuestionText.gameObject.activeSelf);

            if (isPromptVisible == false)
            {
                return;
            }

            mIsHandPromptPanelDismissalActive = true;
            mHandPromptPanelDismissalStartedAtSeconds = Time.unscaledTime;
            mHandPromptPanelDismissalStartAlpha = getHandPromptPanelAlpha();
        }

        public async Task AnimateHandInsertionAsync()
        {
            disableAnsweringPresentation();
            BeginHandPromptDismissal();
            if (isTempleApproachSceneActive())
            {
                applyTempleStageBackgroundPresentation(0.30f);
                applyTopLeftExitButtonLayout();
            }
            else
            {
                applyAnswerStageLayout();
            }

            setObjectActive(mHandImage, false);
            setMouthEffectImagesActive(false, false);
            setEyeBeamImagesActive(false);
            setObjectActive(mRitualHandImage, true);
            placeRitualHandAboveMouth();
            playInterfaceCue(mHandInsertClip, 0.58f);
            Vector2 startPosition = getHandFrontPosition() + new Vector2(0.0f, -330.0f);
            Vector2 frontPosition = getHandFrontPosition() + new Vector2(0.0f, -72.0f);
            Vector2 innerPosition = getHandInnerPosition() + new Vector2(0.0f, -36.0f);

            await animateOverTimeAsync(
                HAND_INSERTION_DURATION_SECONDS,
                progress =>
                {
                    float easedProgress = easeInOut(progress);
                    float reachProgress = easeInOut(Mathf.Clamp01((easedProgress - 0.58f) / 0.42f));
                    Vector2 handPosition = Vector2.Lerp(Vector2.Lerp(startPosition, frontPosition, easeOut(Mathf.Clamp01(easedProgress / 0.72f))), innerPosition, reachProgress);
                    float fadeOutProgress = easeIn(Mathf.Clamp01((easedProgress - 0.82f) / 0.18f));
                    float handAlpha = Mathf.Lerp(Mathf.Lerp(0.0f, 1.0f, easeOut(Mathf.Clamp01(easedProgress / 0.24f))), 0.0f, fadeOutProgress);
                    float verticalLift = Mathf.Sin(easedProgress * Mathf.PI) * 7.0f;
                    float handScale = Mathf.Lerp(0.86f, 1.04f, easeOut(Mathf.Clamp01(easedProgress / 0.70f)));
                    float mouthPulse = Mathf.Sin(easedProgress * Mathf.PI);

                    setRitualHandVisual(handPosition + new Vector2(0.0f, verticalLift), RITUAL_HAND_SIZE_PIXELS, handAlpha, handScale, Mathf.Lerp(-2.0f, 1.0f, easedProgress));
                    setGameplayOverlayAlpha(Mathf.Lerp(0.30f, 0.40f, mouthPulse));

                    if (isTempleApproachSceneActive())
                    {
                        setTempleApproachMouthColor(new Color(1.0f, 0.96f, 0.86f, 1.0f));
                        syncTempleStageMouthOverlay(0.0f);
                    }
                    else
                    {
                        mMouthImage.rectTransform.localScale = Vector3.one * (1.0f + (mouthPulse * 0.045f));
                    }
                });

            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mQuestionText, false);
            resetHandPromptPanelAlpha();
            setObjectActive(mRitualHandImage, false);
            await playMouthJudgementFocusTransitionAsync();
        }

        public void ShowAnswering()
        {
            hideHandPromptPanelImmediately();
            disableAnalyzingPresentation();
            if (isTempleApproachSceneActive())
            {
                applyTempleStageBackgroundPresentation(0.32f);
                applyTopLeftExitButtonLayout();
            }
            else
            {
                applyAnswerStageLayout();
                mBackgroundImage.sprite = mMouthChamberBackgroundSprite;
                setBackgroundTint(STAGE_BACKGROUND_TINT);
                setObjectActive(mBackgroundImage, true);
                setObjectActive(mCarpetImage, false);
            }

            setObjectActive(mSceneOverlayImage, true);
            setGameplayOverlayAlpha(0.32f);
            configureExitButtonAsTopLeftIcon();
            setObjectActive(mExitButton, true);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mQuestionText, false);
            setObjectActive(mStatusPanelImage, false);
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mAnswerTimerText, false);
            setObjectActive(mHandImage, false);
            setObjectActive(mRitualHandImage, false);
            if (isTempleApproachSceneActive())
            {
                syncTempleStageMouthOverlay(0.0f);
                syncMouthEffectImageLayout(mMouthListeningAuraImage, 1.26f);
                syncMouthEffectImageLayout(mMouthAnalyzingAuraImage, 1.18f);
            }
            else
            {
                applyAnsweringFocusLayout();
            }

            setMouthEffectImagesActive(false, false);
            setEyeBeamImagesActive(true);
            enableAnsweringPresentation();
        }

        public void ShowAnalyzing()
        {
            hideHandPromptPanelImmediately();
            disableAnsweringPresentation();
            if (isTempleApproachSceneActive())
            {
                applyTempleStageBackgroundPresentation(0.36f);
                applyTopLeftExitButtonLayout();
            }
            else
            {
                applyAnswerStageLayout();
                mBackgroundImage.sprite = mMouthChamberBackgroundSprite;
                setBackgroundTint(STAGE_BACKGROUND_TINT);
                setObjectActive(mBackgroundImage, true);
                setObjectActive(mCarpetImage, false);
            }

            if (isTempleApproachSceneActive())
            {
                syncTempleStageMouthOverlay(0.0f);
                syncMouthEffectImageLayout(mMouthListeningAuraImage, 1.26f);
                syncMouthEffectImageLayout(mMouthAnalyzingAuraImage, 1.18f);
            }
            else
            {
                applyAnsweringFocusLayout();
            }

            mAnswerInputField.interactable = false;
            setObjectActive(mSceneOverlayImage, true);
            setGameplayOverlayAlpha(0.36f);
            configureExitButtonAsTopLeftIcon();
            setObjectActive(mExitButton, true);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mQuestionText, false);
            setObjectActive(mStatusPanelImage, false);
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mAnswerTimerText, false);
            setObjectActive(mPointerImage, false);
            setObjectActive(mHandImage, false);
            setObjectActive(mRitualHandImage, false);
            setMouthEffectImagesActive(true, true);
            setEyeBeamImagesActive(false);
            enableAnalyzingPresentation();
        }

        private void setRitualHandVisual(Vector2 anchoredPosition, Vector2 sizeDelta, float alpha, float scale, float rotationDegrees)
        {
            if (mRitualHandImage == null)
            {
                return;
            }

            RectTransform ritualHandRectTransform = mRitualHandImage.rectTransform;
            ritualHandRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            ritualHandRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ritualHandRectTransform.anchoredPosition = anchoredPosition;
            ritualHandRectTransform.sizeDelta = sizeDelta;
            ritualHandRectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, rotationDegrees);
            ritualHandRectTransform.localScale = Vector3.one * Mathf.Max(0.0f, scale);
            mRitualHandImage.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(alpha));
        }

        private void setHandVisual(float insertionProgress)
        {
            RectTransform handRectTransform = mHandImage.rectTransform;
            float easedProgress = easeOut(Mathf.Clamp01(insertionProgress));
            Vector2 frontPosition = getHandFrontPosition() + new Vector2(0.0f, -10.0f);
            Vector2 innerPosition = getHandInnerPosition() + new Vector2(0.0f, 10.0f);
            float lateralArcOffset = Mathf.Sin(easedProgress * Mathf.PI) * 4.0f;
            handRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            handRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            handRectTransform.anchoredPosition = Vector2.Lerp(frontPosition, innerPosition, easedProgress) + new Vector2(lateralArcOffset, 0.0f);
            handRectTransform.localRotation = Quaternion.identity;
            handRectTransform.localScale = Vector3.one * Mathf.Lerp(1.05f, 0.86f, easedProgress);
            mHandImage.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(0.94f, 0.82f, easedProgress));
        }

        private void enableAnsweringPresentation()
        {
            mIsAnsweringPresentationActive = true;
            mAnsweringPresentationStartedAtSeconds = Time.unscaledTime;

            if (mQuestionPanelImage != null)
            {
                mQuestionPanelImage.color = Color.white;
            }

            if (mAnalyzingDotsText != null)
            {
                setObjectActive(mAnalyzingDotsText, false);
                setText(mAnalyzingDotsText, string.Empty);
            }
        }

        private void disableAnsweringPresentation()
        {
            mIsAnsweringPresentationActive = false;
            setEyeBeamImagesActive(false);

            if (mQuestionPanelImage != null)
            {
                mQuestionPanelImage.color = Color.white;
            }

            if (mIsAnalyzingPresentationActive == false)
            {
                setMouthEffectImagesActive(false, false);
            }

            if (mAnalyzingDotsText != null && mIsAnalyzingPresentationActive == false)
            {
                setObjectActive(mAnalyzingDotsText, false);
                setText(mAnalyzingDotsText, string.Empty);
                mAnalyzingDotsText.rectTransform.localScale = Vector3.one;
            }
        }

        private void enableAnalyzingPresentation()
        {
            mIsAnalyzingPresentationActive = true;
            mAnalyzingPresentationStartedAtSeconds = Time.unscaledTime;

            if (isTempleApproachSceneActive())
            {
                syncTempleStageMouthOverlay(0.0f);
            }
            else
            {
                mMouthImage.color = new Color(1.0f, 1.0f, 1.0f, 0.56f);
                mMouthImage.rectTransform.localScale = Vector3.one;
            }

            setObjectActive(mAnalyzingDotsText, false);
            setText(mAnalyzingDotsText, string.Empty);
        }

        private void disableAnalyzingPresentation(bool preserveMouthLayout = false)
        {
            mIsAnalyzingPresentationActive = false;
            setEyeBeamImagesActive(false);

            if (mMouthImage != null && preserveMouthLayout == false && isTempleApproachSceneActive() == false)
            {
                mMouthImage.rectTransform.anchoredPosition = getMouthAnchorPosition();
                mMouthImage.color = Color.white;
                mMouthImage.rectTransform.localScale = Vector3.one;
            }

            setMouthEffectImagesActive(false, false);

            if (mAnalyzingDotsText != null)
            {
                setObjectActive(mAnalyzingDotsText, false);
                setText(mAnalyzingDotsText, string.Empty);
                mAnalyzingDotsText.rectTransform.localScale = Vector3.one;
            }
        }
    }
}
