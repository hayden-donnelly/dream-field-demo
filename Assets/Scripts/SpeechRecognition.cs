using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using OpenAI;

public class SpeechRecognition
{
    private OpenAIApi openai = new OpenAIApi();
    private string deviceName;
    private AudioClip clip;
    public bool IsRecording { get; private set; }
    public bool MicrophoneInitialized { get; private set; }
    private int startPosition;
    private float[] samples;

    public void InitializeMicrophone()
    {
        if(MicrophoneInitialized) { return; }
        deviceName = Microphone.devices[0];
        clip = Microphone.Start(deviceName, true, 10, 44100);
        MicrophoneInitialized = true;
    }

    public void StartRecording()
    {
        if(!MicrophoneInitialized)
        {
            Debug.LogWarning("Tried to start recording but microphone is not initialized.");
            return;
        }
        IsRecording = true;
        startPosition = Microphone.GetPosition(null);
        Debug.Log("Recording from " + deviceName);
    }

    public void EndRecording()
    {
        if(!MicrophoneInitialized)
        {
            Debug.LogWarning("Tried to end recording but microphone is not initialized.");
            return;
        }
        IsRecording = false;
        int endPosition = Microphone.GetPosition(null);
        samples = new float[(endPosition - startPosition) * clip.channels];
        clip.GetData(samples, startPosition);
    }

    public async Task<string> GetTranscription()
    {
        byte[] bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
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
