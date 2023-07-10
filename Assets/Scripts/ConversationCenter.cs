using UnityEngine;
using OpenAI;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AI;

public class ConversationCenter : MonoBehaviour
{
    [SerializeField] private SceneLoader sceneLoader;
    private bool isFollowing = false;
    [SerializeField] private float followDistance = 5f;
    [SerializeField] private Transform userTransform;
    [SerializeField] private GameObject recordingNotification;
    [SerializeField] private string voice = "Olivia";
    [SerializeField] private string testText = "test test test";
    [SerializeField] private AudioSource audioSource;
    private NavMeshAgent navMeshAgent;
    private OpenAIApi openai = new OpenAIApi();
    private SpeechRecognition speechRecognition = new SpeechRecognition();
    private TextToSpeech customTTS;
    private List<ChatMessage> messages = new List<ChatMessage>();
    private string prompt = 
        @"You are an assistant inside of a virtual reality game. You have the ability to move 
        around and interact with objects within this game. Your goal is to aid and entertain 
        the user. The user may ask you to complete anyone of the following special tasks:
        1. Move to their location
        2. Stay where you are
        3. Teleport them to a location
        If the user asks you to complete one of these tasks, you should begin your response 
        with the number of the special task that you are completing, and then continue the 
        rest of your response. For example, if the user says 'come here please', this means 
        they are asking you to move to their location, so your response should look like 
        '1 Okay on my way!' If the user is not asking you to complete one of the special 
        tasks, you should respond normally.
        There are two locations that you can teleport the user to.
        1. Space
        2. The lobby
        If the user asks you to teleport them to one of these locations, you should begin
        your response with 3 because that is the number of the special task they're asking you
        to complete, following this you should say the number of the location you are 
        teleporting them to, then continue with the rest of your response. 
        For example, if the user says 'teleport me to space', your response should look like
        '3 1 Okay, teleporting you to space. Hold on tight!'";

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        customTTS = new TextToSpeech();
    }

    private async void Update()
    {
        if(userTransform == null)
        {
            userTransform = GameObject.FindGameObjectWithTag("PlayerOrigin").transform;
        }
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            await customTTS.Speak(testText, voice, audioSource);
        }
        if(Input.GetKeyDown(KeyCode.Space) && !speechRecognition.IsRecording)
        {
            StartRecording();
        }
        if(Input.GetKeyUp(KeyCode.Space) && speechRecognition.IsRecording)
        {
            StopRecording();
        }

        float distanceFromUser = Vector3.Distance(transform.position, userTransform.position);
        if(isFollowing && distanceFromUser > followDistance)
        {
            navMeshAgent.SetDestination(userTransform.position);
        }
        else if(isFollowing && distanceFromUser <= followDistance)
        {
            navMeshAgent.SetDestination(transform.position);
        }
    }

    public void StartRecording()
    {
        if(speechRecognition.IsRecording) { return; }
        recordingNotification.SetActive(true);
        speechRecognition.StartRecording();
    }

    public async void StopRecording()
    {
        if(!speechRecognition.IsRecording) { return; }
        recordingNotification.SetActive(false);
        speechRecognition.EndRecording();
        await TranscribeAndReply();   
    }

    public void InterruptSpeaker()
    {
        Debug.Log("Intterupt not implemented yet");
    }

    private async Task TranscribeAndReply()
    {
        string transcription = await speechRecognition.GetTranscription();
        Debug.Log(transcription);
        string response = await GetReply(transcription);
        if(response == "") { return; }
        (int, string) parsedResponse = ParseForSpecialTask(response);
        response = ExecuteSpecialTask(parsedResponse.Item1, parsedResponse.Item2);
        Debug.Log(parsedResponse.Item2);
        await customTTS.Speak(response, voice, audioSource);
    }

    private string ExecuteSpecialTask(int specialTaskIdentifier, string response)
    {
        if(specialTaskIdentifier == 0) { return response; }
        switch(specialTaskIdentifier)
        {
            case 1:
                isFollowing = true;
                //navMeshAgent.SetDestination(userTransform.position);
                break;
            case 2:
                isFollowing = false;
                navMeshAgent.SetDestination(transform.position);
                break;
            case 3:
                (int, string) parsedResponse = ParseForSpecialTask(response);
                if(parsedResponse.Item1 == 1)
                {
                    sceneLoader.LoadScene("Space");
                    userTransform.position = Vector3.zero;
                    return parsedResponse.Item2;
                }
                else if(parsedResponse.Item1 == 2)
                {
                    sceneLoader.LoadScene("SampleScene2");
                    userTransform.position = Vector3.zero;
                    return parsedResponse.Item2;
                }
                break;
            default:
                Debug.Log("Invalid special task identifier");
                break;
        }
        return response;
    }

    private async Task TestPrompt()
    {
        string response = await GetReply("Hey, please come over here.");
        //string response = await GetReply("Please teleport me to the bowling alley.");
        //string response = await GetReply("Please wait where you are.");
        //string response = await GetReply("How are you today?");
        (int, string) parsedResponse = ParseForSpecialTask(response);
        Debug.Log("Special task identifier: " + parsedResponse.Item1);
        Debug.Log("Response: " + parsedResponse.Item2);
        Debug.Log(response);
        if(parsedResponse.Item1 == 1)
        {
            navMeshAgent.SetDestination(userTransform.position);
        }
    }

    private (int, string) ParseForSpecialTask(string response)
    {
        string specialTaskIdentifierString = response.Substring(0, 1);
        int specialTaskIdentifier = -1;
        if(int.TryParse(specialTaskIdentifierString, out specialTaskIdentifier))
        {
            response = response.Substring(2);
        }
        return (specialTaskIdentifier, response);
    }

    private async Task<string> GetReply(string inputText)
    {
        var newMessage = new ChatMessage() { Role = "user", Content = inputText };
        if(messages.Count == 0) { newMessage.Content = prompt + "\n" + inputText; }
        messages.Add(newMessage);
        
        var completionResponse = 
            await openai.CreateChatCompletion(new CreateChatCompletionRequest()
            {
                Model = "gpt-3.5-turbo-0301", 
                Messages = messages
            });

        if(completionResponse.Choices != null && completionResponse.Choices.Count > 0)
        {
            var message = completionResponse.Choices[0].Message;
            message.Content = message.Content.Trim();
            messages.Add(message);
            return message.Content;
        }
        else
        {
            Debug.LogWarning("No text was generated from this prompt.");
            return "";
        }
    }
}
