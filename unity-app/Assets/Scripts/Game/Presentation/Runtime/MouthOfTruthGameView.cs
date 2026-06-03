using System;
using System.Collections.Generic;
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
    public partial class MouthOfTruthGameView : MonoBehaviour
    {
        private static readonly Vector2 FALLBACK_LEFT_CARD_POSITION = new Vector2(-390.0f, 60.0f);
        private static readonly Vector2 FALLBACK_CENTER_CARD_POSITION = new Vector2(0.0f, 60.0f);
        private static readonly Vector2 FALLBACK_RIGHT_CARD_POSITION = new Vector2(390.0f, 60.0f);
        private static readonly Vector2 FALLBACK_MOUTH_POSITION = new Vector2(0.0f, 60.0f);
        private static readonly Vector2 FALLBACK_HAND_FRONT_POSITION = new Vector2(0.0f, -20.0f);
        private static readonly Vector2 FALLBACK_HAND_INNER_POSITION = new Vector2(0.0f, 230.0f);
        private static readonly Color TITLE_BACKGROUND_TINT = new Color(0.95f, 0.95f, 0.97f, 1.0f);
        private static readonly Color STAGE_BACKGROUND_TINT = new Color(0.82f, 0.82f, 0.86f, 1.0f);
        private const float FRONT_ANCHOR_RADIUS_FACTOR = 0.066f;
        private const float INNER_ANCHOR_RADIUS_FACTOR = 0.040f;
        private const float FRONT_ENTRY_HALF_WIDTH_FACTOR = 0.039f;
        private const float FRONT_ENTRY_HALF_HEIGHT_FACTOR = 0.050f;
        private const float INNER_ENTRY_HALF_WIDTH_FACTOR = 0.027f;
        private const float INNER_ENTRY_HALF_HEIGHT_FACTOR = 0.036f;
        private const float CARD_INTENT_LEFT_MAX_NORMALIZED_X = 0.39f;
        private const float CARD_INTENT_RIGHT_MIN_NORMALIZED_X = 0.61f;
        private const float CARD_INTENT_MIN_NORMALIZED_Y = 0.28f;
        private const float CARD_INTENT_MAX_NORMALIZED_Y = 0.84f;
        private const float HAND_DETECTION_VERTICAL_OFFSET_FACTOR = 0.105f;
        private const float MOUTH_INTENT_LEFT_WIDTH_FACTOR = 0.1108f;
        private const float MOUTH_INTENT_RIGHT_WIDTH_FACTOR = 0.0772f;
        private const float MOUTH_INTENT_LOWER_MARGIN_FACTOR = 0.066f;
        private const float MOUTH_INTENT_UPPER_MARGIN_FACTOR = 0.040f;
        private const float MOUTH_INTENT_INNER_SWITCH_FACTOR = 0.58f;
        private const float BUTTON_INTENT_EXPANSION_PIXELS = 54.0f;
        private const float EXIT_BUTTON_INTENT_EXPANSION_PIXELS = 32.0f;
        private const float CARD_FRONT_READ_HOLD_MINIMUM_SECONDS = 1.875f;
        private const float CARD_FRONT_READ_HOLD_MAXIMUM_SECONDS = 3.225f;
        private const float CARD_FRONT_READ_HOLD_PER_CHARACTER_SECONDS = 0.01875f;
        private const float CARD_FRONT_FOCUS_BEFORE_NARRATION_SECONDS = 1.05f;
        private const float CARD_FRONT_AFTER_NARRATION_HOLD_SECONDS = 0.64f;
        private const float CARD_HOVER_AUDIO_COOLDOWN_SECONDS = 0.60f;
        private const float CARD_SELECTION_CUE_SETTLE_SECONDS = 0.32f;
        private const float CARD_REVEAL_CUE_SETTLE_SECONDS = 0.46f;
        private const float CARD_FLIP_CLOSE_SECONDS = 0.28f;
        private const float CARD_FLIP_OPEN_SECONDS = 0.36f;
        private const float CARD_TO_MOUTH_ABSORPTION_SECONDS = 1.17f;
        private const float HAND_PROMPT_AFTER_CARD_LAUNCH_DELAY_SECONDS = 0.16f;
        private const float FIRST_RUN_TUTORIAL_FALLBACK_DURATION_SECONDS = 4.0f;
        private const float FIRST_RUN_TUTORIAL_DURATION_SCALE = 3.0f;
        private const float HAND_INSERTION_DURATION_SECONDS = 2.35f;
        private const float HAND_PROMPT_PANEL_FALLBACK_HOLD_SECONDS = 1.65f;
        private const float HAND_PROMPT_PANEL_DISMISS_SECONDS = 0.36f;
        private const float MOUTH_JUDGEMENT_FOCUS_SECONDS = 0.72f;
        private const float ANALYSIS_FOCUS_RAMP_SECONDS = 2.10f;
        private const float ANSWER_BEAM_SWEEP_RATE = 0.725f;
        private const float ANSWER_BEAM_SOURCE_Y_FACTOR = 0.108f;
        private const float ANSWER_BEAM_END_BOTTOM_Y_FACTOR = -0.43f;
        private const float ANSWER_BEAM_END_TOP_Y_FACTOR = 0.50f;
        private const float TEMPLE_APPROACH_FORWARD_DURATION_SECONDS = 3.0f;
        private const float TEMPLE_APPROACH_MOUTH_DIM_SECONDS = 0.48f;
        private const float TEMPLE_APPROACH_STAIR_START_SCALE = 1.85f;
        private const float TEMPLE_APPROACH_END_SCALE = 4.36f;
        private const float TEMPLE_ANSWER_FOCUS_SCALE = 4.36f;
        private const float TEMPLE_ANALYSIS_FOCUS_SCALE = 6.82f;
        private const float TEMPLE_RESULT_FOCUS_SCALE = TEMPLE_ANALYSIS_FOCUS_SCALE;
        private const float TEMPLE_APPROACH_START_OVERLAY_ALPHA = 0.42f;
        private const float TEMPLE_APPROACH_STAGE_OVERLAY_ALPHA = 0.18f;
        private const float CARD_SELECTION_SETTLED_OVERLAY_ALPHA = 0.18f;
        private const float CARD_SELECTION_DIM_MOUTH_ALPHA = 0.22f;
        private const float CARD_SELECTION_ENTRANCE_SECONDS = 0.82f;
        private const float CARD_SELECTION_ENTRANCE_SETTLE_SECONDS = 0.22f;
        private const float AMBIENCE_AUDIO_VOLUME = 0.32f;
        private const float INTERFACE_AUDIO_VOLUME = 0.78f;
        private const float INTERFACE_AUDIO_MAX_VOLUME_SCALE = 0.74f;
        private const float INTERFACE_AUDIO_OVERLAP_DUCK_SCALE = 0.72f;
        private const int POINTER_CURSOR_TEXTURE_SIZE = 64;
        private static readonly Vector2 POINTER_CURSOR_SIZE_PIXELS = new Vector2(46.0f, 46.0f);
        private static readonly Vector2 HELD_POINTER_CURSOR_SIZE_PIXELS = new Vector2(58.0f, 58.0f);
        private static readonly Vector2 RITUAL_HAND_SIZE_PIXELS = new Vector2(340.0f, 380.0f);
        private static readonly Vector2 TUTORIAL_HAND_SIZE_PIXELS = new Vector2(260.0f, 290.0f);
        private static readonly Vector2 TUTORIAL_LEAP_MOTION_DEVICE_SIZE_PIXELS = new Vector2(350.0f, 160.0f);
        private static readonly Vector2 ANSWERING_FOCUS_MOUTH_ANCHOR = new Vector2(0.5f, 0.51f);
        private static readonly Vector2 ANSWERING_FOCUS_MOUTH_SIZE_PIXELS = new Vector2(1120.0f, 1120.0f);
        private static readonly Vector2 RESULT_MOUTH_ANCHOR = new Vector2(0.5f, 0.52f);
        private static readonly Vector2 RESULT_MOUTH_SIZE_PIXELS = new Vector2(1680.0f, 1680.0f);
        private static readonly Vector2 RESULT_VERDICT_SIZE_PIXELS = new Vector2(1390.0f, 322.0f);
        private static readonly Vector2 RESULT_SHORT_VERDICT_SIZE_PIXELS = new Vector2(1580.0f, 365.0f);
        private static readonly Color SCENE_OVERLAY_COLOR = new Color(0.03f, 0.02f, 0.02f, 1.0f);
        private static readonly Color STAGE_OVERLAY_TINT = new Color(0.020f, 0.014f, 0.010f, 1.0f);
        private static readonly Vector2 TEMPLE_MOUTH_FOCUS_CENTER = Vector2.zero;
        private static readonly Color POINTER_CURSOR_FILL_COLOR = new Color(0.62f, 0.64f, 0.66f, 0.54f);
        private static readonly Color POINTER_CURSOR_RING_COLOR = new Color(0.90f, 0.91f, 0.92f, 0.86f);
        private static readonly Vector2 STAGE_CARPET_POSITION = new Vector2(0.0f, 166.0f);
        private static readonly Vector2 STAGE_CARPET_SIZE = new Vector2(880.0f, 328.0f);
        private static readonly Color STAGE_CARPET_TINT = new Color(0.58f, 0.52f, 0.48f, 0.82f);
        private static readonly Vector2 TEMPLE_APPROACH_MOUTH_POSITION = new Vector2(0.0f, 90.0f);
        private static readonly Vector2 TEMPLE_APPROACH_MOUTH_SIZE = new Vector2(246.0f, 246.0f);
        private static readonly Vector2 TEMPLE_HAND_FRONT_OFFSET_FACTOR = new Vector2(0.0f, -0.20f);
        private static readonly Vector2 TEMPLE_HAND_INNER_OFFSET_FACTOR = new Vector2(0.0f, -0.17f);
        private readonly Dictionary<EQuestionCardSlot, QuestionCardView> mCardViews = new Dictionary<EQuestionCardSlot, QuestionCardView>();
        private readonly Vector3[] mHitTestWorldCorners = new Vector3[4];
        private readonly Vector3[] mTempleMouthWorldCorners = new Vector3[4];

        private Canvas mCanvas;
        private RectTransform mCanvasRootRectTransform;
        private Transform mCanvasRootTransform;
        private Image mBackgroundImage;
        private Image mSceneOverlayImage;
        private Image mLoadingOverlayImage;
        private Image mTutorialOverlayImage;
        private Image mTutorialDevicePanelImage;
        private Image mTutorialLeapMotionDeviceImage;
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
        private Image mMouthListeningAuraImage;
        private Image mMouthAnalyzingAuraImage;
        private Image mMouthLeftEyeBeamImage;
        private Image mMouthRightEyeBeamImage;
        private GameObject mTempleApproachCameraObject;
        private RectTransform mTempleApproachCameraRectTransform;
        private Image mTempleApproachMouthImage;
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
        private Sprite mLeapMotionDeviceSprite;
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
        private EQuestionCardSlot? mLastAudibleHoveredCardSlotOrNull;
        private Camera mWorldCamera;
        private CardPresentationAnchorSet mCardPresentationAnchorSet;
        private MouthAnchorSet mMouthAnchorSet;
        private bool mUseWorldEnvironmentLayout;
        private EUiActionTarget? mLastHoveredUiActionTargetOrNull;
        private bool mIsAnsweringPresentationActive;
        private bool mIsAnalyzingPresentationActive;
        private bool mIsHandPromptPanelDismissalActive;
        private bool mIsCardAbsorptionPresentationActive;
        private float mAnsweringPresentationStartedAtSeconds;
        private float mAnalyzingPresentationStartedAtSeconds;
        private float mHandPromptPanelDismissalStartedAtSeconds;
        private float mHandPromptPanelDismissalStartAlpha = 1.0f;
        private float mCardAbsorptionPresentationProgress;
        private float mLastCardHoverCueTimeSeconds = -999.0f;

        private bool mStartRequested;
        private bool mTryAgainRequested;
        private bool mBackToTitleRequested;
        private bool mExitRequested;

        public bool IsFirstRunTutorialVisible { get; private set; }

        public float AnalysisFocusRampDurationSeconds => ANALYSIS_FOCUS_RAMP_SECONDS;

        public float AnswerBeamSweepCycleDurationSeconds => 2.0f / ANSWER_BEAM_SWEEP_RATE;

        public bool IsCardAbsorptionPresentationActive => mIsCardAbsorptionPresentationActive;

        public float CardAbsorptionPresentationProgress => mCardAbsorptionPresentationProgress;

        public float HandPromptPanelHoldDurationSeconds => getHandPromptPanelHoldSeconds();

        public async Task InitializeAsync()
        {
            Debug.Log("MouthOfTruthGameView initialization started.");
            ensureEventSystemExists();
            loadUiFonts();
            buildCanvas();
            cacheWorldPresentationReferences();
            buildAudioSources();
            Debug.Log("MouthOfTruthGameView loading sprites.");
            await loadSpritesAsync();
            Debug.Log("MouthOfTruthGameView loading audio.");
            await loadAudioClipsAsync();
            Debug.Log("MouthOfTruthGameView applying theme.");
            applyTheme();
            refreshWorldPresentationLayout();
            ShowStartScreen();
            setObjectActive(mLoadingOverlayImage, false);
        }

        private void LateUpdate()
        {
            ensureAmbiencePlayback();
            stabilizeAudioSourceLevels();
            updateHandPromptPanelDismissal();
            updateAnsweringPresentation();
            updateAnalyzingPresentation();
        }

        private void updateAnsweringPresentation()
        {
            if (mIsAnsweringPresentationActive == false || mMouthImage == null)
            {
                return;
            }

            float elapsedSeconds = Time.unscaledTime - mAnsweringPresentationStartedAtSeconds;
            float slowPulse = (Mathf.Sin(elapsedSeconds * 1.85f) + 1.0f) * 0.5f;
            float quickPulse = (Mathf.Sin(elapsedSeconds * 4.8f) + 1.0f) * 0.5f;
            float breathScale = Mathf.Lerp(1.0f, 1.035f, slowPulse);
            setOverlayTint(new Color(0.018f, 0.012f, 0.010f, 1.0f), Mathf.Lerp(0.30f, 0.36f, slowPulse));

            if (isTempleApproachSceneActive())
            {
                float cameraPulse = slowPulse * 0.024f;
                setTempleCameraPoseCenteredOnMouth(TEMPLE_ANSWER_FOCUS_SCALE + cameraPulse, TEMPLE_MOUTH_FOCUS_CENTER);
                setTempleApproachMouthColor(new Color(1.0f, Mathf.Lerp(0.93f, 1.0f, slowPulse), Mathf.Lerp(0.78f, 0.94f, slowPulse), 1.0f));
                syncTempleStageMouthOverlay(0.0f);
                updateAnsweringEyeBeamImages(elapsedSeconds, quickPulse);
                return;
            }

            mMouthImage.color = new Color(1.0f, Mathf.Lerp(0.93f, 1.0f, slowPulse), Mathf.Lerp(0.78f, 0.94f, slowPulse), 1.0f);
            mMouthImage.rectTransform.localScale = Vector3.one * breathScale;
            updateAnsweringEyeBeamImages(elapsedSeconds, quickPulse);
        }

        private void updateAnalyzingPresentation()
        {
            if (mIsAnalyzingPresentationActive == false || mMouthImage == null)
            {
                return;
            }

            float elapsedSeconds = Time.unscaledTime - mAnalyzingPresentationStartedAtSeconds;
            float pulse = (Mathf.Sin(elapsedSeconds * 3.4f) + 1.0f) * 0.5f;
            float surge = (Mathf.Sin(elapsedSeconds * 5.8f) + 1.0f) * 0.5f;
            float tremor = Mathf.Sin(elapsedSeconds * 10.5f) * 6.6f;
            float verticalTremor = Mathf.Sin(elapsedSeconds * 13.2f) * 2.4f;
            float focusProgress = Mathf.Clamp01(elapsedSeconds / ANALYSIS_FOCUS_RAMP_SECONDS);
            setOverlayTint(new Color(0.024f, 0.016f, 0.012f, 1.0f), Mathf.Lerp(0.34f, 0.40f, pulse));

            if (isTempleApproachSceneActive())
            {
                float focusScale = Mathf.Lerp(TEMPLE_ANSWER_FOCUS_SCALE, TEMPLE_ANALYSIS_FOCUS_SCALE, easeOut(focusProgress));
                float cameraScale = focusScale + (pulse * 0.026f);
                setTempleCameraPoseCenteredOnMouth(cameraScale, TEMPLE_MOUTH_FOCUS_CENTER, tremor * 0.94f, verticalTremor * 0.68f);
                setTempleApproachMouthColor(new Color(1.0f, Mathf.Lerp(0.86f, 0.98f, pulse), Mathf.Lerp(0.74f, 0.90f, pulse), Mathf.Lerp(0.92f, 1.0f, pulse)));
                syncTempleStageMouthOverlay(0.0f);
                updateMouthEffectImage(mMouthListeningAuraImage, new Color(1.0f, 0.64f, 0.40f, Mathf.Lerp(0.12f, 0.20f, pulse)), 1.10f + (surge * 0.12f), -elapsedSeconds * 9.0f);
                updateMouthEffectImage(mMouthAnalyzingAuraImage, new Color(1.0f, 0.34f, 0.26f, Mathf.Lerp(0.18f, 0.29f, surge)), 1.20f + (pulse * 0.16f), elapsedSeconds * 14.0f);
                return;
            }

            mMouthImage.color = new Color(1.0f, Mathf.Lerp(0.86f, 0.98f, pulse), Mathf.Lerp(0.74f, 0.90f, pulse), Mathf.Lerp(0.74f, 0.92f, pulse));
            mMouthImage.rectTransform.anchoredPosition = new Vector2(tremor * 1.34f, verticalTremor * 0.72f);
            mMouthImage.rectTransform.localScale = Vector3.one * (Mathf.Lerp(1.02f, 1.13f, easeOut(focusProgress)) + (pulse * 0.035f));
            updateMouthEffectImage(mMouthListeningAuraImage, new Color(1.0f, 0.64f, 0.40f, Mathf.Lerp(0.12f, 0.20f, pulse)), 1.10f + (surge * 0.12f), -elapsedSeconds * 9.0f);
            updateMouthEffectImage(mMouthAnalyzingAuraImage, new Color(1.0f, 0.34f, 0.26f, Mathf.Lerp(0.18f, 0.29f, surge)), 1.20f + (pulse * 0.16f), elapsedSeconds * 14.0f);
        }

        public void ShowStartScreen()
        {
            resetHandPromptPanelAlpha();
            disableAnsweringPresentation();
            disableAnalyzingPresentation();
            destroyTempleApproachScene();
            resetStageMotionTransforms();
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
            setMouthEffectImagesActive(false, false);
            setObjectActive(mHandImage, false);
            setObjectActive(mRitualHandImage, false);
            setObjectActive(mPointerImage, false);
            setObjectActive(mVerdictImage, false);
            setObjectActive(mVerdictText, false);
            setObjectActive(mTryAgainButton, false);
            setObjectActive(mBackToTitleButton, false);
            setCardsVisible(false);
            hideFirstRunTutorialPresentation();
            setText(mPromptText, string.Empty);
            setText(mStatusText, string.Empty);
            setText(mAnswerTimerText, string.Empty);
            mLastAudibleHoveredCardSlotOrNull = null;
            mLastHoveredUiActionTargetOrNull = null;
            mLastCardHoverCueTimeSeconds = -999.0f;
            refreshWorldPresentationLayout();
            ensureAmbiencePlayback();
        }

        public void ShowCardSelection(QuestionRoundSelection questionRoundSelection)
        {
            resetHandPromptPanelAlpha();
            disableAnsweringPresentation();
            disableAnalyzingPresentation();
            resetStageMotionTransforms();
            applyCardSelectionLayout();
            configureExitButtonAsTopLeftIcon();
            mBackgroundImage.sprite = mCardSelectionBackgroundSprite;
            setBackgroundTint(STAGE_BACKGROUND_TINT);
            bool isTempleApproachSceneVisible = mTempleApproachCameraObject != null;
            setObjectActive(mBackgroundImage, isTempleApproachSceneVisible == false);
            setObjectActive(mCarpetImage, isTempleApproachSceneVisible == false);

            if (isTempleApproachSceneVisible)
            {
                setTempleApproachMouthAlpha(CARD_SELECTION_DIM_MOUTH_ALPHA);
            }

            setObjectActive(mLogoImage, false);
            setObjectActive(mTitleVignetteImage, false);
            setObjectActive(mSceneOverlayImage, true);
            setOverlayTint(STAGE_OVERLAY_TINT, TEMPLE_APPROACH_STAGE_OVERLAY_ALPHA);
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
            setMouthEffectImagesActive(false, false);
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
            setText(mPromptText, "원하는 카드를 손으로 선택하세요.");
            setText(mStatusText, string.Empty);
            setText(mAnswerTimerText, string.Empty);
            mLastAudibleHoveredCardSlotOrNull = null;
            mLastHoveredUiActionTargetOrNull = null;
            mLastCardHoverCueTimeSeconds = -999.0f;
        }

        public async Task PlayCardSelectionEntranceAsync()
        {
            setObjectActive(mPromptText, false);
            setObjectActive(mSceneOverlayImage, true);
            setOverlayTint(STAGE_OVERLAY_TINT, TEMPLE_APPROACH_STAGE_OVERLAY_ALPHA);

            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                pair.Value.SetAlpha(0.0f);
                pair.Value.SetScale(0.92f);
            }

            await animateOverTimeAsync(
                CARD_SELECTION_ENTRANCE_SECONDS,
                progress =>
                {
                    float overlayProgress = easeOut(progress);
                    float overlayAlpha = Mathf.Lerp(TEMPLE_APPROACH_STAGE_OVERLAY_ALPHA, CARD_SELECTION_SETTLED_OVERLAY_ALPHA, overlayProgress);
                    setOverlayTint(STAGE_OVERLAY_TINT, overlayAlpha);

                    foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
                    {
                        float cardProgress = easeOut(Mathf.Clamp01((progress - getCardEntranceDelay(pair.Key)) / 0.70f));
                        pair.Value.SetAlpha(cardProgress);
                        pair.Value.SetScale(Mathf.Lerp(0.92f, 1.0f, cardProgress));
                    }
                });

            await animateOverTimeAsync(
                CARD_SELECTION_ENTRANCE_SETTLE_SECONDS,
                _ =>
                {
                    setOverlayTint(STAGE_OVERLAY_TINT, CARD_SELECTION_SETTLED_OVERLAY_ALPHA);
                });

            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                pair.Value.SetAlpha(1.0f);
                pair.Value.SetScale(1.0f);
            }

            setOverlayTint(STAGE_OVERLAY_TINT, CARD_SELECTION_SETTLED_OVERLAY_ALPHA);
            setObjectActive(mPromptText, true);
        }

        public void UpdateCardHoverVisual(EQuestionCardSlot? hoveredQuestionCardSlotOrNull, float hoverProgress)
        {
            if (hoveredQuestionCardSlotOrNull != mLastAudibleHoveredCardSlotOrNull)
            {
                bool hasEnoughCardHoverAudioGap = Time.unscaledTime - mLastCardHoverCueTimeSeconds >= CARD_HOVER_AUDIO_COOLDOWN_SECONDS;

                if (hoveredQuestionCardSlotOrNull.HasValue && hasEnoughCardHoverAudioGap)
                {
                    mLastCardHoverCueTimeSeconds = Time.unscaledTime;
                    playInterfaceCue(mCardHoverClip, 0.32f);
                }

                mLastAudibleHoveredCardSlotOrNull = hoveredQuestionCardSlotOrNull;
            }

            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                bool isHovered = hoveredQuestionCardSlotOrNull == pair.Key;
                pair.Value.SetVisualState(false, false, isHovered ? hoverProgress : 0.0f);
            }
        }

        public void PreviewCardSelectionFocus(EQuestionCardSlot selectedQuestionCardSlot)
        {
            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                bool isSelected = pair.Key == selectedQuestionCardSlot;
                pair.Value.SetVisualState(isDimmed: isSelected == false, isSelected, 0.0f);
                pair.Value.gameObject.SetActive(isSelected);
            }

            setText(mPromptText, string.Empty);
        }

        public Vector2 GetQuestionCardScreenCenter(EQuestionCardSlot questionCardSlot)
        {
            QuestionCardView questionCardView;
            if (mCardViews.TryGetValue(questionCardSlot, out questionCardView) == false)
            {
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }

            RectTransform cardRectTransform = questionCardView.RectTransform;
            Vector3 worldCenter = cardRectTransform.TransformPoint(cardRectTransform.rect.center);
            return RectTransformUtility.WorldToScreenPoint(mCanvas.worldCamera, worldCenter);
        }

        public async Task PlayAnalysisCompleteTransitionAsync()
        {
            if (mIsAnalyzingPresentationActive == false)
            {
                return;
            }

            if (isTempleApproachSceneActive())
            {
                TempleCameraTransition templeCameraTransition = captureTempleCameraTransition(TEMPLE_RESULT_FOCUS_SCALE, TEMPLE_MOUTH_FOCUS_CENTER);

                await animateOverTimeAsync(
                    0.54f,
                    progress =>
                    {
                        float easedProgress = easeInOut(progress);
                        float pulse = Mathf.Sin(progress * Mathf.PI);
                        float shakeFalloff = 1.0f - easedProgress;
                        float residualShake = Mathf.Sin(progress * Mathf.PI * 7.0f) * shakeFalloff * 4.2f;
                        float residualVerticalShake = Mathf.Sin(progress * Mathf.PI * 8.0f) * shakeFalloff * 1.8f;
                        float cameraScale = templeCameraTransition.GetScale(TEMPLE_RESULT_FOCUS_SCALE, easedProgress) + (pulse * 0.020f);
                        Vector2 cameraPosition = templeCameraTransition.GetPosition(easedProgress);
                        setTempleCameraPose(cameraScale, cameraPosition.y + residualVerticalShake, cameraPosition.x + residualShake);
                        setOverlayTint(new Color(0.022f, 0.016f, 0.014f, 1.0f), Mathf.Lerp(0.39f, 0.38f, easedProgress));
                        setTempleApproachMouthColor(new Color(1.0f, 0.94f, 0.84f, Mathf.Lerp(0.90f, 1.0f, easedProgress)));

                        if (mMouthListeningAuraImage != null)
                        {
                            Color listeningAuraColor = mMouthListeningAuraImage.color;
                            mMouthListeningAuraImage.color = new Color(listeningAuraColor.r, listeningAuraColor.g, listeningAuraColor.b, Mathf.Lerp(listeningAuraColor.a, 0.0f, easedProgress));
                        }

                        if (mMouthAnalyzingAuraImage != null)
                        {
                            Color auraColor = mMouthAnalyzingAuraImage.color;
                            mMouthAnalyzingAuraImage.color = new Color(auraColor.r, auraColor.g, auraColor.b, Mathf.Lerp(auraColor.a, 0.0f, easedProgress));
                        }
                    });

                disableAnalyzingPresentation(preserveMouthLayout: true);
                return;
            }

            RectTransform mouthRectTransform = mMouthImage.rectTransform;
            Vector2 startAnchor = mouthRectTransform.anchorMin;
            Vector2 startPosition = mouthRectTransform.anchoredPosition;
            Vector2 startSize = mouthRectTransform.sizeDelta;
            Vector3 startScale = mouthRectTransform.localScale;

            await animateOverTimeAsync(
                0.54f,
                progress =>
                {
                    float easedProgress = easeInOut(progress);
                    float pulse = Mathf.Sin(progress * Mathf.PI);
                    Vector2 currentAnchor = Vector2.Lerp(startAnchor, RESULT_MOUTH_ANCHOR, easedProgress);
                    mouthRectTransform.anchorMin = currentAnchor;
                    mouthRectTransform.anchorMax = currentAnchor;
                    mouthRectTransform.anchoredPosition = Vector2.Lerp(startPosition, Vector2.zero, easedProgress);
                    mouthRectTransform.sizeDelta = Vector2.Lerp(startSize, RESULT_MOUTH_SIZE_PIXELS, easedProgress);
                    mouthRectTransform.localScale = Vector3.Lerp(startScale, Vector3.one, easedProgress) * (1.0f + (pulse * 0.035f));
                    setOverlayTint(new Color(0.022f, 0.016f, 0.014f, 1.0f), Mathf.Lerp(0.46f, 0.38f, easedProgress));
                    mMouthImage.color = new Color(1.0f, 0.93f, 0.82f, Mathf.Lerp(0.62f, 0.88f, easedProgress));

                    if (mMouthListeningAuraImage != null)
                    {
                        Color listeningAuraColor = mMouthListeningAuraImage.color;
                        mMouthListeningAuraImage.color = new Color(listeningAuraColor.r, listeningAuraColor.g, listeningAuraColor.b, Mathf.Lerp(listeningAuraColor.a, 0.0f, easedProgress));
                    }

                    if (mMouthAnalyzingAuraImage != null)
                    {
                        Color auraColor = mMouthAnalyzingAuraImage.color;
                        mMouthAnalyzingAuraImage.color = new Color(auraColor.r, auraColor.g, auraColor.b, Mathf.Lerp(auraColor.a, 0.0f, easedProgress));
                    }
                });

            disableAnalyzingPresentation(preserveMouthLayout: true);
        }

        private async Task playMouthJudgementFocusTransitionAsync()
        {
            if (isTempleApproachSceneActive())
            {
                TempleCameraTransition templeCameraTransition = captureTempleCameraTransition(TEMPLE_ANSWER_FOCUS_SCALE, TEMPLE_MOUTH_FOCUS_CENTER);

                await animateOverTimeAsync(
                    MOUTH_JUDGEMENT_FOCUS_SECONDS * 1.28f,
                    progress =>
                    {
                        float easedProgress = easeInOut(progress);
                        float pulse = Mathf.Sin(progress * Mathf.PI);
                        float cameraScale = templeCameraTransition.GetScale(TEMPLE_ANSWER_FOCUS_SCALE, easedProgress) + (pulse * 0.018f);
                        Vector2 cameraPosition = templeCameraTransition.GetPosition(easedProgress);
                        setTempleCameraPose(cameraScale, cameraPosition.y, cameraPosition.x);
                        setOverlayTint(new Color(0.025f, 0.015f, 0.012f, 1.0f), Mathf.Lerp(0.36f, 0.46f, easedProgress));
                        setTempleApproachMouthColor(new Color(1.0f, Mathf.Lerp(0.96f, 0.92f, easedProgress), Mathf.Lerp(0.86f, 0.78f, easedProgress), 1.0f));
                    });

                syncTempleStageMouthOverlay(0.0f);
                return;
            }

            RectTransform mouthRectTransform = mMouthImage.rectTransform;
            Vector2 startAnchor = mouthRectTransform.anchorMin;
            Vector2 startPosition = mouthRectTransform.anchoredPosition;
            Vector2 startSize = mouthRectTransform.sizeDelta;
            Vector3 startScale = mouthRectTransform.localScale;
            Color startColor = mMouthImage.color;

            await animateOverTimeAsync(
                MOUTH_JUDGEMENT_FOCUS_SECONDS * 1.28f,
                progress =>
                {
                    float easedProgress = easeInOut(progress);
                    float pulse = Mathf.Sin(progress * Mathf.PI);
                    Vector2 currentAnchor = Vector2.Lerp(startAnchor, ANSWERING_FOCUS_MOUTH_ANCHOR, easedProgress);
                    mouthRectTransform.anchorMin = currentAnchor;
                    mouthRectTransform.anchorMax = currentAnchor;
                    mouthRectTransform.anchoredPosition = Vector2.Lerp(startPosition, Vector2.zero, easedProgress);
                    mouthRectTransform.sizeDelta = Vector2.Lerp(startSize, ANSWERING_FOCUS_MOUTH_SIZE_PIXELS, easedProgress);
                    mouthRectTransform.localScale = Vector3.Lerp(startScale, Vector3.one, easedProgress) * (1.0f + (pulse * 0.035f));
                    setOverlayTint(new Color(0.025f, 0.015f, 0.012f, 1.0f), Mathf.Lerp(0.36f, 0.46f, easedProgress));
                    mMouthImage.color = Color.Lerp(startColor, new Color(1.0f, 0.92f, 0.78f, 0.98f), easedProgress);
                });
        }

        public void ShowResult(EVerdictKind verdictKind, string transcriptText)
        {
            resetHandPromptPanelAlpha();
            disableAnsweringPresentation();
            disableAnalyzingPresentation();
            applyResultLayout(verdictKind);
            configureExitButtonAsTopLeftIcon();
            setCardsVisible(false);
            if (isTempleApproachSceneActive())
            {
                applyTempleStageBackgroundPresentation(0.38f);
            }
            else
            {
                mBackgroundImage.sprite = mMouthChamberBackgroundSprite;
                setBackgroundTint(STAGE_BACKGROUND_TINT);
                setObjectActive(mBackgroundImage, true);
                setObjectActive(mCarpetImage, false);
            }

            setObjectActive(mTitleVignetteImage, false);
            setObjectActive(mSceneOverlayImage, true);
            setGameplayOverlayAlpha(0.38f);
            setObjectActive(mQuestionText, false);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mStatusPanelImage, false);
            setObjectActive(mMouthImage, true);
            setMouthEffectImagesActive(false, false);
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
            if (isTempleApproachSceneActive() == false)
            {
                applyMouthAnchoredLayout();
            }

            if (isTempleApproachSceneActive())
            {
                setTempleApproachMouthColor(Color.white);
                syncTempleStageMouthOverlay(0.0f);
            }
            else
            {
                mMouthImage.color = Color.white;
                mMouthImage.rectTransform.localScale = Vector3.one;
            }

            mVerdictImage.color = Color.white;
            mVerdictImage.rectTransform.localRotation = Quaternion.identity;
            mVerdictImage.rectTransform.localScale = Vector3.one;
            playVerdictCue(verdictKind);
        }

        public async Task PlayResultRevealAnimationAsync(EVerdictKind verdictKind)
        {
            setObjectActive(mTryAgainButton, false);

            if (isTempleApproachSceneActive())
            {
                await playTempleResultRevealAnimationAsync(verdictKind);
            }
            else if (verdictKind == EVerdictKind.True)
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
            setGameplayOverlayAlpha(0.38f);
            if (isTempleApproachSceneActive())
            {
                setTempleApproachMouthColor(Color.white);
                syncTempleStageMouthOverlay(0.0f);
            }
            else
            {
                mMouthImage.color = Color.white;
                mMouthImage.rectTransform.localScale = Vector3.one;
            }

            mVerdictImage.color = Color.white;
            mVerdictImage.rectTransform.localRotation = Quaternion.identity;
            mVerdictImage.rectTransform.localScale = Vector3.one;
        }

        private async Task playTempleResultRevealAnimationAsync(EVerdictKind verdictKind)
        {
            mVerdictImage.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            Color overlayTint = verdictKind switch
            {
                EVerdictKind.True => new Color(0.05f, 0.11f, 0.06f, 1.0f),
                EVerdictKind.False => new Color(0.22f, 0.006f, 0.006f, 1.0f),
                _ => new Color(0.055f, 0.055f, 0.075f, 1.0f),
            };
            Color mouthTint = verdictKind switch
            {
                EVerdictKind.True => new Color(0.88f, 1.0f, 0.82f, 1.0f),
                EVerdictKind.False => new Color(1.0f, 0.68f, 0.58f, 1.0f),
                _ => new Color(0.74f, 0.76f, 0.82f, 1.0f),
            };

            await animateOverTimeAsync(
                verdictKind == EVerdictKind.False ? 0.88f : 0.72f,
                progress =>
                {
                    float easedProgress = easeOut(progress);
                    float pulse = Mathf.Sin(progress * Mathf.PI);
                    float falloff = 1.0f - easedProgress;
                    float residualShake = Mathf.Sin(progress * Mathf.PI * 8.0f) * falloff;
                    float shake = verdictKind == EVerdictKind.False
                        ? Mathf.Sin(progress * Mathf.PI * 12.0f) * falloff
                        : residualShake * 0.45f;
                    float verticalShake = Mathf.Sin(progress * Mathf.PI * 7.0f) * falloff * 0.72f;
                    setTempleCameraPoseCenteredOnMouth(TEMPLE_RESULT_FOCUS_SCALE + (pulse * 0.018f), TEMPLE_MOUTH_FOCUS_CENTER, shake * 1.4f, verticalShake);
                    setOverlayTint(overlayTint, Mathf.Lerp(0.48f, 0.38f, easedProgress));
                    setTempleApproachMouthColor(Color.Lerp(Color.white, mouthTint, 1.0f - easedProgress * 0.25f));
                    mVerdictImage.color = new Color(1.0f, 1.0f, 1.0f, easedProgress);
                    mVerdictImage.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, shake * 2.4f);
                    mVerdictImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.92f + (pulse * 0.08f), 1.0f, easedProgress);
                });
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

        private async Task loadSpritesAsync()
        {
            mCardBackSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.QuestionCardBackPath);
            if (mCardBackSprite == null)
            {
                mCardBackSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.43f, 0.63f, 0.95f, 1.0f));
            }

            mCardFrontSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.QuestionCardFrontPath);
            if (mCardFrontSprite == null)
            {
                mCardFrontSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.96f, 0.93f, 0.88f, 1.0f));
            }

            mButtonFrameSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.PrimaryButtonFramePath);
            if (mButtonFrameSprite == null)
            {
                mButtonFrameSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.38f, 0.21f, 0.11f, 1.0f));
            }

            mStartButtonSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.StartButtonPath);
            if (mStartButtonSprite == null)
            {
                mStartButtonSprite = mButtonFrameSprite;
            }

            mTryAgainButtonSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.TryAgainButtonPath);
            if (mTryAgainButtonSprite == null)
            {
                mTryAgainButtonSprite = mButtonFrameSprite;
            }

            mEndGameButtonSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.EndGameButtonPath);
            if (mEndGameButtonSprite == null)
            {
                mEndGameButtonSprite = mButtonFrameSprite;
            }

            mExitIconButtonSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.ExitIconButtonPath);
            if (mExitIconButtonSprite == null)
            {
                mExitIconButtonSprite = mButtonFrameSprite;
            }

            mPointerCursorSprite = createPointerCursorSprite();
            mRitualHandSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.RitualHandInsertPath);
            if (mRitualHandSprite == null)
            {
                mRitualHandSprite = mPointerCursorSprite;
            }

            mLeapMotionDeviceSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.LeapMotionDevicePath);
            if (mLeapMotionDeviceSprite == null)
            {
                mLeapMotionDeviceSprite = RuntimeSpriteLoader.CreateSolidSprite(Color.clear);
            }

            mVerdictTrueSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.TrueVerdictPath);
            if (mVerdictTrueSprite == null)
            {
                mVerdictTrueSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.45f, 0.80f, 0.54f, 1.0f));
            }

            mVerdictFalseSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.FalseVerdictPath);
            if (mVerdictFalseSprite == null)
            {
                mVerdictFalseSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.84f, 0.38f, 0.43f, 1.0f));
            }

            mVerdictUncertainSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.UncertainVerdictPath);
            if (mVerdictUncertainSprite == null)
            {
                mVerdictUncertainSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.80f, 0.69f, 0.36f, 1.0f));
            }

            mTitleVignetteSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.TitleVignettePath);
            if (mTitleVignetteSprite == null)
            {
                mTitleVignetteSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.0f, 0.0f, 0.0f, 0.30f));
            }

            mQuestionPanelSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.QuestionPanelFramePath);
            if (mQuestionPanelSprite == null)
            {
                mQuestionPanelSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.15f, 0.10f, 0.07f, 0.90f));
            }

            mStatusPanelSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.StatusPanelFramePath);
            if (mStatusPanelSprite == null)
            {
                mStatusPanelSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.08f, 0.05f, 0.03f, 0.76f));
            }

            mResultPanelSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.ResultPanelFramePath);
            if (mResultPanelSprite == null)
            {
                mResultPanelSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.17f, 0.10f, 0.08f, 0.90f));
            }

            mCardGlowSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.CardSelectionGlowPath);
            if (mCardGlowSprite == null)
            {
                mCardGlowSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.90f, 0.72f, 0.25f, 0.35f));
            }

            mDwellFillSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.CardSelectionProgressFillPath);
            if (mDwellFillSprite == null)
            {
                mDwellFillSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.95f, 0.82f, 0.33f, 0.95f));
            }

            mTitleBackgroundSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.TitleBackgroundPath);
            if (mTitleBackgroundSprite == null)
            {
                mTitleBackgroundSprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.12f, 0.09f, 0.07f, 1.0f));
            }

            mCardSelectionBackgroundSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.CardSelectionBackgroundPath);
            if (mCardSelectionBackgroundSprite == null)
            {
                mCardSelectionBackgroundSprite = mTitleBackgroundSprite;
            }

            mMouthChamberBackgroundSprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.MouthChamberBackgroundPath);
            if (mMouthChamberBackgroundSprite == null)
            {
                mMouthChamberBackgroundSprite = mCardSelectionBackgroundSprite == null
                    ? mTitleBackgroundSprite
                    : mCardSelectionBackgroundSprite;
            }

            mCarpetImage.sprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.FloorRunnerPath);
            if (mCarpetImage.sprite == null)
            {
                mCarpetImage.sprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.44f, 0.03f, 0.05f, 1.0f));
            }

            mLogoImage.sprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.TitleLogoPath);
            if (mLogoImage.sprite == null)
            {
                mLogoImage.sprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.82f, 0.71f, 0.52f, 1.0f));
            }

            mMouthImage.sprite = await RuntimeSpriteLoader.LoadSpriteOrNullAsync(MouthOfTruthAssetCatalog.TruthMouthFacePath);
            if (mMouthImage.sprite == null)
            {
                mMouthImage.sprite = RuntimeSpriteLoader.CreateSolidSprite(new Color(0.85f, 0.83f, 0.78f, 1.0f));
            }

            mBackgroundImage.sprite = mTitleBackgroundSprite;
        }

        private void loadUiFonts()
        {
            mUiFont = Resources.Load<Font>(MouthOfTruthAssetCatalog.UiFontResourceName);
            if (mUiFont == null)
            {
                mUiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            mKoreanFallbackFont = Resources.Load<Font>(MouthOfTruthAssetCatalog.KoreanFallbackFontResourceName);
            if (mKoreanFallbackFont == null)
            {
                mKoreanFallbackFont = mUiFont;
            }
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
            mTutorialLeapMotionDeviceImage.sprite = mLeapMotionDeviceSprite;
            mTutorialLeapMotionDeviceImage.preserveAspect = true;
            mTutorialLeapMotionDeviceImage.raycastTarget = false;
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
            mUseWorldEnvironmentLayout = mWorldCamera != null && mCardPresentationAnchorSet != null && mCardPresentationAnchorSet.HasRequiredAnchors() && mMouthAnchorSet != null && mMouthAnchorSet.HasRequiredAnchors();
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

        private static float getCardEntranceDelay(EQuestionCardSlot questionCardSlot)
        {
            return questionCardSlot switch
            {
                EQuestionCardSlot.LeftCard => 0.04f,
                EQuestionCardSlot.CenterCard => 0.00f,
                EQuestionCardSlot.RightCard => 0.08f,
                _ => 0.0f,
            };
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
            Vector2 anchoredPosition;
            return tryProjectWorldAnchor(anchorTransform, fallbackPosition, out anchoredPosition)
                ? anchoredPosition
                : fallbackPosition;
        }

        private Vector2 getMouthAnchorPosition()
        {
            Vector2 mouthCenter;
            Vector2 ignoredMouthSize;
            if (isTempleApproachSceneActive() && tryGetActiveStageMouthLayout(out mouthCenter, out ignoredMouthSize))
            {
                return mouthCenter;
            }

            Vector2 anchoredPosition;
            return tryProjectWorldAnchor(mMouthAnchorSet != null ? mMouthAnchorSet.TruthMouth : null, FALLBACK_MOUTH_POSITION, out anchoredPosition)
                ? anchoredPosition
                : FALLBACK_MOUTH_POSITION;
        }

        private Vector2 getTempleApproachMouthCanvasPosition()
        {
            if (mTempleApproachMouthImage == null || mCanvasRootRectTransform == null)
            {
                return getMouthAnchorPosition();
            }

            RectTransform mouthRectTransform = mTempleApproachMouthImage.rectTransform;
            Vector3 mouthWorldPosition = mouthRectTransform.TransformPoint(mouthRectTransform.rect.center);
            return mCanvasRootRectTransform.InverseTransformPoint(mouthWorldPosition);
        }

        private Vector2 getHandFrontPosition()
        {
            Vector2 mouthCenter;
            Vector2 mouthSize;
            if (isTempleApproachSceneActive() && tryGetActiveStageMouthLayout(out mouthCenter, out mouthSize))
            {
                return mouthCenter + new Vector2(mouthSize.x * TEMPLE_HAND_FRONT_OFFSET_FACTOR.x, mouthSize.y * TEMPLE_HAND_FRONT_OFFSET_FACTOR.y);
            }

            Vector2 anchoredPosition;
            return tryProjectWorldAnchor(mMouthAnchorSet != null ? mMouthAnchorSet.MouthFrontAnchor : null, FALLBACK_HAND_FRONT_POSITION, out anchoredPosition)
                ? anchoredPosition
                : FALLBACK_HAND_FRONT_POSITION;
        }

        private Vector2 getHandInnerPosition()
        {
            Vector2 mouthCenter;
            Vector2 mouthSize;
            if (isTempleApproachSceneActive() && tryGetActiveStageMouthLayout(out mouthCenter, out mouthSize))
            {
                return mouthCenter + new Vector2(mouthSize.x * TEMPLE_HAND_INNER_OFFSET_FACTOR.x, mouthSize.y * TEMPLE_HAND_INNER_OFFSET_FACTOR.y);
            }

            Vector2 anchoredPosition;
            return tryProjectWorldAnchor(mMouthAnchorSet != null ? mMouthAnchorSet.MouthInnerAnchor : null, FALLBACK_HAND_INNER_POSITION, out anchoredPosition)
                ? anchoredPosition
                : FALLBACK_HAND_INNER_POSITION;
        }

        private bool tryProjectWorldAnchor(Transform worldAnchorTransform, Vector2 fallbackPosition, out Vector2 anchoredPosition)
        {
            anchoredPosition = fallbackPosition;

            if (mUseWorldEnvironmentLayout == false || worldAnchorTransform == null || mWorldCamera == null)
            {
                return false;
            }

            Vector3 screenPosition = mWorldCamera.WorldToScreenPoint(worldAnchorTransform.position);

            if (screenPosition.z <= 0.0f)
            {
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(mCanvasRootRectTransform, screenPosition, null, out anchoredPosition);
        }

        private bool tryConvertScreenPointToCanvasPosition(Vector2 screenPosition, out Vector2 anchoredPosition)
        {
            anchoredPosition = default;

            return mCanvasRootRectTransform != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(mCanvasRootRectTransform, screenPosition, null, out anchoredPosition);
        }

        private void applyStartScreenLayout()
        {
            setRectTransformLayout(mLogoImage.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(1000.0f, 560.0f));
            setRectTransformLayout(mStartButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.13f), new Vector2(520.0f, 150.0f));
            applyTopLeftExitButtonLayout();
        }

        private void applyCardSelectionLayout()
        {
            setRectTransformLayout(mPromptText.rectTransform, new Vector2(0.5f, 0.07f), new Vector2(1080.0f, 64.0f));
            mPromptText.fontSize = 30;
            applyTopLeftExitButtonLayout();
        }

        private void applyHandPromptPanelLayout()
        {
            applyTopLeftExitButtonLayout();
            setRectTransformLayout(mQuestionPanelImage.rectTransform, new Vector2(0.5f, 0.105f), new Vector2(1500.0f, 122.0f));
            setRectTransformLayout(mQuestionText.rectTransform, new Vector2(0.5f, 0.105f), new Vector2(1320.0f, 70.0f));
            setRectTransformLayout(mHandImage.rectTransform, new Vector2(0.5f, 0.21f), HELD_POINTER_CURSOR_SIZE_PIXELS);
            mQuestionText.fontSize = 30;
            mQuestionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void applyNarrationLayout()
        {
            applyTopLeftExitButtonLayout();
            setRectTransformLayout(mMouthImage.rectTransform, new Vector2(0.5f, 0.53f), new Vector2(640.0f, 640.0f));
            mMouthImage.rectTransform.localScale = Vector3.one;
            setRectTransformLayout(mQuestionPanelImage.rectTransform, new Vector2(0.5f, 0.105f), new Vector2(1500.0f, 122.0f));
            setRectTransformLayout(mQuestionText.rectTransform, new Vector2(0.5f, 0.105f), new Vector2(1320.0f, 70.0f));
            mQuestionText.fontSize = 30;
            mQuestionText.alignment = TextAnchor.MiddleCenter;
            mQuestionText.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void applyAwaitingHandInsertionLayout()
        {
            applyTopLeftExitButtonLayout();
            setRectTransformLayout(mMouthImage.rectTransform, new Vector2(0.5f, 0.56f), new Vector2(700.0f, 700.0f));
            mMouthImage.rectTransform.localScale = Vector3.one;
            setRectTransformLayout(mQuestionPanelImage.rectTransform, new Vector2(0.5f, 0.105f), new Vector2(1500.0f, 122.0f));
            setRectTransformLayout(mQuestionText.rectTransform, new Vector2(0.5f, 0.105f), new Vector2(1320.0f, 70.0f));
            setRectTransformLayout(mHandImage.rectTransform, new Vector2(0.5f, 0.22f), HELD_POINTER_CURSOR_SIZE_PIXELS);
            mQuestionText.fontSize = 30;
            mQuestionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void applyAnswerStageLayout()
        {
            applyTopLeftExitButtonLayout();
            setRectTransformLayout(mMouthImage.rectTransform, new Vector2(0.5f, 0.60f), new Vector2(760.0f, 760.0f));
            mMouthImage.rectTransform.localScale = Vector3.one;
            setRectTransformLayout(mQuestionPanelImage.rectTransform, new Vector2(0.5f, 0.105f), new Vector2(1500.0f, 122.0f));
            setRectTransformLayout(mQuestionText.rectTransform, new Vector2(0.5f, 0.105f), new Vector2(1320.0f, 70.0f));
            setRectTransformLayout(mHandImage.rectTransform, new Vector2(0.5f, 0.21f), HELD_POINTER_CURSOR_SIZE_PIXELS);
            mQuestionText.fontSize = 30;
            mQuestionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void applyAnsweringFocusLayout()
        {
            applyTopLeftExitButtonLayout();
            setRectTransformLayout(mMouthImage.rectTransform, ANSWERING_FOCUS_MOUTH_ANCHOR, ANSWERING_FOCUS_MOUTH_SIZE_PIXELS);
            mMouthImage.rectTransform.anchoredPosition = Vector2.zero;
            mMouthImage.rectTransform.localScale = Vector3.one;
            mMouthImage.color = Color.white;
            syncMouthEffectImageLayout(mMouthListeningAuraImage, 1.26f);
            syncMouthEffectImageLayout(mMouthAnalyzingAuraImage, 1.18f);
        }

        private void updateMouthEffectImage(Image effectImage, Color color, float scale, float rotationDegrees)
        {
            if (effectImage == null || effectImage.gameObject.activeSelf == false || mMouthImage == null)
            {
                return;
            }

            syncMouthEffectImageLayout(effectImage, 1.28f);
            effectImage.color = color;
            effectImage.rectTransform.localScale = mMouthImage.rectTransform.localScale * Mathf.Max(0.01f, scale);
            effectImage.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, rotationDegrees);
            placeMouthEffectImagesBehindMouth();
        }

        private void updateAnsweringEyeBeamImages(float elapsedSeconds, float quickPulse)
        {
            if (mMouthImage == null)
            {
                return;
            }

            float mouthWidth = mMouthImage.rectTransform.sizeDelta.x;
            float mouthHeight = mMouthImage.rectTransform.sizeDelta.y;
            float sweepProgress = Mathf.PingPong(elapsedSeconds * ANSWER_BEAM_SWEEP_RATE, 1.0f);
            float easedSweepProgress = easeInOut(sweepProgress);
            float sourceYOffset = mouthHeight * ANSWER_BEAM_SOURCE_Y_FACTOR;
            float endYOffset = mouthHeight * Mathf.Lerp(ANSWER_BEAM_END_BOTTOM_Y_FACTOR, ANSWER_BEAM_END_TOP_Y_FACTOR, easedSweepProgress);
            float beamHeight = Mathf.Max(1.0f, Mathf.Abs(sourceYOffset - endYOffset));
            float beamVerticalScale = endYOffset > sourceYOffset ? -1.0f : 1.0f;
            float beamAlpha = Mathf.Lerp(0.44f, 0.74f, Mathf.Pow(quickPulse, 1.18f));
            Vector2 beamSize = new Vector2(mouthWidth * 1.003f, beamHeight);
            Color beamColor = new Color(0.95f, 0.20f, 0.14f, beamAlpha);
            updateEyeBeamImage(mMouthLeftEyeBeamImage, new Vector2(-(mouthWidth * 0.084f), sourceYOffset), beamSize, beamColor, -2.6f, new Vector2(0.5f, 1.0f), beamVerticalScale);
            updateEyeBeamImage(mMouthRightEyeBeamImage, new Vector2(mouthWidth * 0.058f, sourceYOffset), beamSize, beamColor, 2.6f, new Vector2(0.5f, 1.0f), beamVerticalScale);
        }

        private void updateEyeBeamImage(Image beamImage, Vector2 offsetFromMouthCenter, Vector2 sizeDelta, Color color, float rotationDegrees, Vector2 pivot, float verticalScale)
        {
            if (beamImage == null || beamImage.gameObject.activeSelf == false || mMouthImage == null)
            {
                return;
            }

            RectTransform mouthRectTransform = mMouthImage.rectTransform;
            RectTransform beamRectTransform = beamImage.rectTransform;
            beamRectTransform.anchorMin = mouthRectTransform.anchorMin;
            beamRectTransform.anchorMax = mouthRectTransform.anchorMax;
            beamRectTransform.pivot = pivot;
            beamRectTransform.anchoredPosition = mouthRectTransform.anchoredPosition + offsetFromMouthCenter;
            beamRectTransform.sizeDelta = sizeDelta;
            beamRectTransform.localScale = new Vector3(1.0f, verticalScale, 1.0f);
            beamRectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, rotationDegrees);
            beamImage.color = color;
            placeEyeBeamImagesAboveMouth();
        }

        private void syncMouthEffectImageLayout(Image effectImage, float sizeMultiplier)
        {
            if (effectImage == null || mMouthImage == null)
            {
                return;
            }

            RectTransform mouthRectTransform = mMouthImage.rectTransform;
            RectTransform effectRectTransform = effectImage.rectTransform;
            effectRectTransform.anchorMin = mouthRectTransform.anchorMin;
            effectRectTransform.anchorMax = mouthRectTransform.anchorMax;
            effectRectTransform.anchoredPosition = mouthRectTransform.anchoredPosition;
            effectRectTransform.sizeDelta = mouthRectTransform.sizeDelta * Mathf.Max(0.01f, sizeMultiplier);
            effectRectTransform.localRotation = Quaternion.identity;
        }

        private void setMouthEffectImagesActive(bool isListeningAuraActive, bool isAnalyzingAuraActive)
        {
            setObjectActive(mMouthListeningAuraImage, isListeningAuraActive);
            setObjectActive(mMouthAnalyzingAuraImage, isAnalyzingAuraActive);
            placeMouthEffectImagesBehindMouth();
        }

        private void setEyeBeamImagesActive(bool isActive)
        {
            setObjectActive(mMouthLeftEyeBeamImage, isActive);
            setObjectActive(mMouthRightEyeBeamImage, isActive);
            placeEyeBeamImagesAboveMouth();
        }

        private void placeMouthEffectImagesBehindMouth()
        {
            if (mMouthImage == null)
            {
                return;
            }

            int mouthSiblingIndex = mMouthImage.transform.GetSiblingIndex();

            if (mMouthListeningAuraImage != null)
            {
                int targetSiblingIndex = Mathf.Min(mCanvasRootTransform.childCount - 1, mouthSiblingIndex + 1);
                mMouthListeningAuraImage.transform.SetSiblingIndex(targetSiblingIndex);
            }

            if (mMouthAnalyzingAuraImage != null)
            {
                int targetSiblingIndex = Mathf.Min(mCanvasRootTransform.childCount - 1, mouthSiblingIndex + 2);
                mMouthAnalyzingAuraImage.transform.SetSiblingIndex(targetSiblingIndex);
            }
        }

        private void placeEyeBeamImagesAboveMouth()
        {
            if (mMouthImage == null)
            {
                return;
            }

            int mouthSiblingIndex = mMouthImage.transform.GetSiblingIndex();
            int targetSiblingIndex = Mathf.Min(mCanvasRootTransform.childCount - 1, mouthSiblingIndex + 1);

            if (mMouthLeftEyeBeamImage != null)
            {
                mMouthLeftEyeBeamImage.transform.SetSiblingIndex(targetSiblingIndex);
            }

            if (mMouthRightEyeBeamImage != null)
            {
                mMouthRightEyeBeamImage.transform.SetSiblingIndex(targetSiblingIndex);
            }
        }

        private void applyResultLayout(EVerdictKind verdictKind)
        {
            if (isTempleApproachSceneActive())
            {
                syncTempleStageMouthOverlay(0.0f);
            }
            else
            {
                setRectTransformLayout(mMouthImage.rectTransform, RESULT_MOUTH_ANCHOR, RESULT_MOUTH_SIZE_PIXELS);
                mMouthImage.rectTransform.localScale = Vector3.one;
            }

            Vector2 verdictSizePixels = verdictKind == EVerdictKind.True || verdictKind == EVerdictKind.False
                ? RESULT_SHORT_VERDICT_SIZE_PIXELS
                : RESULT_VERDICT_SIZE_PIXELS;
            setRectTransformLayout(mVerdictImage.rectTransform, new Vector2(0.5f, 0.54f), verdictSizePixels);
            setRectTransformLayout(mHandImage.rectTransform, new Vector2(0.5f, 0.18f), HELD_POINTER_CURSOR_SIZE_PIXELS);
            setRectTransformLayout(mTryAgainButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.105f), new Vector2(360.0f, 100.0f));
            applyTopLeftExitButtonLayout();
        }

        private void applyTopLeftExitButtonLayout()
        {
            setRectTransformLayout(mExitButton.GetComponent<RectTransform>(), new Vector2(0.06f, 0.90f), new Vector2(78.0f, 78.0f));
        }

        private void setOverlayAlpha(float alpha)
        {
            setOverlayTint(SCENE_OVERLAY_COLOR, alpha);
        }

        private void setGameplayOverlayAlpha(float alpha)
        {
            if (isTempleApproachSceneActive())
            {
                setOverlayTint(STAGE_OVERLAY_TINT, alpha);
                return;
            }

            setOverlayAlpha(alpha);
        }

        private void setOverlayTint(Color tintColor, float alpha)
        {
            if (mSceneOverlayImage == null)
            {
                return;
            }

            mSceneOverlayImage.color = new Color(tintColor.r, tintColor.g, tintColor.b, Mathf.Clamp01(alpha));
        }

        private float getHandPromptPanelHoldSeconds()
        {
            return mHandPromptClip != null
                ? Mathf.Max(0.55f, mHandPromptClip.length)
                : HAND_PROMPT_PANEL_FALLBACK_HOLD_SECONDS;
        }

        private void hideHandPromptPanelImmediately()
        {
            mIsHandPromptPanelDismissalActive = false;
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mQuestionText, false);
            resetHandPromptPanelAlpha();
        }

        private void hideHandPromptPanelAfterFade()
        {
            mIsHandPromptPanelDismissalActive = false;
            setHandPromptPanelAlpha(0.0f);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mQuestionText, false);
        }

        private void resetHandPromptPanelAlpha()
        {
            mIsHandPromptPanelDismissalActive = false;
            mHandPromptPanelDismissalStartAlpha = 1.0f;
            setHandPromptPanelAlpha(1.0f);
        }

        private void updateHandPromptPanelDismissal()
        {
            if (mIsHandPromptPanelDismissalActive == false)
            {
                return;
            }

            float elapsedSeconds = Time.unscaledTime - mHandPromptPanelDismissalStartedAtSeconds;
            float progress = Mathf.Clamp01(elapsedSeconds / HAND_PROMPT_PANEL_DISMISS_SECONDS);
            float easedProgress = easeOut(progress);
            setHandPromptPanelAlpha(Mathf.Lerp(mHandPromptPanelDismissalStartAlpha, 0.0f, easedProgress));

            if (progress < 1.0f)
            {
                return;
            }

            hideHandPromptPanelAfterFade();
        }

        private float getHandPromptPanelAlpha()
        {
            if (mQuestionPanelImage != null)
            {
                return mQuestionPanelImage.color.a;
            }

            return mQuestionText != null ? mQuestionText.color.a : 0.0f;
        }

        private void setHandPromptPanelAlpha(float alpha)
        {
            float clampedAlpha = Mathf.Clamp01(alpha);

            if (mQuestionPanelImage != null)
            {
                Color panelColor = mQuestionPanelImage.color;
                mQuestionPanelImage.color = new Color(panelColor.r, panelColor.g, panelColor.b, clampedAlpha);
            }

            if (mQuestionText != null)
            {
                Color textColor = mQuestionText.color;
                mQuestionText.color = new Color(textColor.r, textColor.g, textColor.b, clampedAlpha);
            }
        }

        private void setBackgroundTint(Color tintColor)
        {
            if (mBackgroundImage == null)
            {
                return;
            }

            mBackgroundImage.color = tintColor;
        }

        private void resetStageMotionTransforms()
        {
            if (mBackgroundImage != null)
            {
                RectTransform backgroundRectTransform = mBackgroundImage.rectTransform;
                backgroundRectTransform.pivot = new Vector2(0.5f, 0.5f);
                backgroundRectTransform.anchorMin = Vector2.zero;
                backgroundRectTransform.anchorMax = Vector2.one;
                backgroundRectTransform.offsetMin = Vector2.zero;
                backgroundRectTransform.offsetMax = Vector2.zero;
                backgroundRectTransform.anchoredPosition = Vector2.zero;
                backgroundRectTransform.localScale = Vector3.one;
                backgroundRectTransform.localRotation = Quaternion.identity;
            }

            if (mCarpetImage != null)
            {
                RectTransform carpetRectTransform = mCarpetImage.rectTransform;
                carpetRectTransform.pivot = new Vector2(0.5f, 0.5f);
                carpetRectTransform.anchorMin = new Vector2(0.5f, 0.0f);
                carpetRectTransform.anchorMax = new Vector2(0.5f, 0.0f);
                carpetRectTransform.anchoredPosition = STAGE_CARPET_POSITION;
                carpetRectTransform.sizeDelta = STAGE_CARPET_SIZE;
                carpetRectTransform.localScale = Vector3.one;
                carpetRectTransform.localRotation = Quaternion.identity;
                mCarpetImage.color = STAGE_CARPET_TINT;
            }

            if (mMouthImage != null)
            {
                mMouthImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                mMouthImage.rectTransform.localRotation = Quaternion.identity;
            }
        }

        private void setRectTransformLayout(RectTransform rectTransform, Vector2 anchor, Vector2 sizeDelta)
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
            mSceneOverlayImage = createFullScreenImage("SceneOverlay", mCanvasRootTransform, new Color(0.01f, 0.01f, 0.015f, 0.0f));
            mCarpetImage = createImage("RedCarpet", mCanvasRootTransform, new Vector2(0.5f, 0.0f), new Vector2(0.5f, 0.0f), STAGE_CARPET_POSITION, STAGE_CARPET_SIZE, STAGE_CARPET_TINT);
            mTitleVignetteImage = createFullScreenImage("TitleVignette", mCanvasRootTransform, new Color(1.0f, 1.0f, 1.0f, 0.55f));
            mLogoImage = createImage("Logo", mCanvasRootTransform, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(0.0f, 0.0f), new Vector2(840.0f, 360.0f), Color.white);
            mQuestionPanelImage = createImage("QuestionPanel", mCanvasRootTransform, new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), new Vector2(0.0f, 0.0f), new Vector2(1280.0f, 170.0f), Color.white);
            mStatusPanelImage = createImage("StatusPanel", mCanvasRootTransform, new Vector2(0.5f, 0.105f), new Vector2(0.5f, 0.105f), new Vector2(0.0f, 0.0f), new Vector2(1320.0f, 150.0f), Color.white);
            mResultPanelImage = createImage("ResultPanel", mCanvasRootTransform, new Vector2(0.5f, 0.39f), new Vector2(0.5f, 0.39f), new Vector2(0.0f, 0.0f), new Vector2(980.0f, 420.0f), Color.white);
            mPromptText = createText("PromptText", mCanvasRootTransform, new Vector2(0.5f, 0.13f), new Vector2(0.5f, 0.13f), new Vector2(0.0f, 0.0f), new Vector2(700.0f, 80.0f), 38, FontStyle.Bold);
            mStatusText = createText("StatusText", mCanvasRootTransform, new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f), new Vector2(0.0f, 0.0f), new Vector2(1200.0f, 70.0f), 26, FontStyle.Bold);
            mQuestionText = createText("QuestionText", mCanvasRootTransform, new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), new Vector2(0.0f, 0.0f), new Vector2(1200.0f, 140.0f), 34, FontStyle.Bold);
            mAnalyzingDotsText = createText("AnalyzingDotsText", mCanvasRootTransform, new Vector2(0.5f, 0.57f), new Vector2(0.5f, 0.57f), Vector2.zero, new Vector2(360.0f, 140.0f), 90, FontStyle.Bold);
            mAnswerTimerText = createText("AnswerTimerText", mCanvasRootTransform, new Vector2(0.85f, 0.92f), new Vector2(0.85f, 0.92f), new Vector2(0.0f, 0.0f), new Vector2(320.0f, 50.0f), 22, FontStyle.Normal);
            mMouthImage = createImage("TruthMouth", mCanvasRootTransform, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), new Vector2(0.0f, 60.0f), new Vector2(430.0f, 430.0f), Color.white);
            mMouthListeningAuraImage = createImage("MouthListeningAura", mCanvasRootTransform, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), new Vector2(0.0f, 60.0f), new Vector2(640.0f, 640.0f), Color.clear);
            mMouthListeningAuraImage.sprite = createRadialGlowSprite();
            mMouthListeningAuraImage.raycastTarget = false;
            mMouthAnalyzingAuraImage = createImage("MouthAnalyzingAura", mCanvasRootTransform, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), new Vector2(0.0f, 60.0f), new Vector2(720.0f, 720.0f), Color.clear);
            mMouthAnalyzingAuraImage.sprite = createRingGlowSprite();
            mMouthAnalyzingAuraImage.raycastTarget = false;
            mMouthLeftEyeBeamImage = createImage("MouthLeftEyeBeam", mCanvasRootTransform, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), new Vector2(0.0f, 60.0f), new Vector2(360.0f, 72.0f), Color.clear);
            mMouthLeftEyeBeamImage.sprite = createEyeBeamSprite(isSourceOnRight: false);
            mMouthLeftEyeBeamImage.type = Image.Type.Simple;
            mMouthLeftEyeBeamImage.raycastTarget = false;
            mMouthRightEyeBeamImage = createImage("MouthRightEyeBeam", mCanvasRootTransform, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), new Vector2(0.0f, 60.0f), new Vector2(360.0f, 72.0f), Color.clear);
            mMouthRightEyeBeamImage.sprite = createEyeBeamSprite(isSourceOnRight: true);
            mMouthRightEyeBeamImage.type = Image.Type.Simple;
            mMouthRightEyeBeamImage.raycastTarget = false;
            placeMouthEffectImagesBehindMouth();
            mHandImage = createImage("HeldPointer", mCanvasRootTransform, new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), new Vector2(0.0f, 0.0f), HELD_POINTER_CURSOR_SIZE_PIXELS, Color.white);
            mRitualHandImage = createImage("RitualHand", mCanvasRootTransform, new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), Vector2.zero, RITUAL_HAND_SIZE_PIXELS, Color.white);
            mPointerImage = createImage("InputPointer", mCanvasRootTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, POINTER_CURSOR_SIZE_PIXELS, Color.white);
            mVerdictImage = createImage("VerdictImage", mCanvasRootTransform, new Vector2(0.5f, 0.63f), new Vector2(0.5f, 0.63f), new Vector2(0.0f, 0.0f), new Vector2(820.0f, 240.0f), Color.white);
            mVerdictText = createText("VerdictText", mCanvasRootTransform, new Vector2(0.5f, 0.49f), new Vector2(0.5f, 0.49f), new Vector2(0.0f, 0.0f), new Vector2(640.0f, 80.0f), 48, FontStyle.Bold);
            mAnswerInputField = createInputField();
            mStartButton = createButton("StartButton", "START GAME", new Vector2(0.5f, 0.22f), new Vector2(280.0f, 80.0f), () => mStartRequested = true);
            mTryAgainButton = createButton("TryAgainButton", "TRY AGAIN", new Vector2(0.5f, 0.26f), new Vector2(280.0f, 80.0f), () => mTryAgainRequested = true);
            mBackToTitleButton = createButton("BackToTitleButton", "BACK TO TITLE", new Vector2(0.5f, 0.18f), new Vector2(340.0f, 72.0f), () => mBackToTitleRequested = true);
            mExitButton = createButton("ExitButton", "EXIT GAME", new Vector2(0.5f, 0.12f), new Vector2(320.0f, 72.0f), () => mExitRequested = true);

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
            mTutorialOverlayImage = createFullScreenImage("FirstRunTutorialOverlay", mCanvasRootTransform, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            mTutorialDevicePanelImage = createImage("FirstRunTutorialPanel", mCanvasRootTransform, new Vector2(0.5f, 0.46f), new Vector2(0.5f, 0.46f), Vector2.zero, new Vector2(900.0f, 520.0f), new Color(0.12f, 0.12f, 0.135f, 0.96f));
            mTutorialLeapMotionDeviceImage = createImage("FirstRunTutorialLeapMotionDevice", mCanvasRootTransform, new Vector2(0.5f, 0.46f), new Vector2(0.5f, 0.46f), Vector2.zero, TUTORIAL_LEAP_MOTION_DEVICE_SIZE_PIXELS, Color.white);
            mTutorialHandImage = createImage("FirstRunTutorialHand", mCanvasRootTransform, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), Vector2.zero, TUTORIAL_HAND_SIZE_PIXELS, Color.white);
            mTutorialTitleText = createText("FirstRunTutorialTitle", mCanvasRootTransform, new Vector2(0.5f, 0.755f), new Vector2(0.5f, 0.755f), Vector2.zero, new Vector2(980.0f, 64.0f), 34, FontStyle.Bold);
            mTutorialBodyText = createText("FirstRunTutorialBody", mCanvasRootTransform, new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(1040.0f, 72.0f), 26, FontStyle.Normal);
            mTutorialStepText = createText("FirstRunTutorialStep", mCanvasRootTransform, new Vector2(0.5f, 0.17f), new Vector2(0.5f, 0.17f), Vector2.zero, new Vector2(980.0f, 54.0f), 24, FontStyle.Bold);
            mTutorialOverlayImage.transform.SetAsLastSibling();
            mTutorialDevicePanelImage.transform.SetAsLastSibling();
            mTutorialLeapMotionDeviceImage.transform.SetAsLastSibling();
            mTutorialHandImage.transform.SetAsLastSibling();
            mTutorialTitleText.transform.SetAsLastSibling();
            mTutorialBodyText.transform.SetAsLastSibling();
            mTutorialStepText.transform.SetAsLastSibling();
            setObjectActive(mTutorialOverlayImage, false);
            setObjectActive(mTutorialDevicePanelImage, false);
            setObjectActive(mTutorialLeapMotionDeviceImage, false);
            setObjectActive(mTutorialHandImage, false);
            setObjectActive(mTutorialTitleText, false);
            setObjectActive(mTutorialBodyText, false);
            setObjectActive(mTutorialStepText, false);
            setMouthEffectImagesActive(false, false);
            setEyeBeamImagesActive(false);
            mLoadingOverlayImage = createFullScreenImage("LoadingOverlay", mCanvasRootTransform, Color.black);
            mLoadingOverlayImage.transform.SetAsLastSibling();
            mLoadingOverlayImage.raycastTarget = true;
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
            questionCardView.Initialize(questionCardSlot, mCanvasRootTransform, mCardBackSprite, mUiFont, mKoreanFallbackFont);
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

        private Image createImage(string objectName, Transform parentTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
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

        private Text createText(string objectName, Transform parentTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, int fontSize, FontStyle fontStyle)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parentTransform, false);
            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            Text text = textObject.AddComponent<Text>();
            Font textFont = mUiFont;
            if (textFont == null)
            {
                textFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            text.font = textFont;
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

            string safeValue = string.IsNullOrEmpty(value) ? string.Empty : value;
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

            Text placeholderText = createText("Placeholder", inputFieldObject.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-60.0f, -20.0f), 24, FontStyle.Italic);
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.color = new Color(0.80f, 0.74f, 0.66f, 0.7f);
            setText(placeholderText, "입력된 답변이 이 영역에 표시됩니다.");

            Text valueText = createText("Text", inputFieldObject.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-60.0f, -20.0f), 24, FontStyle.Normal);
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

        private Button createButton(string objectName, string labelText, Vector2 anchoredPosition, Vector2 sizeDelta, Action clickedAction)
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

            Text label = createText("Label", buttonObject.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20.0f, -20.0f), 34, FontStyle.Bold);
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

        private bool isScreenPointOverButton(Button button, Vector2 screenPosition, float intentExpansionPixels)
        {
            return button != null
                && button.gameObject.activeInHierarchy
                && button.interactable
                && (isScreenPointOverRectTransform(button.GetComponent<RectTransform>(), screenPosition) || isScreenPointOverExpandedRectTransform(button.GetComponent<RectTransform>(), screenPosition, intentExpansionPixels));
        }

        private bool isScreenPointOverRectTransform(RectTransform rectTransform, Vector2 screenPosition)
        {
            return rectTransform != null
                && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, null);
        }

        private bool isScreenPointOverExpandedRectTransform(RectTransform rectTransform, Vector2 screenPosition, float expansionPixels)
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
                Vector2 screenCorner = RectTransformUtility.WorldToScreenPoint(null, mHitTestWorldCorners[cornerIndex]);
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
                button.image.color = Color.Lerp(Color.white, new Color(1.0f, 0.92f, 0.78f, 1.0f), effectiveHoverProgress);
            }

            Text label = button.GetComponentInChildren<Text>();

            if (label != null)
            {
                label.color = Color.Lerp(new Color(0.88f, 0.84f, 0.76f, 1.0f), new Color(1.0f, 0.97f, 0.84f, 1.0f), effectiveHoverProgress);
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

        private static float easeInOut(float progress)
        {
            float clampedProgress = Mathf.Clamp01(progress);
            return clampedProgress * clampedProgress * (3.0f - (2.0f * clampedProgress));
        }

        private static bool isInsideAnchorWindow(Vector2 offsetFromAnchor, float halfWidth, float halfHeight)
        {
            return Mathf.Abs(offsetFromAnchor.x) <= halfWidth
                && Mathf.Abs(offsetFromAnchor.y) <= halfHeight;
        }

        private void addTextShadow(GameObject textObject)
        {
            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.05f, 0.03f, 0.02f, 0.92f);
            shadow.effectDistance = new Vector2(2.0f, -2.0f);
        }
    }
}
