using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
#if INCLUDE_INPUT_SYSTEM
using UnityEngine.InputSystem.XR;
#endif
#if INCLUDE_LEGACY_INPUT_HELPERS
using UnityEngine.SpatialTracking;
#endif

using Unity.XR.CoreUtils;

namespace LAI.XR
{
    [AddComponentMenu("LAI/XR/XR Origin")]
    [DisallowMultipleComponent]
    public class XROrigin : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The Camera to associate with the XR device.")]
        Camera m_Camera;

        public Camera Camera
        {
            get => m_Camera;
            set => m_Camera = value;
        }

        // The parent <c>Transform</c> for all "trackables" (for example, planes and feature points).
        public Transform TrackablesParent { get; private set; }

        public event Action<ARTrackablesParentTransformChangedEventArgs> TrackablesParentTransformChanged;

        public enum TrackingOriginMode
        {
            // Uses the default Tracking Origin Mode of the input device.
            // When changing to this value after startup, the Tracking Origin Mode will not be changed.
            NotSpecified,
            // Input devices will be tracked relative to the first known location.
            // Represents a device-relative tracking origin.
            Device,
            // Input devices will be tracked relative to a location on the floor.
            Floor,
        }

        // This is the average seated height, which is 44 inches.
        private const float defaultCameraYOffset = 1.1176f;

        public GameObject Origin;

        [SerializeField] private GameObject cameraFloorOffsetObject;

        public void SetCameraFloorOffsetObject(GameObject cameraFloorOffsetObject)
        {
            this.cameraFloorOffsetObject = cameraFloorOffsetObject;
            MoveOffsetHeight();
        }

        [SerializeField]
        TrackingOriginMode m_RequestedTrackingOriginMode = TrackingOriginMode.NotSpecified;

        public TrackingOriginMode RequestedTrackingOriginMode
        {
            get => m_RequestedTrackingOriginMode;
            set
            {
                m_RequestedTrackingOriginMode = value;
                TryInitializeCamera();
            }
        }

        [SerializeField]
        float m_CameraYOffset = defaultCameraYOffset;

        public float CameraYOffset
        {
            get => m_CameraYOffset;
            set
            {
                m_CameraYOffset = value;
                MoveOffsetHeight();
            }
        }

        public TrackingOriginModeFlags CurrentTrackingOriginMode { get; private set; }

        public Vector3 OriginInCameraSpacePos => m_Camera.transform.InverseTransformPoint(Origin.transform.position);

        public Vector3 CameraInOriginSpacePos => Origin.transform.InverseTransformPoint(m_Camera.transform.position);

        public float CameraInOriginSpaceHeight => CameraInOriginSpacePos.y;

        static readonly List<XRInputSubsystem> s_InputSubsystems = new List<XRInputSubsystem>();

        // Bookkeeping to track lazy initialization of the tracking origin mode type.
        bool m_CameraInitialized;
        bool m_CameraInitializing;

        void MoveOffsetHeight()
        {
            if (!Application.isPlaying)
                return;

            switch (CurrentTrackingOriginMode)
            {
                case TrackingOriginModeFlags.Floor:
                    MoveOffsetHeight(0f);
                    break;
                case TrackingOriginModeFlags.Device:
                    MoveOffsetHeight(m_CameraYOffset);
                    break;
                default:
                    return;
            }
        }

        void MoveOffsetHeight(float y)
        {
            if (cameraFloorOffsetObject != null)
            {
                var offsetTransform = cameraFloorOffsetObject.transform;
                var desiredPosition = offsetTransform.localPosition;
                desiredPosition.y = y;
                offsetTransform.localPosition = desiredPosition;
            }
        }

        void TryInitializeCamera()
        {
            if (!Application.isPlaying)
                return;

            m_CameraInitialized = SetupCamera();
            if (!m_CameraInitialized & !m_CameraInitializing)
                StartCoroutine(RepeatInitializeCamera());
        }

