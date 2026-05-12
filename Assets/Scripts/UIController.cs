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

        // aplica no áudio e faz verificação de segurança
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(musicVol);
            AudioManager.Instance.SetSFXVolume(sfxVol);
        }
    }

    public void MusicVolume()
    {
        // verifica se o AudioManager existe antes de tentar mudar o volume
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(_musicSlider.value);
        }
    }

    public void SFXVolume()
    {
        // mesma coisa para os efeitos sonoros
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(_sfxSlider.value);
        }
    }
}