using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WhiteNoise : MonoBehaviour
{
    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = AudioClip.Create("WhiteNoise", 44100 * 2, 1, 44100, true, OnAudioRead);
        _audioSource.loop = true;
        _audioSource.Play();
    }

    void OnAudioRead(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Random.value;
        }
    }
}
