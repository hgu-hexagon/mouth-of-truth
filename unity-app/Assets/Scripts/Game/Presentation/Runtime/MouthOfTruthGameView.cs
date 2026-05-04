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
        private const float ANSWER_HOLD_CORRIDOR_HALF_WIDTH_FACTOR = 0.12f;
        private const float ANSWER_HOLD_CORRIDOR_MARGIN_FACTOR = 0.05f;
        private const float CARD_FRONT_READ_HOLD_MINIMUM_SECONDS = 1.875f;
        private const float CARD_FRONT_READ_HOLD_MAXIMUM_SECONDS = 3.225f;
        private const float CARD_FRONT_READ_HOLD_PER_CHARACTER_SECONDS = 0.01875f;

        private readonly Dictionary<EQuestionCardSlot, QuestionCardView> mCardViews =
            new Dictionary<EQuestionCardSlot, QuestionCardView>();

        private Canvas mCanvas;
        private RectTransform mCanvasRootRectTransform;
        private Transform mCanvasRootTransform;
        private Image mBackgroundImage;
        private Image mSceneOverlayImage;
        private Image mLoadingOverlayImage;
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
        private Sprite mHandCursorSprite;
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
        private AudioClip mHandPauseClip;
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

        private bool mStartRequested;
        private bool mTryAgainRequested;
        private bool mBackToTitleRequested;
        private bool mExitRequested;

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
            setText(mQuestionText, "답변을 듣고 있습니다" + new string('.', dotCount));

            if (mQuestionPanelImage == null)
            {
                return;
            }

            float pulse = (Mathf.Sin(elapsedSeconds * 3.0f) + 1.0f) * 0.5f;
            mQuestionPanelImage.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(0.86f, 1.0f, pulse));
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

            float pulse = (Mathf.Sin(elapsedSeconds * 2.6f) + 1.0f) * 0.5f;
            mMouthImage.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(0.48f, 0.64f, pulse));
            mMouthImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.985f, 1.01f, pulse);
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
        }

        public void UpdateCardHoverVisual(EQuestionCardSlot? hoveredQuestionCardSlot, float hoverProgress)
        {
            if (hoveredQuestionCardSlot != mLastAudibleHoveredCardSlot)
            {
                if (hoveredQuestionCardSlot.HasValue)
                {
                    playInterfaceCue(mCardHoverClip, 0.65f);
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
            QuestionDefinition questionDefinition)
        {
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mQuestionText, false);
            setObjectActive(mSceneOverlayImage, true);
            setOverlayAlpha(0.12f);
            playInterfaceCue(mCardSelectClip, 0.90f);

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
            playInterfaceCue(mCardRevealClip, 0.85f);

            await animateOverTimeAsync(
                0.16f,
                progress =>
                {
                    float easedProgress = easeOut(progress);
                    selectedCardView.SetScale(Mathf.Lerp(1.22f, 1.26f, easedProgress));
                });

            float cardFrontReadHoldDurationSeconds = getCardFrontReadHoldDurationSeconds(questionDefinition.Text);
            await animateOverTimeAsync(
                cardFrontReadHoldDurationSeconds,
                progress =>
                {
                    float pulse = Mathf.Sin(progress * Mathf.PI) * 0.012f;
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

        public void ShowNarratingQuestion(string questionText)
        {
            disableAnsweringPresentation();
            disableAnalyzingPresentation();
            disableHeldHandPresentation();
            applyNarrationLayout();
            mBackgroundImage.sprite = mMouthChamberBackgroundSprite;
            setBackgroundTint(STAGE_BACKGROUND_TINT);
            setObjectActive(mBackgroundImage, true);
            setObjectActive(mCarpetImage, false);
            setObjectActive(mSceneOverlayImage, true);
            setOverlayAlpha(0.24f);
            configureExitButtonAsTopLeftIcon();
            setObjectActive(mExitButton, true);
            setObjectActive(mQuestionPanelImage, true);
            setObjectActive(mQuestionText, true);
            setObjectActive(mStatusPanelImage, false);
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mAnswerTimerText, false);
            setObjectActive(mMouthImage, true);
            setObjectActive(mHandImage, false);
            setObjectActive(mPointerImage, false);
            setText(mQuestionText, questionText);
            applyMouthAnchoredLayout();
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
        }

        public async Task AnimateHandInsertionAsync()
        {
            disableAnsweringPresentation();
            disableHeldHandPresentation();
            applyAnswerStageLayout();
            setObjectActive(mPointerImage, false);
            setObjectActive(mHandImage, true);
            playInterfaceCue(mHandInsertClip, 0.9f);

            await animateOverTimeAsync(
                0.45f,
                progress =>
                {
                    float easedProgress = easeOut(progress);
                    setHandVisual(Mathf.Lerp(0.0f, 1.0f, easedProgress));
                    mMouthImage.rectTransform.localScale =
                        Vector3.one * Mathf.Lerp(1.0f, 1.08f, easedProgress);
                });
        }

        public async Task AnimateHandRemovalAsync()
        {
            disableAnsweringPresentation();
            disableHeldHandPresentation();
            playInterfaceCue(mHandPauseClip, 0.9f);
            await animateOverTimeAsync(
                0.35f,
                progress => setHandVisual(Mathf.Lerp(1.0f, 0.0f, easeOut(progress))));
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
            setObjectActive(mQuestionPanelImage, true);
            setObjectActive(mQuestionText, true);
            setObjectActive(mStatusPanelImage, false);
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mAnswerTimerText, false);
            setText(mQuestionText, "답변을 듣고 있습니다.");
            setObjectActive(mPointerImage, false);
            applyMouthAnchoredLayout();
            enableHeldHandPresentation(baseProgress: 0.78f, pulseAmplitude: 0.007f, pulseSpeed: 1.3f);
            enableAnsweringPresentation();
        }

        public void ShowAnswerPaused()
        {
            disableAnsweringPresentation();
            disableAnalyzingPresentation();
            applyAnswerStageLayout();
            mBackgroundImage.sprite = mMouthChamberBackgroundSprite;
            setBackgroundTint(STAGE_BACKGROUND_TINT);
            setObjectActive(mBackgroundImage, true);
            setObjectActive(mCarpetImage, false);
            mAnswerInputField.interactable = false;
            setObjectActive(mSceneOverlayImage, true);
            setOverlayAlpha(0.30f);
            configureExitButtonAsTopLeftIcon();
            setObjectActive(mExitButton, true);
            setObjectActive(mQuestionPanelImage, true);
            setObjectActive(mQuestionText, true);
            setObjectActive(mStatusPanelImage, false);
            setObjectActive(mPromptText, false);
            setObjectActive(mStatusText, false);
            setObjectActive(mAnswerTimerText, false);
            setText(mQuestionText, "손을 다시 올리면 답변이 이어집니다.");
            setObjectActive(mHandImage, false);
            applyMouthAnchoredLayout();
            disableHeldHandPresentation();
        }

        public void ShowAnalyzing()
        {
            disableAnsweringPresentation();
            disableHeldHandPresentation();
            applyAnswerStageLayout();
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
            setHandVisual(0.82f);
            setObjectActive(mHandImage, true);
            enableAnalyzingPresentation();
        }

        public async Task PlayAnalysisCompleteTransitionAsync()
        {
            if (mIsAnalyzingPresentationActive == false)
            {
                return;
            }

            await animateOverTimeAsync(
                0.45f,
                progress =>
                {
                    float easedProgress = easeOut(progress);
                    setOverlayAlpha(Mathf.Lerp(0.34f, 0.48f, easedProgress));
                    mMouthImage.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(0.58f, 0.42f, easedProgress));
                    mMouthImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.035f, easedProgress);
                    mAnalyzingDotsText.color = new Color(1.0f, 0.92f, 0.78f, Mathf.Lerp(1.0f, 0.0f, easedProgress));
                });

            disableAnalyzingPresentation();
        }

        public void ShowResult(EVerdictKind verdictKind, string transcriptText)
        {
            disableAnsweringPresentation();
            disableAnalyzingPresentation();
            disableHeldHandPresentation();
            applyResultLayout(verdictKind);
            configureExitButtonAsEndGameButton();
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
            playVerdictCue(verdictKind);
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

                return null;
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

            if (isScreenPointOverButton(mStartButton, screenPosition))
            {
                return EUiActionTarget.StartGame;
            }

            if (isScreenPointOverButton(mTryAgainButton, screenPosition))
            {
                return EUiActionTarget.TryAgain;
            }

            if (isScreenPointOverButton(mExitButton, screenPosition))
            {
                return EUiActionTarget.ExitGame;
            }

            if (isScreenPointOverButton(mBackToTitleButton, screenPosition))
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
            return EvaluateHandAnchorState(
                pointerCanvasPosition,
                getHandFrontPosition(),
                getHandInnerPosition(),
                mouthDiameterPixels);
        }

        public bool IsAnswerHoldMaintained(Vector2? pointerScreenPosition)
        {
            if (pointerScreenPosition.HasValue == false)
            {
                return false;
            }

            if (tryConvertScreenPointToCanvasPosition(
                    pointerScreenPosition.Value,
                    out Vector2 pointerCanvasPosition) == false)
            {
                return false;
            }

            float mouthDiameterPixels = Mathf.Max(
                1.0f,
                Mathf.Min(mMouthImage.rectTransform.rect.width, mMouthImage.rectTransform.rect.height));
            return EvaluateAnswerHoldState(
                pointerCanvasPosition,
                getHandFrontPosition(),
                getHandInnerPosition(),
                mouthDiameterPixels);
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

        public static bool EvaluateAnswerHoldState(
            Vector2 pointerCanvasPosition,
            Vector2 handFrontPosition,
            Vector2 handInnerPosition,
            float mouthDiameterPixels)
        {
            if (EvaluateHandAnchorState(
                    pointerCanvasPosition,
                    handFrontPosition,
                    handInnerPosition,
                    mouthDiameterPixels) != EHandAnchorState.OutsideMouth)
            {
                return true;
            }

            float clampedMouthDiameterPixels = Mathf.Max(1.0f, mouthDiameterPixels);
            float corridorHalfWidth = clampedMouthDiameterPixels * ANSWER_HOLD_CORRIDOR_HALF_WIDTH_FACTOR;
            float corridorMargin = clampedMouthDiameterPixels * ANSWER_HOLD_CORRIDOR_MARGIN_FACTOR;
            float minimumY = Mathf.Min(handFrontPosition.y, handInnerPosition.y) - corridorMargin;
            float maximumY = Mathf.Max(handFrontPosition.y, handInnerPosition.y) + corridorMargin;

            return Mathf.Abs(pointerCanvasPosition.x - handFrontPosition.x) <= corridorHalfWidth
                && pointerCanvasPosition.y >= minimumY
                && pointerCanvasPosition.y <= maximumY;
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
            pointerRectTransform.anchoredPosition = anchoredPosition + new Vector2(0.0f, -58.0f);
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
            mHandCursorSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.HandPointerPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.88f, 0.64f, 0.72f, 1.0f));
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
            mHandPauseClip =
                await RuntimeAudioClipLoader.LoadClipAsync(MouthOfTruthAssetCatalog.HandPausePath);
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
            mSceneOverlayImage.color = new Color(0.03f, 0.02f, 0.02f, 0.0f);
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
            mHandImage.sprite = mHandCursorSprite;
            mHandImage.preserveAspect = true;
            mHandImage.raycastTarget = false;
            mPointerImage.sprite = mHandCursorSprite;
            mPointerImage.preserveAspect = true;
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
                new Vector2(250.0f, 320.0f));
            mQuestionText.fontSize = 30;
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
                new Vector2(220.0f, 300.0f));
            mQuestionText.fontSize = 30;
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
                new Vector2(280.0f, 380.0f));
            setRectTransformLayout(
                mTryAgainButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.225f),
                new Vector2(360.0f, 100.0f));
            setRectTransformLayout(
                mExitButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.145f),
                new Vector2(360.0f, 100.0f));
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
            if (mSceneOverlayImage == null)
            {
                return;
            }

            Color overlayColor = mSceneOverlayImage.color;
            overlayColor.a = Mathf.Clamp01(alpha);
            mSceneOverlayImage.color = overlayColor;
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
                new Vector2(0.0f, 220.0f),
                new Vector2(950.0f, 420.0f),
                Color.white);
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
                FontStyle.Normal);
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
                "Hand",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.22f),
                new Vector2(0.5f, 0.22f),
                new Vector2(0.0f, 0.0f),
                new Vector2(180.0f, 220.0f),
                Color.white);
            mPointerImage = createImage(
                "HandPointer",
                mCanvasRootTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(120.0f, 160.0f),
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

            mInterfaceAudioSource = gameObject.AddComponent<AudioSource>();
            mInterfaceAudioSource.loop = false;
            mInterfaceAudioSource.playOnAwake = false;
            mInterfaceAudioSource.volume = 0.85f;
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
                    playInterfaceCue(mButtonConfirmClip, 0.85f);
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

        private bool isScreenPointOverButton(Button button, Vector2 screenPosition)
        {
            return button != null
                && button.gameObject.activeInHierarchy
                && button.interactable
                && isScreenPointOverRectTransform(
                    button.GetComponent<RectTransform>(),
                    screenPosition);
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

        private float easeOut(float progress)
        {
            float inverse = 1.0f - progress;
            return 1.0f - (inverse * inverse * inverse);
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
            handRectTransform.localRotation =
                Quaternion.Euler(0.0f, 0.0f, Mathf.Lerp(-3.0f, 1.5f, easedProgress));
            handRectTransform.localScale = Vector3.one * Mathf.Lerp(0.98f, 0.74f, easedProgress);
            mHandImage.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(0.98f, 0.90f, easedProgress));
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
        }

        private void disableAnsweringPresentation()
        {
            mIsAnsweringPresentationActive = false;

            if (mQuestionPanelImage != null)
            {
                mQuestionPanelImage.color = Color.white;
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
                mMouthImage.color = Color.white;
                mMouthImage.rectTransform.localScale = Vector3.one;
            }

            if (mAnalyzingDotsText != null)
            {
                setObjectActive(mAnalyzingDotsText, false);
                setText(mAnalyzingDotsText, string.Empty);
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

            mInterfaceAudioSource.PlayOneShot(audioClip, volumeScale);
        }

        private void playVerdictCue(EVerdictKind verdictKind)
        {
            AudioClip verdictClip = verdictKind switch
            {
                EVerdictKind.True => mResultTrueClip,
                EVerdictKind.False => mResultFalseClip,
                _ => mResultUncertainClip,
            };

            playInterfaceCue(verdictClip, 0.95f);
        }

        private void addTextShadow(GameObject textObject)
        {
            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.05f, 0.03f, 0.02f, 0.92f);
            shadow.effectDistance = new Vector2(2.0f, -2.0f);
        }
    }
}
