using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public Sound[] musicSounds, sfxSounds; 
    public AudioSource musicSource, sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PlayMusic("Fase1");
    }


    public void PlayMusic(string name, bool loop = true)
    {
        Sound s = Array.Find(musicSounds, x => x.name == name);

        if (s == null)
        {
            Debug.Log("Music not found");
        }

        else
        {
            musicSource.clip = s.clip;
            musicSource.loop = loop;
            musicSource.Play();
        }
    }

    public void PlaySFX (string name)
    {
        Sound s = Array.Find (sfxSounds, x=> x.name == name);

        if (s == null)
        {
            Debug.Log("Sound not found");
        }

        else
        {
            sfxSource.PlayOneShot(s.clip);
        }
    }
}
