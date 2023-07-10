using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PromptTesting : MonoBehaviour
{
    [SerializeField] private string prompt = "What is this place?";
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
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space) && !speechRecognition.IsRecording)
        {
            speechRecognition.StartRecording();
        }
        else if (Input.GetKeyUp(KeyCode.Space) && speechRecognition.IsRecording)
        {
            speechRecognition.EndRecording();
            ProcessRecording();
        }
    }

    private async Task ProcessRecording()
    {
        string transcription = await speechRecognition.GetTranscription();
        string response = await brain.ThinkAndReply(transcription);
        await textToSpeech.Speak(response, "Olivia", audioSource);
    }
}
