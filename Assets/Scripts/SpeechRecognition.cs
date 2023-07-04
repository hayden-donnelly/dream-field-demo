using UnityEngine;
using System.IO;
//using HuggingFace.API;
using System.Threading.Tasks;
using OpenAI;

public class SpeechRecognition
{
    private OpenAIApi openai = new OpenAIApi();
    private string deviceName;
    private AudioClip clip;
    byte[] bytes;
    public bool IsRecording { get; private set; }

    public void StartRecording()
    {
        deviceName = Microphone.devices[0];
        Debug.Log("Recording from " + deviceName);
        clip = Microphone.Start(deviceName, true, 10, 44100);
        IsRecording = true;
    }

    public void EndRecording()
    {
        var position = Microphone.GetPosition(null);
        Microphone.End(deviceName);
        var samples = new float[position * clip.channels];
        clip.GetData(samples, 0);
        bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
        IsRecording = false;
    }

    /*public string GetTranscription()
    {
        string apiResponse = "";
        HuggingFaceAPI.AutomaticSpeechRecognition(bytes, response => {
            apiResponse = response;
        }, error => { 
            Debug.Log(error); 
            apiResponse = error;
        });
        return apiResponse;
    }*/

    public async Task<string> GetTranscription()
    {
        var request = new CreateAudioTranscriptionsRequest
        {
            FileData = new FileData() {Data = bytes, Name = "audio.wav"},
            Model = "whisper-1",
            Language = "en"
        };
        var response = await openai.CreateAudioTranscription(request);
        return response.Text;
    }
    
    private byte[] EncodeAsWAV(float[] samples, int frequency, int channels) 
    {
        using (var memoryStream = new MemoryStream(44 + samples.Length * 2)) 
        {
            using (var writer = new BinaryWriter(memoryStream)) 
            {
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples.Length * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);
                writer.Write("data".ToCharArray());
                writer.Write(samples.Length * 2);

                foreach (var sample in samples) 
                {
                    writer.Write((short)(sample * short.MaxValue));
                }
            }
            return memoryStream.ToArray();
        }
    }
}
