/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2026.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

#if XR_MANAGEMENT_AVAILABLE
using UnityEngine.XR.Management;
#endif

namespace Leap
{

    /// <summary>
    /// Wraps various (but not all) "XR" calls with Unity 5.6-supporting "VR" calls
  	/// via #ifdefs.
  	/// </summary>
    public static class XRSupportUtil
    {

        private static System.Collections.Generic.List<XRNodeState> nodeStates =
          new System.Collections.Generic.List<XRNodeState>();
        static List<Vector3> _boundaryPoints = new List<Vector3>();
        static List<XRDisplaySubsystem> _displaySubsystems = new List<XRDisplaySubsystem>();

        public static bool IsXREnabled()
        {
#if XR_MANAGEMENT_AVAILABLE
            return XRGeneralSettings.Instance.Manager != null;
#else
            SubsystemManager.GetSubsystems(_displaySubsystems);
            return _displaySubsystems.Count > 0;
#endif
        }

        public static bool IsXRDevicePresent()
        {
#if XR_MANAGEMENT_AVAILABLE
            return XRGeneralSettings.Instance.Manager.activeLoader != null;
#else
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
            return devices.Count > 0;
#endif
        }

        static bool outputPresenceWarning = false;
        public static bool IsUserPresent(bool defaultPresence = true)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);

            if (devices.Count == 0 && !outputPresenceWarning)
            {
                Debug.LogWarning("No head-mounted devices found. Possibly no HMD is available to the XR system.");
                outputPresenceWarning = true;
            }

            if (devices.Count != 0)
            {
                var device = devices[0];
                if (device.TryGetFeatureValue(CommonUsages.userPresence, out var userPresent))
                {
                    return userPresent;
                }
            }
            return defaultPresence;
        }

        public static Vector3 GetXRNodeCenterEyeLocalPosition()
        {
            InputTracking.GetNodeStates(nodeStates);
            Vector3 position;

            foreach (XRNodeState state in nodeStates)
            {
                if (state.nodeType == XRNode.CenterEye &&
                    state.TryGetPosition(out position))
                {
                    return position;
                }
            }

            return Vector3.zero;
        }

        public static Quaternion GetXRNodeCenterEyeLocalRotation()
        {
            InputTracking.GetNodeStates(nodeStates);
            Quaternion rotation;

            foreach (XRNodeState state in nodeStates)
            {
                if (state.nodeType == XRNode.CenterEye &&
                    state.TryGetRotation(out rotation))
                {
                    return rotation;
                }
            }

            return Quaternion.identity;
        }

        public static Vector3 GetXRNodeHeadLocalPosition()
        {
            InputTracking.GetNodeStates(nodeStates);
            Vector3 position;

            foreach (XRNodeState state in nodeStates)
            {
                if (state.nodeType == XRNode.Head &&
                   state.TryGetPosition(out position))
                {
                    return position;
                }
            }

            return Vector3.zero;
        }

        public static Quaternion GetXRNodeHeadLocalRotation()
        {
            InputTracking.GetNodeStates(nodeStates);
            Quaternion rotation;

            foreach (XRNodeState state in nodeStates)
            {
                if (state.nodeType == XRNode.Head &&
                    state.TryGetRotation(out rotation))
                {
                    return rotation;
                }
            }

            return Quaternion.identity;
        }

        public static Vector3 GetXRNodeLocalPosition(int node)
        {
            InputTracking.GetNodeStates(nodeStates);
            Vector3 position;

            foreach (XRNodeState state in nodeStates)
            {
                if (state.nodeType == (XRNode)node &&
                   state.TryGetPosition(out position))
                {
                    return position;
                }
            }

            return Vector3.zero;
        }

        public static Quaternion GetXRNodeLocalRotation(int node)
        {
            InputTracking.GetNodeStates(nodeStates);
            Quaternion rotation;

            foreach (XRNodeState state in nodeStates)
            {
                if (state.nodeType == (XRNode)node &&
                    state.TryGetRotation(out rotation))
                {
                    return rotation;
                }
            }
            return Quaternion.identity;
        }

        public static void Recenter()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
            if (devices.Count == 0)
                return;

            var hmdDevice = devices[0];
            hmdDevice.subsystem.TryRecenter();
#pragma warning disable CS0618 // Type or member is obsolete
            InputTracking.Recenter();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public static float GetGPUTime()
        {
            float gpuTime = 0f;

            SubsystemManager.GetSubsystems(_displaySubsystems);

            foreach (XRDisplaySubsystem displaySubsystem in _displaySubsystems)
            {
                if (displaySubsystem == null || !displaySubsystem.running)
                {
                    continue;
                }

                if (displaySubsystem.TryGetAppGPUTimeLastFrame(out gpuTime))
                {
                    return gpuTime;
                }
            }

            return gpuTime;
        }

        public static string GetLoadedDeviceName()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);

            if (devices.Count == 0)
            {
                return string.Empty;
            }

            return devices[0].name ?? string.Empty;
        }

        /// <summary> Returns whether there's a floor available. </summary>
        public static bool IsRoomScale()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);

            if (devices.Count == 0)
            {
                return false;
            }

            var hmdDevice = devices[0];
            return hmdDevice.subsystem.GetTrackingOriginMode().HasFlag(TrackingOriginModeFlags.Floor);
        }

        /// <summary> Returns whether the playspace is larger than 1m on its shortest side. </summary>
        public static bool IsLargePlayspace()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);

            if (devices.Count == 0)
            {
                return false;
            }

            var hmdDevice = devices[0];
            hmdDevice.subsystem.TryGetBoundaryPoints(_boundaryPoints);
            Bounds playspaceSize = new Bounds();
            foreach (Vector3 boundaryPoint in _boundaryPoints)
            {
                playspaceSize.Encapsulate(boundaryPoint);
            }

            return playspaceSize.size.magnitude > 1f; // Playspace is greater than 1m on its shortest axis
        }
    }
}
