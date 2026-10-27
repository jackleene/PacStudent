using System.Collections.Generic;
using UnityEngine;

// Cached grid decisions for ghost 4. No Unity pathfinding or per-frame searches.
public sealed class GhostPatrolRoute
{
    private static readonly Vector2Int[] Directions =
        { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    private readonly Dictionary<(Vector2Int cell, Vector2Int incoming), (Vector2Int next, int distance)> routes =
        new Dictionary<(Vector2Int, Vector2Int), (Vector2Int, int)>();

    public GhostPatrolRoute(LevelGenerator level)
    {
        var pending = new Queue<(Vector2Int cell, Vector2Int incoming)>();
        // The upper-left corridor touches the outside wall. Keeping that wall on
        // the left traces its clockwise boundary, including its recesses.
        var step = (cell: new Vector2Int(1, -1), incoming: Vector2Int.up);
        while (!routes.ContainsKey(step))
        {
            Vector2Int next = FollowWall(level, step.cell, step.incoming);
            if (next == Vector2Int.zero) break;
            routes.Add(step, (next, 0));
            pending.Enqueue(step);
            step = (step.cell + next, next);
        }

        // Work backwards from the directed patrol loop. Each other cell/heading
        // receives a route back to it without reversing at an open junction.
        // This also handles returning to Normal after fleeing into inner corridors.
        while (pending.Count > 0)
        {
            step = pending.Dequeue();
            Vector2Int previousCell = step.cell - step.incoming;
            if (!level.IsGhostWalkable(previousCell, false)) continue;
            foreach (Vector2Int incoming in Directions)
            {
                var previous = (cell: previousCell, incoming);
                if (routes.ContainsKey(previous) || !level.IsGhostWalkable(previousCell - incoming, false)) continue;
                if (step.incoming == -incoming && HasForwardExit(level, previousCell, incoming)) continue;
                routes.Add(previous, (step.incoming, routes[step].distance + 1));
                pending.Enqueue(previous);
            }
        }
    }

    public Vector2Int ChooseDirection(Vector2Int cell, Vector2Int incoming, Vector2Int[] choices, int count)
    {
        if (routes.TryGetValue((cell, incoming), out var route)) return route.next;

        // The initial heading just outside the spawn gate comes from a cell
        // ordinary corridor movement cannot enter. Join via a legal next step.
        Vector2Int best = choices[0];
        int shortest = int.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (routes.TryGetValue((cell + choices[i], choices[i]), out route) && route.distance < shortest)
            {
                shortest = route.distance;
                best = choices[i];
            }
        }
        return best;
    }

    private static Vector2Int FollowWall(LevelGenerator level, Vector2Int cell, Vector2Int incoming)
    {
        Vector2Int left = new Vector2Int(-incoming.y, incoming.x);
        if (level.IsGhostWalkable(cell + left, false)) return left;
        if (level.IsGhostWalkable(cell + incoming, false)) return incoming;
        if (level.IsGhostWalkable(cell - left, false)) return -left;
        if (level.IsGhostWalkable(cell - incoming, false)) return -incoming;
        return Vector2Int.zero;
    }

    private static bool HasForwardExit(LevelGenerator level, Vector2Int cell, Vector2Int incoming)
    {
        foreach (Vector2Int candidate in Directions)
            if (candidate != -incoming && level.IsGhostWalkable(cell + candidate, false)) return true;
        return false;
    }
}
