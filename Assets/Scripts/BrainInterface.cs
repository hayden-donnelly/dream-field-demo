using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class BrainInterface : MonoBehaviour
{
    [SerializeField] private string voice = "Olivia";
    [SerializeField] private InputActionAsset controls;
    [SerializeField] private EventBus eventBus;
    private Brain brain;
    private SpeechRecognition speechRecognition;
    private TextToSpeech textToSpeech;
    private AudioSource audioSource;

    private void Start()
    {
        eventBus = EventBusHelper.GetEventBus(eventBus);
        brain = new Brain();
        brain.BrainEventBus = eventBus;
        speechRecognition = new SpeechRecognition();
        speechRecognition.InitializeMicrophone();
        textToSpeech = new TextToSpeech();
        audioSource = GetComponent<AudioSource>();

        InputActionMap basicMap = controls.FindActionMap("Basic");
        basicMap.Enable();
        InputAction recordAction = basicMap.FindAction("Record");
        recordAction.started += context => 
        {
            if(speechRecognition.IsRecording) { return; }
            eventBus.OnRecordingStarted.Invoke();
            speechRecognition.StartRecording();
        };
        recordAction.canceled += context => 
        {
            if(!speechRecognition.IsRecording) { return; }
            eventBus.OnRecordingEnded.Invoke();
            speechRecognition.EndRecording();
            ProcessRecording();
        };
        
        InputAction resetConvoAction = basicMap.FindAction("ResetConversation");
        resetConvoAction.started += context => { brain.InitializeConversationHistory(); };
    }

    private async Task ProcessRecording()
    {
        string transcription = await speechRecognition.GetTranscription();
        string response = await brain.ThinkAndReply(transcription);
        await textToSpeech.Speak(response, voice, audioSource);
    }
}
