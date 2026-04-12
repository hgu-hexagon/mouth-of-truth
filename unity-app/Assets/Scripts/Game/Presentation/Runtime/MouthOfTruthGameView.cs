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

        private readonly Dictionary<EQuestionCardSlot, QuestionCardView> mCardViews =
            new Dictionary<EQuestionCardSlot, QuestionCardView>();

        private Canvas mCanvas;
        private RectTransform mCanvasRootRectTransform;
        private Transform mCanvasRootTransform;
        private Image mBackgroundImage;
        private Image mCarpetImage;
        private Image mTitleVignetteImage;
        private Image mLogoImage;
        private Image mQuestionPanelImage;
        private Image mStatusPanelImage;
        private Image mResultPanelImage;
        private Text mPromptText;
        private Text mQuestionText;
        private Text mStatusText;
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
        private Sprite mCardBackSprite;
        private Sprite mButtonFrameSprite;
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
        private EQuestionCardSlot? mLastAudibleHoveredCardSlot;
        private Camera mWorldCamera;
        private CardPresentationAnchorSet mCardPresentationAnchorSet;
        private MouthAnchorSet mMouthAnchorSet;
        private bool mUseWorldEnvironmentLayout;
        private EUiActionTarget? mLastHoveredUiActionTarget;

        private bool mStartRequested;
        private bool mTryAgainRequested;
        private bool mBackToTitleRequested;

        public async Task InitializeAsync()
        {
            ensureEventSystemExists();
            buildCanvas();
            cacheWorldPresentationReferences();
            buildAudioSources();
            await loadSpritesAsync();
            await loadAudioClipsAsync();
            applyTheme();
            refreshWorldPresentationLayout();
            ShowStartScreen();
        }

        public void ShowStartScreen()
        {
            setObjectActive(mLogoImage, true);
            setObjectActive(mTitleVignetteImage, true);
            setObjectActive(mStartButton, true);
            setObjectActive(mBackgroundImage, mUseWorldEnvironmentLayout == false);
            setObjectActive(mCarpetImage, mUseWorldEnvironmentLayout == false);
            setObjectActive(mQuestionText, false);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mStatusPanelImage, true);
            setObjectActive(mResultPanelImage, false);
            setObjectActive(mStatusText, true);
            setObjectActive(mAnswerInputField, false);
            setObjectActive(mMouthImage, false);
            setObjectActive(mHandImage, false);
            setObjectActive(mPointerImage, false);
            setObjectActive(mVerdictImage, false);
            setObjectActive(mVerdictText, false);
            setObjectActive(mTryAgainButton, false);
            setObjectActive(mBackToTitleButton, false);
            setCardsVisible(false);
            mPromptText.text = "START GAME";
            mStatusText.text = "손으로 START GAME을 선택하거나 마우스로 클릭하세요.";
            mAnswerTimerText.text = string.Empty;
            mLastAudibleHoveredCardSlot = null;
            mLastHoveredUiActionTarget = null;
            refreshWorldPresentationLayout();
            ensureAmbiencePlayback();
        }

        public void ShowCardSelection(QuestionRoundSelection questionRoundSelection)
        {
            setObjectActive(mBackgroundImage, mUseWorldEnvironmentLayout == false);
            setObjectActive(mCarpetImage, mUseWorldEnvironmentLayout == false);
            setObjectActive(mLogoImage, false);
            setObjectActive(mTitleVignetteImage, false);
            setObjectActive(mStartButton, false);
            setObjectActive(mQuestionText, false);
            setObjectActive(mQuestionPanelImage, false);
            setObjectActive(mStatusPanelImage, true);
            setObjectActive(mResultPanelImage, false);
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
            mPromptText.text = "질문 카드를 선택하세요";
            mStatusText.text = "카드 위에 포인터를 올리고 0.7초 유지하면 선택됩니다.";
            mAnswerTimerText.text = "시연 시간 15초";
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

        public async Task PlayQuestionRevealAsync(
            EQuestionCardSlot selectedQuestionCardSlot,
            QuestionDefinition questionDefinition)
        {
            mPromptText.text = "진실의 입이 질문을 받아들이고 있습니다.";
            mStatusText.text = "선택된 카드가 중앙으로 이동합니다.";
            playInterfaceCue(mCardSelectClip, 0.90f);

            foreach (KeyValuePair<EQuestionCardSlot, QuestionCardView> pair in mCardViews)
            {
                bool isSelected = pair.Key == selectedQuestionCardSlot;
                pair.Value.SetVisualState(isDimmed: isSelected == false, isSelected, 0.0f);
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

            selectedCardView.SetFront(questionDefinition.Text);
            mQuestionText.text = questionDefinition.Text;
            setObjectActive(mQuestionPanelImage, true);
            setObjectActive(mQuestionText, true);
            playInterfaceCue(mCardRevealClip, 0.85f);
        }

        public void ShowNarratingQuestion(string questionText)
        {
            setObjectActive(mQuestionPanelImage, true);
            setObjectActive(mQuestionText, true);
            setObjectActive(mMouthImage, true);
            setObjectActive(mHandImage, false);
            setObjectActive(mPointerImage, false);
            mQuestionText.text = questionText;
            mPromptText.text = "질문 낭독 중";
            mStatusText.text = "진실의 입이 질문을 읽고 있습니다.";
            applyMouthAnchoredLayout();
        }

        public void ShowAwaitingHandInsertion()
        {
            setObjectActive(mMouthImage, true);
            setObjectActive(mHandImage, true);
            setObjectActive(mPointerImage, false);
            setObjectActive(mAnswerInputField, true);
            setObjectActive(mAnswerTimerText, true);
            setObjectActive(mQuestionPanelImage, true);
            setObjectActive(mStatusPanelImage, true);
            setObjectActive(mResultPanelImage, false);
            mAnswerInputField.text = string.Empty;
            mAnswerInputField.interactable = false;
            mPromptText.text = "손을 입 안에 넣으세요";
            mStatusText.text = "손이나 포인터를 입 쪽으로 가져가면 자동으로 들어가고 답변이 시작됩니다.";
            applyMouthAnchoredLayout();
            setHandVisual(0.0f);
        }

        public async Task AnimateHandInsertionAsync()
        {
            setObjectActive(mHandImage, true);
            playInterfaceCue(mHandInsertClip, 0.9f);

            await animateOverTimeAsync(
                0.45f,
                progress => setHandVisual(Mathf.Lerp(0.0f, 1.0f, easeOut(progress))));
        }

        public async Task AnimateHandRemovalAsync()
        {
            playInterfaceCue(mHandPauseClip, 0.9f);
            await animateOverTimeAsync(
                0.35f,
                progress => setHandVisual(Mathf.Lerp(1.0f, 0.0f, easeOut(progress))));
        }

        public void ShowAnswering()
        {
            mPromptText.text = "답변 중";
            mStatusText.text = "답변을 진행하세요. 손이 입 밖으로 나오면 일시정지됩니다.";
            setObjectActive(mPointerImage, false);
            applyMouthAnchoredLayout();
        }

        public void ShowAnswerPaused()
        {
            mAnswerInputField.interactable = false;
            mPromptText.text = "답변 일시정지";
            mStatusText.text = "손이나 포인터를 다시 입 쪽으로 가져가면 답변이 이어집니다.";
            setObjectActive(mPointerImage, false);
            applyMouthAnchoredLayout();
        }

        public void ShowAnalyzing()
        {
            mAnswerInputField.interactable = false;
            mPromptText.text = "분석 중";
            mStatusText.text = "진실의 입이 답변을 분석하고 있습니다.";
            setObjectActive(mPointerImage, false);
            applyMouthAnchoredLayout();
        }

        public void ShowResult(EVerdictKind verdictKind, string transcriptText)
        {
            setCardsVisible(false);
            setObjectActive(mBackgroundImage, mUseWorldEnvironmentLayout == false);
            setObjectActive(mCarpetImage, mUseWorldEnvironmentLayout == false);
            setObjectActive(mTitleVignetteImage, false);
            setObjectActive(mQuestionText, true);
            setObjectActive(mQuestionPanelImage, true);
            setObjectActive(mStatusPanelImage, true);
            setObjectActive(mMouthImage, true);
            setObjectActive(mHandImage, true);
            setObjectActive(mPointerImage, false);
            setObjectActive(mVerdictImage, true);
            setObjectActive(mVerdictText, true);
            setObjectActive(mResultPanelImage, true);
            setObjectActive(mTryAgainButton, true);
            setObjectActive(mBackToTitleButton, true);
            setObjectActive(mAnswerInputField, false);
            mAnswerInputField.interactable = false;

            mVerdictImage.sprite = verdictKind switch
            {
                EVerdictKind.True => mVerdictTrueSprite,
                EVerdictKind.False => mVerdictFalseSprite,
                _ => mVerdictUncertainSprite,
            };
            mVerdictText.text = verdictKind switch
            {
                EVerdictKind.True => "TRUE",
                EVerdictKind.False => "FALSE",
                _ => "UNCERTAIN",
            };
            mPromptText.text = "결과";
            mStatusText.text = verdictKind switch
            {
                EVerdictKind.True => "진실의 입이 답변을 진실로 판정했습니다.",
                EVerdictKind.False => "진실의 입이 답변을 거짓으로 판정했습니다.",
                _ => "진실의 입이 답변을 확정적으로 판정하지 못했습니다.",
            };
            applyMouthAnchoredLayout();
            playVerdictCue(verdictKind);
        }

        public void UpdateAnswerMetrics(float elapsedAnswerSeconds, float elapsedSilenceSeconds)
        {
            mAnswerTimerText.text =
                $"답변 {elapsedAnswerSeconds:0.0}s / 무음 {elapsedSilenceSeconds:0.0}s";
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

            float mouthDiameterPixels = Mathf.Max(
                1.0f,
                Mathf.Min(mMouthImage.rectTransform.rect.width, mMouthImage.rectTransform.rect.height));
            float frontAnchorRadiusPixels = mouthDiameterPixels * 0.34f;
            float innerAnchorRadiusPixels = mouthDiameterPixels * 0.18f;
            Vector2 screenPosition = pointerScreenPosition.Value;
            float distanceToInnerAnchor = Vector2.Distance(screenPosition, getHandInnerPosition());

            if (distanceToInnerAnchor <= innerAnchorRadiusPixels)
            {
                return EHandAnchorState.AtInnerAnchor;
            }

            float distanceToFrontAnchor = Vector2.Distance(screenPosition, getHandFrontPosition());

            if (distanceToFrontAnchor <= frontAnchorRadiusPixels
                || distanceToInnerAnchor <= frontAnchorRadiusPixels)
            {
                return EHandAnchorState.AtFrontAnchor;
            }

            return EHandAnchorState.OutsideMouth;
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

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    mCanvasRootRectTransform,
                    pointerScreenPosition.Value,
                    null,
                    out Vector2 anchoredPosition) == false)
            {
                setObjectActive(mPointerImage, false);
                return;
            }

            setObjectActive(mPointerImage, true);
            RectTransform pointerRectTransform = mPointerImage.rectTransform;
            pointerRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            pointerRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
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
        }

        public void ClearAnswerTranscript()
        {
            SetAnswerTranscriptText(string.Empty);
        }

        public void SetAnswerTranscriptPlaceholder(string placeholderText)
        {
            if (mAnswerInputField?.placeholder is Text placeholderLabel)
            {
                placeholderLabel.text = placeholderText ?? string.Empty;
            }
        }

        public void SetAnswerTranscriptEditable(bool isEditable)
        {
            if (mAnswerInputField == null)
            {
                return;
            }

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

        private async Task loadSpritesAsync()
        {
            mCardBackSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.QuestionCardBackPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.43f, 0.63f, 0.95f, 1.0f));
            mButtonFrameSprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.PrimaryButtonFramePath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.38f, 0.21f, 0.11f, 1.0f));
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

            mBackgroundImage.sprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.TitleBackgroundPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.12f, 0.09f, 0.07f, 1.0f));
            mCarpetImage.sprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.FloorRunnerPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.44f, 0.03f, 0.05f, 1.0f));
            mLogoImage.sprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.TitleLogoPath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.82f, 0.71f, 0.52f, 1.0f));
            mMouthImage.sprite =
                await RuntimeSpriteLoader.LoadSpriteAsync(MouthOfTruthAssetCatalog.TruthMouthFacePath)
                ?? RuntimeSpriteLoader.CreateSolidSprite(new Color(0.85f, 0.83f, 0.78f, 1.0f));
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
            mCarpetImage.preserveAspect = true;
            mTitleVignetteImage.sprite = mTitleVignetteSprite;
            mTitleVignetteImage.type = Image.Type.Sliced;
            mLogoImage.preserveAspect = true;
            mMouthImage.preserveAspect = true;
            mHandImage.sprite = mHandCursorSprite;
            mHandImage.preserveAspect = true;
            mPointerImage.sprite = mHandCursorSprite;
            mPointerImage.preserveAspect = true;
            mVerdictImage.preserveAspect = true;
            mQuestionPanelImage.sprite = mQuestionPanelSprite;
            mQuestionPanelImage.type = Image.Type.Sliced;
            mStatusPanelImage.sprite = mStatusPanelSprite;
            mStatusPanelImage.type = Image.Type.Sliced;
            mResultPanelImage.sprite = mResultPanelSprite;
            mResultPanelImage.type = Image.Type.Sliced;

            mStartButton.image.sprite = mButtonFrameSprite;
            mTryAgainButton.image.sprite = mButtonFrameSprite;
            mBackToTitleButton.image.sprite = mButtonFrameSprite;

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

            mTitleVignetteImage.transform.SetSiblingIndex(mLogoImage.transform.GetSiblingIndex());
            mQuestionPanelImage.transform.SetSiblingIndex(mQuestionText.transform.GetSiblingIndex());
            mStatusPanelImage.transform.SetSiblingIndex(mPromptText.transform.GetSiblingIndex());
            mResultPanelImage.transform.SetSiblingIndex(mVerdictImage.transform.GetSiblingIndex());

            createCardView(EQuestionCardSlot.LeftCard, FALLBACK_LEFT_CARD_POSITION);
            createCardView(EQuestionCardSlot.CenterCard, FALLBACK_CENTER_CARD_POSITION);
            createCardView(EQuestionCardSlot.RightCard, FALLBACK_RIGHT_CARD_POSITION);
            mPointerImage.transform.SetAsLastSibling();
            mPointerImage.raycastTarget = false;
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
            questionCardView.Initialize(questionCardSlot, mCanvasRootTransform, mCardBackSprite);
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.94f, 0.90f, 0.82f, 1.0f);
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
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
            backgroundImage.color = new Color(0.07f, 0.05f, 0.04f, 0.78f);

            InputField inputField = inputFieldObject.AddComponent<InputField>();

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
            placeholderText.text = "입력된 답변이 이 영역에 표시됩니다.";

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
            buttonImage.color = new Color(0.42f, 0.18f, 0.10f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
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
                24,
                FontStyle.Bold);
            label.text = labelText;
            return button;
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
                    new Color(0.42f, 0.18f, 0.10f, 0.95f),
                    new Color(0.74f, 0.54f, 0.26f, 1.0f),
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

        private float easeOut(float progress)
        {
            float inverse = 1.0f - progress;
            return 1.0f - (inverse * inverse * inverse);
        }

        private void setHandVisual(float insertionProgress)
        {
            RectTransform handRectTransform = mHandImage.rectTransform;
            handRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            handRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            handRectTransform.anchoredPosition = Vector2.Lerp(
                getHandFrontPosition(),
                getHandInnerPosition(),
                insertionProgress);
            handRectTransform.localScale = Vector3.one * Mathf.Lerp(1.0f, 0.80f, insertionProgress);
            mHandImage.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(0.92f, 0.78f, insertionProgress));
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
    }
}
