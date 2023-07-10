using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using LMNT;
using UnityEngine.Networking;

public class TextToSpeech
{
    private List<Voice> voiceList;
    private string apiKey;

    public TextToSpeech()
    {
        apiKey = LMNTLoader.LoadApiKey();
        voiceList = LMNTLoader.LoadVoices();
    }

    public async Task Speak(string text, string voice, AudioSource audioSource)
    {
        WWWForm form = new WWWForm();
        form.AddField("voice", LookupByName(voice));
        form.AddField("text", text);

        UnityWebRequest request = UnityWebRequest.Post(Constants.LMNT_SYNTHESIZE_URL, form);
        DownloadHandlerAudioClip handler = 
            new DownloadHandlerAudioClip(Constants.LMNT_SYNTHESIZE_URL, AudioType.WAV);
        request.SetRequestHeader("X-API-Key", apiKey);
        // TODO: do not hard-code; find a clean way to get package version at runtime
        request.SetRequestHeader("X-Client", "unity/0.1.0");
        request.downloadHandler = handler;
        request.SendWebRequest();

        await RequestIsDone(request);
        audioSource.clip = handler.audioClip;
        audioSource.Play();
    }

    private async Task RequestIsDone(UnityWebRequest request)
    {
        while(!request.isDone)
        {
            await Task.Delay(2);
        }
    }

    private string LookupByName(string name) 
    {
        return voiceList.Find(v => v.name == name).id;
    }
}
