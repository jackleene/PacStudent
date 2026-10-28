using System.Collections.Generic;
using UnityEngine;

// Wired only in InnovationScene. The existing motor still owns movement and states.
public class CooperativeGhostNavigation : MonoBehaviour
{
    [SerializeField] private LevelGenerator level;
    [SerializeField] private RoundController round;
    [SerializeField] private PacStudentController player;
    [SerializeField] private GhostController[] ghosts;
    [SerializeField, Min(0f)] private float overlapPenalty = 2f;
    [SerializeField, Min(0.1f)] private float replanInterval = 0.5f;

    private GhostPathfinder pathfinder;
    private List<Vector2Int>[] paths;
    private int[] order;
    private float[] priorities;
    private bool[] participants;
    private readonly Dictionary<Vector2Int, int> reservations = new Dictionary<Vector2Int, int>();
    private readonly List<Vector2Int> shortestPath = new List<Vector2Int>();
    private Vector2Int plannedTarget;
    private double nextPlanTime;
    private bool dirty = true;

    private void Awake()
    {
        var cells = new List<Vector2Int>();
        Bounds bounds = level.LevelBounds;
        for (int y = Mathf.CeilToInt(bounds.min.y); y <= Mathf.FloorToInt(bounds.max.y); y++)
            for (int x = Mathf.CeilToInt(bounds.min.x); x <= Mathf.FloorToInt(bounds.max.x); x++)
            {
                var cell = new Vector2Int(x, y);
                if (level.IsGhostWalkable(cell, false)) cells.Add(cell);
            }
        pathfinder = new GhostPathfinder(cells);
        paths = new List<Vector2Int>[ghosts.Length];
        order = new int[ghosts.Length];
        priorities = new float[ghosts.Length];
        participants = new bool[ghosts.Length];
        for (int i = 0; i < paths.Length; i++) paths[i] = new List<Vector2Int>();
    }

    public void Invalidate() => dirty = true;

    public bool TryChooseDirection(GhostController ghost, Vector2Int[] choices, int count, out Vector2Int direction)
    {
        direction = Vector2Int.zero;
        if (!round.IsPlaying || !ghost.CanCoordinate) return false;
        int index = System.Array.IndexOf(ghosts, ghost);
        if (index < 0) return false;
        Vector2Int target = Vector2Int.RoundToInt(player.transform.position);
        for (int i = 0; i < ghosts.Length; i++)
            if (participants[i] != ghosts[i].CanCoordinate) dirty = true;
        if (dirty || target != plannedTarget || Time.timeAsDouble >= nextPlanTime) Plan(target);
        if (TryReadStep(index, choices, count, out direction)) return true;
        // A long frame or reset may advance beyond a cached route. Replan all UFOs
        // together, retaining nearest-first priority regardless of Update order.
        Plan(target);
        return TryReadStep(index, choices, count, out direction);
    }

    private bool TryReadStep(int index, Vector2Int[] choices, int count, out Vector2Int direction)
    {
        direction = Vector2Int.zero;
        List<Vector2Int> path = paths[index];
        int step = path.IndexOf(ghosts[index].PlanningCell);
        if (step < 0 || step + 1 >= path.Count) return false;
        Vector2Int candidate = path[step + 1] - path[step];
        for (int i = 0; i < count; i++)
            if (candidate == choices[i]) { direction = candidate; return true; }
        return false;
    }

    private void Plan(Vector2Int target)
    {
        plannedTarget = target;
        dirty = false;
        nextPlanTime = Time.timeAsDouble + replanInterval;
        pathfinder.SetTarget(target);
        reservations.Clear();
        int count = 0;
        for (int i = 0; i < ghosts.Length; i++)
        {
            paths[i].Clear();
            participants[i] = ghosts[i].CanCoordinate;
            if (!participants[i]) continue;
            int distance = pathfinder.DistanceToTarget(ghosts[i].PlanningCell);
            if (distance < 0) continue;
            priorities[i] = distance + ghosts[i].DistanceToPlanningCell;
            int at = count++;
            while (at > 0 && ComesBefore(i, order[at - 1]))
            {
                order[at] = order[at - 1];
                at--;
            }
            order[at] = i;
        }
        for (int rank = 0; rank < count; rank++)
        {
            int i = order[rank];
            GhostController ghost = ghosts[i];
            List<Vector2Int> path = paths[i];
            if (!pathfinder.FindPath(ghost.PlanningCell, ghost.Heading, reservations, overlapPenalty, path)) continue;
            // Avoid abandoning pursuit for a huge detour just to avoid overlap.
            pathfinder.FindPath(ghost.PlanningCell, ghost.Heading, null, 0f, shortestPath);
            if (path.Count - 1 > (shortestPath.Count - 1) * 1.75f + 4f)
            {
                path.Clear();
                path.AddRange(shortestPath);
            }
            // The common destination is deliberately free; everyone must be able to converge.
            for (int step = 1; step + 1 < path.Count; step++)
            {
                reservations.TryGetValue(path[step], out int used);
                reservations[path[step]] = used + 1;
            }
        }
    }

    private bool ComesBefore(int a, int b)
    {
        return priorities[a] < priorities[b]
            || (priorities[a] == priorities[b] && ghosts[a].GhostNumber < ghosts[b].GhostNumber);
    }
}
