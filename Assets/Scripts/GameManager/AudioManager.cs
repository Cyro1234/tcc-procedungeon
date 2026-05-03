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
        LoadVolume();
        PlayMusic("Fase1");

        void LoadVolume()
{
    float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
    float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

    musicSource.volume = musicVol;
    sfxSource.volume = sfxVol;
}
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
    
    // adicionar toggle de music e sfx depois

    // Scroller do áudio da música/sfx no menu de pause
    public void MusicVolume(float volume)
    {
        musicSource.volume = volume;
    }
    public void SFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }

    // salva a opção de volume do usuário toda vez que o jogo é reiniciado
    public void SetMusicVolume(float volume)
    {
        musicSource.volume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }
}
    