        bool SetupCamera()
        {
            var initialized = true;

            SubsystemManager.GetInstances(s_InputSubsystems);
            if (s_InputSubsystems.Count > 0)
            {
                foreach (var inputSubsystem in s_InputSubsystems)
                {
                    if (SetupCamera(inputSubsystem))
                    {
                        // It is possible this could happen more than
                        // once so unregister the callback first just in case.
                        inputSubsystem.trackingOriginUpdated -= OnInputSubsystemTrackingOriginUpdated;
                        inputSubsystem.trackingOriginUpdated += OnInputSubsystemTrackingOriginUpdated;
                    }
                    else
                    {
                        initialized = false;
                    }
                }
            }

            return initialized;
        }

        bool SetupCamera(XRInputSubsystem inputSubsystem)
        {
            if (inputSubsystem == null)
                return false;

            var successful = true;

            switch (m_RequestedTrackingOriginMode)
            {
                case TrackingOriginMode.NotSpecified:
                    CurrentTrackingOriginMode = inputSubsystem.GetTrackingOriginMode();
                    break;
                case TrackingOriginMode.Device:
                case TrackingOriginMode.Floor:
                {
                    var supportedModes = inputSubsystem.GetSupportedTrackingOriginModes();

                    // We need to check for Unknown because we may not be in a state where we can read this data yet.
                    if (supportedModes == TrackingOriginModeFlags.Unknown)
                        return false;

                    // Convert from the request enum to the flags enum that is used by the subsystem
                    var equivalentFlagsMode = m_RequestedTrackingOriginMode == TrackingOriginMode.Device
                        ? TrackingOriginModeFlags.Device
                        : TrackingOriginModeFlags.Floor;

                    // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags -- Treated like Flags enum when querying supported modes
                    if ((supportedModes & equivalentFlagsMode) == 0)
                    {
                        m_RequestedTrackingOriginMode = TrackingOriginMode.NotSpecified;
                        CurrentTrackingOriginMode = inputSubsystem.GetTrackingOriginMode();
                        Debug.LogWarning($"Attempting to set the tracking origin mode to {equivalentFlagsMode}, but that is not supported by the SDK." +
                            $" Supported types: {supportedModes:F}. Using the current mode of {CurrentTrackingOriginMode} instead.", this);
                    }
                    else
                    {
                        successful = inputSubsystem.TrySetTrackingOriginMode(equivalentFlagsMode);
                    }
                }
                    break;
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(TrackingOriginMode)}={m_RequestedTrackingOriginMode}");
                    return false;
            }

            if (successful)
                MoveOffsetHeight();

            if (CurrentTrackingOriginMode == TrackingOriginModeFlags.Device || m_RequestedTrackingOriginMode == TrackingOriginMode.Device)
                successful = inputSubsystem.TryRecenter();

