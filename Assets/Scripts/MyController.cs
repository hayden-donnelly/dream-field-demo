using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LAI.XR
{
    [RequireComponent(typeof(LineRenderer))]
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
        private MyXROrigin xrOrigin;
        [Header("Turning")]
        [SerializeField] private bool snapTurn = true;
        [SerializeField] private float snapTurnAngle = 45f;
        [SerializeField] private float snapTurnCooldown = 0.2f;
        private bool snapTurnCooledown = true;
        [SerializeField] private float smoothTurnSpeed = 60f;
        private bool isRotating = false;
        private Vector2 rotationInput;
        [Header("Movement")]
        [SerializeField] private bool teleportMode = true;
        [SerializeField] private float movementSpeed = 1f;
        [SerializeField] private float maxTeleportationDistance = 6.0f;
        [SerializeField] private float maxTeleportationAngle = 45f;
        private bool inTeleportContext = false;
        private bool teleportTargetIsValid = false;
        private Vector3 teleportationTarget;
        private LineRenderer lineRenderer;

        private void Start()
        {
            originTransform = origin.transform;
            xrOrigin = origin.GetComponent<MyXROrigin>();
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.enabled = false;

            string controllerName = 
                (controllerType == ControllerType.Left) ? leftHandName : rightHandName;
            InputActionMap handBase = actionAsset.FindActionMap("XRI " + controllerName);
            handBase.Enable();
            InputActionMap locomotion = 
                actionAsset.FindActionMap("XRI " + controllerName + " Locomotion");
            locomotion.Enable();
            InputActionMap interaction = 
                actionAsset.FindActionMap("XRI " + controllerName + " Interaction");
            interaction.Enable();

            SetHandPositionCallback(handBase);
            SetHandRotationCallback(handBase);

            if(controllerType == ControllerType.Right) { SetTurnCallback(locomotion); }
            else if(controllerType == ControllerType.Left)
            {
                if(teleportMode) { SetTeleportCallback(interaction); }
                else { SetMoveCallback(locomotion); }
            }
        }

        private void SetHandPositionCallback(InputActionMap handBase)
        {
            InputAction positionAction = handBase.FindAction("Position");
            positionAction.performed += context => 
            { 
                UpdateHandPosition(context.ReadValue<Vector3>()); 
            };
        }

        private void SetHandRotationCallback(InputActionMap handBase)
        {
            InputAction rotationAction = handBase.FindAction("Rotation");
            rotationAction.performed += context => 
            { 
                UpdateHandRotation(context.ReadValue<Quaternion>()); 
            };
        }

        private void SetTurnCallback(InputActionMap locomotion)
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

        private void SetTeleportCallback(InputActionMap interaction)
        {
            InputAction teleportContext = interaction.FindAction("Select");
            teleportContext.performed += context => 
            {
                inTeleportContext = true;
                lineRenderer.enabled = true;
            };
            teleportContext.canceled += context => 
            { 
                inTeleportContext = false; 
                lineRenderer.enabled = false;
            };
            
            InputAction teleport = interaction.FindAction("Activate");
            teleport.performed += context => { Teleport(); };
        }

        private void SetMoveCallback(InputActionMap locomotion)
        {
            InputAction movement = locomotion.FindAction("Move");
            movement.performed += context => 
            { 
                Vector2 movementInput = context.ReadValue<Vector2>();
                Move(movementInput);
            };
        }

        private void Update()
        {
            if(isRotating)
            {
                if(snapTurn) { SnapRotateBody(rotationInput); }
                else { SmoothRotateBody(rotationInput); }
            }
            if(inTeleportContext) 
            {
                GetTeleportTarget();
                SetTeleportLineColor();
                SetTeleportLinePosition();
            }
        }

        private void UpdateHandPosition(Vector3 position)
        {
            transform.localPosition = position;
        }

        private void UpdateHandRotation(Quaternion rotation)
        {
            transform.localRotation = rotation;
        }

        private void SnapRotateBody(Vector2 rotationInput)   
        {
            if(!snapTurnCooledown) { return; }
            if(rotationInput.x > 0) { originTransform.Rotate(0, snapTurnAngle, 0); }
            else if(rotationInput.x < 0) { originTransform.Rotate(0, -snapTurnAngle, 0); }
            StartCoroutine(SnapTurnCooldown());
        }

        private IEnumerator SnapTurnCooldown()
        {
            snapTurnCooledown = false;
            yield return new WaitForSeconds(snapTurnCooldown);
            snapTurnCooledown = true;
        }

        private void SmoothRotateBody(Vector2 rotationInput)   
        {
            float yRotation = rotationInput.x * smoothTurnSpeed * Time.deltaTime;
            originTransform.Rotate(0, yRotation, 0);
        }

        private void GetTeleportTarget()
        {
            RaycastHit hit;
            Vector3 rayDirection = transform.forward;
            teleportTargetIsValid = false;
            teleportationTarget = Vector3.zero;
            if(Physics.Raycast(transform.position, rayDirection, out hit))
            {
                if(Vector3.Angle(Vector3.up, hit.normal) > maxTeleportationAngle) { return; }
                if(hit.distance > maxTeleportationDistance) { return; }
                teleportTargetIsValid = true;
                teleportationTarget = hit.point;   
            }
        }

        private void SetTeleportLineColor()
        {
            if(!teleportTargetIsValid) 
            { 
                lineRenderer.startColor = Color.red;
                lineRenderer.endColor = Color.red;
            }
            else
            { 
                lineRenderer.startColor = Color.green;
                lineRenderer.endColor = Color.green;
            }
        }

        private void SetTeleportLinePosition()
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(
                1, transform.position + transform.forward * maxTeleportationDistance
            );
        }

        private void Teleport()
        {
            if(!inTeleportContext || !teleportTargetIsValid) { return; }
            originTransform.position = teleportationTarget;
        }

        private void Move(Vector2 movementInput)
        {
            Vector3 originForward = originTransform.forward;
            Vector3 originRight = originTransform.right;
            Vector3 movement = originForward * movementInput.y + originRight * movementInput.x;
            originTransform.Translate(movement * movementSpeed * Time.deltaTime);
        }
    }
}