using UnityEngine;
using UnityEngine.InputSystem;

public class PacStudentController : MonoBehaviour
{
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private RoundController round;
    [SerializeField] private PacStudentInteractions interactions;
    [SerializeField, Min(0.01f)] private float speed = 5f;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource moveAudio;
    [SerializeField] private AudioClip moveClip;
    [SerializeField] private AudioClip pelletClip;
    [SerializeField] private ParticleSystem movementDust;
    [SerializeField] private AnimationClip deathAnimation;
    [SerializeField] private AudioSource deathAudio;
    [SerializeField] private ParticleSystem deathParticles;

    public float DeathDuration => deathAnimation.length;
    public float MoveSpeed => speed;
    public Vector2Int FacingDirection => GetDirection(isLerping ? currentInput : animationDirection);

    private Key lastInput = Key.None;
    private Key currentInput = Key.None;
    private Vector2Int currentCell;
    private Vector2Int targetCell;
    private Vector3 moveStart;
    private Vector3 moveEnd;
    private float moveElapsed;
    private bool isLerping;
    private Key animationDirection = Key.D;
    private Vector3 spawnPosition;
    private bool isDead;

    private void Start()
    {
        if (levelGenerator == null || speed <= 0f)
        {
            Debug.LogError("PacStudent needs a LevelGenerator and a positive move speed.", this);
            enabled = false;
            return;
        }

        currentCell = Vector2Int.RoundToInt(transform.position);
        transform.position = new Vector3(currentCell.x, currentCell.y, transform.position.z);
        spawnPosition = transform.position;

        if (animator != null)
        {
            animator.enabled = true;
            animator.Play("PacStudent_Right", 0, 0f);
            animator.Update(0f);
            animator.speed = 0f;
        }
        SetMovementFeedback(false);
    }

    private void Update()
    {
        // Movement feedback must not freeze or overwrite the death animation.
        if (isDead) return;
        if (round != null && !round.IsPlaying)
        {
            SetMovementFeedback(false);
            return;
        }

        ReadInput();
        Move(Time.deltaTime);
        if (isDead) return;
        SetMovementFeedback(isLerping);
    }

    public void BeginDeath()
    {
        if (isDead) return;
        isDead = true;
        isLerping = false;
        lastInput = currentInput = Key.None;
        SetMovementFeedback(false);
        movementDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        animator.speed = 1f;
        animator.Play("PacStudent_Dead", 0, 0f);
        animator.Update(0f);
        deathParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        deathParticles.Play();
        // Start both together and match the sound's duration to the animation.
        deathAudio.pitch = deathAudio.clip.length / DeathDuration;
        deathAudio.Play();
    }

