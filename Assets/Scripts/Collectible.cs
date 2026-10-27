using UnityEngine;

public class Collectible : MonoBehaviour
{
    [SerializeField] private int points = 10;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private bool powerPill;
    private bool collected;

    public bool IsPowerPill => powerPill;

    public bool TryCollect(out int awardedPoints, out AudioClip sound)
    {
        awardedPoints = points;
        sound = pickupSound;
        if (collected || !isActiveAndEnabled) return false;

        // Trigger callbacks and movement sweeps can both find the same item.
        // Disable immediately so each item scores only once before deferred destruction.
        collected = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}
