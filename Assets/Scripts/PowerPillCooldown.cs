using UnityEngine;

[RequireComponent(typeof(Collectible), typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class PowerPillCooldown : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float cooldownSeconds = 50f;
    [SerializeField, Range(0f, 1f)] private float pendingAlpha = 0.1f;
    [SerializeField] private Material cooldownMaterial;
    [SerializeField] private Material arcMaterial;

    public bool IsCoolingDown { get; private set; }
    public float Progress => IsCoolingDown ? Mathf.Clamp01(elapsed / cooldownSeconds) : 1f;
    public bool CanCollect => !IsCoolingDown && round != null && round.IsPlaying;

    private RoundController round;
    private Collectible pickup;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D pickupCollider;
    private Animator animator;
    private Material originalMaterial;
    private MaterialPropertyBlock properties;
    private bool animatorWasEnabled;
    private float elapsed;
    private LineRenderer cooldownArc;
    private const int ArcSegments = 64;
    private static readonly Color ArcColor = new Color(0f, 1f, 0.9764706f, 1f);

    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int PendingAlphaId = Shader.PropertyToID("_PendingAlpha");
    private static readonly int BoundsId = Shader.PropertyToID("_SpriteBounds");
    private static readonly int SweepSignsId = Shader.PropertyToID("_SweepSigns");

    private void Awake()
    {
        pickup = GetComponent<Collectible>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        pickupCollider = GetComponent<CircleCollider2D>();
        animator = GetComponent<Animator>();
        originalMaterial = spriteRenderer.sharedMaterial;
        properties = new MaterialPropertyBlock();
        var arcObject = new GameObject("Cooldown Arc");
        arcObject.transform.SetParent(transform, false);
        cooldownArc = arcObject.AddComponent<LineRenderer>();
        cooldownArc.sharedMaterial = arcMaterial;
        cooldownArc.useWorldSpace = true;
        cooldownArc.startColor = cooldownArc.endColor = ArcColor;
        cooldownArc.numCornerVertices = 2;
        cooldownArc.numCapVertices = 2;
        cooldownArc.sortingLayerID = spriteRenderer.sortingLayerID;
        cooldownArc.sortingOrder = spriteRenderer.sortingOrder + 1;
        cooldownArc.enabled = false;
    }

    public void Initialize(RoundController controller)
    {
        if (round != null) round.PlayerRespawned -= Refresh;
        round = controller;
        if (round == null || cooldownMaterial == null || arcMaterial == null || !pickup.IsPowerPill)
        {
            Debug.LogError("A reusable power pill needs a round, cooldown and arc materials, and power-pill pickup.", this);
            enabled = false;
            return;
        }
        round.PlayerRespawned += Refresh;
    }

    internal void BeginCooldown()
    {
        IsCoolingDown = true;
        elapsed = 0f;
        pickupCollider.enabled = false;
        animatorWasEnabled = animator != null && animator.enabled;
        if (animator != null) animator.enabled = false;
        spriteRenderer.sharedMaterial = cooldownMaterial;
        Bounds bounds = spriteRenderer.sprite.bounds;
        properties.SetVector(BoundsId, new Vector4(bounds.center.x, bounds.center.y, bounds.size.x, bounds.size.y));
        Vector3 scale = transform.lossyScale;
        // Keep the reveal clockwise on screen even in the mirrored map quadrants.
        properties.SetVector(SweepSignsId, new Vector4(Mathf.Sign(scale.x), Mathf.Sign(scale.y), 0f, 0f));
        properties.SetFloat(PendingAlphaId, pendingAlpha);
        UpdateVisual();
    }

    private void Update()
    {
        if (!IsCoolingDown || round == null || !round.IsPlaying) return;
        Advance(Time.deltaTime);
    }

    private void Advance(float deltaTime)
    {
        elapsed = Mathf.Min(cooldownSeconds, elapsed + deltaTime);
        if (elapsed >= cooldownSeconds) Refresh();
        else UpdateVisual();
    }

    private void UpdateVisual()
    {
        properties.SetFloat(ProgressId, Progress);
        spriteRenderer.SetPropertyBlock(properties);
        UpdateArc();
    }

    private void UpdateArc()
    {
        float progress = Progress;
        cooldownArc.enabled = progress > 0f;
        if (!cooldownArc.enabled) return;
        Bounds bounds = spriteRenderer.bounds;
        Vector3 scale = transform.lossyScale;
        float size = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.y) + 0.04f * size;
        cooldownArc.widthMultiplier = 0.035f * size;
        int segments = Mathf.Max(1, Mathf.CeilToInt(ArcSegments * progress));
        cooldownArc.positionCount = segments + 1;
        // World-space points keep the arc clockwise in every mirrored quadrant.
        for (int i = 0; i <= segments; i++)
        {
            float angle = progress * Mathf.PI * 2f * i / segments;
            cooldownArc.SetPosition(i, bounds.center + new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * radius);
        }
    }

    private void Refresh()
    {
        if (!IsCoolingDown) return;
        IsCoolingDown = false;
        elapsed = 0f;
        cooldownArc.enabled = false;
        spriteRenderer.SetPropertyBlock(null);
        spriteRenderer.sharedMaterial = originalMaterial;
        if (animator != null) animator.enabled = animatorWasEnabled;
        pickup.ResetPickup();
        pickupCollider.enabled = true;
    }

    private void OnDestroy()
    {
        if (round != null) round.PlayerRespawned -= Refresh;
    }
}
