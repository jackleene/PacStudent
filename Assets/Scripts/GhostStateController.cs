using UnityEngine;

public enum GhostState { Normal, Scared, Recovering, Dead }

[RequireComponent(typeof(Animator), typeof(CircleCollider2D))]
public class GhostStateController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public GhostState State { get; private set; }
    private double deadUntil;
    private Vector3 spawnPosition;

    private static readonly int NormalAnimation = Animator.StringToHash("Ghost1_Right");
    private static readonly int ScaredAnimation = Animator.StringToHash("Ghost_Scared_Right");
    private static readonly int RecoveringAnimation = Animator.StringToHash("Ghost_Recovering_Right");
    private static readonly int DeadAnimation = Animator.StringToHash("Ghost_Dead");

    private void Awake()
    {
        spawnPosition = transform.position;
        State = GhostState.Normal;
        animator.Play(NormalAnimation, 0, 0f);
    }

    public void ResetToSpawn()
    {
        transform.position = spawnPosition;
        deadUntil = 0d;
        State = GhostState.Normal;
        animator.Play(NormalAnimation, 0, 0f);
    }

    public void ApplyGlobalState(GhostState globalState, double now)
    {
        // Each ghost keeps its own three-second death timer, even if another
        // power pill resets the global timer or the global timer expires.
        if (State == GhostState.Dead && now < deadUntil) return;
        SetState(globalState);
    }

    public bool TryEat(double now)
    {
        if (State != GhostState.Scared && State != GhostState.Recovering) return false;
        deadUntil = now + 3d;
        SetState(GhostState.Dead);
        return true;
    }

    private void SetState(GhostState state)
    {
        if (State == state) return;
        State = state;
        int animation = state switch
        {
            GhostState.Scared => ScaredAnimation,
            GhostState.Recovering => RecoveringAnimation,
            GhostState.Dead => DeadAnimation,
            _ => NormalAnimation
        };
        // All four ghosts share these state names through Animator overrides.
        animator.Play(animation, 0, 0f);
    }
}
