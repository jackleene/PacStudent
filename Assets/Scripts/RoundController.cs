using System.Collections;
using UnityEngine;

public class RoundController : MonoBehaviour
{
    [SerializeField] private HUDController hud;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private Transform characters;
    [SerializeField] private GhostStateController[] ghosts;
    [SerializeField] private PacStudentController player;
    [SerializeField] private LevelGenerator levelGenerator;

    public bool IsPlaying { get; private set; }
    public double ElapsedSeconds { get; private set; }
    public int Score { get; private set; }
    public int Lives { get; private set; } = 3;
    public bool IsDying { get; private set; }
    public bool IsGameOver { get; private set; }
    public double GhostSecondsRemaining => System.Math.Max(0d, scaredUntil - Time.timeAsDouble);

    private Collider2D[] characterColliders;
    private bool[] colliderStates;
    private double playStartedAt;
    private double scaredUntil;
    private bool roundStarted;

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
        hud.SetLives(Lives);
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
        roundStarted = true;
        IsPlaying = true;
    }

    private void Update()
    {
        // Ordinary deaths remain part of the round time; Game Over freezes it.
        if (roundStarted && !IsGameOver)
        {
            ElapsedSeconds = Time.timeAsDouble - playStartedAt;
            hud.SetGameTime(ElapsedSeconds);
        }
        if (!IsPlaying) return;
        RefreshGhostStates();
    }

    public void KillPlayer()
    {
        if (!IsPlaying || Lives <= 0) return;
        IsPlaying = false;
        IsDying = true;
        ElapsedSeconds = Time.timeAsDouble - playStartedAt;
        hud.SetGameTime(ElapsedSeconds);
        Lives--;
        hud.SetLives(Lives);
        foreach (Collider2D characterCollider in characterColliders)
            if (characterCollider != null) characterCollider.enabled = false;
        audioManager.StopMusic();
        player.BeginDeath();
        StartCoroutine(RespawnAfterDeath());
        if (Lives == 0) EndRound();
    }

    private IEnumerator RespawnAfterDeath()
    {
        yield return new WaitForSeconds(player.DeathDuration);
        IsDying = false;
        if (IsGameOver) yield break;

        scaredUntil = 0d;
        hud.SetGhostTime(0d);
        player.Respawn();
        foreach (GhostStateController ghost in ghosts) ghost.ResetToSpawn();
        Physics2D.SyncTransforms();
        for (int i = 0; i < characterColliders.Length; i++)
            if (characterColliders[i] != null) characterColliders[i].enabled = colliderStates[i];
        audioManager.PlayNormalMusic();
        IsPlaying = true;
    }

    public void PelletCollected(Collectible item)
    {
        if (!IsPlaying) return;
        if (levelGenerator.RemoveCollectedPellet(item) && levelGenerator.RemainingPellets == 0)
            EndRound();
    }

    private void EndRound()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        IsPlaying = false;
        ElapsedSeconds = Time.timeAsDouble - playStartedAt;
        hud.SetGameTime(ElapsedSeconds);
        foreach (Collider2D characterCollider in characterColliders)
            if (characterCollider != null) characterCollider.enabled = false;
        player.StopMovement();
        audioManager.StopMusic();
        hud.SetGhostTime(0d);
        hud.ShowGameOver();
        HighScoreRecord.SaveIfBetter(gameObject.scene.name, Score, ElapsedSeconds);
        StartCoroutine(ReturnAfterGameOver());
    }

    private IEnumerator ReturnAfterGameOver()
    {
        yield return new WaitForSeconds(3f);
        hud.ReturnToStart();
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
        if (!ghost.TryEat()) return false;
        AddScore(300);
        RefreshGhostStates();
        return true;
    }

    public void ReviveGhost(GhostStateController ghost)
    {
        if (!IsPlaying) return;
        ghost.ReviveAtSpawn(GetGlobalGhostState());
        // Keep dead music until the last returning ghost reaches its own spawn.
        RefreshGhostStates();
    }

    private GhostState GetGlobalGhostState()
    {
        double remaining = GhostSecondsRemaining;
        return remaining <= 0d ? GhostState.Normal
            : remaining <= 3d ? GhostState.Recovering : GhostState.Scared;
    }

    private void RefreshGhostStates()
    {
        double remaining = GhostSecondsRemaining;
        GhostState globalState = GetGlobalGhostState();
        bool anyDead = false;
        foreach (GhostStateController ghost in ghosts)
        {
            ghost.ApplyGlobalState(globalState);
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
