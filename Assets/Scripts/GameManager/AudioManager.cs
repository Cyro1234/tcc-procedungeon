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
    SincronizarSliders(musicVol, sfxVol);
}
    }
    // fix slider sfx e musica
    private void SincronizarSliders(float musicVol, float sfxVol)
    {
        foreach (var slider in FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var evento = slider.onValueChanged;
            for (int i = 0; i < evento.GetPersistentEventCount(); i++)
            {
                if (evento.GetPersistentTarget(i) != this) continue;
                string metodo = evento.GetPersistentMethodName(i);
                if (metodo == nameof(SetMusicVolume) || metodo == nameof(MusicVolume))
                    slider.SetValueWithoutNotify(musicVol);
                else if (metodo == nameof(SetSFXVolume) || metodo == nameof(SFXVolume))
                    slider.SetValueWithoutNotify(sfxVol);
            }
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

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
    }

    public AudioClip ObterSFX(string name)
    {
        Sound s = Array.Find(sfxSounds, x => x.name == name);
        return s != null ? s.clip : null;
    }

    // Scroller do áudio da música/sfx no menu de pause
    public void MusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
    }

    public void SFXVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = volume;
        }
    }

    // salva a opção de volume do usuário toda vez que o jogo é reiniciado
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = volume;
            PlayerPrefs.SetFloat("MusicVolume", volume);
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = volume;
            PlayerPrefs.SetFloat("SFXVolume", volume);
        }
    }
}
    