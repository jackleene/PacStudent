using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip normalMusic;

    public void PlayIntroMusic()
    {
        PlayMusic(introMusic);
    }

    public void PlayNormalMusic()
    {
        PlayMusic(normalMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }
}
