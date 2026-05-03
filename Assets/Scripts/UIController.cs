using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public Slider _musicSlider, _sfxSlider;

    void OnEnable()
    {
        // pega valores salvos
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // atualiza os sliders
        _musicSlider.value = musicVol;
        _sfxSlider.value = sfxVol;

        // aplica no áudio
        AudioManager.Instance.SetMusicVolume(musicVol);
        AudioManager.Instance.SetSFXVolume(sfxVol);
    }

    public void MusicVolume()
    {
        AudioManager.Instance.SetMusicVolume(_musicSlider.value);
    }

    public void SFXVolume()
    {
        AudioManager.Instance.SetSFXVolume(_sfxSlider.value);
    }
}