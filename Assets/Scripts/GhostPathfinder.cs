using System;
using System.Collections.Generic;
using UnityEngine;

// A cached corridor graph. Search buffers are reused for all four UFOs.
public sealed class GhostPathfinder
{
    private static readonly Vector2Int[] Directions =
        { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    private readonly Vector2Int[] cells;
    private readonly Dictionary<Vector2Int, int> indices = new Dictionary<Vector2Int, int>();
    private readonly int[,] neighbours;
    private readonly int[] distances, pending, parents;
    private readonly float[] costs;
    private readonly bool[] open, closed;
    private int target = -1;

    public GhostPathfinder(IEnumerable<Vector2Int> walkableCells)
    {
        cells = new List<Vector2Int>(walkableCells).ToArray();
        for (int i = 0; i < cells.Length; i++) indices.Add(cells[i], i);
        neighbours = new int[cells.Length, 4];
        for (int i = 0; i < cells.Length; i++)
            for (int d = 0; d < 4; d++)
                neighbours[i, d] = indices.TryGetValue(cells[i] + Directions[d], out int next) ? next : -1;
        distances = new int[cells.Length];
        pending = new int[cells.Length];
        parents = new int[cells.Length];
        costs = new float[cells.Length];
        open = new bool[cells.Length];
        closed = new bool[cells.Length];
        Array.Fill(distances, -1);
    }

    public void SetTarget(Vector2Int cell)
    {
        Array.Fill(distances, -1);
        target = indices.TryGetValue(cell, out int index) ? index : -1;
        // A player at a tunnel endpoint is approached from the nearest legal cell.
        if (target < 0)
        {
            int nearest = int.MaxValue;
            for (int i = 0; i < cells.Length; i++)
            {
                Vector2Int delta = cells[i] - cell;
                int distance = Math.Abs(delta.x) + Math.Abs(delta.y);
                if (distance >= nearest) continue;
                nearest = distance;
                target = i;
            }
        }
        if (target < 0) return;
        int head = 0, tail = 0;
        pending[tail++] = target;
        distances[target] = 0;
        while (head < tail)
        {
            int current = pending[head++];
            for (int d = 0; d < 4; d++)
            {
                int next = neighbours[current, d];
                if (next < 0 || distances[next] >= 0) continue;
                distances[next] = distances[current] + 1;
                pending[tail++] = next;
            }
        }
    }

    public int DistanceToTarget(Vector2Int cell)
    {
        return indices.TryGetValue(cell, out int index) ? distances[index] : -1;
    }

    public bool FindPath(Vector2Int from, Vector2Int heading,
        Dictionary<Vector2Int, int> reservations, float overlapPenalty, List<Vector2Int> path)
    {
        path.Clear();
        if (target < 0 || !indices.TryGetValue(from, out int start) || distances[start] < 0) return false;
        Array.Fill(costs, float.PositiveInfinity);
        Array.Clear(open, 0, open.Length);
        Array.Clear(closed, 0, closed.Length);
        costs[start] = 0f;
        parents[start] = -1;
        open[start] = true;
        bool hasForwardExit = false;
        for (int d = 0; d < 4; d++)
            if (neighbours[start, d] >= 0 && Directions[d] != -heading) hasForwardExit = true;

        while (true)
        {
            int current = -1;
            float best = float.PositiveInfinity;
            // Small fixed maze: scanning the reusable buffer avoids queue-node allocations.
            for (int i = 0; i < cells.Length; i++)
            {
                float estimate = costs[i] + distances[i];
                if (!open[i] || estimate >= best) continue;
                current = i;
                best = estimate;
            }
            if (current < 0) return false;
            if (current == target)
            {
                for (int i = current; i >= 0; i = parents[i]) path.Add(cells[i]);
                path.Reverse();
                return true;
            }
            open[current] = false;
            closed[current] = true;
            for (int d = 0; d < 4; d++)
            {
                int next = neighbours[current, d];
                if (next < 0 || closed[next] || distances[next] < 0) continue;
                // Preserve the motor's rule: reverse only when there is no forward exit.
                if (current == start && hasForwardExit && Directions[d] == -heading) continue;
                int reserved = 0;
                if (next != target && reservations != null) reservations.TryGetValue(cells[next], out reserved);
                float cost = costs[current] + 1f + reserved * Mathf.Max(0f, overlapPenalty);
                if (cost >= costs[next]) continue;
                costs[next] = cost;
                parents[next] = current;
                open[next] = true;
            }
        }
    }
}
