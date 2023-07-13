using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LAI.XR
{
    public class MyController : MonoBehaviour
    {
        private enum ControllerType
        {
            Left,
            Right
        }
        [SerializeField] private ControllerType controllerType;
        [SerializeField] private InputActionAsset actionAsset;
        private const string leftHandName = "LeftHand";
        private const string rightHandName = "RightHand";

        private void Start()
        {
            string controllerName = 
                (controllerType == ControllerType.Left) ? leftHandName : rightHandName;
            InputActionMap handBase = actionAsset.FindActionMap("XRI " + controllerName);
            handBase.Enable();

            InputAction positionAction = handBase.FindAction("Position");
            positionAction.performed += context => 
            { 
                UpdatePosition(context.ReadValue<Vector3>()); 
            };
            InputAction rotationAction = handBase.FindAction("Rotation");
            rotationAction.performed += context => 
            { 
                UpdateRotation(context.ReadValue<Quaternion>()); 
            };
        }

        private void UpdatePosition(Vector3 position)
        {
            transform.position = position;
        }

        private void UpdateRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }
    }
}