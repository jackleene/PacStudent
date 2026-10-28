using UnityEngine;

public class Collectible : MonoBehaviour
{
    [SerializeField] private int points = 10;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private bool powerPill;
    private bool collected;
    private PowerPillCooldown cooldown;

    public bool IsPowerPill => powerPill;

    private void Awake() => cooldown = GetComponent<PowerPillCooldown>();

    internal void ResetPickup() => collected = false;

    public bool TryCollect(out int awardedPoints, out AudioClip sound)
    {
        awardedPoints = points;
        sound = pickupSound;
        if (collected || !isActiveAndEnabled) return false;
        if (cooldown != null && !cooldown.CanCollect) return false;

        // Trigger callbacks and movement sweeps can both find the same item.
        // Mark immediately so each pickup scores only once, including reusable guns.
        collected = true;
        if (cooldown != null) cooldown.BeginCooldown();
        else
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        return true;
    }
}
