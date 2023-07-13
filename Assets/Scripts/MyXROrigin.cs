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
    [AddComponentMenu("LAI/XR/My XR Origin")]
    [DisallowMultipleComponent]
    public class MyXROrigin : MonoBehaviour
    {
        public GameObject Origin;

        public enum TrackingOriginMode
        {
            // Uses the default Tracking Origin Mode of the input device.
            // When changing to this value after startup, the Tracking Origin 
            // Mode will not be changed.
            NotSpecified,
            // Input devices will be tracked relative to the first known location.
            // Represents a device-relative tracking origin.
            Device,
            // Input devices will be tracked relative to a location on the floor.
            Floor,
        }
        [SerializeField] private TrackingOriginMode requestedTrackingOriginMode = 
            TrackingOriginMode.NotSpecified;
        public TrackingOriginMode RequestedTrackingOriginMode
        {
            get => requestedTrackingOriginMode;
            set
            {
                requestedTrackingOriginMode = value;
                TryInitializeCamera();
            }
        }

        [SerializeField] private Camera xrCamera;
        private const float defaultCameraYOffset = 1.1176f; // Average seated height.
        [SerializeField] private GameObject cameraFloorOffsetObject;
        [SerializeField] private float cameraYOffset = defaultCameraYOffset;
        public float CameraYOffset
        {
            get => cameraYOffset;
            set
            {
                cameraYOffset = value;
                MoveOffsetHeight();
            }
        }

        public void SetCameraFloorOffsetObject(GameObject cameraFloorOffsetObject)
        {
            this.cameraFloorOffsetObject = cameraFloorOffsetObject;
            MoveOffsetHeight();
        }


        public TrackingOriginModeFlags CurrentTrackingOriginMode { get; private set; }

        public Vector3 OriginInCameraSpacePos => 
            xrCamera.transform.InverseTransformPoint(Origin.transform.position);

        public Vector3 CameraInOriginSpacePos => 
            Origin.transform.InverseTransformPoint(xrCamera.transform.position);

        public float CameraInOriginSpaceHeight => CameraInOriginSpacePos.y;

        static readonly List<XRInputSubsystem> s_InputSubsystems = new List<XRInputSubsystem>();

        // Bookkeeping to track lazy initialization of the tracking origin mode type.
        private bool cameraInitialized;
        private bool cameraInitializing;

        private void MoveOffsetHeight()
        {
            if (!Application.isPlaying)
                return;

            switch (CurrentTrackingOriginMode)
            {
                case TrackingOriginModeFlags.Floor:
                    MoveOffsetHeightToSpecified(0f);
                    break;
                case TrackingOriginModeFlags.Device:
                    MoveOffsetHeightToSpecified(cameraYOffset);
                    break;
                default:
                    return;
            }
        }

        private void MoveOffsetHeightToSpecified(float y)
        {
            if(cameraFloorOffsetObject != null)
            {
                Transform offsetTransform = cameraFloorOffsetObject.transform;
                Vector3 desiredPosition = offsetTransform.localPosition;
                desiredPosition.y = y;
                offsetTransform.localPosition = desiredPosition;
            }
        }

        private void TryInitializeCamera()
        {
            if(!Application.isPlaying) { return; }

            cameraInitialized = SetupCamera();
            if(!cameraInitialized & !cameraInitializing)
            {
                StartCoroutine(RepeatInitializeCamera());
            }
        }

        private bool SetupCamera()
        {
            bool initialized = true;

            SubsystemManager.GetInstances(s_InputSubsystems);
            if(s_InputSubsystems.Count > 0)
            {
                foreach(XRInputSubsystem inputSubsystem in s_InputSubsystems)
                {
                    if(SetupCameraWithInputSubsystem(inputSubsystem))
                    {
                        // It is possible this could happen more than
                        // once so unregister the callback first just in case.
                        inputSubsystem.trackingOriginUpdated -= 
                            OnInputSubsystemTrackingOriginUpdated;
                        inputSubsystem.trackingOriginUpdated += 
                            OnInputSubsystemTrackingOriginUpdated;
                    }
                    else
                    {
                        initialized = false;
                    }
                }
            }

            return initialized;
        }

        private bool SetupCameraWithInputSubsystem(XRInputSubsystem inputSubsystem)
        {
            if(inputSubsystem == null) { return false; }

            bool successful = true;

            switch(requestedTrackingOriginMode)
            {
                case TrackingOriginMode.NotSpecified:
                    CurrentTrackingOriginMode = inputSubsystem.GetTrackingOriginMode();
                    break;
                case TrackingOriginMode.Device:
                case TrackingOriginMode.Floor:
                    TrackingOriginModeFlags supportedModes = 
                        inputSubsystem.GetSupportedTrackingOriginModes();

                    // We need to check for Unknown because we may not be in a state where we 
                    // can read this data yet.
                    if(supportedModes == TrackingOriginModeFlags.Unknown) { return false; }

                    // Convert request enum to the flags enum that is used by the subsystem.
                    TrackingOriginModeFlags equivalentFlagsMode = 
                        (requestedTrackingOriginMode == TrackingOriginMode.Device)
                        ? TrackingOriginModeFlags.Device : TrackingOriginModeFlags.Floor;

                    // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags 
                    // -- Treated like Flags enum when querying supported modes
                    if((supportedModes & equivalentFlagsMode) == 0)
                    {
                        requestedTrackingOriginMode = TrackingOriginMode.NotSpecified;
                        CurrentTrackingOriginMode = inputSubsystem.GetTrackingOriginMode();
                        string warningMessage = 
                            "Attempting to set the tracking origin mode to " +
                            $"{equivalentFlagsMode}, but that is not supported by the SDK. " +
                            $"Supported types: {supportedModes:F}. Using the current mode of " +
                            $"{CurrentTrackingOriginMode} instead.";
                        Debug.LogWarning(warningMessage, this);
                    }
                    else
                    {
                        successful = 
                            inputSubsystem.TrySetTrackingOriginMode(equivalentFlagsMode);
                    }
                    break;
                default:
                    string assertionText = 
                        $"Unhandled {nameof(TrackingOriginMode)}={requestedTrackingOriginMode}";
                    Assert.IsTrue(false, assertionText);
                    return false;
            }

            if(successful) { MoveOffsetHeight(); }

            bool trackingModeIsDevice = 
                CurrentTrackingOriginMode == TrackingOriginModeFlags.Device || 
                requestedTrackingOriginMode == TrackingOriginMode.Device;
            if(trackingModeIsDevice) { successful = inputSubsystem.TryRecenter(); }

            return successful;
        }

        private IEnumerator RepeatInitializeCamera()
        {
            cameraInitializing = true;
            while(!cameraInitialized)
            {
                yield return null;
                if(!cameraInitialized)
                {
                    cameraInitialized = SetupCamera();
                }
            }
            cameraInitializing = false;
        }

        private void OnInputSubsystemTrackingOriginUpdated(XRInputSubsystem inputSubsystem)
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
            if(xrCamera == null || Origin == null) { return false; }

            // Rotate around the camera position
            Origin.transform.RotateAround(xrCamera.transform.position, vector, angleDegrees);
            return true;
        }

        public bool MatchOriginUp(Vector3 destinationUp)
        {
            if(Origin == null) { return false; }

            if(Origin.transform.up == destinationUp) { return true; }

            Quaternion rigUp = Quaternion.FromToRotation(Origin.transform.up, destinationUp);
            Origin.transform.rotation = rigUp * transform.rotation;
            return true;
        }

        public bool MatchOriginUpCameraForward(
            Vector3 destinationUp, Vector3 destinationForward
        )
        {
            if(xrCamera != null && MatchOriginUp(destinationUp))
            {
                // Project current camera's forward vector on the destination plane, 
                // whose normal vector is destinationUp.
                Vector3 projectedCamForward = Vector3.ProjectOnPlane(
                    xrCamera.transform.forward, destinationUp
                ).normalized;

                // The angle that we want the XROrigin to rotate is the signed angle between 
                // projectedCamForward and destinationForward, after the up vectors are matched.
                float signedAngle = Vector3.SignedAngle(
                    projectedCamForward, destinationForward, destinationUp
                );

                RotateAroundCameraPosition(destinationUp, signedAngle);
                return true;
            }

            return false;
        }

        public bool MatchOriginUpOriginForward(
            Vector3 destinationUp, Vector3 destinationForward
        )
        {
            if(Origin != null && MatchOriginUp(destinationUp))
            {
                // The angle that we want the XR Origin to rotate is the signed angle between 
                // the origin's forward and destinationForward, after the up vectors 
                // are matched.
                float signedAngle = Vector3.SignedAngle(
                    Origin.transform.forward, destinationForward, destinationUp
                );

                RotateAroundCameraPosition(destinationUp, signedAngle);
                return true;
            }

            return false;
        }

        public bool MoveCameraToWorldLocation(Vector3 desiredWorldLocation)
        {
            if(xrCamera == null) { return false; }

            Matrix4x4 rot = Matrix4x4.Rotate(xrCamera.transform.rotation);
            Vector3 delta = rot.MultiplyPoint3x4(OriginInCameraSpacePos);
            Origin.transform.position = delta + desiredWorldLocation;
            return true;
        }

        protected void Awake()
        {
            if(cameraFloorOffsetObject == null)
            {
                string warningMessage = 
                    @"No Camera Floor Offset Object specified for XR Origin, 
                    using attached GameObject.";
                Debug.LogWarning(warningMessage, this);
                cameraFloorOffsetObject = gameObject;
            }

            if(xrCamera == null)
            {
                Camera mainCamera = Camera.main;
                if(mainCamera != null) { xrCamera = mainCamera; }
                else
                {
                    string warningMessage = 
                        @"No Main Camera is found for XR Origin, 
                        please assign the Camera field manually.";
                    Debug.LogWarning(warningMessage, this);
                }
            }

            if(xrCamera)
            {
                #if INCLUDE_INPUT_SYSTEM && INCLUDE_LEGACY_INPUT_HELPERS
                var trackedPoseDriver = 
                    xrCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                var trackedPoseDriverOld = 
                    xrCamera.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
                
                if(trackedPoseDriver == null && trackedPoseDriverOld == null)
                {
                    string warningMessage = 
                        $"Camera \"{xrCamera.name}\" does not use a Tracked Pose Driver " +
                        "(Input System), so its transform will not be updated by an XR " + 
                        "device. In order for this to be updated, please add a Tracked Pose " +
                        "Driver (Input System) with bindings for position and rotation of " +
                        "the center eye.";
                    Debug.LogWarning(warningMessage, this);
                }

                #elif !INCLUDE_INPUT_SYSTEM && INCLUDE_LEGACY_INPUT_HELPERS
                var trackedPoseDriverOld = 
                    xrCamera.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
                
                if(trackedPoseDriverOld == null)
                {
                    string warningMessage = 
                        $"Camera \"{xrCamera.name}\" does not use a Tracked Pose Driver, " +
                        "and com.unity.xr.legacyinputhelpers is installed. Although the " +
                        "Tracked Pose Driver from Legacy Input Helpers can be used, it is " +
                        "recommended to install com.unity.inputsystem instead and add a " +
                        "Tracked Pose Driver (Input System) with bindings for position and " +
                        "rotation of the center eye.";
                    Debug.LogWarning(warningMessage, this);
                }

                #elif INCLUDE_INPUT_SYSTEM && !INCLUDE_LEGACY_INPUT_HELPERS
                var trackedPoseDriver = 
                    xrCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                
                if(trackedPoseDriver == null)
                {
                    string warningMessage = 
                        $"Camera \"{xrCamera.name}\" does not use a Tracked Pose Driver " +
                        "(Input System), so its transform will not be updated by an XR " +
                        "device. In order for this to be updated, please add a Tracked Pose " +
                        "Driver (Input System) with bindings for position and rotation of " +
                        "the center eye.";
                    Debug.LogWarning(warningMessage, this);
                }

                #elif !INCLUDE_INPUT_SYSTEM && !INCLUDE_LEGACY_INPUT_HELPERS
                string warningMessage = 
                    $"Camera \"{xrCamera.name}\" does not use a Tracked Pose Driver and " +
                    "com.unity.inputsystem is not installed, so its transform will not be " +
                    "updated by an XR device. In order for this to be updated, please " +
                    "com.unity.inputsystem and add a Tracked Pose Driver (Input System) " +
                    "with bindings for position and rotation of the center eye.";
                Debug.LogWarning(warningMessage, this);
                #endif
            }
        }

        private Pose GetCameraOriginPose()
        {
            Pose localOriginPose = Pose.identity;
            Transform parent = xrCamera.transform.parent;

            return parent ? parent.TransformPose(localOriginPose) : localOriginPose;
        }

        protected void OnEnable() => Application.onBeforeRender += OnBeforeRender;

        protected void OnDisable() => Application.onBeforeRender -= OnBeforeRender;

        private void OnBeforeRender()
        {
            if(xrCamera)
            {
                Pose pose = GetCameraOriginPose();
            }
        }

        protected void OnValidate()
        {
            if(Origin == null) { Origin = gameObject; }

            if(Application.isPlaying && isActiveAndEnabled)
            {
                // Respond to the mode changing by re-initializing the camera,
                // or just update the offset height in order to avoid recentering.
                if (IsModeStale()) { TryInitializeCamera(); }
                else { MoveOffsetHeight(); }
            }

            bool IsModeStale()
            {
                if(s_InputSubsystems.Count <= 0) { return false; }
                foreach (var inputSubsystem in s_InputSubsystems)
                {
                    // Convert from the request enum to the flags enum that is used by 
                    // the subsystem.
                    TrackingOriginModeFlags equivalentFlagsMode;
                    switch (requestedTrackingOriginMode)
                    {
                        case TrackingOriginMode.NotSpecified:
                            // Don't need to initialize the camera since we don't set the 
                            // mode when NotSpecified (we just keep the current value).
                            return false;
                        case TrackingOriginMode.Device:
                            equivalentFlagsMode = TrackingOriginModeFlags.Device;
                            break;
                        case TrackingOriginMode.Floor:
                            equivalentFlagsMode = TrackingOriginModeFlags.Floor;
                            break;
                        default:
                            string assertionText = 
                                $"Unhandled {nameof(TrackingOriginMode)}=" +
                                $"{requestedTrackingOriginMode}";
                            Assert.IsTrue(false, assertionText);
                            return false;
                    }

                    if(inputSubsystem != null && 
                        inputSubsystem.GetTrackingOriginMode() != equivalentFlagsMode)
                    {
                        return true;
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
            foreach(XRInputSubsystem inputSubsystem in s_InputSubsystems)
            {
                if(inputSubsystem != null)
                {
                    inputSubsystem.trackingOriginUpdated -= 
                        OnInputSubsystemTrackingOriginUpdated;
                }
            }
        }
    }
}
