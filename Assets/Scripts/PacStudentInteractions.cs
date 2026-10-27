using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
public class PacStudentInteractions : MonoBehaviour
{
    [SerializeField] private RoundController round;
    [SerializeField] private AudioSource effectsAudio;
    [SerializeField] private AudioClip wallHitSound;
    [SerializeField] private ParticleSystem wallImpact;

    private CircleCollider2D playerCollider;
    private Collider2D contactedWall;
    private ContactFilter2D pickupFilter;
    private readonly RaycastHit2D[] pathHits = new RaycastHit2D[16];
    private readonly Collider2D[] overlapHits = new Collider2D[16];

    private bool CanInteract => isActiveAndEnabled && round != null && round.IsPlaying && playerCollider.enabled;

    private void Awake()
    {
        playerCollider = GetComponent<CircleCollider2D>();
        pickupFilter = ContactFilter2D.noFilter;
        pickupFilter.useTriggers = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Collect(other);
    }

    public void CollectAlongPath(Vector2 from, Vector2 to)
    {
        if (!CanInteract) return;
        float radius = playerCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Vector2 displacement = to - from;
        int count = Physics2D.CircleCast(from, radius, displacement.normalized, pickupFilter, pathHits, displacement.magnitude);
        for (int i = 0; i < count; i++) Collect(pathHits[i].collider);
        // Sweeps include every traversed tile even when a slow frame crosses several.
        count = Physics2D.OverlapCircle(to, radius, pickupFilter, overlapHits);
        for (int i = 0; i < count; i++) Collect(overlapHits[i]);
    }

    private void Collect(Collider2D other)
    {
        if (!CanInteract || other == null || !other.TryGetComponent(out Collectible item)) return;
        if (!item.TryCollect(out int points, out AudioClip sound)) return;
        round.AddScore(points);
        if (sound != null) effectsAudio.PlayOneShot(sound);
    }

    public void HitWall(Collider2D wall)
    {
        if (!CanInteract || wall == null || wall == contactedWall) return;
        contactedWall = wall;
        // Movement rejects the blocked tile before penetration. Use the wall's
        // axis-aligned box boundary for the effect so it matches the tile grid.
        Vector2 contact = wall.bounds.ClosestPoint(transform.position);
        wallImpact.transform.position = new Vector3(contact.x, contact.y, transform.position.z);
        wallImpact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        wallImpact.Play();
        effectsAudio.PlayOneShot(wallHitSound);
    }

    public void ClearWallContact()
    {
        contactedWall = null;
    }
}
