using MouthOfTruth.Game.Input;
using MouthOfTruth.Game.Session;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MouthOfTruth.Game.App
{
    public partial class MouthOfTruthAppController
    {
        private const float POINTER_REACQUIRE_GUARD_SECONDS = 0.45f;
        private const float POST_CARD_SELECTION_POINTER_SETTLE_SECONDS = 0.0f;
        private const float HAND_PROMPT_DISMISS_MOVEMENT_MIN_PIXELS = 28.0f;
        private const float HAND_PROMPT_DISMISS_MOVEMENT_SCREEN_HEIGHT_FACTOR = 0.018f;

        private bool mWasPointerAvailableLastFrame;
        private float mPointerReacquireGuardRemainingSeconds;
        private float mPointerPresentationOverrideRemainingSeconds;
        private Vector2? mPresentedPointerScreenPositionOrNull;
        private Vector2? mHandPromptDismissalBaselineScreenPositionOrNull;
        private Vector2 mPointerPresentationRebaseInputScreenPosition;
        private Vector2 mPointerPresentationRebaseOutputScreenPosition;
        private EHandAnchorState mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;
        private bool mHasDismissedHandPromptByMovement;
        private bool mIsPointerPresentationRebaseActive;
        private bool mShouldRebasePointerAfterPresentationOverride;

        private Vector2? tryGetPointerScreenPositionOrNull()
        {
            if (mHandInteractionInputAdapter == null)
            {
                return null;
            }

            Vector2 screenPosition;
            if (mHandInteractionInputAdapter.TryGetPointerScreenPosition(out screenPosition))
            {
                return screenPosition;
            }

            return null;
        }

        private void updatePointerPresentation(Vector2? pointerScreenPositionOrNull)
        {
            bool isCinematicTransition = mIsTransitionBusy
                && (mGameStateMachine.CurrentState == EGameFlowState.InsertingHand || mGameStateMachine.CurrentState == EGameFlowState.ShowingResult);
            bool shouldShowPointer = pointerScreenPositionOrNull.HasValue
                && isCinematicTransition == false
                && (mGameStateMachine.CurrentState == EGameFlowState.StartScreen || mGameView.IsFirstRunTutorialVisible || mGameStateMachine.CurrentState == EGameFlowState.AwaitingCardSelection || mGameStateMachine.CurrentState == EGameFlowState.ShowingResult || mGameStateMachine.CurrentState == EGameFlowState.AwaitingHandInsertion || mGameStateMachine.CurrentState == EGameFlowState.AnswerPaused || mGameStateMachine.CurrentState == EGameFlowState.Answering);

            mGameView.UpdatePointerVisual(shouldShowPointer, pointerScreenPositionOrNull);
        }

        private bool updatePointerActivationGuard(Vector2? pointerScreenPositionOrNull)
        {
            if (pointerScreenPositionOrNull.HasValue == false)
            {
                mWasPointerAvailableLastFrame = false;
                mPointerReacquireGuardRemainingSeconds = 0.0f;
                resetPointerActivationDwellState();
                return false;
            }

            if (mWasPointerAvailableLastFrame == false)
            {
                mPointerReacquireGuardRemainingSeconds = POINTER_REACQUIRE_GUARD_SECONDS;
                resetPointerActivationDwellState();
            }

            mWasPointerAvailableLastFrame = true;

            if (mPointerReacquireGuardRemainingSeconds <= 0.0f)
            {
                return true;
            }

            mPointerReacquireGuardRemainingSeconds = Mathf.Max(0.0f, mPointerReacquireGuardRemainingSeconds - Time.deltaTime);
            resetPointerActivationDwellState();
            return false;
        }

        private void resetPointerActivationDwellState()
        {
            mUiActionDwellSelectionTracker?.Reset();

            if (mGameStateMachine?.CurrentState == EGameFlowState.AwaitingCardSelection)
            {
                mGameStateMachine.ResetCardSelectionHover();
            }

            mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;
            mGameView?.UpdateCardHoverVisual(null, 0.0f);
            mGameView?.UpdateActionButtonHoverVisual(null, 0.0f);
        }

        private static void applyRuntimeCursorPresentation(bool isFocused)
        {
            Cursor.visible = isFocused == false;
            Cursor.lockState = CursorLockMode.None;
        }

        private static void restoreSystemCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void updateHandPromptDismissal(Vector2? pointerScreenPositionOrNull)
        {
            if (mPointerPresentationOverrideRemainingSeconds > 0.0f || pointerScreenPositionOrNull.HasValue == false)
            {
                mHandPromptDismissalBaselineScreenPositionOrNull = null;
                return;
            }

            if (mHasDismissedHandPromptByMovement)
            {
                return;
            }

            if (mHandPromptDismissalBaselineScreenPositionOrNull.HasValue == false)
            {
                mHandPromptDismissalBaselineScreenPositionOrNull = pointerScreenPositionOrNull.Value;
                return;
            }

            float dismissalMovementThresholdPixels = Mathf.Max(HAND_PROMPT_DISMISS_MOVEMENT_MIN_PIXELS, Screen.height * HAND_PROMPT_DISMISS_MOVEMENT_SCREEN_HEIGHT_FACTOR);
            float pointerMovementPixels = Vector2.Distance(mHandPromptDismissalBaselineScreenPositionOrNull.Value, pointerScreenPositionOrNull.Value);

            if (pointerMovementPixels < dismissalMovementThresholdPixels)
            {
                return;
            }

            mHasDismissedHandPromptByMovement = true;
            mGameView.BeginHandPromptDismissal();
        }

        private void resetHandPromptDismissalTracking()
        {
            mHandPromptDismissalBaselineScreenPositionOrNull = null;
            mHasDismissedHandPromptByMovement = false;
        }

        private void resetInteractionSelectionState()
        {
            mUiActionDwellSelectionTracker?.Reset();
            mLastObservedHandAnchorState = EHandAnchorState.OutsideMouth;
            mWasPointerAvailableLastFrame = false;
            mPointerReacquireGuardRemainingSeconds = 0.0f;
            mPointerPresentationOverrideRemainingSeconds = 0.0f;
            mPresentedPointerScreenPositionOrNull = null;
            resetPointerPresentationRebase();
            resetHandPromptDismissalTracking();
            mGameStateMachine?.ResetCardSelectionHover();
            mGameView.UpdateActionButtonHoverVisual(null, 0.0f);
        }

        private Vector2? getPresentedPointerScreenPositionOrNull(Vector2? pointerScreenPositionOrNull)
        {
            if (mPointerPresentationOverrideRemainingSeconds > 0.0f)
            {
                mPointerPresentationOverrideRemainingSeconds = Mathf.Max(0.0f, mPointerPresentationOverrideRemainingSeconds - Time.deltaTime);
                mPresentedPointerScreenPositionOrNull = getBottomCenterPointerScreenPosition();
                warpSystemPointerToBottomCenter();

                if (mPointerPresentationOverrideRemainingSeconds <= 0.0f)
                {
                    mShouldRebasePointerAfterPresentationOverride = true;
                    mIsPointerPresentationRebaseActive = false;
                }

                return mPresentedPointerScreenPositionOrNull;
            }

            if (pointerScreenPositionOrNull.HasValue == false)
            {
                mPresentedPointerScreenPositionOrNull = null;
                if (mIsPointerPresentationRebaseActive)
                {
                    mIsPointerPresentationRebaseActive = false;
                    mShouldRebasePointerAfterPresentationOverride = true;
                }

                return null;
            }

            if (mShouldRebasePointerAfterPresentationOverride)
            {
                beginPointerPresentationRebase(pointerScreenPositionOrNull.Value, getBottomCenterPointerScreenPosition());
            }

            mPresentedPointerScreenPositionOrNull = mIsPointerPresentationRebaseActive ? getRebasedPointerScreenPosition(pointerScreenPositionOrNull.Value) : pointerScreenPositionOrNull.Value;
            return mPresentedPointerScreenPositionOrNull;
        }

        private void beginBottomCenterPointerSettle(float durationSeconds = POST_CARD_SELECTION_POINTER_SETTLE_SECONDS)
        {
            mPointerPresentationOverrideRemainingSeconds = Mathf.Max(0.0f, durationSeconds);
            mPresentedPointerScreenPositionOrNull = getBottomCenterPointerScreenPosition();
            mShouldRebasePointerAfterPresentationOverride = mPointerPresentationOverrideRemainingSeconds > 0.0f;
            mIsPointerPresentationRebaseActive = false;
            warpSystemPointerToBottomCenter();
            mWasPointerAvailableLastFrame = false;
            mPointerReacquireGuardRemainingSeconds = 0.0f;
            resetPointerActivationDwellState();
        }

        private void beginPointerPresentationRebase(Vector2 inputScreenPosition, Vector2 outputScreenPosition)
        {
            mPointerPresentationRebaseInputScreenPosition = inputScreenPosition;
            mPointerPresentationRebaseOutputScreenPosition = outputScreenPosition;
            mShouldRebasePointerAfterPresentationOverride = false;
            mIsPointerPresentationRebaseActive = true;
        }

        private Vector2 getRebasedPointerScreenPosition(Vector2 inputScreenPosition)
        {
            Vector2 rebasedPointerScreenPosition = mPointerPresentationRebaseOutputScreenPosition + inputScreenPosition - mPointerPresentationRebaseInputScreenPosition;
            return new Vector2(Mathf.Clamp(rebasedPointerScreenPosition.x, 0.0f, Screen.width), Mathf.Clamp(rebasedPointerScreenPosition.y, 0.0f, Screen.height));
        }

        private void resetPointerPresentationRebase()
        {
            mIsPointerPresentationRebaseActive = false;
            mShouldRebasePointerAfterPresentationOverride = false;
            mPointerPresentationRebaseInputScreenPosition = Vector2.zero;
            mPointerPresentationRebaseOutputScreenPosition = Vector2.zero;
        }

        private static Vector2 getBottomCenterPointerScreenPosition()
        {
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.08f);
        }

        private static void warpSystemPointerToBottomCenter()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse currentMouse = Mouse.current;

            if (currentMouse != null)
            {
                currentMouse.WarpCursorPosition(getBottomCenterPointerScreenPosition());
            }
#endif
        }
    }
}
