using System.Collections;
using UnityEngine;

public class RoundController : MonoBehaviour
{
    [SerializeField] private HUDController hud;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private Transform characters;
    [SerializeField] private GhostStateController[] ghosts;

    public bool IsPlaying { get; private set; }
    public double ElapsedSeconds { get; private set; }
    public int Score { get; private set; }
    public double GhostSecondsRemaining => System.Math.Max(0d, scaredUntil - Time.timeAsDouble);

    private Collider2D[] characterColliders;
    private bool[] colliderStates;
    private double playStartedAt;
    private double scaredUntil;

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
        hud.SetScore(0);
        hud.SetGhostTime(0d);
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
        RefreshGhostStates();
    }

    public void ActivatePowerPill()
    {
        if (!IsPlaying) return;
        scaredUntil = Time.timeAsDouble + 10d;
        RefreshGhostStates();
    }

    public bool TryEatGhost(GhostStateController ghost)
    {
        if (!IsPlaying) return false;
        // Resolve expired timers before evaluating a collision in this frame.
        RefreshGhostStates();
        if (!ghost.TryEat(Time.timeAsDouble)) return false;
        AddScore(300);
        RefreshGhostStates();
        return true;
    }

    private void RefreshGhostStates()
    {
        double now = Time.timeAsDouble;
        double remaining = GhostSecondsRemaining;
        GhostState globalState = remaining <= 0d ? GhostState.Normal
            : remaining <= 3d ? GhostState.Recovering : GhostState.Scared;
        bool anyDead = false;
        foreach (GhostStateController ghost in ghosts)
        {
            ghost.ApplyGlobalState(globalState, now);
            anyDead |= ghost.State == GhostState.Dead;
        }
        hud.SetGhostTime(remaining);
        // A dead ghost takes music priority until every dead ghost has recovered.
        if (anyDead) audioManager.PlayDeadMusic();
        else if (remaining > 0d) audioManager.PlayScaredMusic();
        else audioManager.PlayNormalMusic();
    }

    public void AddScore(int points)
    {
        if (!IsPlaying || points <= 0) return;
        Score += points;
        hud.SetScore(Score);
    }
}
