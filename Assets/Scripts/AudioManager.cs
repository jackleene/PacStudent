using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioClip scaredMusic;
    [SerializeField] private AudioClip deadMusic;

    public void PlayIntroMusic()
    {
        PlayMusic(introMusic);
    }

    public void PlayNormalMusic()
    {
        PlayMusic(normalMusic);
    }

    public void PlayScaredMusic()
    {
        PlayMusic(scaredMusic);
    }

    public void PlayDeadMusic()
    {
        PlayMusic(deadMusic);
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }

    private void PlayMusic(AudioClip clip)
    {
        if (audioSource.clip == clip && audioSource.isPlaying) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }
}
