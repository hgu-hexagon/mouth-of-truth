using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MouthOfTruth.Game.Analysis;
using MouthOfTruth.Game.Data;
using MouthOfTruth.Game.Input;
using MouthOfTruth.Game.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MouthOfTruth.Game.Presentation.Runtime
{
    [DisallowMultipleComponent]
    public class MouthOfTruthGameView : MonoBehaviour
    {
        private static readonly Vector2 FALLBACK_LEFT_CARD_POSITION = new Vector2(-390.0f, 60.0f);
        private static readonly Vector2 FALLBACK_CENTER_CARD_POSITION = new Vector2(0.0f, 60.0f);
        private static readonly Vector2 FALLBACK_RIGHT_CARD_POSITION = new Vector2(390.0f, 60.0f);
        private static readonly Vector2 FALLBACK_MOUTH_POSITION = new Vector2(0.0f, 60.0f);
        private static readonly Vector2 FALLBACK_HAND_FRONT_POSITION = new Vector2(0.0f, -20.0f);
        private static readonly Vector2 FALLBACK_HAND_INNER_POSITION = new Vector2(0.0f, 230.0f);
        private static readonly Color TITLE_BACKGROUND_TINT = new Color(0.95f, 0.95f, 0.97f, 1.0f);
        private static readonly Color STAGE_BACKGROUND_TINT = new Color(0.82f, 0.82f, 0.86f, 1.0f);
        private const float FRONT_ANCHOR_RADIUS_FACTOR = 0.17f;
        private const float INNER_ANCHOR_RADIUS_FACTOR = 0.085f;
        private const float FRONT_ENTRY_HALF_WIDTH_FACTOR = 0.12f;
        private const float FRONT_ENTRY_HALF_HEIGHT_FACTOR = 0.15f;
        private const float INNER_ENTRY_HALF_WIDTH_FACTOR = 0.07f;
        private const float INNER_ENTRY_HALF_HEIGHT_FACTOR = 0.09f;
        private const float CARD_INTENT_LEFT_MAX_NORMALIZED_X = 0.39f;
        private const float CARD_INTENT_RIGHT_MIN_NORMALIZED_X = 0.61f;
        private const float CARD_INTENT_MIN_NORMALIZED_Y = 0.28f;
        private const float CARD_INTENT_MAX_NORMALIZED_Y = 0.84f;
        private const float MOUTH_INTENT_HALF_WIDTH_FACTOR = 0.26f;
        private const float MOUTH_INTENT_LOWER_MARGIN_FACTOR = 0.28f;
        private const float MOUTH_INTENT_UPPER_MARGIN_FACTOR = 0.22f;
        private const float MOUTH_INTENT_INNER_SWITCH_FACTOR = 0.58f;
        private const float BUTTON_INTENT_EXPANSION_PIXELS = 54.0f;
        private const float EXIT_BUTTON_INTENT_EXPANSION_PIXELS = 32.0f;
        private const float CARD_FRONT_READ_HOLD_MINIMUM_SECONDS = 1.875f;
        private const float CARD_FRONT_READ_HOLD_MAXIMUM_SECONDS = 3.225f;
        private const float CARD_FRONT_READ_HOLD_PER_CHARACTER_SECONDS = 0.01875f;
        private const float CARD_FRONT_FOCUS_BEFORE_NARRATION_SECONDS = 0.72f;
        private const float CARD_FRONT_AFTER_NARRATION_HOLD_SECONDS = 0.58f;
        private const float CARD_HOVER_AUDIO_COOLDOWN_SECONDS = 0.22f;
        private const float FIRST_RUN_TUTORIAL_FALLBACK_DURATION_SECONDS = 4.0f;
        private const float FIRST_RUN_TUTORIAL_DURATION_SCALE = 3.0f;
        private const float HAND_INSERTION_DURATION_SECONDS = 2.85f;
        private const float MOUTH_JUDGEMENT_FOCUS_SECONDS = 0.72f;
        private const float INTERFACE_AUDIO_MAX_VOLUME_SCALE = 0.74f;
        private const float INTERFACE_AUDIO_OVERLAP_DUCK_SCALE = 0.72f;
        private const int POINTER_CURSOR_TEXTURE_SIZE = 64;
        private static readonly Vector2 POINTER_CURSOR_SIZE_PIXELS = new Vector2(46.0f, 46.0f);
        private static readonly Vector2 HELD_POINTER_CURSOR_SIZE_PIXELS = new Vector2(58.0f, 58.0f);
        private static readonly Vector2 RITUAL_HAND_SIZE_PIXELS = new Vector2(340.0f, 380.0f);
        private static readonly Color SCENE_OVERLAY_COLOR = new Color(0.03f, 0.02f, 0.02f, 1.0f);
        private static readonly Color POINTER_CURSOR_FILL_COLOR = new Color(0.62f, 0.64f, 0.66f, 0.54f);
        private static readonly Color POINTER_CURSOR_RING_COLOR = new Color(0.90f, 0.91f, 0.92f, 0.86f);

        private readonly Dictionary<EQuestionCardSlot, QuestionCardView> mCardViews =
            new Dictionary<EQuestionCardSlot, QuestionCardView>();
        private readonly Vector3[] mHitTestWorldCorners = new Vector3[4];

        private Canvas mCanvas;
        private RectTransform mCanvasRootRectTransform;
        private Transform mCanvasRootTransform;
        private Image mBackgroundImage;
        private Image mSceneOverlayImage;
        private Image mLoadingOverlayImage;
        private Image mTutorialOverlayImage;
        private Image mTutorialDevicePanelImage;
        private Image mTutorialHandImage;
        private Text mTutorialTitleText;
        private Text mTutorialBodyText;
        private Text mTutorialStepText;
        private Image mCarpetImage;
        private Image mTitleVignetteImage;
        private Image mLogoImage;
        private Image mQuestionPanelImage;
        private Image mStatusPanelImage;
        private Image mResultPanelImage;
        private Text mPromptText;
        private Text mQuestionText;
        private Text mStatusText;
        private Text mAnalyzingDotsText;
        private Text mAnswerTimerText;
        private InputField mAnswerInputField;
        private Image mMouthImage;
        private Image mHandImage;
        private Image mRitualHandImage;
        private Image mPointerImage;
        private Image mVerdictImage;
        private Text mVerdictText;
        private Button mStartButton;
        private Button mTryAgainButton;
        private Button mBackToTitleButton;
        private Button mExitButton;
        private Sprite mCardBackSprite;
        private Sprite mCardFrontSprite;
        private Sprite mButtonFrameSprite;
        private Sprite mStartButtonSprite;
        private Sprite mTryAgainButtonSprite;
        private Sprite mEndGameButtonSprite;
        private Sprite mExitIconButtonSprite;
        private Sprite mPointerCursorSprite;
        private Sprite mRitualHandSprite;
        private Sprite mVerdictTrueSprite;
        private Sprite mVerdictFalseSprite;
        private Sprite mVerdictUncertainSprite;
        private Sprite mTitleVignetteSprite;
        private Sprite mQuestionPanelSprite;
        private Sprite mStatusPanelSprite;
        private Sprite mResultPanelSprite;
        private Sprite mCardGlowSprite;
        private Sprite mDwellFillSprite;
        private Sprite mTitleBackgroundSprite;
        private Sprite mCardSelectionBackgroundSprite;
        private Sprite mMouthChamberBackgroundSprite;
        private AudioSource mAmbienceAudioSource;
        private AudioSource mInterfaceAudioSource;
        private AudioClip mTitleAmbienceClip;
        private AudioClip mButtonConfirmClip;
        private AudioClip mCardHoverClip;
        private AudioClip mCardSelectClip;
        private AudioClip mCardRevealClip;
        private AudioClip mHandInsertClip;
        private AudioClip mHandPromptClip;
        private AudioClip mResultTrueClip;
        private AudioClip mResultFalseClip;
        private AudioClip mResultUncertainClip;
        private Font mUiFont;
        private Font mKoreanFallbackFont;
        private EQuestionCardSlot? mLastAudibleHoveredCardSlot;
        private Camera mWorldCamera;
        private CardPresentationAnchorSet mCardPresentationAnchorSet;
        private MouthAnchorSet mMouthAnchorSet;
        private bool mUseWorldEnvironmentLayout;
        private EUiActionTarget? mLastHoveredUiActionTarget;
        private bool mUseHeldHandPresentation;
        private bool mIsAnsweringPresentationActive;
        private bool mIsAnalyzingPresentationActive;
        private float mHeldHandBaseProgress;
        private float mHeldHandPulseAmplitude;
        private float mHeldHandPulseSpeed;
        private float mAnsweringPresentationStartedAtSeconds;
        private float mAnalyzingPresentationStartedAtSeconds;
        private float mLastCardHoverCueTimeSeconds = -999.0f;

        private bool mStartRequested;
        private bool mTryAgainRequested;
        private bool mBackToTitleRequested;
        private bool mExitRequested;

        [Serializable]
        private sealed class TutorialSequenceMetadata
        {
            public float fr;
            public float ip;
            public float op;
        }

        public async Task InitializeAsync()
        {
            ensureEventSystemExists();
            loadUiFonts();
            buildCanvas();
            cacheWorldPresentationReferences();
            buildAudioSources();
            await loadSpritesAsync();
            await loadAudioClipsAsync();
            applyTheme();
            refreshWorldPresentationLayout();
            ShowStartScreen();
            setObjectActive(mLoadingOverlayImage, false);
        }

        private void LateUpdate()
        {
            ensureAmbiencePlayback();
            updateHeldHandPresentation();
            updateAnsweringPresentation();
            updateAnalyzingPresentation();
        }

        private void updateAnsweringPresentation()
        {
            if (mIsAnsweringPresentationActive == false || mQuestionText == null)
            {
                return;
            }

            float elapsedSeconds = Time.unscaledTime - mAnsweringPresentationStartedAtSeconds;
            int dotCount = 1 + (Mathf.FloorToInt(elapsedSeconds * 2.0f) % 3);
            setText(mAnalyzingDotsText, new string('.', dotCount));

            if (mQuestionPanelImage == null)
            {
                return;
            }

            float pulse = (Mathf.Sin(elapsedSeconds * 3.0f) + 1.0f) * 0.5f;
            float mouthPulse = (Mathf.Sin(elapsedSeconds * 2.45f) + 1.0f) * 0.5f;
            mQuestionPanelImage.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(0.86f, 1.0f, pulse));
            setOverlayTint(new Color(0.025f, 0.018f, 0.014f, 1.0f), Mathf.Lerp(0.26f, 0.36f, pulse));

            if (mMouthImage != null)
            {
                mMouthImage.color = new Color(1.0f, 0.96f, 0.88f, Mathf.Lerp(0.94f, 1.0f, mouthPulse));
                mMouthImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.02f, 1.075f, mouthPulse);
            }

            if (mAnalyzingDotsText != null)
            {
                mAnalyzingDotsText.rectTransform.anchoredPosition = getMouthAnchorPosition() + new Vector2(0.0f, -108.0f);
                mAnalyzingDotsText.color = new Color(1.0f, 0.82f, 0.58f, Mathf.Lerp(0.55f, 0.95f, pulse));
                mAnalyzingDotsText.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.72f, 0.94f, pulse);
            }
        }

        private void updateHeldHandPresentation()
        {
            if (mUseHeldHandPresentation == false || mHandImage == null || mHandImage.gameObject.activeSelf == false)
            {
                return;
            }

            float insertionProgress = Mathf.Clamp01(
                mHeldHandBaseProgress
                + (Mathf.Sin(Time.unscaledTime * mHeldHandPulseSpeed) * mHeldHandPulseAmplitude));
            setHandVisual(insertionProgress);
        }

        private void updateAnalyzingPresentation()
        {
            if (mIsAnalyzingPresentationActive == false || mAnalyzingDotsText == null)
            {
                return;
            }

            float elapsedSeconds = Time.unscaledTime - mAnalyzingPresentationStartedAtSeconds;
            int dotCount = 1 + (Mathf.FloorToInt(elapsedSeconds * 2.2f) % 3);
            setText(mAnalyzingDotsText, new string('.', dotCount));

            if (mMouthImage == null)
            {
                return;
            }

            float pulse = (Mathf.Sin(elapsedSeconds * 3.2f) + 1.0f) * 0.5f;
            float tremor = Mathf.Sin(elapsedSeconds * 8.0f) * 3.0f;
            float focusProgress = Mathf.Clamp01(elapsedSeconds / 0.85f);
            setOverlayTint(new Color(0.02f, 0.015f, 0.018f, 1.0f), Mathf.Lerp(0.50f, 0.68f, pulse));
            mMouthImage.color = new Color(0.94f, 0.91f, 0.84f, Mathf.Lerp(0.46f, 0.74f, pulse));
            mMouthImage.rectTransform.anchoredPosition = getMouthAnchorPosition() + new Vector2(tremor, 0.0f);
            mMouthImage.rectTransform.localScale = Vector3.one * (Mathf.Lerp(1.08f, 1.26f, easeOut(focusProgress)) + (pulse * 0.055f));
            mAnalyzingDotsText.color = new Color(1.0f, 0.83f, 0.58f, Mathf.Lerp(0.70f, 1.0f, pulse));
            mAnalyzingDotsText.rectTransform.anchoredPosition = getMouthAnchorPosition() + new Vector2(0.0f, -40.0f);
            mAnalyzingDotsText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.08f, 1.34f, pulse);
        }

        public void ShowStartScreen()
        {
            disableAnsweringPresentation();
            disableAnalyzingPresentation();
            disableHeldHandPresentation();
            applyStartScreenLayout();
            configureExitButtonAsTopLeftIcon();
            mBackgroundImage.sprite = mTitleBackgroundSprite;
            setBackgroundTint(TITLE_BACKGROUND_TINT);
            setObjectActive(mLogoImage, true);
            setObjectActive(mTitleVignetteImage, true);
            setObjectActive(mSceneOverlayImage, false);
            setObjectActive(mStartButton, true);
            setObjectActive(mExitButton, true);
            setObjectActive(mBackgroundImage, true);
            setObjectActive(mCarpetImage, false);
            setObjectActive(mQuestionText, false);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mStatusPanelImage, false);
            setObjectActive(mResultPanelImage, false);
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mAnswerTimerText, false);
            setObjectActive(mAnswerInputField, false);
            setObjectActive(mMouthImage, false);
            setObjectActive(mHandImage, false);
            setObjectActive(mRitualHandImage, false);
            setObjectActive(mPointerImage, false);
            setObjectActive(mVerdictImage, false);
            setObjectActive(mVerdictText, false);
            setObjectActive(mTryAgainButton, false);
            setObjectActive(mBackToTitleButton, false);
            setCardsVisible(false);
            setText(mPromptText, string.Empty);
            setText(mStatusText, string.Empty);
            setText(mAnswerTimerText, string.Empty);
            mLastAudibleHoveredCardSlot = null;
            mLastHoveredUiActionTarget = null;
            mLastCardHoverCueTimeSeconds = -999.0f;
            refreshWorldPresentationLayout();
            ensureAmbiencePlayback();
        }

        public void ShowCardSelection(QuestionRoundSelection questionRoundSelection)
        {
            disableAnsweringPresentation();
            disableAnalyzingPresentation();
            disableHeldHandPresentation();
            applyCardSelectionLayout();
            configureExitButtonAsTopLeftIcon();
            mBackgroundImage.sprite = mCardSelectionBackgroundSprite;
            setBackgroundTint(STAGE_BACKGROUND_TINT);
            setObjectActive(mBackgroundImage, true);
            setObjectActive(mCarpetImage, true);
            setObjectActive(mLogoImage, false);
            setObjectActive(mTitleVignetteImage, false);
            setObjectActive(mSceneOverlayImage, false);
            setObjectActive(mStartButton, false);
            setObjectActive(mExitButton, true);
            setObjectActive(mQuestionText, false);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mStatusPanelImage, false);
            setObjectActive(mResultPanelImage, false);
            setObjectActive(mPromptText, true);
            setObjectActive(mStatusText, false);
            setObjectActive(mAnswerTimerText, false);
            setObjectActive(mAnswerInputField, false);
            setObjectActive(mMouthImage, false);
            setObjectActive(mHandImage, false);
            setObjectActive(mRitualHandImage, false);
            setObjectActive(mPointerImage, false);
            setObjectActive(mVerdictImage, false);
            setObjectActive(mVerdictText, false);
            setObjectActive(mTryAgainButton, false);
            setObjectActive(mBackToTitleButton, false);
            setCardsVisible(true);

            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                pair.Value.SetBack(mCardBackSprite);
                pair.Value.ResetTransformState();
                pair.Value.SetVisualState(false, false, 0.0f);
                pair.Value.ResetHoverState();
            }

            applyCardAnchorPositions();
            setText(mPromptText, "원하는 질문을 손가락으로 선택하세요.");
            setText(mStatusText, string.Empty);
            setText(mAnswerTimerText, string.Empty);
            mLastAudibleHoveredCardSlot = null;
            mLastHoveredUiActionTarget = null;
            mLastCardHoverCueTimeSeconds = -999.0f;
        }

        public void UpdateCardHoverVisual(EQuestionCardSlot? hoveredQuestionCardSlot, float hoverProgress)
        {
            if (hoveredQuestionCardSlot != mLastAudibleHoveredCardSlot)
            {
                bool hasEnoughCardHoverAudioGap = Time.unscaledTime - mLastCardHoverCueTimeSeconds >= CARD_HOVER_AUDIO_COOLDOWN_SECONDS;

                if (hoveredQuestionCardSlot.HasValue && hasEnoughCardHoverAudioGap)
                {
                    mLastCardHoverCueTimeSeconds = Time.unscaledTime;
                    playInterfaceCue(mCardHoverClip, 0.32f);
                }

                mLastAudibleHoveredCardSlot = hoveredQuestionCardSlot;
            }

            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                bool isHovered = hoveredQuestionCardSlot == pair.Key;
                pair.Value.SetVisualState(false, false, isHovered ? hoverProgress : 0.0f);
            }
        }

        public void PreviewCardSelectionFocus(EQuestionCardSlot selectedQuestionCardSlot)
        {
            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                bool isSelected = pair.Key == selectedQuestionCardSlot;
                pair.Value.SetVisualState(isDimmed: isSelected == false, isSelected, 0.0f);
            }

            setText(mPromptText, string.Empty);
        }

        public async Task PlayQuestionRevealAsync(
            EQuestionCardSlot selectedQuestionCardSlot,
            QuestionDefinition questionDefinition,
            Func<Task> questionNarrationTaskFactory = null)
        {
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mQuestionText, false);
            setObjectActive(mSceneOverlayImage, true);
            setOverlayAlpha(0.12f);
            playInterfaceCue(mCardSelectClip, 0.58f);

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
                    selectedCardView.RectTransform.anchoredPosition =
                        Vector2.Lerp(startPosition, endPosition, easedProgress);
                    selectedCardView.RectTransform.localScale =
                        Vector3.one * Mathf.Lerp(1.0f, 1.22f, easedProgress);
                });

            selectedCardView.SetFront(mCardFrontSprite, questionDefinition.Text);
            playInterfaceCue(mCardRevealClip, 0.58f);

            await animateOverTimeAsync(
                0.16f,
                progress =>
                {
                    float easedProgress = easeOut(progress);
                    selectedCardView.SetScale(Mathf.Lerp(1.22f, 1.26f, easedProgress));
                });

            await animateOverTimeAsync(
                CARD_FRONT_FOCUS_BEFORE_NARRATION_SECONDS,
                progress =>
                {
                    float pulse = Mathf.Sin(progress * Mathf.PI) * 0.006f;
                    selectedCardView.SetScale(1.26f + pulse);
                });

            Task questionNarrationTask = questionNarrationTaskFactory?.Invoke() ?? Task.CompletedTask;
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

            prepareCardLaunchPresentation();
            Vector2 launchStartPosition = selectedCardView.RectTransform.anchoredPosition;
            Vector2 launchTargetPosition = getMouthAnchorPosition() + new Vector2(0.0f, -24.0f);

            await animateOverTimeAsync(
                0.78f,
                progress =>
                {
                    float easedProgress = easeOut(progress);
                    Vector2 basePosition = Vector2.Lerp(
                        launchStartPosition,
                        launchTargetPosition,
                        easedProgress);
                    float arcLift = Mathf.Sin(easedProgress * Mathf.PI) * 56.0f;
                    selectedCardView.RectTransform.anchoredPosition =
                        basePosition + new Vector2(0.0f, arcLift);
                    selectedCardView.SetScale(Mathf.Lerp(1.26f, 0.82f, easedProgress));
                    selectedCardView.SetAlpha(Mathf.Lerp(1.0f, 0.0f, easedProgress));
                    mMouthImage.rectTransform.localScale =
                        Vector3.one * Mathf.Lerp(0.94f, 1.0f, easedProgress);
                });

            setCardsVisible(false);
            selectedCardView.SetAlpha(1.0f);
            selectedCardView.ResetTransformState();
        }

        public async Task PlayFirstRunTutorialAsync()
        {
            float tutorialDurationSeconds = getFirstRunTutorialDurationSeconds() * FIRST_RUN_TUTORIAL_DURATION_SCALE;
            configureExitButtonAsTopLeftIcon();
            setObjectActive(mTutorialOverlayImage, true);
            setObjectActive(mTutorialDevicePanelImage, true);
            setObjectActive(mTutorialHandImage, true);
            setObjectActive(mTutorialTitleText, true);
            setObjectActive(mTutorialBodyText, true);
            setObjectActive(mTutorialStepText, true);
            setObjectActive(mStartButton, false);
            setObjectActive(mTryAgainButton, false);
            setObjectActive(mBackToTitleButton, false);
            setObjectActive(mExitButton, true);
            mTutorialOverlayImage.transform.SetAsLastSibling();
            mTutorialDevicePanelImage.transform.SetAsLastSibling();
            mTutorialHandImage.transform.SetAsLastSibling();
            mTutorialTitleText.transform.SetAsLastSibling();
            mTutorialBodyText.transform.SetAsLastSibling();
            mTutorialStepText.transform.SetAsLastSibling();
            mExitButton.transform.SetAsLastSibling();
            setText(mTutorialTitleText, "손을 장치 위에서 천천히 움직여 주세요");
            setText(mTutorialBodyText, "손끝 방향으로 버튼과 카드를 가리키고, 같은 위치에 잠시 머물면 선택됩니다.");
            RectTransform handRectTransform = mTutorialHandImage.rectTransform;

            await animateOverTimeAsync(
                tutorialDurationSeconds,
                progress =>
                {
                    float firstSegmentProgress = Mathf.Clamp01(progress / 0.48f);
                    float secondSegmentProgress = Mathf.Clamp01((progress - 0.38f) / 0.62f);
                    Vector2 hoverStartPosition = new Vector2(0.0f, -130.0f);
                    Vector2 hoverReadyPosition = new Vector2(0.0f, -18.0f);
                    handRectTransform.anchoredPosition = progress < 0.48f
                        ? Vector2.Lerp(hoverStartPosition, hoverReadyPosition, easeOut(firstSegmentProgress))
                        : getTutorialScanPosition(secondSegmentProgress);
                    handRectTransform.localScale = Vector3.one * Mathf.Lerp(0.78f, 1.02f, easeOut(firstSegmentProgress));
                    mTutorialOverlayImage.color = new Color(0.055f, 0.056f, 0.064f, 1.0f);
                    setText(mTutorialStepText, progress < 0.48f
                        ? "1. 손을 Leap Motion 위에 자연스럽게 올립니다."
                        : "2. 왼쪽, 가운데, 오른쪽 방향을 천천히 가리킵니다.");
                });

            setObjectActive(mTutorialOverlayImage, false);
            setObjectActive(mTutorialDevicePanelImage, false);
            setObjectActive(mTutorialHandImage, false);
            setObjectActive(mTutorialTitleText, false);
            setObjectActive(mTutorialBodyText, false);
            setObjectActive(mTutorialStepText, false);
        }

        public void ShowAwaitingHandInsertion()
        {
            disableAnsweringPresentation();
            disableAnalyzingPresentation();
            disableHeldHandPresentation();
            applyAwaitingHandInsertionLayout();
            mBackgroundImage.sprite = mMouthChamberBackgroundSprite;
            setBackgroundTint(STAGE_BACKGROUND_TINT);
            setObjectActive(mBackgroundImage, true);
            setObjectActive(mCarpetImage, false);
            setObjectActive(mSceneOverlayImage, true);
            setOverlayAlpha(0.26f);
            configureExitButtonAsTopLeftIcon();
            setObjectActive(mExitButton, true);
            setObjectActive(mMouthImage, true);
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
            applyMouthAnchoredLayout();
            setHandVisual(0.0f);
            playInterfaceCue(mHandPromptClip, 0.70f);
        }

        public async Task AnimateHandInsertionAsync()
        {
            disableAnsweringPresentation();
            disableHeldHandPresentation();
            applyAnswerStageLayout();
            setObjectActive(mHandImage, false);
            setObjectActive(mRitualHandImage, true);
            placeRitualHandAboveMouth();
            playInterfaceCue(mHandInsertClip, 0.68f);
            Vector2 startPosition = getHandFrontPosition() + new Vector2(0.0f, -270.0f);
            Vector2 frontPosition = getHandFrontPosition() + new Vector2(0.0f, -42.0f);
            Vector2 innerPosition = getHandInnerPosition() + new Vector2(0.0f, -18.0f);

            await animateOverTimeAsync(
                HAND_INSERTION_DURATION_SECONDS,
                progress =>
                {
                    float easedProgress = easeInOut(progress);
                    float approachProgress = Mathf.Clamp01(easedProgress / 0.46f);
                    float insertionProgress = Mathf.Clamp01((easedProgress - 0.40f) / 0.60f);
                    Vector2 handPosition = easedProgress < 0.46f
                        ? Vector2.Lerp(startPosition, frontPosition, easeOut(approachProgress))
                        : Vector2.Lerp(frontPosition, innerPosition, easeInOut(insertionProgress));
                    float handAlpha = easedProgress < 0.82f
                        ? Mathf.Lerp(0.0f, 1.0f, easeOut(Mathf.Clamp01(easedProgress / 0.30f)))
                        : Mathf.Lerp(1.0f, 0.72f, Mathf.Clamp01((easedProgress - 0.82f) / 0.18f));
                    float arc = Mathf.Sin(easedProgress * Mathf.PI) * 9.0f;
                    float handScale = easedProgress < 0.50f
                        ? Mathf.Lerp(0.72f, 1.08f, easeOut(easedProgress / 0.50f))
                        : Mathf.Lerp(1.08f, 0.94f, easeInOut((easedProgress - 0.50f) / 0.50f));
                    float mouthPulse = Mathf.Sin(easedProgress * Mathf.PI);
                    float mouthPull = Mathf.Clamp01((easedProgress - 0.50f) / 0.50f);

                    setRitualHandVisual(
                        handPosition + new Vector2(arc, 0.0f),
                        RITUAL_HAND_SIZE_PIXELS,
                        handAlpha,
                        handScale,
                        Mathf.Lerp(-5.0f, 2.0f, easedProgress));
                    setOverlayAlpha(Mathf.Lerp(0.30f, 0.48f, mouthPulse));
                    mMouthImage.rectTransform.localScale =
                        Vector3.one * (1.0f + (mouthPulse * 0.12f) + (mouthPull * 0.055f));
                });

            await animateOverTimeAsync(
                0.46f,
                progress =>
                {
                    float easedProgress = easeIn(progress);
                    setRitualHandVisual(
                        innerPosition + new Vector2(0.0f, Mathf.Lerp(0.0f, 42.0f, easedProgress)),
                        RITUAL_HAND_SIZE_PIXELS,
                        Mathf.Lerp(0.72f, 0.0f, easedProgress),
                        Mathf.Lerp(0.94f, 0.82f, easedProgress),
                        Mathf.Lerp(2.0f, 0.0f, easedProgress));
                    setOverlayAlpha(Mathf.Lerp(0.48f, 0.36f, easedProgress));
                });

            setObjectActive(mRitualHandImage, false);
            await playMouthJudgementFocusTransitionAsync();
        }

        public void ShowAnswering()
        {
            disableAnalyzingPresentation();
            applyAnswerStageLayout();
            mBackgroundImage.sprite = mMouthChamberBackgroundSprite;
            setBackgroundTint(STAGE_BACKGROUND_TINT);
            setObjectActive(mBackgroundImage, true);
            setObjectActive(mCarpetImage, false);
            setObjectActive(mSceneOverlayImage, true);
            setOverlayAlpha(0.28f);
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
            applyAnsweringFocusLayout();
            disableHeldHandPresentation();
            enableAnsweringPresentation();
        }

        public void ShowAnalyzing()
        {
            disableAnsweringPresentation();
            disableHeldHandPresentation();
            applyAnswerStageLayout();
            applyAnsweringFocusLayout();
            mBackgroundImage.sprite = mMouthChamberBackgroundSprite;
            setBackgroundTint(STAGE_BACKGROUND_TINT);
            setObjectActive(mBackgroundImage, true);
            setObjectActive(mCarpetImage, false);
            mAnswerInputField.interactable = false;
            setObjectActive(mSceneOverlayImage, true);
            setOverlayAlpha(0.34f);
            configureExitButtonAsTopLeftIcon();
            setObjectActive(mExitButton, true);
            setObjectActive(mQuestionPanelImage, true);
            setObjectActive(mQuestionText, true);
            setObjectActive(mStatusPanelImage, false);
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mAnswerTimerText, false);
            setText(mQuestionText, "진실의 입이 답을 살피고 있습니다.");
            setObjectActive(mPointerImage, false);
            applyMouthAnchoredLayout();
            setObjectActive(mHandImage, false);
            setObjectActive(mRitualHandImage, false);
            enableAnalyzingPresentation();
        }

        public async Task PlayAnalysisCompleteTransitionAsync()
        {
            if (mIsAnalyzingPresentationActive == false)
            {
                return;
            }

            await animateOverTimeAsync(
                0.18f,
                progress =>
                {
                    float easedProgress = easeOut(progress);
                    setOverlayTint(new Color(0.02f, 0.015f, 0.018f, 1.0f), Mathf.Lerp(0.56f, 0.48f, easedProgress));
                    mMouthImage.color = new Color(1.0f, 0.93f, 0.82f, Mathf.Lerp(0.62f, 0.42f, easedProgress));
                    mMouthImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.03f, 1.08f, easedProgress);
                    mAnalyzingDotsText.color = new Color(1.0f, 0.92f, 0.78f, Mathf.Lerp(1.0f, 0.0f, easedProgress));
                });

            disableAnalyzingPresentation();
        }

        private async Task playMouthJudgementFocusTransitionAsync()
        {
            Vector2 mouthAnchorPosition = getMouthAnchorPosition();

            await animateOverTimeAsync(
                MOUTH_JUDGEMENT_FOCUS_SECONDS,
                progress =>
                {
                    float easedProgress = easeInOut(progress);
                    float pulse = Mathf.Sin(progress * Mathf.PI);
                    setOverlayTint(new Color(0.025f, 0.015f, 0.012f, 1.0f), Mathf.Lerp(0.36f, 0.50f, easedProgress));
                    mMouthImage.color = new Color(1.0f, 0.94f, 0.84f, Mathf.Lerp(1.0f, 0.86f, easedProgress));
                    mMouthImage.rectTransform.anchoredPosition = mouthAnchorPosition;
                    mMouthImage.rectTransform.localScale = Vector3.one * (Mathf.Lerp(1.08f, 1.26f, easedProgress) + (pulse * 0.035f));
                });
        }

        public void ShowResult(EVerdictKind verdictKind, string transcriptText)
        {
            disableAnsweringPresentation();
            disableAnalyzingPresentation();
            disableHeldHandPresentation();
            applyResultLayout(verdictKind);
            configureExitButtonAsTopLeftIcon();
            setCardsVisible(false);
            mBackgroundImage.sprite = mMouthChamberBackgroundSprite;
            setBackgroundTint(STAGE_BACKGROUND_TINT);
            setObjectActive(mBackgroundImage, true);
            setObjectActive(mCarpetImage, false);
            setObjectActive(mTitleVignetteImage, false);
            setObjectActive(mSceneOverlayImage, true);
            setOverlayAlpha(0.38f);
            setObjectActive(mQuestionText, false);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mStatusPanelImage, false);
            setObjectActive(mMouthImage, true);
            setObjectActive(mHandImage, false);
            setObjectActive(mRitualHandImage, false);
            setObjectActive(mVerdictImage, true);
            setObjectActive(mVerdictText, false);
            setObjectActive(mResultPanelImage, false);
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mAnswerTimerText, false);
            setObjectActive(mTryAgainButton, true);
            setObjectActive(mBackToTitleButton, false);
            setObjectActive(mExitButton, true);
            setObjectActive(mAnswerInputField, false);
            mAnswerInputField.interactable = false;

            mVerdictImage.sprite = verdictKind switch
            {
                EVerdictKind.True => mVerdictTrueSprite,
                EVerdictKind.False => mVerdictFalseSprite,
                _ => mVerdictUncertainSprite,
            };
            string verdictText = verdictKind switch
            {
                EVerdictKind.True => "TRUE",
                EVerdictKind.False => "FALSE",
                _ => "UNCERTAIN",
            };
            setText(mVerdictText, verdictText);
            applyMouthAnchoredLayout();
            mMouthImage.color = Color.white;
            mMouthImage.rectTransform.localScale = Vector3.one;
            mVerdictImage.color = Color.white;
            mVerdictImage.rectTransform.localRotation = Quaternion.identity;
            mVerdictImage.rectTransform.localScale = Vector3.one;
            playVerdictCue(verdictKind);
        }

        public async Task PlayResultRevealAnimationAsync(EVerdictKind verdictKind)
        {
            setObjectActive(mTryAgainButton, false);

            if (verdictKind == EVerdictKind.True)
            {
                await playTrueRevealAnimationAsync();
            }
            else if (verdictKind == EVerdictKind.False)
            {
                await playFalseRevealAnimationAsync();
            }
            else
            {
                await playUncertainRevealAnimationAsync();
            }

            setObjectActive(mTryAgainButton, true);
            setOverlayAlpha(0.38f);
            mMouthImage.color = Color.white;
            mMouthImage.rectTransform.localScale = Vector3.one;
            mVerdictImage.color = Color.white;
            mVerdictImage.rectTransform.localRotation = Quaternion.identity;
            mVerdictImage.rectTransform.localScale = Vector3.one;
        }

        private async Task playTrueRevealAnimationAsync()
        {
            mVerdictImage.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);

            await animateOverTimeAsync(
                0.68f,
                progress =>
                {
                    float easedProgress = easeOut(progress);
                    float glow = Mathf.Sin(easedProgress * Mathf.PI);
                    setOverlayTint(new Color(0.05f, 0.11f, 0.06f, 1.0f), Mathf.Lerp(0.46f, 0.32f, easedProgress));
                    mMouthImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.025f, 1.0f, easedProgress);
                    mMouthImage.color = new Color(0.88f, 1.0f, 0.82f, Mathf.Lerp(0.86f, 1.0f, easedProgress));
                    mVerdictImage.color = new Color(1.0f, 1.0f, 1.0f, easedProgress);
                    mVerdictImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.90f, 1.0f + (glow * 0.02f), easedProgress);
                });
        }

        private async Task playFalseRevealAnimationAsync()
        {
            mVerdictImage.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);

            await animateOverTimeAsync(
                0.32f,
                progress =>
                {
                    float easedProgress = easeInOut(progress);
                    setOverlayTint(new Color(0.28f, 0.012f, 0.008f, 1.0f), Mathf.Lerp(0.42f, 0.70f, easedProgress));
                    mMouthImage.color = new Color(1.0f, 0.68f, 0.58f, Mathf.Lerp(0.88f, 1.0f, easedProgress));
                    mMouthImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.22f, easedProgress);
                });

            await animateOverTimeAsync(
                0.56f,
                progress =>
                {
                    float easedProgress = easeOut(progress);
                    float shake = Mathf.Sin(progress * Mathf.PI * 14.0f) * (1.0f - easedProgress);
                    setOverlayTint(new Color(0.22f, 0.006f, 0.006f, 1.0f), Mathf.Lerp(0.70f, 0.40f, easedProgress));
                    mMouthImage.color = new Color(1.0f, Mathf.Lerp(0.60f, 1.0f, easedProgress), Mathf.Lerp(0.54f, 1.0f, easedProgress), 1.0f);
                    mMouthImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.22f, 1.0f, easedProgress);
                    mVerdictImage.color = new Color(1.0f, 1.0f, 1.0f, easedProgress);
                    mVerdictImage.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, shake * 2.5f);
                    mVerdictImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.34f, 1.0f, easedProgress);
                });
        }

        private async Task playUncertainRevealAnimationAsync()
        {
            mVerdictImage.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);

            await animateOverTimeAsync(
                0.72f,
                progress =>
                {
                    float easedProgress = easeInOut(progress);
                    float wobble = Mathf.Sin(progress * Mathf.PI * 7.0f) * (1.0f - easedProgress);
                    float flickerAlpha = Mathf.Lerp(0.25f, 1.0f, easedProgress)
                        + (Mathf.Sin(progress * Mathf.PI * 9.0f) * 0.08f * (1.0f - easedProgress));
                    setOverlayTint(new Color(0.055f, 0.055f, 0.075f, 1.0f), Mathf.Lerp(0.48f, 0.40f, easedProgress));
                    mMouthImage.color = new Color(0.74f, 0.76f, 0.82f, Mathf.Lerp(0.74f, 1.0f, easedProgress));
                    mMouthImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.985f, 1.0f, easedProgress);
                    mVerdictImage.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(flickerAlpha));
                    mVerdictImage.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, wobble * 2.5f);
                    mVerdictImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.0f, easedProgress);
                });
        }

        public void UpdateAnswerMetrics(float elapsedAnswerSeconds, float elapsedSilenceSeconds)
        {
            _ = elapsedAnswerSeconds;
            _ = elapsedSilenceSeconds;
        }

        public EQuestionCardSlot? GetHoveredQuestionCardSlot(Vector2? pointerScreenPosition)
        {
            if (pointerScreenPosition.HasValue)
            {
                foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
                {
                    if (pair.Value.gameObject.activeInHierarchy == false)
                    {
                        continue;
                    }

                    if (isScreenPointOverRectTransform(pair.Value.RectTransform, pointerScreenPosition.Value))
                    {
                        return pair.Key;
                    }
                }

                return EvaluateQuestionCardIntentSlot(
                    pointerScreenPosition.Value,
                    Screen.width,
                    Screen.height);
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

        public EUiActionTarget? GetHoveredUiActionTarget(Vector2? pointerScreenPosition)
        {
            if (pointerScreenPosition.HasValue == false)
            {
                return null;
            }

            Vector2 screenPosition = pointerScreenPosition.Value;

            if (isScreenPointOverButton(
                    mStartButton,
                    screenPosition,
                    BUTTON_INTENT_EXPANSION_PIXELS))
            {
                return EUiActionTarget.StartGame;
            }

            if (isScreenPointOverButton(
                    mTryAgainButton,
                    screenPosition,
                    BUTTON_INTENT_EXPANSION_PIXELS))
            {
                return EUiActionTarget.TryAgain;
            }

            if (isScreenPointOverButton(
                    mExitButton,
                    screenPosition,
                    EXIT_BUTTON_INTENT_EXPANSION_PIXELS))
            {
                return EUiActionTarget.ExitGame;
            }

            if (isScreenPointOverButton(
                    mBackToTitleButton,
                    screenPosition,
                    BUTTON_INTENT_EXPANSION_PIXELS))
            {
                return EUiActionTarget.BackToTitle;
            }

            return null;
        }

        public void UpdateActionButtonHoverVisual(
            EUiActionTarget? hoveredUiActionTarget,
            float hoverProgress)
        {
            if (hoveredUiActionTarget != mLastHoveredUiActionTarget)
            {
                mLastHoveredUiActionTarget = hoveredUiActionTarget;
            }

            updateButtonVisual(mStartButton, hoveredUiActionTarget == EUiActionTarget.StartGame, hoverProgress);
            updateButtonVisual(mTryAgainButton, hoveredUiActionTarget == EUiActionTarget.TryAgain, hoverProgress);
            updateButtonVisual(mExitButton, hoveredUiActionTarget == EUiActionTarget.ExitGame, hoverProgress);
            updateButtonVisual(
                mBackToTitleButton,
                hoveredUiActionTarget == EUiActionTarget.BackToTitle,
                hoverProgress);
        }

        public EHandAnchorState GetHandAnchorState(Vector2? pointerScreenPosition)
        {
            if (pointerScreenPosition.HasValue == false)
            {
                return EHandAnchorState.OutsideMouth;
            }

            if (tryConvertScreenPointToCanvasPosition(
                    pointerScreenPosition.Value,
                    out Vector2 pointerCanvasPosition) == false)
            {
                return EHandAnchorState.OutsideMouth;
            }

            float mouthDiameterPixels = Mathf.Max(
                1.0f,
                Mathf.Min(mMouthImage.rectTransform.rect.width, mMouthImage.rectTransform.rect.height));
            EHandAnchorState exactAnchorState = EvaluateHandAnchorState(
                pointerCanvasPosition,
                getHandFrontPosition(),
                getHandInnerPosition(),
                mouthDiameterPixels);

            if (exactAnchorState != EHandAnchorState.OutsideMouth)
            {
                return exactAnchorState;
            }

            return EvaluateMouthIntentAnchorState(
                pointerCanvasPosition,
                getHandFrontPosition(),
                getHandInnerPosition(),
                mouthDiameterPixels);
        }

        public static EQuestionCardSlot? EvaluateQuestionCardIntentSlot(
            Vector2 screenPosition,
            float screenWidth,
            float screenHeight)
        {
            if (screenWidth <= 0.0f || screenHeight <= 0.0f)
            {
                return null;
            }

            float normalizedX = Mathf.Clamp01(screenPosition.x / screenWidth);
            float normalizedY = Mathf.Clamp01(screenPosition.y / screenHeight);

            if (normalizedY < CARD_INTENT_MIN_NORMALIZED_Y
                || normalizedY > CARD_INTENT_MAX_NORMALIZED_Y)
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

        public static EHandAnchorState EvaluateHandAnchorState(
            Vector2 pointerCanvasPosition,
            Vector2 handFrontPosition,
            Vector2 handInnerPosition,
            float mouthDiameterPixels)
        {
            float clampedMouthDiameterPixels = Mathf.Max(1.0f, mouthDiameterPixels);
            float frontAnchorRadiusPixels = clampedMouthDiameterPixels * FRONT_ANCHOR_RADIUS_FACTOR;
            float innerAnchorRadiusPixels = clampedMouthDiameterPixels * INNER_ANCHOR_RADIUS_FACTOR;
            float distanceToInnerAnchor = Vector2.Distance(pointerCanvasPosition, handInnerPosition);
            Vector2 innerAnchorOffset = pointerCanvasPosition - handInnerPosition;

            if (distanceToInnerAnchor <= innerAnchorRadiusPixels
                || isInsideAnchorWindow(
                    innerAnchorOffset,
                    clampedMouthDiameterPixels * INNER_ENTRY_HALF_WIDTH_FACTOR,
                    clampedMouthDiameterPixels * INNER_ENTRY_HALF_HEIGHT_FACTOR))
            {
                return EHandAnchorState.AtInnerAnchor;
            }

            float distanceToFrontAnchor = Vector2.Distance(pointerCanvasPosition, handFrontPosition);
            Vector2 frontAnchorOffset = pointerCanvasPosition - handFrontPosition;

            if (distanceToFrontAnchor <= frontAnchorRadiusPixels
                || isInsideAnchorWindow(
                    frontAnchorOffset,
                    clampedMouthDiameterPixels * FRONT_ENTRY_HALF_WIDTH_FACTOR,
                    clampedMouthDiameterPixels * FRONT_ENTRY_HALF_HEIGHT_FACTOR))
            {
                return EHandAnchorState.AtFrontAnchor;
            }

            return EHandAnchorState.OutsideMouth;
        }

        public static EHandAnchorState EvaluateMouthIntentAnchorState(
            Vector2 pointerCanvasPosition,
            Vector2 handFrontPosition,
            Vector2 handInnerPosition,
            float mouthDiameterPixels)
        {
            float clampedMouthDiameterPixels = Mathf.Max(1.0f, mouthDiameterPixels);
            float intentHalfWidth = clampedMouthDiameterPixels * MOUTH_INTENT_HALF_WIDTH_FACTOR;
            float minimumY = Mathf.Min(handFrontPosition.y, handInnerPosition.y)
                - (clampedMouthDiameterPixels * MOUTH_INTENT_LOWER_MARGIN_FACTOR);
            float maximumY = Mathf.Max(handFrontPosition.y, handInnerPosition.y)
                + (clampedMouthDiameterPixels * MOUTH_INTENT_UPPER_MARGIN_FACTOR);
            float centerX = Mathf.Lerp(handFrontPosition.x, handInnerPosition.x, 0.5f);

            if (Mathf.Abs(pointerCanvasPosition.x - centerX) > intentHalfWidth
                || pointerCanvasPosition.y < minimumY
                || pointerCanvasPosition.y > maximumY)
            {
                return EHandAnchorState.OutsideMouth;
            }

            float innerSwitchY = Mathf.Lerp(
                handFrontPosition.y,
                handInnerPosition.y,
                MOUTH_INTENT_INNER_SWITCH_FACTOR);
            return pointerCanvasPosition.y >= innerSwitchY
                ? EHandAnchorState.AtInnerAnchor
                : EHandAnchorState.AtFrontAnchor;
        }

        public void UpdatePointerVisual(bool isVisible, Vector2? pointerScreenPosition)
        {
            if (mPointerImage == null)
            {
                return;
            }

            if (isVisible == false || pointerScreenPosition.HasValue == false)
            {
                setObjectActive(mPointerImage, false);
                return;
            }

            if (tryConvertScreenPointToCanvasPosition(
                    pointerScreenPosition.Value,
                    out Vector2 anchoredPosition) == false)
            {
                setObjectActive(mPointerImage, false);
                return;
            }

            setObjectActive(mPointerImage, true);
            RectTransform pointerRectTransform = mPointerImage.rectTransform;
            pointerRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            pointerRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            pointerRectTransform.sizeDelta = POINTER_CURSOR_SIZE_PIXELS;
            pointerRectTransform.anchoredPosition = anchoredPosition;
        }

        public string GetAnswerTranscript()
        {
            return mAnswerInputField?.text ?? string.Empty;
        }

        public void SetAnswerTranscriptText(string transcriptText)
        {
            if (mAnswerInputField == null)
            {
                return;
            }

            mAnswerInputField.SetTextWithoutNotify(transcriptText ?? string.Empty);
            setText(mAnswerInputField.textComponent, transcriptText ?? string.Empty);
        }

        public void ClearAnswerTranscript()
        {
            SetAnswerTranscriptText(string.Empty);
        }

        public void SetAnswerTranscriptPlaceholder(string placeholderText)
        {
            if (mAnswerInputField?.placeholder is Text placeholderLabel)
            {
                setText(placeholderLabel, placeholderText ?? string.Empty);
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

        private async Task loadSpritesAsync()
        {
            mCardBackSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.QuestionCardBackPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.43f, 0.63f, 0.95f, 1.0f));
            mCardFrontSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.QuestionCardFrontPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.96f, 0.93f, 0.88f, 1.0f));
            mButtonFrameSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.PrimaryButtonFramePath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.38f, 0.21f, 0.11f, 1.0f));
            mStartButtonSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.StartButtonPath)
                ?? mButtonFrameSprite;
            mTryAgainButtonSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.TryAgainButtonPath)
                ?? mButtonFrameSprite;
            mEndGameButtonSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.EndGameButtonPath)
                ?? mButtonFrameSprite;
            mExitIconButtonSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.ExitIconButtonPath)
                ?? mButtonFrameSprite;
            mPointerCursorSprite = createPointerCursorSprite();
            mRitualHandSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.RitualHandInsertPath)
                ?? mPointerCursorSprite;
            mVerdictTrueSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.TrueVerdictPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.45f, 0.80f, 0.54f, 1.0f));
            mVerdictFalseSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.FalseVerdictPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.84f, 0.38f, 0.43f, 1.0f));
            mVerdictUncertainSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.UncertainVerdictPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.80f, 0.69f, 0.36f, 1.0f));
            mTitleVignetteSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.TitleVignettePath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.0f, 0.0f, 0.0f, 0.30f));
            mQuestionPanelSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.QuestionPanelFramePath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.15f, 0.10f, 0.07f, 0.90f));
            mStatusPanelSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.StatusPanelFramePath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.08f, 0.05f, 0.03f, 0.76f));
            mResultPanelSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.ResultPanelFramePath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.17f, 0.10f, 0.08f, 0.90f));
            mCardGlowSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.CardSelectionGlowPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.90f, 0.72f, 0.25f, 0.35f));
            mDwellFillSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.CardSelectionProgressFillPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.95f, 0.82f, 0.33f, 0.95f));

            mTitleBackgroundSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.TitleBackgroundPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.12f, 0.09f, 0.07f, 1.0f));
            mCardSelectionBackgroundSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.CardSelectionBackgroundPath)
                ?? mTitleBackgroundSprite;
            mMouthChamberBackgroundSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.MouthChamberBackgroundPath)
                ?? mCardSelectionBackgroundSprite
                ?? mTitleBackgroundSprite;
            mCarpetImage.sprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.FloorRunnerPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.44f, 0.03f, 0.05f, 1.0f));
            mLogoImage.sprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.TitleLogoPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.82f, 0.71f, 0.52f, 1.0f));
            mMouthImage.sprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.TruthMouthFacePath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.85f, 0.83f, 0.78f, 1.0f));
            mBackgroundImage.sprite = mTitleBackgroundSprite;
        }

        private void loadUiFonts()
        {
            mUiFont = Resources.Load<Font>(MouthOfTruthAssetCatalog.UiFontResourceName)
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mKoreanFallbackFont = Resources.Load<Font>(MouthOfTruthAssetCatalog.KoreanFallbackFontResourceName)
                ?? mUiFont;
        }

        private async Task loadAudioClipsAsync()
        {
            mTitleAmbienceClip =
                await RuntimeAudioClipLoader.LoadClipAsync(MouthOfTruthAssetCatalog.TitleAmbiencePath);
            mButtonConfirmClip =
                await RuntimeAudioClipLoader.LoadClipAsync(MouthOfTruthAssetCatalog.ButtonConfirmPath);
            mCardHoverClip =
                await RuntimeAudioClipLoader.LoadClipAsync(MouthOfTruthAssetCatalog.CardHoverPath);
            mCardSelectClip =
                await RuntimeAudioClipLoader.LoadClipAsync(MouthOfTruthAssetCatalog.CardSelectPath);
            mCardRevealClip =
                await RuntimeAudioClipLoader.LoadClipAsync(MouthOfTruthAssetCatalog.CardRevealPath);
            mHandInsertClip =
                await RuntimeAudioClipLoader.LoadClipAsync(MouthOfTruthAssetCatalog.HandInsertPath);
            mHandPromptClip =
                await RuntimeAudioClipLoader.LoadClipAsync(MouthOfTruthAssetCatalog.HandPromptPath);
            mResultTrueClip =
                await RuntimeAudioClipLoader.LoadClipAsync(MouthOfTruthAssetCatalog.ResultTruePath);
            mResultFalseClip =
                await RuntimeAudioClipLoader.LoadClipAsync(MouthOfTruthAssetCatalog.ResultFalsePath);
            mResultUncertainClip =
                await RuntimeAudioClipLoader.LoadClipAsync(MouthOfTruthAssetCatalog.ResultUncertainPath);
        }

        private void applyTheme()
        {
            mBackgroundImage.type = Image.Type.Sliced;
            mBackgroundImage.preserveAspect = true;
            mSceneOverlayImage.color = new Color(SCENE_OVERLAY_COLOR.r, SCENE_OVERLAY_COLOR.g, SCENE_OVERLAY_COLOR.b, 0.0f);
            mSceneOverlayImage.raycastTarget = false;
            mCarpetImage.preserveAspect = true;
            mCarpetImage.raycastTarget = false;
            mTitleVignetteImage.sprite = mTitleVignetteSprite;
            mTitleVignetteImage.type = Image.Type.Sliced;
            mTitleVignetteImage.raycastTarget = false;
            mLogoImage.preserveAspect = true;
            mLogoImage.raycastTarget = false;
            mMouthImage.preserveAspect = true;
            mMouthImage.raycastTarget = false;
            mHandImage.sprite = mPointerCursorSprite;
            mHandImage.preserveAspect = true;
            mHandImage.raycastTarget = false;
            mRitualHandImage.sprite = mRitualHandSprite;
            mRitualHandImage.preserveAspect = true;
            mRitualHandImage.raycastTarget = false;
            mPointerImage.sprite = mPointerCursorSprite;
            mPointerImage.preserveAspect = true;
            mPointerImage.raycastTarget = false;
            mTutorialHandImage.sprite = mRitualHandSprite;
            mTutorialHandImage.preserveAspect = true;
            mTutorialHandImage.raycastTarget = false;
            mTutorialOverlayImage.raycastTarget = false;
            mTutorialDevicePanelImage.type = Image.Type.Sliced;
            mTutorialDevicePanelImage.raycastTarget = false;
            mVerdictImage.preserveAspect = true;
            mVerdictImage.raycastTarget = false;
            mQuestionPanelImage.sprite = mQuestionPanelSprite;
            mQuestionPanelImage.type = Image.Type.Sliced;
            mQuestionPanelImage.raycastTarget = false;
            mStatusPanelImage.sprite = mStatusPanelSprite;
            mStatusPanelImage.type = Image.Type.Sliced;
            mStatusPanelImage.raycastTarget = false;
            mResultPanelImage.sprite = mResultPanelSprite;
            mResultPanelImage.type = Image.Type.Sliced;
            mResultPanelImage.raycastTarget = false;
            if (mAnswerInputField?.image != null)
            {
                mAnswerInputField.image.sprite = mStatusPanelSprite;
                mAnswerInputField.image.type = Image.Type.Sliced;
                mAnswerInputField.image.color = new Color(1.0f, 1.0f, 1.0f, 0.96f);
            }

            mStartButton.image.sprite = mStartButtonSprite;
            mTryAgainButton.image.sprite = mTryAgainButtonSprite;
            mBackToTitleButton.image.sprite = mButtonFrameSprite;
            mExitButton.image.sprite = mExitIconButtonSprite;
            mStartButton.image.type = Image.Type.Simple;
            mTryAgainButton.image.type = Image.Type.Simple;
            mBackToTitleButton.image.type = Image.Type.Sliced;
            mExitButton.image.type = Image.Type.Simple;
            mStartButton.image.preserveAspect = true;
            mTryAgainButton.image.preserveAspect = true;
            mExitButton.image.preserveAspect = true;
            setButtonLabelVisible(mStartButton, false);
            setButtonLabelVisible(mTryAgainButton, false);
            setButtonLabelVisible(mExitButton, false);

            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                pair.Value.SetBack(mCardBackSprite);
                pair.Value.SetDecorSprites(mCardGlowSprite, mDwellFillSprite);
            }
        }

        private void cacheWorldPresentationReferences()
        {
            mWorldCamera = Camera.main;

            if (mWorldCamera == null)
            {
                mWorldCamera = FindAnyObjectByType<Camera>();
            }

            mCardPresentationAnchorSet = FindAnyObjectByType<CardPresentationAnchorSet>();
            mMouthAnchorSet = FindAnyObjectByType<MouthAnchorSet>();
            mUseWorldEnvironmentLayout =
                mWorldCamera != null
                && mCardPresentationAnchorSet != null
                && mCardPresentationAnchorSet.HasRequiredAnchors()
                && mMouthAnchorSet != null
                && mMouthAnchorSet.HasRequiredAnchors();
        }

        private void refreshWorldPresentationLayout()
        {
            if (mUseWorldEnvironmentLayout == false)
            {
                return;
            }

            applyCardAnchorPositions();
            applyMouthAnchoredLayout();
            setHandVisual(0.0f);
        }

        private void applyCardAnchorPositions()
        {
            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                pair.Value.SetAnchoredPosition(getCardAnchorPosition(pair.Key));
            }
        }

        private void applyMouthAnchoredLayout()
        {
            RectTransform mouthRectTransform = mMouthImage.rectTransform;
            mouthRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            mouthRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            mouthRectTransform.anchoredPosition = getMouthAnchorPosition();
        }

        private Vector2 getCenteredCardRevealPosition()
        {
            return getCardAnchorPosition(EQuestionCardSlot.CenterCard) + new Vector2(0.0f, 10.0f);
        }

        private Vector2 getCardAnchorPosition(EQuestionCardSlot questionCardSlot)
        {
            Vector2 fallbackPosition = questionCardSlot switch
            {
                EQuestionCardSlot.LeftCard => FALLBACK_LEFT_CARD_POSITION,
                EQuestionCardSlot.CenterCard => FALLBACK_CENTER_CARD_POSITION,
                EQuestionCardSlot.RightCard => FALLBACK_RIGHT_CARD_POSITION,
                _ => FALLBACK_CENTER_CARD_POSITION,
            };

            if (mUseWorldEnvironmentLayout == false)
            {
                return fallbackPosition;
            }

            Transform anchorTransform = mCardPresentationAnchorSet.GetAnchor(questionCardSlot);
            return tryProjectWorldAnchor(anchorTransform, fallbackPosition, out Vector2 anchoredPosition)
                ? anchoredPosition
                : fallbackPosition;
        }

        private Vector2 getMouthAnchorPosition()
        {
            return tryProjectWorldAnchor(
                mMouthAnchorSet != null ? mMouthAnchorSet.TruthMouth : null,
                FALLBACK_MOUTH_POSITION,
                out Vector2 anchoredPosition)
                ? anchoredPosition
                : FALLBACK_MOUTH_POSITION;
        }

        private Vector2 getHandFrontPosition()
        {
            return tryProjectWorldAnchor(
                mMouthAnchorSet != null ? mMouthAnchorSet.MouthFrontAnchor : null,
                FALLBACK_HAND_FRONT_POSITION,
                out Vector2 anchoredPosition)
                ? anchoredPosition
                : FALLBACK_HAND_FRONT_POSITION;
        }

        private Vector2 getHandInnerPosition()
        {
            return tryProjectWorldAnchor(
                mMouthAnchorSet != null ? mMouthAnchorSet.MouthInnerAnchor : null,
                FALLBACK_HAND_INNER_POSITION,
                out Vector2 anchoredPosition)
                ? anchoredPosition
                : FALLBACK_HAND_INNER_POSITION;
        }

        private bool tryProjectWorldAnchor(
            Transform worldAnchorTransform,
            Vector2 fallbackPosition,
            out Vector2 anchoredPosition)
        {
            anchoredPosition = fallbackPosition;

            if (mUseWorldEnvironmentLayout == false
                || worldAnchorTransform == null
                || mWorldCamera == null)
            {
                return false;
            }

            Vector3 screenPosition = mWorldCamera.WorldToScreenPoint(worldAnchorTransform.position);

            if (screenPosition.z <= 0.0f)
            {
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mCanvasRootRectTransform,
                screenPosition,
                null,
                out anchoredPosition);
        }

        private bool tryConvertScreenPointToCanvasPosition(
            Vector2 screenPosition,
            out Vector2 anchoredPosition)
        {
            anchoredPosition = default;

            return mCanvasRootRectTransform != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    mCanvasRootRectTransform,
                    screenPosition,
                    null,
                    out anchoredPosition);
        }

        private void applyStartScreenLayout()
        {
            setRectTransformLayout(
                mLogoImage.rectTransform,
                new Vector2(0.5f, 0.55f),
                new Vector2(1000.0f, 560.0f));
            setRectTransformLayout(
                mStartButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.13f),
                new Vector2(520.0f, 150.0f));
            applyTopLeftExitButtonLayout();
        }

        private void prepareCardLaunchPresentation()
        {
            applyNarrationLayout();
            mBackgroundImage.sprite = mMouthChamberBackgroundSprite;
            setBackgroundTint(STAGE_BACKGROUND_TINT);
            setObjectActive(mBackgroundImage, true);
            setObjectActive(mCarpetImage, false);
            setObjectActive(mSceneOverlayImage, true);
            setOverlayAlpha(0.18f);
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mQuestionText, false);
            setObjectActive(mMouthImage, true);
            setObjectActive(mRitualHandImage, false);
            applyMouthAnchoredLayout();
            mMouthImage.rectTransform.localScale = Vector3.one * 0.94f;
        }

        private void applyCardSelectionLayout()
        {
            setRectTransformLayout(
                mPromptText.rectTransform,
                new Vector2(0.5f, 0.07f),
                new Vector2(1080.0f, 64.0f));
            mPromptText.fontSize = 30;
            applyTopLeftExitButtonLayout();
        }

        private void applyNarrationLayout()
        {
            applyTopLeftExitButtonLayout();
            setRectTransformLayout(
                mMouthImage.rectTransform,
                new Vector2(0.5f, 0.53f),
                new Vector2(640.0f, 640.0f));
            mMouthImage.rectTransform.localScale = Vector3.one;
            setRectTransformLayout(
                mQuestionPanelImage.rectTransform,
                new Vector2(0.5f, 0.105f),
                new Vector2(1500.0f, 122.0f));
            setRectTransformLayout(
                mQuestionText.rectTransform,
                new Vector2(0.5f, 0.105f),
                new Vector2(1320.0f, 70.0f));
            mQuestionText.fontSize = 30;
            mQuestionText.alignment = TextAnchor.MiddleCenter;
            mQuestionText.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void applyAwaitingHandInsertionLayout()
        {
            applyTopLeftExitButtonLayout();
            setRectTransformLayout(
                mMouthImage.rectTransform,
                new Vector2(0.5f, 0.56f),
                new Vector2(700.0f, 700.0f));
            mMouthImage.rectTransform.localScale = Vector3.one;
            setRectTransformLayout(
                mQuestionPanelImage.rectTransform,
                new Vector2(0.5f, 0.105f),
                new Vector2(1500.0f, 122.0f));
            setRectTransformLayout(
                mQuestionText.rectTransform,
                new Vector2(0.5f, 0.105f),
                new Vector2(1320.0f, 70.0f));
            setRectTransformLayout(
                mHandImage.rectTransform,
                new Vector2(0.5f, 0.22f),
                HELD_POINTER_CURSOR_SIZE_PIXELS);
            mQuestionText.fontSize = 30;
            mQuestionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void applyAnswerStageLayout()
        {
            applyTopLeftExitButtonLayout();
            setRectTransformLayout(
                mMouthImage.rectTransform,
                new Vector2(0.5f, 0.60f),
                new Vector2(760.0f, 760.0f));
            mMouthImage.rectTransform.localScale = Vector3.one;
            setRectTransformLayout(
                mQuestionPanelImage.rectTransform,
                new Vector2(0.5f, 0.105f),
                new Vector2(1500.0f, 122.0f));
            setRectTransformLayout(
                mQuestionText.rectTransform,
                new Vector2(0.5f, 0.105f),
                new Vector2(1320.0f, 70.0f));
            setRectTransformLayout(
                mHandImage.rectTransform,
                new Vector2(0.5f, 0.21f),
                HELD_POINTER_CURSOR_SIZE_PIXELS);
            mQuestionText.fontSize = 30;
            mQuestionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void applyAnsweringFocusLayout()
        {
            applyTopLeftExitButtonLayout();
            setRectTransformLayout(
                mMouthImage.rectTransform,
                new Vector2(0.5f, 0.51f),
                new Vector2(1080.0f, 1080.0f));
            mMouthImage.rectTransform.anchoredPosition = Vector2.zero;
            mMouthImage.rectTransform.localScale = Vector3.one * 1.08f;
            mMouthImage.color = Color.white;
        }

        private void applyResultLayout(EVerdictKind verdictKind)
        {
            setRectTransformLayout(
                mMouthImage.rectTransform,
                new Vector2(0.5f, 0.52f),
                new Vector2(1680.0f, 1680.0f));
            mMouthImage.rectTransform.localScale = Vector3.one;
            setRectTransformLayout(
                mVerdictImage.rectTransform,
                new Vector2(0.5f, 0.54f),
                verdictKind == EVerdictKind.Uncertain
                    ? new Vector2(1320.0f, 306.0f)
                    : new Vector2(1040.0f, 270.0f));
            setRectTransformLayout(
                mHandImage.rectTransform,
                new Vector2(0.5f, 0.18f),
                HELD_POINTER_CURSOR_SIZE_PIXELS);
            setRectTransformLayout(
                mTryAgainButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.225f),
                new Vector2(360.0f, 100.0f));
            applyTopLeftExitButtonLayout();
        }

        private void applyTopLeftExitButtonLayout()
        {
            setRectTransformLayout(
                mExitButton.GetComponent<RectTransform>(),
                new Vector2(0.06f, 0.90f),
                new Vector2(78.0f, 78.0f));
        }

        private void setOverlayAlpha(float alpha)
        {
            setOverlayTint(SCENE_OVERLAY_COLOR, alpha);
        }

        private void setOverlayTint(Color tintColor, float alpha)
        {
            if (mSceneOverlayImage == null)
            {
                return;
            }

            mSceneOverlayImage.color = new Color(
                tintColor.r,
                tintColor.g,
                tintColor.b,
                Mathf.Clamp01(alpha));
        }

        private void setBackgroundTint(Color tintColor)
        {
            if (mBackgroundImage == null)
            {
                return;
            }

            mBackgroundImage.color = tintColor;
        }

        private void setRectTransformLayout(
            RectTransform rectTransform,
            Vector2 anchor,
            Vector2 sizeDelta)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = sizeDelta;
        }

        private void buildCanvas()
        {
            mCanvas = gameObject.AddComponent<Canvas>();
            mCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mCanvas.sortingOrder = 10;
            gameObject.AddComponent<GraphicRaycaster>();
            CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            GameObject canvasRootObject = new GameObject("CanvasRoot", typeof(RectTransform));
            canvasRootObject.transform.SetParent(transform, false);
            mCanvasRootTransform = canvasRootObject.transform;
            mCanvasRootRectTransform = canvasRootObject.GetComponent<RectTransform>();
            mCanvasRootRectTransform.anchorMin = Vector2.zero;
            mCanvasRootRectTransform.anchorMax = Vector2.one;
            mCanvasRootRectTransform.offsetMin = Vector2.zero;
            mCanvasRootRectTransform.offsetMax = Vector2.zero;

            mBackgroundImage = createFullScreenImage("Background", mCanvasRootTransform, Color.white);
            mSceneOverlayImage = createFullScreenImage(
                "SceneOverlay",
                mCanvasRootTransform,
                new Color(0.01f, 0.01f, 0.015f, 0.0f));
            mCarpetImage = createImage(
                "RedCarpet",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.0f),
                new Vector2(0.5f, 0.0f),
                new Vector2(0.0f, 218.0f),
                new Vector2(1010.0f, 430.0f),
                new Color(0.62f, 0.56f, 0.52f, 0.84f));
            mTitleVignetteImage = createFullScreenImage(
                "TitleVignette",
                mCanvasRootTransform,
                new Color(1.0f, 1.0f, 1.0f, 0.55f));
            mLogoImage = createImage(
                "Logo",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.75f),
                new Vector2(0.5f, 0.75f),
                new Vector2(0.0f, 0.0f),
                new Vector2(840.0f, 360.0f),
                Color.white);
            mQuestionPanelImage = createImage(
                "QuestionPanel",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.84f),
                new Vector2(0.5f, 0.84f),
                new Vector2(0.0f, 0.0f),
                new Vector2(1280.0f, 170.0f),
                Color.white);
            mStatusPanelImage = createImage(
                "StatusPanel",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.105f),
                new Vector2(0.5f, 0.105f),
                new Vector2(0.0f, 0.0f),
                new Vector2(1320.0f, 150.0f),
                Color.white);
            mResultPanelImage = createImage(
                "ResultPanel",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.39f),
                new Vector2(0.5f, 0.39f),
                new Vector2(0.0f, 0.0f),
                new Vector2(980.0f, 420.0f),
                Color.white);
            mPromptText = createText(
                "PromptText",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.13f),
                new Vector2(0.5f, 0.13f),
                new Vector2(0.0f, 0.0f),
                new Vector2(700.0f, 80.0f),
                38,
                FontStyle.Bold);
            mStatusText = createText(
                "StatusText",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.08f),
                new Vector2(0.5f, 0.08f),
                new Vector2(0.0f, 0.0f),
                new Vector2(1200.0f, 70.0f),
                26,
                FontStyle.Bold);
            mQuestionText = createText(
                "QuestionText",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.84f),
                new Vector2(0.5f, 0.84f),
                new Vector2(0.0f, 0.0f),
                new Vector2(1200.0f, 140.0f),
                34,
                FontStyle.Bold);
            mAnalyzingDotsText = createText(
                "AnalyzingDotsText",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.57f),
                new Vector2(0.5f, 0.57f),
                Vector2.zero,
                new Vector2(360.0f, 140.0f),
                90,
                FontStyle.Bold);
            mAnswerTimerText = createText(
                "AnswerTimerText",
                mCanvasRootTransform,
                new Vector2(0.85f, 0.92f),
                new Vector2(0.85f, 0.92f),
                new Vector2(0.0f, 0.0f),
                new Vector2(320.0f, 50.0f),
                22,
                FontStyle.Normal);
            mMouthImage = createImage(
                "TruthMouth",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.50f),
                new Vector2(0.5f, 0.50f),
                new Vector2(0.0f, 60.0f),
                new Vector2(430.0f, 430.0f),
                Color.white);
            mHandImage = createImage(
                "HeldPointer",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.22f),
                new Vector2(0.5f, 0.22f),
                new Vector2(0.0f, 0.0f),
                HELD_POINTER_CURSOR_SIZE_PIXELS,
                Color.white);
            mRitualHandImage = createImage(
                "RitualHand",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.22f),
                new Vector2(0.5f, 0.22f),
                Vector2.zero,
                RITUAL_HAND_SIZE_PIXELS,
                Color.white);
            mPointerImage = createImage(
                "InputPointer",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                POINTER_CURSOR_SIZE_PIXELS,
                Color.white);
            mVerdictImage = createImage(
                "VerdictImage",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.63f),
                new Vector2(0.5f, 0.63f),
                new Vector2(0.0f, 0.0f),
                new Vector2(820.0f, 240.0f),
                Color.white);
            mVerdictText = createText(
                "VerdictText",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.49f),
                new Vector2(0.5f, 0.49f),
                new Vector2(0.0f, 0.0f),
                new Vector2(640.0f, 80.0f),
                48,
                FontStyle.Bold);
            mAnswerInputField = createInputField();
            mStartButton = createButton(
                "StartButton",
                "START GAME",
                new Vector2(0.5f, 0.22f),
                new Vector2(280.0f, 80.0f),
                () => mStartRequested = true);
            mTryAgainButton = createButton(
                "TryAgainButton",
                "TRY AGAIN",
                new Vector2(0.5f, 0.26f),
                new Vector2(280.0f, 80.0f),
                () => mTryAgainRequested = true);
            mBackToTitleButton = createButton(
                "BackToTitleButton",
                "BACK TO TITLE",
                new Vector2(0.5f, 0.18f),
                new Vector2(340.0f, 72.0f),
                () => mBackToTitleRequested = true);
            mExitButton = createButton(
                "ExitButton",
                "EXIT GAME",
                new Vector2(0.5f, 0.12f),
                new Vector2(320.0f, 72.0f),
                () => mExitRequested = true);

            mTitleVignetteImage.transform.SetSiblingIndex(mLogoImage.transform.GetSiblingIndex());
            placeImageBehindText(mQuestionPanelImage.transform, mQuestionText.transform);
            placeImageBehindText(mStatusPanelImage.transform, mPromptText.transform);
            placeImageBehindText(mResultPanelImage.transform, mVerdictImage.transform);

            createCardView(EQuestionCardSlot.LeftCard, FALLBACK_LEFT_CARD_POSITION);
            createCardView(EQuestionCardSlot.CenterCard, FALLBACK_CENTER_CARD_POSITION);
            createCardView(EQuestionCardSlot.RightCard, FALLBACK_RIGHT_CARD_POSITION);
            mPointerImage.transform.SetAsLastSibling();
            mPointerImage.raycastTarget = false;
            mAnalyzingDotsText.transform.SetAsLastSibling();
            mTutorialOverlayImage = createFullScreenImage(
                "FirstRunTutorialOverlay",
                mCanvasRootTransform,
                new Color(0.0f, 0.0f, 0.0f, 0.0f));
            mTutorialDevicePanelImage = createImage(
                "FirstRunTutorialPanel",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.46f),
                new Vector2(0.5f, 0.46f),
                Vector2.zero,
                new Vector2(900.0f, 520.0f),
                new Color(0.12f, 0.12f, 0.135f, 0.96f));
            mTutorialHandImage = createImage(
                "FirstRunTutorialHand",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.48f),
                new Vector2(0.5f, 0.48f),
                Vector2.zero,
                new Vector2(260.0f, 290.0f),
                Color.white);
            mTutorialTitleText = createText(
                "FirstRunTutorialTitle",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.72f),
                new Vector2(0.5f, 0.72f),
                Vector2.zero,
                new Vector2(980.0f, 64.0f),
                34,
                FontStyle.Bold);
            mTutorialBodyText = createText(
                "FirstRunTutorialBody",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.25f),
                new Vector2(0.5f, 0.25f),
                Vector2.zero,
                new Vector2(1040.0f, 72.0f),
                26,
                FontStyle.Normal);
            mTutorialStepText = createText(
                "FirstRunTutorialStep",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.17f),
                new Vector2(0.5f, 0.17f),
                Vector2.zero,
                new Vector2(980.0f, 54.0f),
                24,
                FontStyle.Bold);
            mTutorialOverlayImage.transform.SetAsLastSibling();
            mTutorialDevicePanelImage.transform.SetAsLastSibling();
            mTutorialHandImage.transform.SetAsLastSibling();
            mTutorialTitleText.transform.SetAsLastSibling();
            mTutorialBodyText.transform.SetAsLastSibling();
            mTutorialStepText.transform.SetAsLastSibling();
            setObjectActive(mTutorialOverlayImage, false);
            setObjectActive(mTutorialDevicePanelImage, false);
            setObjectActive(mTutorialHandImage, false);
            setObjectActive(mTutorialTitleText, false);
            setObjectActive(mTutorialBodyText, false);
            setObjectActive(mTutorialStepText, false);
            mLoadingOverlayImage = createFullScreenImage("LoadingOverlay", mCanvasRootTransform, Color.black);
            mLoadingOverlayImage.transform.SetAsLastSibling();
            mLoadingOverlayImage.raycastTarget = true;
        }

        private void buildAudioSources()
        {
            mAmbienceAudioSource = gameObject.AddComponent<AudioSource>();
            mAmbienceAudioSource.loop = true;
            mAmbienceAudioSource.playOnAwake = false;
            mAmbienceAudioSource.volume = 0.32f;
            mAmbienceAudioSource.spatialBlend = 0.0f;
            mAmbienceAudioSource.priority = 0;
            mAmbienceAudioSource.dopplerLevel = 0.0f;
            mAmbienceAudioSource.ignoreListenerPause = true;

            mInterfaceAudioSource = gameObject.AddComponent<AudioSource>();
            mInterfaceAudioSource.loop = false;
            mInterfaceAudioSource.playOnAwake = false;
            mInterfaceAudioSource.volume = 0.78f;
            mInterfaceAudioSource.spatialBlend = 0.0f;
            mInterfaceAudioSource.priority = 16;
            mInterfaceAudioSource.dopplerLevel = 0.0f;
            mInterfaceAudioSource.ignoreListenerPause = true;
        }

        private void ensureEventSystemExists()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private void createCardView(EQuestionCardSlot questionCardSlot, Vector2 anchoredPosition)
        {
            GameObject cardObject = new GameObject(questionCardSlot.ToString());
            QuestionCardView questionCardView = cardObject.AddComponent<QuestionCardView>();
            questionCardView.Initialize(
                questionCardSlot,
                mCanvasRootTransform,
                mCardBackSprite,
                mUiFont,
                mKoreanFallbackFont);
            questionCardView.SetAnchoredPosition(anchoredPosition);
            mCardViews.Add(questionCardSlot, questionCardView);
        }

        private Image createFullScreenImage(string objectName, Transform parentTransform, Color color)
        {
            GameObject imageObject = new GameObject(objectName);
            imageObject.transform.SetParent(parentTransform, false);
            RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Sprite createPointerCursorSprite()
        {
            Texture2D texture = new Texture2D(
                POINTER_CURSOR_TEXTURE_SIZE,
                POINTER_CURSOR_TEXTURE_SIZE,
                TextureFormat.RGBA32,
                mipChain: false);
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
            return Sprite.Create(
                texture,
                new Rect(0.0f, 0.0f, POINTER_CURSOR_TEXTURE_SIZE, POINTER_CURSOR_TEXTURE_SIZE),
                new Vector2(0.5f, 0.5f),
                POINTER_CURSOR_TEXTURE_SIZE);
        }

        private Image createImage(
            string objectName,
            Transform parentTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            GameObject imageObject = new GameObject(objectName);
            imageObject.transform.SetParent(parentTransform, false);
            RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private void placeImageBehindText(Transform imageTransform, Transform textTransform)
        {
            if (imageTransform == null || textTransform == null)
            {
                return;
            }

            int targetIndex = Mathf.Max(0, textTransform.GetSiblingIndex() - 1);
            imageTransform.SetSiblingIndex(targetIndex);
        }

        private Text createText(
            string objectName,
            Transform parentTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            int fontSize,
            FontStyle fontStyle)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parentTransform, false);
            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            Text text = textObject.AddComponent<Text>();
            text.font = mUiFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.94f, 0.90f, 0.82f, 1.0f);
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            addTextShadow(textObject);
            return text;
        }

        private void setText(Text text, string value)
        {
            if (text == null)
            {
                return;
            }

            string safeValue = value ?? string.Empty;
            text.font = containsHangul(safeValue) ? mKoreanFallbackFont : mUiFont;
            text.text = safeValue;
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

        private InputField createInputField()
        {
            GameObject inputFieldObject = new GameObject("AnswerInputField");
            inputFieldObject.transform.SetParent(mCanvasRootTransform, false);
            RectTransform rectTransform = inputFieldObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.14f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.14f);
            rectTransform.anchoredPosition = new Vector2(0.0f, 0.0f);
            rectTransform.sizeDelta = new Vector2(920.0f, 96.0f);

            Image backgroundImage = inputFieldObject.AddComponent<Image>();
            backgroundImage.color = Color.white;

            InputField inputField = inputFieldObject.AddComponent<InputField>();
            inputField.transition = Selectable.Transition.None;

            Text placeholderText = createText(
                "Placeholder",
                inputFieldObject.transform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(-60.0f, -20.0f),
                24,
                FontStyle.Italic);
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.color = new Color(0.80f, 0.74f, 0.66f, 0.7f);
            setText(placeholderText, "입력된 답변이 이 영역에 표시됩니다.");

            Text valueText = createText(
                "Text",
                inputFieldObject.transform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(-60.0f, -20.0f),
                24,
                FontStyle.Normal);
            valueText.alignment = TextAnchor.MiddleLeft;
            valueText.color = new Color(0.94f, 0.90f, 0.82f, 1.0f);
            valueText.supportRichText = false;

            inputField.textComponent = valueText;
            inputField.placeholder = placeholderText;
            inputField.lineType = InputField.LineType.MultiLineNewline;
            inputField.characterLimit = 240;
            inputField.interactable = false;
            return inputField;
        }

        private Button createButton(
            string objectName,
            string labelText,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Action clickedAction)
        {
            GameObject buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(mCanvasRootTransform, false);
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = Color.white;

            Button button = buttonObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(
                () =>
                {
                    playInterfaceCue(mButtonConfirmClip, 0.68f);
                    clickedAction?.Invoke();
                });

            Text label = createText(
                "Label",
                buttonObject.transform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(-20.0f, -20.0f),
                34,
                FontStyle.Bold);
            setText(label, labelText);
            return button;
        }

        private void placeRitualHandAboveMouth()
        {
            if (mRitualHandImage == null || mMouthImage == null)
            {
                return;
            }

            int mouthSiblingIndex = mMouthImage.transform.GetSiblingIndex();
            int targetSiblingIndex = Mathf.Min(mCanvasRootTransform.childCount - 1, mouthSiblingIndex + 1);
            mRitualHandImage.transform.SetSiblingIndex(targetSiblingIndex);

            if (mQuestionPanelImage != null && mQuestionPanelImage.gameObject.activeSelf)
            {
                mQuestionPanelImage.transform.SetAsLastSibling();
            }

            if (mQuestionText != null && mQuestionText.gameObject.activeSelf)
            {
                mQuestionText.transform.SetAsLastSibling();
            }

            if (mVerdictImage != null && mVerdictImage.gameObject.activeSelf)
            {
                mVerdictImage.transform.SetAsLastSibling();
            }

            if (mTryAgainButton != null && mTryAgainButton.gameObject.activeSelf)
            {
                mTryAgainButton.transform.SetAsLastSibling();
            }

            if (mExitButton != null && mExitButton.gameObject.activeSelf)
            {
                mExitButton.transform.SetAsLastSibling();
            }
        }

        private void configureExitButtonAsTopLeftIcon()
        {
            if (mExitButton?.image == null)
            {
                return;
            }

            mExitButton.image.sprite = mExitIconButtonSprite;
            mExitButton.image.type = Image.Type.Simple;
            mExitButton.image.preserveAspect = true;
            setButtonLabelVisible(mExitButton, false);
        }

        private void configureExitButtonAsEndGameButton()
        {
            if (mExitButton?.image == null)
            {
                return;
            }

            mExitButton.image.sprite = mEndGameButtonSprite;
            mExitButton.image.type = Image.Type.Simple;
            mExitButton.image.preserveAspect = true;
            setButtonLabelVisible(mExitButton, false);
        }

        private void setButtonLabelVisible(Button button, bool isVisible)
        {
            Text label = button != null ? button.GetComponentInChildren<Text>(includeInactive: true) : null;

            if (label != null)
            {
                label.gameObject.SetActive(isVisible);
            }
        }

        private bool isScreenPointOverButton(
            Button button,
            Vector2 screenPosition,
            float intentExpansionPixels)
        {
            return button != null
                && button.gameObject.activeInHierarchy
                && button.interactable
                && (
                    isScreenPointOverRectTransform(button.GetComponent<RectTransform>(), screenPosition)
                    || isScreenPointOverExpandedRectTransform(
                        button.GetComponent<RectTransform>(),
                        screenPosition,
                        intentExpansionPixels)
                );
        }

        private bool isScreenPointOverRectTransform(
            RectTransform rectTransform,
            Vector2 screenPosition)
        {
            return rectTransform != null
                && RectTransformUtility.RectangleContainsScreenPoint(
                    rectTransform,
                    screenPosition,
                    null);
        }

        private bool isScreenPointOverExpandedRectTransform(
            RectTransform rectTransform,
            Vector2 screenPosition,
            float expansionPixels)
        {
            if (rectTransform == null || expansionPixels <= 0.0f)
            {
                return false;
            }

            rectTransform.GetWorldCorners(mHitTestWorldCorners);
            Vector2 firstScreenCorner = RectTransformUtility.WorldToScreenPoint(null, mHitTestWorldCorners[0]);
            float minimumX = firstScreenCorner.x;
            float maximumX = firstScreenCorner.x;
            float minimumY = firstScreenCorner.y;
            float maximumY = firstScreenCorner.y;

            for (int cornerIndex = 1; cornerIndex < mHitTestWorldCorners.Length; cornerIndex += 1)
            {
                Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(
                    null,
                    mHitTestWorldCorners[cornerIndex]);
                minimumX = Mathf.Min(minimumX, screenCorner.x);
                maximumX = Mathf.Max(maximumX, screenCorner.x);
                minimumY = Mathf.Min(minimumY, screenCorner.y);
                maximumY = Mathf.Max(maximumY, screenCorner.y);
            }

            return screenPosition.x >= minimumX - expansionPixels
                && screenPosition.x <= maximumX + expansionPixels
                && screenPosition.y >= minimumY - expansionPixels
                && screenPosition.y <= maximumY + expansionPixels;
        }

        private void updateButtonVisual(Button button, bool isHovered, float hoverProgress)
        {
            if (button == null)
            {
                return;
            }

            float effectiveHoverProgress = isHovered ? Mathf.Clamp01(hoverProgress) : 0.0f;
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            rectTransform.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.06f, effectiveHoverProgress);

            if (button.image != null)
            {
                button.image.color = Color.Lerp(
                    Color.white,
                    new Color(1.0f, 0.92f, 0.78f, 1.0f),
                    effectiveHoverProgress);
            }

            Text label = button.GetComponentInChildren<Text>();

            if (label != null)
            {
                label.color = Color.Lerp(
                    new Color(0.88f, 0.84f, 0.76f, 1.0f),
                    new Color(1.0f, 0.97f, 0.84f, 1.0f),
                    effectiveHoverProgress);
            }
        }

        private void setCardsVisible(bool isVisible)
        {
            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                pair.Value.gameObject.SetActive(isVisible);
            }
        }

        private void setObjectActive(Component component, bool isActive)
        {
            if (component == null)
            {
                return;
            }

            component.gameObject.SetActive(isActive);
        }

        private async Task animateOverTimeAsync(float durationSeconds, Action<float> updateAction)
        {
            if (durationSeconds <= 0.0f)
            {
                updateAction?.Invoke(1.0f);
                return;
            }

            float elapsedSeconds = 0.0f;

            while (elapsedSeconds < durationSeconds)
            {
                elapsedSeconds += Time.deltaTime;
                updateAction?.Invoke(Mathf.Clamp01(elapsedSeconds / durationSeconds));
                await Task.Yield();
            }

            updateAction?.Invoke(1.0f);
        }

        private static float getCardFrontReadHoldDurationSeconds(string questionText)
        {
            int questionLength = string.IsNullOrWhiteSpace(questionText)
                ? 0
                : questionText.Trim().Length;
            float weightedDuration = questionLength * CARD_FRONT_READ_HOLD_PER_CHARACTER_SECONDS;
            return Mathf.Clamp(
                CARD_FRONT_READ_HOLD_MINIMUM_SECONDS + weightedDuration,
                CARD_FRONT_READ_HOLD_MINIMUM_SECONDS,
                CARD_FRONT_READ_HOLD_MAXIMUM_SECONDS);
        }

        private static Vector2 getTutorialScanPosition(float progress)
        {
            float clampedProgress = Mathf.Clamp01(progress);

            if (clampedProgress < 0.33f)
            {
                return Vector2.Lerp(
                    new Vector2(-220.0f, -20.0f),
                    new Vector2(0.0f, -4.0f),
                    easeInOutStatic(clampedProgress / 0.33f));
            }

            if (clampedProgress < 0.66f)
            {
                return Vector2.Lerp(
                    new Vector2(0.0f, -4.0f),
                    new Vector2(220.0f, -20.0f),
                    easeInOutStatic((clampedProgress - 0.33f) / 0.33f));
            }

            return Vector2.Lerp(
                new Vector2(220.0f, -20.0f),
                new Vector2(0.0f, -4.0f),
                easeInOutStatic((clampedProgress - 0.66f) / 0.34f));
        }

        private static float getFirstRunTutorialDurationSeconds()
        {
            try
            {
                if (File.Exists(MouthOfTruthAssetCatalog.FirstRunTutorialSequencePath) == false)
                {
                    return FIRST_RUN_TUTORIAL_FALLBACK_DURATION_SECONDS;
                }

                string json = File.ReadAllText(MouthOfTruthAssetCatalog.FirstRunTutorialSequencePath);
                TutorialSequenceMetadata metadata = JsonUtility.FromJson<TutorialSequenceMetadata>(json);

                if (metadata == null || metadata.fr <= 0.0f || metadata.op <= metadata.ip)
                {
                    return FIRST_RUN_TUTORIAL_FALLBACK_DURATION_SECONDS;
                }

                return Mathf.Clamp(
                    (metadata.op - metadata.ip) / metadata.fr,
                    3.0f,
                    5.0f);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to read first-run tutorial sequence metadata.\n" + exception);
                return FIRST_RUN_TUTORIAL_FALLBACK_DURATION_SECONDS;
            }
        }

        private static float easeInOutStatic(float progress)
        {
            float clampedProgress = Mathf.Clamp01(progress);
            return clampedProgress * clampedProgress * (3.0f - (2.0f * clampedProgress));
        }

        private float easeOut(float progress)
        {
            float inverse = 1.0f - progress;
            return 1.0f - (inverse * inverse * inverse);
        }

        private float easeIn(float progress)
        {
            float clampedProgress = Mathf.Clamp01(progress);
            return clampedProgress * clampedProgress * clampedProgress;
        }

        private float easeInOut(float progress)
        {
            float clampedProgress = Mathf.Clamp01(progress);
            return clampedProgress * clampedProgress * (3.0f - (2.0f * clampedProgress));
        }

        private void setRitualHandVisual(
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float alpha,
            float scale,
            float rotationDegrees)
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
            handRectTransform.anchoredPosition = Vector2.Lerp(
                frontPosition,
                innerPosition,
                easedProgress)
                + new Vector2(lateralArcOffset, 0.0f);
            handRectTransform.localRotation = Quaternion.identity;
            handRectTransform.localScale = Vector3.one * Mathf.Lerp(1.05f, 0.86f, easedProgress);
            mHandImage.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(0.94f, 0.82f, easedProgress));
        }

        private void enableHeldHandPresentation(
            float baseProgress,
            float pulseAmplitude,
            float pulseSpeed)
        {
            mUseHeldHandPresentation = true;
            mHeldHandBaseProgress = Mathf.Clamp01(baseProgress);
            mHeldHandPulseAmplitude = Mathf.Max(0.0f, pulseAmplitude);
            mHeldHandPulseSpeed = Mathf.Max(0.0f, pulseSpeed);
            setHandVisual(mHeldHandBaseProgress);
        }

        private void disableHeldHandPresentation()
        {
            mUseHeldHandPresentation = false;
            mHeldHandBaseProgress = 0.0f;
            mHeldHandPulseAmplitude = 0.0f;
            mHeldHandPulseSpeed = 0.0f;
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
                setObjectActive(mAnalyzingDotsText, true);
                mAnalyzingDotsText.transform.SetAsLastSibling();
                setText(mAnalyzingDotsText, ".");
            }
        }

        private void disableAnsweringPresentation()
        {
            mIsAnsweringPresentationActive = false;

            if (mQuestionPanelImage != null)
            {
                mQuestionPanelImage.color = Color.white;
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
            mMouthImage.color = new Color(1.0f, 1.0f, 1.0f, 0.56f);
            mMouthImage.rectTransform.localScale = Vector3.one;
            setObjectActive(mAnalyzingDotsText, true);
            mAnalyzingDotsText.transform.SetAsLastSibling();
            mAnalyzingDotsText.color = new Color(1.0f, 0.92f, 0.78f, 1.0f);
            setText(mAnalyzingDotsText, "...");
        }

        private void disableAnalyzingPresentation()
        {
            mIsAnalyzingPresentationActive = false;

            if (mMouthImage != null)
            {
                mMouthImage.rectTransform.anchoredPosition = getMouthAnchorPosition();
                mMouthImage.color = Color.white;
                mMouthImage.rectTransform.localScale = Vector3.one;
            }

            if (mAnalyzingDotsText != null)
            {
                setObjectActive(mAnalyzingDotsText, false);
                setText(mAnalyzingDotsText, string.Empty);
                mAnalyzingDotsText.rectTransform.localScale = Vector3.one;
            }
        }

        private static bool isInsideAnchorWindow(
            Vector2 offsetFromAnchor,
            float halfWidth,
            float halfHeight)
        {
            return Mathf.Abs(offsetFromAnchor.x) <= halfWidth
                && Mathf.Abs(offsetFromAnchor.y) <= halfHeight;
        }

        private void ensureAmbiencePlayback()
        {
            if (mAmbienceAudioSource == null
                || mTitleAmbienceClip == null
                || mAmbienceAudioSource.isPlaying)
            {
                return;
            }

            mAmbienceAudioSource.clip = mTitleAmbienceClip;
            mAmbienceAudioSource.Play();
        }

        private void playInterfaceCue(AudioClip audioClip, float volumeScale)
        {
            if (mInterfaceAudioSource == null || audioClip == null)
            {
                return;
            }

            float safeVolumeScale = Mathf.Clamp(volumeScale, 0.0f, INTERFACE_AUDIO_MAX_VOLUME_SCALE);

            if (mInterfaceAudioSource.isPlaying)
            {
                safeVolumeScale *= INTERFACE_AUDIO_OVERLAP_DUCK_SCALE;
            }

            mInterfaceAudioSource.PlayOneShot(audioClip, safeVolumeScale);
        }

        private void playVerdictCue(EVerdictKind verdictKind)
        {
            AudioClip verdictClip = verdictKind switch
            {
                EVerdictKind.True => mResultTrueClip,
                EVerdictKind.False => mResultFalseClip,
                _ => mResultUncertainClip,
            };

            playInterfaceCue(verdictClip, 0.62f);
        }

        private void addTextShadow(GameObject textObject)
        {
            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.05f, 0.03f, 0.02f, 0.92f);
            shadow.effectDistance = new Vector2(2.0f, -2.0f);
        }
    }
}
