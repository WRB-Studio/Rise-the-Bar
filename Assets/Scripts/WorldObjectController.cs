using System.Collections.Generic;
using UnityEngine;

public class WorldObjectController : MonoBehaviour
{
    public List<GameObject> prefabs;
    public List<Color> colors;
    public Vector2 minMaxScale;
    public Vector2 ySpawnRange;
    public Vector2 xSpawnRangeVariation;
    public Vector2Int minMaxSpawnCount;
    public int maxOnScreen;
    public Vector2 minMaxSpeed;
    public float spawnInterval;
    public Transform spawnParent;

    private Camera mainCamera;
    private float spawnTimer;
    private readonly List<WorldObject> activeObjects = new List<WorldObject>();

    private class WorldObject
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public float speed;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (!mainCamera || prefabs == null || prefabs.Count == 0 || colors == null || colors.Count == 0)
        {
            enabled = false;
            return;
        }
        SpawnObjects(true);
    }

    private void Update()
    {
        float leftEdge = mainCamera.ViewportToWorldPoint(Vector3.zero).x;
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            WorldObject worldObject = activeObjects[i];
            if (!worldObject.transform)
            {
                activeObjects.RemoveAt(i);
                continue;
            }
            worldObject.transform.Translate(Vector3.left * (worldObject.speed * Time.deltaTime), Space.World);
            if (worldObject.renderer.bounds.max.x < leftEdge)
            {
                Destroy(worldObject.transform.gameObject);
                activeObjects.RemoveAt(i);
            }
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= Mathf.Max(0.05f, spawnInterval))
        {
            spawnTimer = 0f;
            SpawnObjects(false);
        }
    }

    private void SpawnObjects(bool initial)
    {
        int spawnCount = initial ? minMaxSpawnCount.y : Random.Range(minMaxSpawnCount.x, minMaxSpawnCount.y + 1);
        spawnCount = Mathf.Clamp(spawnCount, 0, Mathf.Max(0, maxOnScreen - activeObjects.Count));
        float rightEdge = mainCamera.ViewportToWorldPoint(Vector3.right).x;
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            if (!prefab)
                continue;
            GameObject newObject = Instantiate(prefab, spawnParent);
            if (!newObject.TryGetComponent(out SpriteRenderer renderer))
            {
                Destroy(newObject);
                continue;
            }
            float scale = Random.Range(minMaxScale.x, minMaxScale.y);
            newObject.transform.localScale = new Vector3(Random.value < 0.5f ? scale : -scale, scale, 1f);
            float x = initial
                ? Random.Range(-xSpawnRangeVariation.y, xSpawnRangeVariation.y)
                : rightEdge + Random.Range(xSpawnRangeVariation.x, xSpawnRangeVariation.y) + renderer.bounds.size.x;
            newObject.transform.position = new Vector3(x, Random.Range(ySpawnRange.x, ySpawnRange.y), 0f);
            renderer.color = colors[Random.Range(0, colors.Count)];
            activeObjects.Add(new WorldObject
            {
                transform = newObject.transform,
                renderer = renderer,
                speed = Random.Range(minMaxSpeed.x, minMaxSpeed.y)
            });
        }
    }
}
