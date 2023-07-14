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
        [SerializeField] private GameObject origin;
        private Transform originTransform;
        [Header("Turning")]
        [SerializeField] private bool snapTurn = true;
        [SerializeField] private float snapTurnAngle = 45f;
        [SerializeField] private float snapTurnCooldown = 0.5f;
        private bool canSnapTurn = true;
        [SerializeField] private float smoothTurnSpeed = 60f;
        private bool isRotating = false;
        private Vector2 rotationInput;

        private void Start()
        {
            originTransform = origin.transform;

            string controllerName = 
                (controllerType == ControllerType.Left) ? leftHandName : rightHandName;
            InputActionMap handBase = actionAsset.FindActionMap("XRI " + controllerName);
            handBase.Enable();

            InputAction positionAction = handBase.FindAction("Position");
            positionAction.performed += context => 
            { 
                UpdateHandPosition(context.ReadValue<Vector3>()); 
            };
            InputAction rotationAction = handBase.FindAction("Rotation");
            rotationAction.performed += context => 
            { 
                UpdateHandRotation(context.ReadValue<Quaternion>()); 
            };

            InputActionMap locomotion = 
                actionAsset.FindActionMap("XRI " + controllerName + " Locomotion");
            locomotion.Enable();
            if(controllerType == ControllerType.Right)
            {
                InputAction turn = locomotion.FindAction("Turn");
                turn.performed += context => 
                { 
                    rotationInput = context.ReadValue<Vector2>();
                    isRotating = true;
                };
                turn.canceled += context => 
                { 
                    rotationInput = Vector2.zero;
                    isRotating = false;
                };
            }
        }

        private void Update()
        {
            if(isRotating)
            {
                if(snapTurn) { SnapRotateBody(rotationInput); }
                else { SmoothRotateBody(rotationInput); }
            }
        }

        private void UpdateHandPosition(Vector3 position)
        {
            transform.position = position;
        }

        private void UpdateHandRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        private void SnapRotateBody(Vector2 rotationInput)   
        {
            if(!canSnapTurn) { return; }
            if(rotationInput.x > 0) { originTransform.Rotate(0, snapTurnAngle, 0); }
            else if(rotationInput.x < 0) { originTransform.Rotate(0, -snapTurnAngle, 0); }
            StartCoroutine(SnapTurnCooldown());
        }

        private IEnumerator SnapTurnCooldown()
        {
            canSnapTurn = false;
            yield return new WaitForSeconds(snapTurnCooldown);
            canSnapTurn = true;
        }

        private void SmoothRotateBody(Vector2 rotationInput)   
        {
            float yRotation = rotationInput.x * smoothTurnSpeed * Time.deltaTime;
            originTransform.Rotate(0, yRotation, 0);
        }
    }
}