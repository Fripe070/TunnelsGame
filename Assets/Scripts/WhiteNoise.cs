#region

using UnityEngine;
using Random = System.Random;

#endregion

[RequireComponent(typeof(AudioSource))]
public class WhiteNoise : MonoBehaviour
{
    private AudioSource _audioSource;
    private Random _random;

    void Start()
    {
        _random = new Random();
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = AudioClip.Create("WhiteNoise", 44100 * 2, 1, 44100, true, OnAudioRead);
        _audioSource.loop = true;
        _audioSource.Play();
    }

    void OnAudioRead(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (float)_random.NextDouble();
        }
    }
}
