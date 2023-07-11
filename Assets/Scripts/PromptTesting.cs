using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class PromptTesting : MonoBehaviour
{
    [SerializeField] private InputActionAsset controls;
    private Brain brain;
    private SpeechRecognition speechRecognition;
    private TextToSpeech textToSpeech;
    private AudioSource audioSource;

    private void Start()
    {
        brain = new Brain();
        speechRecognition = new SpeechRecognition();
        textToSpeech = new TextToSpeech();
        audioSource = GetComponent<AudioSource>();

        InputActionMap basicMap = controls.FindActionMap("Basic");
        basicMap.Enable();
        InputAction recordAction = basicMap.FindAction("Record");
        recordAction.started += context => 
        {
            if(speechRecognition.IsRecording) { return; }
            speechRecognition.StartRecording();
        };
        recordAction.canceled += context => 
        {
            if(!speechRecognition.IsRecording) { return; }
            speechRecognition.EndRecording();
            ProcessRecording();
        };
    }

    private async Task ProcessRecording()
    {
        string transcription = await speechRecognition.GetTranscription();
        string response = await brain.ThinkAndReply(transcription);
        await textToSpeech.Speak(response, "Olivia", audioSource);
    }
}
