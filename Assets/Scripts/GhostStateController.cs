using UnityEngine;

public enum GhostState { Normal, Scared, Recovering, Dead }

[RequireComponent(typeof(Animator), typeof(CircleCollider2D))]
public class GhostStateController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public GhostState State { get; private set; }
    private double deadUntil;
    private Vector3 spawnPosition;
    private GhostController movement;
    private int facing;

    private static readonly int[] NormalAnimations = CreateAnimations("Ghost1_");
    private static readonly int[] ScaredAnimations = CreateAnimations("Ghost_Scared_");
    private static readonly int[] RecoveringAnimations = CreateAnimations("Ghost_Recovering_");
    private static readonly int DeadAnimation = Animator.StringToHash("Ghost_Dead");

    private void Awake()
    {
        spawnPosition = transform.position;
        movement = GetComponent<GhostController>();
        State = GhostState.Normal;
        PlayAnimation();
    }

    public void ResetToSpawn()
    {
        transform.position = spawnPosition;
        deadUntil = 0d;
        State = GhostState.Normal;
        facing = 0;
        PlayAnimation();
        if (movement != null) movement.ResetMovement();
    }

    public void FaceDirection(Vector2Int direction)
    {
        int next = direction == Vector2Int.up ? 3 : direction == Vector2Int.left ? 2
            : direction == Vector2Int.down ? 1 : 0;
        if (facing == next) return;
        facing = next;
        if (State != GhostState.Dead) PlayAnimation();
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
        PlayAnimation();
    }

    private void PlayAnimation()
    {
        int animation = State switch
        {
            GhostState.Scared => ScaredAnimations[facing],
            GhostState.Recovering => RecoveringAnimations[facing],
            GhostState.Dead => DeadAnimation,
            _ => NormalAnimations[facing]
        };
        // All four ghosts share these state names through Animator overrides.
        animator.Play(animation, 0, 0f);
    }

    private static int[] CreateAnimations(string prefix)
    {
        return new[] { Animator.StringToHash(prefix + "Right"), Animator.StringToHash(prefix + "Down"),
            Animator.StringToHash(prefix + "Left"), Animator.StringToHash(prefix + "Up") };
    }
}
