using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class CustomXRInput : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private XRRayInteractor rayInteractor;
    [SerializeField] private ConversationCenter conversationCenter;

    private void OnEnable()
    {
        inputActionAsset.Enable();
    }

    private void Start()
    {
        rayInteractor.enabled = false;
        InputActionMap leftActions = 
            inputActionAsset.FindActionMap("XRI LeftHand Interaction");
        InputAction locomotionAction = leftActions.FindAction("Activate");
        locomotionAction.started += context => rayInteractor.enabled = true;
        locomotionAction.canceled += context => rayInteractor.enabled = false;

        InputAction conversationAction = leftActions.FindAction("Face Button A");
        conversationAction.started += context => conversationCenter.StartRecording();
        conversationAction.canceled += context => conversationCenter.StopRecording();

        InputAction interruptAction = leftActions.FindAction("Face Button B");
        conversationAction.started += context => conversationCenter.InterruptSpeaker();

        InputActionMap rightActions = 
            inputActionAsset.FindActionMap("XRI RightHand Interaction");
        conversationAction = rightActions.FindAction("Face Button A");
        conversationAction.started += context => conversationCenter.StartRecording();
        conversationAction.canceled += context => conversationCenter.StopRecording();

        interruptAction = rightActions.FindAction("Face Button B");
        conversationAction.started += context => conversationCenter.InterruptSpeaker();
    }

    private void OnDisable()
    {
        inputActionAsset.Disable();
    }
}
