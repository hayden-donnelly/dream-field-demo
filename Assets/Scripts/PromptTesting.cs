using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PromptTesting : MonoBehaviour
{
    [SerializeField] private string prompt = "What is this place?";
    NewConversationCenter conversationCenter;

    private async void Start()
    {
        conversationCenter = new NewConversationCenter();
        await conversationCenter.TranscribeAndReply(prompt);
    }
}
