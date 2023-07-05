using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioVisualizer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    // The sphere to scale according to the audio volume
    [SerializeField] private Transform sphere;

    // The minimum and maximum scale of the sphere
    public float minScale = 0.5f;
    public float maxScale = 2f;

    // The current volume of the audio source
    private float volume;

    // Update is called once per frame
    void Update()
    {
        // Get the current volume of the audio source using RMS (root mean square)
        float[] samples = new float[256];
        audioSource.GetOutputData(samples, 0);
        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        volume = Mathf.Sqrt(sum / samples.Length);

        // Scale the sphere according to the volume, using a linear mapping
        float scale = Mathf.Lerp(minScale, maxScale, volume);
        sphere.localScale = new Vector3(scale, scale, scale);
    }
}
