using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip normalMusic;

    private void Start()
    {
        StartCoroutine(PlayIntroThenNormalMusic());
    }

    private IEnumerator PlayIntroThenNormalMusic()
    {
        audioSource.clip = introMusic;
        audioSource.loop = false;
        audioSource.Play();

        float timer = 0f;

        while (audioSource.isPlaying && timer < 3f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        audioSource.Stop();

        audioSource.clip = normalMusic;
        audioSource.loop = true;
        audioSource.Play();
    }
}