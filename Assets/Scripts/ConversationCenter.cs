using UnityEngine;
using OpenAI;
using System.Collections.Generic;
using System.Threading.Tasks;
using LMNT;

public class ConversationCenter : MonoBehaviour
{
    [SerializeField] private string voice = "Olivia";
    [SerializeField] private string testText = "test test test";
    [SerializeField] private AudioSource audioSource;
    private OpenAIApi openai = new OpenAIApi();
    private SpeechRecognition speechRecognition = new SpeechRecognition();
    private CustomTTS customTTS;
    private List<ChatMessage> messages = new List<ChatMessage>();
    private string prompt = 
        @"Act as a random stranger in a chat room and reply to the questions. 
        Don't break character. Don't ever mention that you are an AI model.";

    private void Start()
    {
        customTTS = new CustomTTS();
    }

    private async void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            await customTTS.Speak(testText, voice, audioSource);
        }
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if(!speechRecognition.IsRecording)
            {
                speechRecognition.StartRecording();
            }
            else
            {
                speechRecognition.EndRecording();
            }
        }

        if(Input.GetKeyDown(KeyCode.Return))
        {
            await TranscribeAndReply();
        }
    }

    private async Task TranscribeAndReply()
    {
        string transcription = await speechRecognition.GetTranscription();
        Debug.Log(transcription);
        string reply = await GetReply(transcription);
        Debug.Log(reply);
        await customTTS.Speak(reply, voice, audioSource);
    }
    
    private async Task<string> GetReply(string inputText)
    {
        var newMessage = new ChatMessage() { Role = "user", Content = inputText };
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
