using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class AutoInteractionReference : MonoBehaviour
{
    [SerializeField] private TeleportationArea teleportationArea;

    private void Start()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("PlayerInteraction");
        if(obj != null)
        {
        }
        else
        {
            Debug.LogWarning("No InteractionManager found");
        }
    }
}
