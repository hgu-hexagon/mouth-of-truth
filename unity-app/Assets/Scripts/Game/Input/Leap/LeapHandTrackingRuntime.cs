using System;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Leap;

namespace MouthOfTruth.Game.Input.Leap
{
    [DisallowMultipleComponent]
    public class LeapHandTrackingRuntime : MonoBehaviour
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private const string DEFAULT_SERVICE_IP = "127.0.0.1";
        private const string DEFAULT_SERVICE_PORT = "12345";
        private const string DEFAULT_SERVER_NAMESPACE = "Leap Service";
#else
        private const string DEFAULT_SERVER_NAMESPACE = "Leap Service";
#endif

        private const string RUNTIME_OBJECT_NAME = "LeapHandTrackingRuntime";
        private const float DEFAULT_POINTER_MIN_X = -0.18f;
        private const float DEFAULT_POINTER_MAX_X = 0.18f;
        private const float DEFAULT_POINTER_MIN_Y = 0.05f;
        private const float DEFAULT_POINTER_MAX_Y = 0.34f;
        private const float DEFAULT_POINTER_SMOOTHING = 18.0f;
        private const float DEFAULT_POINTER_LOSS_GRACE_SECONDS = 0.55f;

        [SerializeField]
        private string mServerNamespace = DEFAULT_SERVER_NAMESPACE;

        [SerializeField]
        private float mPointerMinX = DEFAULT_POINTER_MIN_X;

        [SerializeField]
        private float mPointerMaxX = DEFAULT_POINTER_MAX_X;

        [SerializeField]
        private float mPointerMinY = DEFAULT_POINTER_MIN_Y;

        [SerializeField]
        private float mPointerMaxY = DEFAULT_POINTER_MAX_Y;

        [SerializeField]
        private float mPointerSmoothing = DEFAULT_POINTER_SMOOTHING;

        [SerializeField]
        private float mPointerLossGraceSeconds = DEFAULT_POINTER_LOSS_GRACE_SECONDS;

        [SerializeField]
        private bool mShouldLogStateChanges = true;

        private LeapServiceProvider mLeapServiceProvider;
        private Vector2 mSmoothedPointerScreenPosition;
        private float mLastTrackedPointerRealtime = float.NegativeInfinity;
        private bool mHasTrackedPointer;
        private bool mPreviousServiceConnectedState;
        private bool mPreviousDeviceConnectedState;
        private bool mPreviousTrackedPointerState;

        public bool IsTrackingServiceConnected { get; private set; }

        public bool IsTrackingDeviceConnected { get; private set; }

        public bool ShouldOwnPointerInput =>
            IsTrackingDeviceConnected
            || mHasTrackedPointer
            || Time.realtimeSinceStartup - mLastTrackedPointerRealtime <= mPointerLossGraceSeconds;

        public string LastTrackingMessage { get; private set; } = "Leap hand tracking runtime is idle.";

        public static LeapHandTrackingRuntime EnsureInstance()
        {
            LeapHandTrackingRuntime existingRuntime = FindAnyObjectByType<LeapHandTrackingRuntime>();

            if (existingRuntime != null)
            {
                return existingRuntime;
            }

            GameObject runtimeObject = new GameObject(RUNTIME_OBJECT_NAME);
            DontDestroyOnLoad(runtimeObject);
            return runtimeObject.AddComponent<LeapHandTrackingRuntime>();
        }

        private void Awake()
        {
            ensureServiceProviderExists();
        }

        private void OnEnable()
        {
            ensureServiceProviderExists();
        }

        private void Update()
        {
            ensureServiceProviderExists();
            updateTrackingState();
        }

        public bool TryGetPointerScreenPosition(out Vector2 screenPosition)
        {
            if (mHasTrackedPointer == false)
            {
                screenPosition = default;
                return false;
            }

            screenPosition = mSmoothedPointerScreenPosition;
            return true;
        }

        private void ensureServiceProviderExists()
        {
            if (mLeapServiceProvider != null)
            {
                return;
            }

            mLeapServiceProvider = GetComponent<LeapServiceProvider>();

            if (mLeapServiceProvider == null)
            {
                mLeapServiceProvider = gameObject.AddComponent<LeapServiceProvider>();
            }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            mLeapServiceProvider.SetTargetServiceIPPortToConnectTo(DEFAULT_SERVICE_IP, DEFAULT_SERVICE_PORT);
#else
            mLeapServiceProvider.SetTargetServerNamespaceToConnectTo(mServerNamespace);
#endif
            mLeapServiceProvider.enabled = true;
            LastTrackingMessage = $"Leap tracking service provider initialized for '{mServerNamespace}'.";
        }

