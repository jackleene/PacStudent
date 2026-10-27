using System.Collections;
using UnityEngine;

public class RoundController : MonoBehaviour
{
    [SerializeField] private HUDController hud;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private Transform characters;

    public bool IsPlaying { get; private set; }
    public double ElapsedSeconds { get; private set; }

    private Collider2D[] characterColliders;
    private bool[] colliderStates;
    private double playStartedAt;

    private void Awake()
    {
        // Cache the authored states so countdown gating also covers colliders
        // added by the later collision stage, without enabling disabled ones.
        characterColliders = characters.GetComponentsInChildren<Collider2D>(true);
        colliderStates = new bool[characterColliders.Length];
        for (int i = 0; i < characterColliders.Length; i++)
        {
            colliderStates[i] = characterColliders[i].enabled;
            characterColliders[i].enabled = false;
        }
        hud.SetGameTime(0d);
        hud.ShowCountdown("3");
    }

    private IEnumerator Start()
    {
        audioManager.PlayIntroMusic();
        yield return new WaitForSeconds(1f);
        hud.ShowCountdown("2");
        yield return new WaitForSeconds(1f);
        hud.ShowCountdown("1");
        yield return new WaitForSeconds(1f);
        hud.ShowCountdown("GO!");
        yield return new WaitForSeconds(1f);

        hud.HideCountdown();
        audioManager.PlayNormalMusic();
        for (int i = 0; i < characterColliders.Length; i++)
        {
            if (characterColliders[i] != null) characterColliders[i].enabled = colliderStates[i];
        }
        playStartedAt = Time.timeAsDouble;
        IsPlaying = true;
    }

    private void Update()
    {
        if (!IsPlaying) return;
        ElapsedSeconds = Time.timeAsDouble - playStartedAt;
        hud.SetGameTime(ElapsedSeconds);
    }
}
