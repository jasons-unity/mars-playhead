using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayTone : MonoBehaviour
{

    //public AudioClip tone;
    public double frequency = 440.0;
    private double increment;
    private double phase;
    private double sampling_frequency = 48000.0;

    private float gain = 0;

    // Start is called before the first frame update
    void Start()
    {

        //GetComponent<AudioSource>().playOnAwake = false;
        //GetComponent<AudioSource>().clip = tone;
    }

    // Update is called once per frame
    void Update()
    {
        frequency = transform.position.x * 100;
    }

    private void OnTriggerEnter(Collider col)
    {
        gain = 0.5f;
    }

    private void OnTriggerExit(Collider other)
    {
        gain = 0;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        increment = frequency * 2.0 * Mathf.PI / sampling_frequency;

        for (int i = 0; i < data.Length; i += channels)
        {
            phase += increment;
            data[i] = (float) (gain * Mathf.Sin((float) phase));

            if (channels == 2)
            {
                data[i + 1] = data[i];
            }

            if (phase > (Mathf.PI * 2))
            {
                phase = 0.0;
            }
        }

        // When we have data to play
        if (!data.First().Equals(0.0f))
        {
            var startValue = data[1000];

        }
    }
}