    public void StopMovement()
    {
        isLerping = false;
        lastInput = currentInput = Key.None;
        // The final-life death animation still finishes beneath the Game Over overlay.
        if (isDead) return;
        SetMovementFeedback(false);
        movementDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void Respawn()
    {
        deathAudio.Stop();
        deathParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        transform.position = spawnPosition;
        currentCell = targetCell = Vector2Int.RoundToInt(spawnPosition);
        moveStart = moveEnd = spawnPosition;
        moveElapsed = 0f;
        isLerping = false;
        lastInput = currentInput = Key.None;
        animationDirection = Key.D;
        interactions.ClearWallContact();
        animator.Play("PacStudent_Right", 0, 0f);
        animator.Update(0f);
        isDead = false;
        SetMovementFeedback(false);
    }

    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.wKey.wasPressedThisFrame) lastInput = Key.W;
        if (keyboard.aKey.wasPressedThisFrame) lastInput = Key.A;
        if (keyboard.sKey.wasPressedThisFrame) lastInput = Key.S;
        if (keyboard.dKey.wasPressedThisFrame) lastInput = Key.D;
    }

    private void Move(float deltaTime)
    {
        float remainingTime = deltaTime;
        float duration = 1f / speed;

        while (remainingTime > 0f)
        {
            if (!isLerping && !TryStartMove())
            {
                return;
            }

            float step = Mathf.Min(remainingTime, duration - moveElapsed);
            moveElapsed += step;
            remainingTime -= step;
            Vector3 previousPosition = transform.position;
            transform.position = Vector3.Lerp(moveStart, moveEnd, moveElapsed / duration);
            if (interactions != null) interactions.CollectAlongPath(previousPosition, transform.position);
            // A swept contact can start death inside this segment. Do not finish
            // the old lerp or consume the rest of a long frame after it.
            if (isDead || (round != null && !round.IsPlaying)) return;

            if (moveElapsed >= duration)
            {
                transform.position = moveEnd;
                currentCell = targetCell;
                isLerping = false;
                if (levelGenerator.TryGetTunnelExit(currentCell, GetDirection(currentInput), out Vector2Int exit))
                {
                    currentCell = exit;
                    transform.position = new Vector3(exit.x, exit.y, transform.position.z);
                    // Keep the input buffer and direction; it now points inward.
                    // Never sweep the teleport jump through the middle of the map.
                }
            }

            // Carry unused frame time into the next tile to keep the same speed
            // at different frame rates, without skipping any grid decisions.
        }

        // Start the next segment at the boundary so feedback stays continuous
        // while travelling along a corridor, even on exact-duration frames.
        if (!isLerping && deltaTime > 0f)
        {
            TryStartMove();
        }
    }

    private void SetMovementFeedback(bool moving)
    {
        if (animator != null)
        {
            if (moving && animationDirection != currentInput)
            {
                animationDirection = currentInput;
                switch (currentInput)
                {
                    case Key.W: animator.Play("PacStudent_Up", 0, 0f); break;
                    case Key.A: animator.Play("PacStudent_Left", 0, 0f); break;
                    case Key.S: animator.Play("PacStudent_Down", 0, 0f); break;
                    case Key.D: animator.Play("PacStudent_Right", 0, 0f); break;
                }
            }
            animator.speed = moving ? 1f : 0f;
        }

        if (moveAudio != null)
        {
            if (moving)
            {
                AudioClip clip = levelGenerator.HasPellet(targetCell) ? pelletClip : moveClip;
                if (moveAudio.clip != clip)
                {
                    moveAudio.Stop();
                    moveAudio.clip = clip;
                }
                moveAudio.loop = true;
                if (!moveAudio.isPlaying && clip != null) moveAudio.Play();
            }
            else if (moveAudio.isPlaying)
            {
                moveAudio.Stop();
            }
        }

        if (movementDust != null)
        {
            if (moving && !movementDust.isPlaying) movementDust.Play();
            else if (!moving && movementDust.isPlaying)
                movementDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void OnDisable()
    {
        SetMovementFeedback(false);
    }

    private bool TryStartMove()
    {
        Vector2Int requestedDirection = GetDirection(lastInput);
        Vector2Int direction = requestedDirection;
        if (direction != Vector2Int.zero && levelGenerator.IsWalkable(currentCell + direction))
        {
            currentInput = lastInput;
        }
        else
        {
            direction = GetDirection(currentInput);
            if (direction == Vector2Int.zero || !levelGenerator.IsWalkable(currentCell + direction))
            {
                Vector2Int blockedDirection = direction != Vector2Int.zero ? direction : requestedDirection;
                if (interactions != null && blockedDirection != Vector2Int.zero)
                    interactions.HitWall(levelGenerator.GetWall(currentCell + blockedDirection));
                return false;
            }
        }

        targetCell = currentCell + direction;
        if (interactions != null) interactions.ClearWallContact();
        moveStart = transform.position;
        moveEnd = new Vector3(targetCell.x, targetCell.y, moveStart.z);
        moveElapsed = 0f;
        isLerping = true;
        return true;
    }

    private static Vector2Int GetDirection(Key key)
    {
        switch (key)
        {
            case Key.W: return Vector2Int.up;
            case Key.A: return Vector2Int.left;
            case Key.S: return Vector2Int.down;
            case Key.D: return Vector2Int.right;
            default: return Vector2Int.zero;
        }
    }
}
