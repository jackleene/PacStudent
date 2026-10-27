using UnityEngine;
using UnityEngine.InputSystem;

public class PacStudentController : MonoBehaviour
{
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private RoundController round;
    [SerializeField, Min(0.01f)] private float speed = 5f;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource moveAudio;
    [SerializeField] private AudioClip moveClip;
    [SerializeField] private AudioClip pelletClip;
    [SerializeField] private ParticleSystem movementDust;

    private Key lastInput = Key.None;
    private Key currentInput = Key.None;
    private Vector2Int currentCell;
    private Vector2Int targetCell;
    private Vector3 moveStart;
    private Vector3 moveEnd;
    private float moveElapsed;
    private bool isLerping;
    private Key animationDirection = Key.D;

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
        if (round != null && !round.IsPlaying)
        {
            SetMovementFeedback(false);
            return;
        }

        ReadInput();
        Move(Time.deltaTime);
        SetMovementFeedback(isLerping);
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
            transform.position = Vector3.Lerp(moveStart, moveEnd, moveElapsed / duration);

            if (moveElapsed >= duration)
            {
                transform.position = moveEnd;
                currentCell = targetCell;
                isLerping = false;
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
        Vector2Int direction = GetDirection(lastInput);
        if (direction != Vector2Int.zero && levelGenerator.IsWalkable(currentCell + direction))
        {
            currentInput = lastInput;
        }
        else
        {
            direction = GetDirection(currentInput);
            if (direction == Vector2Int.zero || !levelGenerator.IsWalkable(currentCell + direction))
            {
                return false;
            }
        }

        targetCell = currentCell + direction;
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