            return successful;
        }

        IEnumerator RepeatInitializeCamera()
        {
            m_CameraInitializing = true;
            while (!m_CameraInitialized)
            {
                yield return null;
                if (!m_CameraInitialized)
                    m_CameraInitialized = SetupCamera();
            }
            m_CameraInitializing = false;
        }

        void OnInputSubsystemTrackingOriginUpdated(XRInputSubsystem inputSubsystem)
        {
            CurrentTrackingOriginMode = inputSubsystem.GetTrackingOriginMode();
            MoveOffsetHeight();
        }

        public bool RotateAroundCameraUsingOriginUp(float angleDegrees)
        {
            return RotateAroundCameraPosition(Origin.transform.up, angleDegrees);
        }

        public bool RotateAroundCameraPosition(Vector3 vector, float angleDegrees)
        {
            if (m_Camera == null || Origin == null)
            {
                return false;
            }

            // Rotate around the camera position
            Origin.transform.RotateAround(m_Camera.transform.position, vector, angleDegrees);

            return true;
        }

        public bool MatchOriginUp(Vector3 destinationUp)
        {
            if (Origin == null)
            {
                return false;
            }

            if (Origin.transform.up == destinationUp)
                return true;

            var rigUp = Quaternion.FromToRotation(Origin.transform.up, destinationUp);
            Origin.transform.rotation = rigUp * transform.rotation;

            return true;
        }

        public bool MatchOriginUpCameraForward(Vector3 destinationUp, Vector3 destinationForward)
        {
            if (m_Camera != null && MatchOriginUp(destinationUp))
            {
                // Project current camera's forward vector on the destination plane, whose normal vector is destinationUp.
                var projectedCamForward = Vector3.ProjectOnPlane(m_Camera.transform.forward, destinationUp).normalized;

                // The angle that we want the XROrigin to rotate is the signed angle between projectedCamForward and destinationForward, after the up vectors are matched.
                var signedAngle = Vector3.SignedAngle(projectedCamForward, destinationForward, destinationUp);

                RotateAroundCameraPosition(destinationUp, signedAngle);

                return true;
            }

            return false;
        }

        public bool MatchOriginUpOriginForward(Vector3 destinationUp, Vector3 destinationForward)
        {
            if (Origin != null && MatchOriginUp(destinationUp))
            {
                // The angle that we want the XR Origin to rotate is the signed angle between the origin's forward and destinationForward, after the up vectors are matched.
                var signedAngle = Vector3.SignedAngle(Origin.transform.forward, destinationForward, destinationUp);

                RotateAroundCameraPosition(destinationUp, signedAngle);

                return true;
            }

            return false;
        }

        public bool MoveCameraToWorldLocation(Vector3 desiredWorldLocation)
        {
            if (m_Camera == null)
            {
                return false;
            }

            var rot = Matrix4x4.Rotate(m_Camera.transform.rotation);
            var delta = rot.MultiplyPoint3x4(OriginInCameraSpacePos);
            Origin.transform.position = delta + desiredWorldLocation;

            return true;
        }

        protected void Awake()
        {
            if (cameraFloorOffsetObject == null)
            {
                Debug.LogWarning("No Camera Floor Offset Object specified for XR Origin, using attached GameObject.", this);
                cameraFloorOffsetObject = gameObject;
            }

            if (m_Camera == null)
            {
                var mainCamera = Camera.main;
                if (mainCamera != null)
                    m_Camera = mainCamera;
                else
                    Debug.LogWarning("No Main Camera is found for XR Origin, please assign the Camera field manually.", this);
            }

            // This will be the parent GameObject for any trackables (such as planes) for which
            // we want a corresponding GameObject.
            TrackablesParent = (new GameObject("Trackables")).transform;
            TrackablesParent.SetParent(transform, false);
            TrackablesParent.localPosition = Vector3.zero;
            TrackablesParent.localRotation = Quaternion.identity;
            TrackablesParent.localScale = Vector3.one;

            if (m_Camera)
            {
#if INCLUDE_INPUT_SYSTEM && INCLUDE_LEGACY_INPUT_HELPERS
                var trackedPoseDriver = m_Camera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                var trackedPoseDriverOld = m_Camera.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
                if (trackedPoseDriver == null && trackedPoseDriverOld == null)
                {
                    Debug.LogWarning(
                        $"Camera \"{m_Camera.name}\" does not use a Tracked Pose Driver (Input System), " +
                        "so its transform will not be updated by an XR device.  In order for this to be " +
                        "updated, please add a Tracked Pose Driver (Input System) with bindings for position and rotation of the center eye.", this);
                }
#elif !INCLUDE_INPUT_SYSTEM && INCLUDE_LEGACY_INPUT_HELPERS
                var trackedPoseDriverOld = m_Camera.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
                if (trackedPoseDriverOld == null)
                {
                    Debug.LogWarning(
                        $"Camera \"{m_Camera.name}\" does not use a Tracked Pose Driver, and com.unity.xr.legacyinputhelpers is installed. " +
                        "Although the Tracked Pose Driver from Legacy Input Helpers can be used, it is recommended to " +
                        "install com.unity.inputsystem instead and add a Tracked Pose Driver (Input System) with bindings for position and rotation of the center eye.", this);
                }
#elif INCLUDE_INPUT_SYSTEM && !INCLUDE_LEGACY_INPUT_HELPERS
                var trackedPoseDriver = m_Camera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                if (trackedPoseDriver == null)
                {
                    Debug.LogWarning(
                        $"Camera \"{m_Camera.name}\" does not use a Tracked Pose Driver (Input System), " +
                        "so its transform will not be updated by an XR device.  In order for this to be " +
                        "updated, please add a Tracked Pose Driver (Input System) with bindings for position and rotation of the center eye.", this);
                }
#elif !INCLUDE_INPUT_SYSTEM && !INCLUDE_LEGACY_INPUT_HELPERS
                Debug.LogWarning(
                    $"Camera \"{m_Camera.name}\" does not use a Tracked Pose Driver and com.unity.inputsystem is not installed, " +
                    "so its transform will not be updated by an XR device.  In order for this to be " +
                    "updated, please install com.unity.inputsystem and add a Tracked Pose Driver (Input System) with bindings for position and rotation of the center eye.", this);
#endif
            }
        }

        Pose GetCameraOriginPose()
        {
            var localOriginPose = Pose.identity;
            var parent = m_Camera.transform.parent;

            return parent
                ? parent.TransformPose(localOriginPose)
                : localOriginPose;
        }

        protected void OnEnable() => Application.onBeforeRender += OnBeforeRender;

        protected void OnDisable() => Application.onBeforeRender -= OnBeforeRender;

        void OnBeforeRender()
        {
            if (m_Camera)
            {
                var pose = GetCameraOriginPose();
                TrackablesParent.position = pose.position;
                TrackablesParent.rotation = pose.rotation;
            }

            if (TrackablesParent.hasChanged)
            {
                //TrackablesParentTransformChanged?.Invoke(
                //    new ARTrackablesParentTransformChangedEventArgs(this, TrackablesParent));
                //TrackablesParent.hasChanged = false;
            }
        }

        protected void OnValidate()
        {
            if (Origin == null)
                Origin = gameObject;

            if (Application.isPlaying && isActiveAndEnabled)
            {
                // Respond to the mode changing by re-initializing the camera,
                // or just update the offset height in order to avoid recentering.
                if (IsModeStale())
                    TryInitializeCamera();
                else
                    MoveOffsetHeight();
            }

            bool IsModeStale()
            {
                if (s_InputSubsystems.Count > 0)
                {
                    foreach (var inputSubsystem in s_InputSubsystems)
                    {
                        // Convert from the request enum to the flags enum that is used by the subsystem
                        TrackingOriginModeFlags equivalentFlagsMode;
                        switch (m_RequestedTrackingOriginMode)
                        {
                            case TrackingOriginMode.NotSpecified:
                                // Don't need to initialize the camera since we don't set the mode when NotSpecified (we just keep the current value)
                                return false;
                            case TrackingOriginMode.Device:
                                equivalentFlagsMode = TrackingOriginModeFlags.Device;
                                break;
                            case TrackingOriginMode.Floor:
                                equivalentFlagsMode = TrackingOriginModeFlags.Floor;
                                break;
                            default:
                                Assert.IsTrue(false, $"Unhandled {nameof(TrackingOriginMode)}={m_RequestedTrackingOriginMode}");
                                return false;
                        }

                        if (inputSubsystem != null && inputSubsystem.GetTrackingOriginMode() != equivalentFlagsMode)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        protected void Start()
        {
            TryInitializeCamera();
        }

        protected void OnDestroy()
        {
            foreach (var inputSubsystem in s_InputSubsystems)
            {
                if (inputSubsystem != null)
                    inputSubsystem.trackingOriginUpdated -= OnInputSubsystemTrackingOriginUpdated;
            }
        }
    }
}
