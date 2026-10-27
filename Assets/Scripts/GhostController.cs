using UnityEngine;

[RequireComponent(typeof(GhostStateController))]
public class GhostController : MonoBehaviour
{
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private RoundController round;
    [SerializeField] private PacStudentController player;
    [SerializeField, Range(1, 4)] private int ghostNumber = 1;

    private GhostStateController ghostState;
    private PacStudentInteractions playerInteractions;
    private CircleCollider2D ghostCollider;
    private CircleCollider2D playerCollider;
    private ContactFilter2D contactFilter;
    private readonly RaycastHit2D[] contactHits = new RaycastHit2D[16];
    private readonly Vector2Int[] choices = new Vector2Int[4];
    private static readonly Vector2Int[] Directions =
        { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    private Vector2Int currentCell;
    private Vector2Int targetCell;
    private Vector2Int direction;
    private Vector2Int spawnExit;
    private Vector3 moveStart;
    private Vector3 moveEnd;
    private float moveProgress;
    private bool isMoving;
    private bool leavingSpawn;

    private void Awake()
    {
        ghostState = GetComponent<GhostStateController>();
        ghostCollider = GetComponent<CircleCollider2D>();
        playerCollider = player.GetComponent<CircleCollider2D>();
        playerInteractions = player.GetComponent<PacStudentInteractions>();
        contactFilter = ContactFilter2D.noFilter;
        contactFilter.useTriggers = true;
        spawnExit = levelGenerator.GetGhostExit(ghostNumber);
        ResetMovement();
    }

    public void ResetMovement()
    {
        currentCell = targetCell = Vector2Int.RoundToInt(transform.position);
        moveStart = moveEnd = transform.position;
        moveProgress = 0f;
        direction = Vector2Int.zero;
        isMoving = false;
        leavingSpawn = levelGenerator.IsGhostSpawnArea(currentCell);
    }

    private void Update()
    {
        // Dead ghosts retain the existing timed recovery until the return-to-spawn stage.
        if (!round.IsPlaying || ghostState.State == GhostState.Dead) return;
        Move(Time.deltaTime);
    }

    private void Move(float deltaTime)
    {
        float remainingDistance = player.MoveSpeed * 0.9f * deltaTime;
        while (remainingDistance > 0f)
        {
            if (!isMoving && !TryStartMove()) return;
            float step = Mathf.Min(remainingDistance, 1f - moveProgress);
            moveProgress += step;
            remainingDistance -= step;
            Vector3 previous = transform.position;
            transform.position = Vector3.Lerp(moveStart, moveEnd, moveProgress);
            CheckPlayerContact(previous, transform.position);
            if (!round.IsPlaying || ghostState.State == GhostState.Dead) return;

            if (moveProgress >= 1f)
            {
                transform.position = moveEnd;
                currentCell = targetCell;
                isMoving = false;
                if (leavingSpawn && currentCell == spawnExit) leavingSpawn = false;
            }
        }
    }

    private bool TryStartMove()
    {
        Vector2Int nextDirection;
        if (leavingSpawn)
        {
            // Align inside the house, then go straight through the assigned gate.
            nextDirection = currentCell.x != spawnExit.x
                ? new Vector2Int(spawnExit.x > currentCell.x ? 1 : -1, 0)
                : new Vector2Int(0, spawnExit.y > currentCell.y ? 1 : -1);
            if (!levelGenerator.IsGhostWalkable(currentCell + nextDirection, true)) return false;
        }
        else
        {
            int count = 0;
            foreach (Vector2Int candidate in Directions)
            {
                if (candidate == -direction) continue;
                if (levelGenerator.IsGhostWalkable(currentCell + candidate, false)) choices[count++] = candidate;
            }
            if (count > 0) nextDirection = choices[Random.Range(0, count)];
            else if (direction != Vector2Int.zero && levelGenerator.IsGhostWalkable(currentCell - direction, false))
                nextDirection = -direction;
            else return false;
        }

        direction = nextDirection;
        targetCell = currentCell + direction;
        moveStart = transform.position;
        moveEnd = new Vector3(targetCell.x, targetCell.y, moveStart.z);
        moveProgress = 0f;
        isMoving = true;
        ghostState.FaceDirection(direction);
        return true;
    }

    private void CheckPlayerContact(Vector3 from, Vector3 to)
    {
        if (!ghostCollider.enabled || !playerCollider.enabled) return;
        Vector2 displacement = to - from;
        if (displacement.sqrMagnitude == 0f) return;
        // Moving ghosts must also detect a stationary player during a long frame.
        Physics2D.SyncTransforms();
        float radius = ghostCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        int count = Physics2D.CircleCast(from, radius, displacement.normalized, contactFilter, contactHits, displacement.magnitude);
        for (int i = 0; i < count; i++)
        {
            if (contactHits[i].collider != playerCollider) continue;
            if (ghostState.State != GhostState.Dead)
            {
                Vector2 contact = (Vector2)from + displacement.normalized * contactHits[i].distance;
                transform.position = new Vector3(contact.x, contact.y, transform.position.z);
                // Keep the interrupted lerp consistent if the current three-second
                // death timer recovers this ghost before return-to-spawn is added.
                moveProgress = Vector3.Distance(moveStart, transform.position);
                playerInteractions.ContactGhost(ghostState);
            }
            return;
        }
    }
}
