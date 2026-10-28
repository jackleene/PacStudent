using System.Collections.Generic;
using UnityEngine;

public class EnergyProjectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float speed = 12f;
    [SerializeField, Range(0.05f, 0.45f)] private float collisionRadius = 0.3f;
    [SerializeField] private Transform visual;

    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(64);
    private readonly HashSet<GhostStateController> defeated = new HashSet<GhostStateController>();
    private RoundController round;
    private LevelGenerator level;
    private Vector2 direction;
    private ContactFilter2D filter;
    private float age;
    private bool finished;

    public void Launch(RoundController roundController, LevelGenerator levelGenerator, Vector2Int facing)
    {
        round = roundController;
        level = levelGenerator;
        direction = facing == Vector2Int.zero ? Vector2.right : facing;
        filter = ContactFilter2D.noFilter;
        filter.useTriggers = true;
    }

    private void Update()
    {
        if (finished) return;
        if (!round.IsPlaying) { Finish(); return; }
        Advance(Time.deltaTime);
    }

    private void Advance(float deltaTime)
    {
        if (finished) return;
        float distance = speed * deltaTime;
        Vector2 from = transform.position;
        Physics2D.SyncTransforms();
        Physics2D.CircleCast(from, collisionRadius, direction, filter, hits, distance);

        float wallDistance = float.PositiveInfinity;
        foreach (RaycastHit2D hit in hits)
        {
            Vector2Int cell = Vector2Int.RoundToInt(hit.collider.transform.position);
            if (level.GetWall(cell) == hit.collider) wallDistance = Mathf.Min(wallDistance, hit.distance);
        }
        // Resolve the nearest wall first, so a long frame cannot hit a UFO behind it.
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.distance >= wallDistance || !hit.collider.TryGetComponent(out GhostStateController ghost)) continue;
            if (!defeated.Contains(ghost) && round.TryHitGhostWithProjectile(ghost)) defeated.Add(ghost);
        }

        Vector2 position = from + direction * Mathf.Min(distance, wallDistance);
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        age += deltaTime;
        visual.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(age * 12f));
        if (wallDistance <= distance || age >= 4f || !level.LevelBounds.Contains(transform.position)) Finish();
    }

    private void Finish()
    {
        finished = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