        private void updateTrackingState()
        {
            Controller leapController = mLeapServiceProvider != null
                ? mLeapServiceProvider.GetLeapController()
                : null;
            Frame currentFrame = mLeapServiceProvider != null
                ? mLeapServiceProvider.CurrentFrame
                : null;
            Device currentDevice = mLeapServiceProvider != null
                ? mLeapServiceProvider.CurrentDevice
                : null;

            IsTrackingServiceConnected = leapController != null && leapController.IsServiceConnected;
            IsTrackingDeviceConnected = currentDevice != null || (leapController != null && leapController.Devices != null && leapController.Devices.ActiveDevices.Any());

            Hand primaryHand = selectPrimaryHandOrNull(currentFrame);

            if (primaryHand == null)
            {
                if (Time.realtimeSinceStartup - mLastTrackedPointerRealtime > mPointerLossGraceSeconds)
                {
                    mHasTrackedPointer = false;
                    LastTrackingMessage = IsTrackingDeviceConnected
                        ? "Leap device is connected, but no hand is currently tracked."
                        : "Leap device is not currently connected.";
                }

                logTrackingStateChangesIfNeeded();
                return;
            }

            Vector2 targetPointerScreenPosition = mapPrimaryHandToScreen(primaryHand);

            if (mHasTrackedPointer == false)
            {
                mSmoothedPointerScreenPosition = targetPointerScreenPosition;
            }
            else
            {
                float smoothingFactor = 1.0f - Mathf.Exp(-mPointerSmoothing * Time.unscaledDeltaTime);
                mSmoothedPointerScreenPosition = Vector2.Lerp(mSmoothedPointerScreenPosition, targetPointerScreenPosition, smoothingFactor);
            }

            mHasTrackedPointer = true;
            mLastTrackedPointerRealtime = Time.realtimeSinceStartup;
            LastTrackingMessage = $"Leap hand tracked at {mSmoothedPointerScreenPosition}.";
            logTrackingStateChangesIfNeeded();
        }

        private Hand selectPrimaryHandOrNull(Frame currentFrame)
        {
            if (currentFrame == null || currentFrame.Hands == null || currentFrame.Hands.Count == 0)
            {
                return null;
            }

            return currentFrame.Hands
                .OrderByDescending(hand => hand.Confidence)
                .ThenByDescending(hand => hand.TimeVisible)
                .FirstOrDefault();
        }

        private Vector2 mapPrimaryHandToScreen(Hand hand)
        {
            Vector3 pointerPosition = hand.Index.IsExtended
                ? hand.Index.TipPosition
                : hand.StabilizedPalmPosition;
            float normalizedX = Mathf.InverseLerp(mPointerMinX, mPointerMaxX, pointerPosition.x);
            float normalizedY = Mathf.InverseLerp(mPointerMinY, mPointerMaxY, pointerPosition.y);

            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);

            return new Vector2(normalizedX * Screen.width, normalizedY * Screen.height);
        }

        private void logTrackingStateChangesIfNeeded()
        {
            if (mShouldLogStateChanges == false)
            {
                return;
            }

            if (mPreviousServiceConnectedState != IsTrackingServiceConnected)
            {
                Debug.Log($"LeapHandTrackingRuntime: tracking service connected = {IsTrackingServiceConnected}.");
                mPreviousServiceConnectedState = IsTrackingServiceConnected;
            }

            if (mPreviousDeviceConnectedState != IsTrackingDeviceConnected)
            {
                Debug.Log($"LeapHandTrackingRuntime: tracking device connected = {IsTrackingDeviceConnected}.");
                mPreviousDeviceConnectedState = IsTrackingDeviceConnected;
            }

            if (mPreviousTrackedPointerState != mHasTrackedPointer)
            {
                Debug.Log($"LeapHandTrackingRuntime: pointer tracked = {mHasTrackedPointer}.");
                mPreviousTrackedPointerState = mHasTrackedPointer;
            }
        }
    }
}
