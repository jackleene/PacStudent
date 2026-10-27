using UnityEngine;

public class CherryController : MonoBehaviour
{
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private Camera levelCamera;
    [SerializeField] private SpriteRenderer cherryPrefab;
    [SerializeField, Min(0.01f)] private float speed = 5f;
    [SerializeField, Min(0f)] private float spawnDelay = 5f;

    private SpriteRenderer currentCherry;
    private bool hasActiveCherry;
    private float nextSpawnTime;
    private float travelTime;
    private float travelDuration;
    private float previousAngle = -1f;
    private Vector3 startPosition;
    private Vector3 endPosition;

    private void Start()
    {
        if (levelGenerator == null || levelCamera == null || cherryPrefab == null || speed <= 0f)
        {
            Debug.LogError("CherryController needs a level, camera, cherry prefab and positive speed.", this);
            enabled = false;
            return;
        }
        nextSpawnTime = Time.time + spawnDelay;
    }

    private void Update()
    {
        if (hasActiveCherry)
        {
            // Also restart the delay when a later collision system eats the cherry.
            if (currentCherry == null)
            {
                hasActiveCherry = false;
                nextSpawnTime = Time.time + spawnDelay;
                return;
            }

            travelTime += Time.deltaTime;
            currentCherry.transform.position = Vector3.Lerp(startPosition, endPosition, travelTime / travelDuration);
            if (travelTime >= travelDuration)
            {
                Destroy(currentCherry.gameObject);
                currentCherry = null;
                hasActiveCherry = false;
                nextSpawnTime = Time.time + spawnDelay;
            }
            return;
        }

        if (Time.time >= nextSpawnTime) SpawnCherry();
    }

    private void SpawnCherry()
    {
        float angle = previousAngle < 0f ? Random.Range(0f, 360f)
            : (previousAngle + Random.Range(30f, 330f)) % 360f;
        previousAngle = angle;
        float radians = angle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
        Bounds bounds = levelGenerator.LevelBounds;
        Vector3 center = bounds.center;
        float spriteMargin = cherryPrefab.sprite.bounds.extents.magnitude
            * Mathf.Max(cherryPrefab.transform.localScale.x, cherryPrefab.transform.localScale.y) + 0.1f;

        startPosition = center + direction * (DistanceToEdge(center, direction, bounds.min, bounds.max) + spriteMargin);

        float cameraDepth = Vector3.Dot(center - levelCamera.transform.position, levelCamera.transform.forward);
        Vector3 viewMin = levelCamera.ViewportToWorldPoint(new Vector3(0f, 0f, cameraDepth));
        Vector3 viewMax = levelCamera.ViewportToWorldPoint(new Vector3(1f, 1f, cameraDepth));
        float endDistance = Mathf.Max(
            DistanceToEdge(center, -direction, bounds.min, bounds.max),
            DistanceToEdge(center, -direction, viewMin, viewMax));
        endPosition = center - direction * (endDistance + spriteMargin);

        currentCherry = Instantiate(cherryPrefab, startPosition, Quaternion.identity, transform);
        travelTime = 0f;
        travelDuration = Vector3.Distance(startPosition, endPosition) / speed;
        hasActiveCherry = true;
    }

    private static float DistanceToEdge(Vector3 origin, Vector3 direction, Vector3 min, Vector3 max)
    {
        float x = Mathf.Abs(direction.x) < 0.0001f ? float.PositiveInfinity
            : ((direction.x > 0f ? max.x : min.x) - origin.x) / direction.x;
        float y = Mathf.Abs(direction.y) < 0.0001f ? float.PositiveInfinity
            : ((direction.y > 0f ? max.y : min.y) - origin.y) / direction.y;
        return Mathf.Min(x, y);
    }
}
