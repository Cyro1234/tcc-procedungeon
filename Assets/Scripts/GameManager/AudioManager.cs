using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private AudioSource musicSource;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip gameOverMusic;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        musicSource = GetComponent<AudioSource>();
        PlayGameplayMusic();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void PlayGameplayMusic()
    {
        if (musicSource.clip == gameplayMusic && musicSource.isPlaying) return;

        musicSource.clip = gameplayMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayGameOverMusic()
    {
        if (musicSource.clip == gameOverMusic && musicSource.isPlaying) return;

        musicSource.clip = gameOverMusic;
        musicSource.loop = false;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
}
