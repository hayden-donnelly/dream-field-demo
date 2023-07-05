using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LMNT;

public class SpeechTrigger : MonoBehaviour
{
    [SerializeField] private LMNTSpeech speech;

    private void Start()
    {
        speech.dialogue = "Test test I am a robot.";
        StartCoroutine(speech.Talk());
    }
}
