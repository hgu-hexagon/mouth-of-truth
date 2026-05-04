using UnityEngine;

namespace MouthOfTruth.Game.Input.Leap
{
    public class LeapHandInputAdapter : IHandInteractionInputAdapter, IHandInteractionFallbackGate
    {
        private readonly LeapHandTrackingRuntime mLeapHandTrackingRuntime;

        public LeapHandInputAdapter()
        {
            mLeapHandTrackingRuntime = LeapHandTrackingRuntime.EnsureInstance();
        }

        public bool ShouldSuppressFallbackInput =>
            mLeapHandTrackingRuntime != null && mLeapHandTrackingRuntime.ShouldOwnPointerInput;

        public bool TryGetPointerScreenPosition(out Vector2 screenPosition)
        {
            if (mLeapHandTrackingRuntime == null)
            {
                screenPosition = default;
                return false;
            }

            return mLeapHandTrackingRuntime.TryGetPointerScreenPosition(out screenPosition);
        }

        public bool WasReturnToTitleTriggeredThisFrame()
        {
            return false;
        }
    }
}
