using System.Collections;
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
    private List<WorldObject> activeObjects = new List<WorldObject>();

    private void Start()
    {
        mainCamera = Camera.main; // Hauptkamera holen
        startSpawn();
        StartCoroutine(SpawnObjects());
    }

    private void Update()
    {
        // Bewege die Objekte und prüfe, ob sie den linken Bildschirmrand erreicht haben
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            activeObjects[i].transform.Translate(-activeObjects[i].speed * Time.deltaTime, 0, 0);
            if (activeObjects[i].transform.position.x < mainCamera.ScreenToWorldPoint(Vector3.zero).x)
            {
                Destroy(activeObjects[i]);
                activeObjects.RemoveAt(i);
            }
        }
    }

    private IEnumerator SpawnObjects()
    {
        while (true)
        {
            // Wenn die maximale Anzahl erreicht ist, warte bis zum nächsten Frame
            if (activeObjects.Count >= maxOnScreen)
            {
                yield return null;
                continue;
            }

            int spawnCount = Random.Range(minMaxSpawnCount.x, minMaxSpawnCount.y + 1);
            spawnCount = Mathf.Min(spawnCount, maxOnScreen - activeObjects.Count); // Nicht mehr spawnen als max erlaubt

            for (int i = 0; i < spawnCount; i++)
            {
                GameObject newObj = Instantiate(prefabs[Random.Range(0, prefabs.Count)], spawnParent);

                float randomScale = Random.Range(minMaxScale.x, minMaxScale.y);
                newObj.transform.localScale = new Vector2(Random.value < 0.5f ? randomScale : -randomScale, randomScale);

                SpriteRenderer spriteRenderer = newObj.GetComponent<SpriteRenderer>(); // Zugriff auf SpriteRenderer optimieren
                float spriteWidth = spriteRenderer != null ? spriteRenderer.bounds.size.x : 0f;
                float spawnY = Random.Range(ySpawnRange.x, ySpawnRange.y);
                float spawnX = mainCamera.ScreenToWorldPoint(new Vector3(mainCamera.pixelWidth, 0, 0)).x + Random.Range(xSpawnRangeVariation.x, xSpawnRangeVariation.y) + spriteWidth;
                newObj.transform.position = new Vector3(spawnX, spawnY, 0);

                spriteRenderer.color = colors[Random.Range(0, colors.Count)];

                WorldObject worldObjectScript = newObj.AddComponent<WorldObject>();
                worldObjectScript.speed = Random.Range(minMaxSpeed.x, minMaxSpeed.y);

                activeObjects.Add(worldObjectScript);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void startSpawn()
    {
        int spawnCount = minMaxSpawnCount.y;
        spawnCount = Mathf.Min(spawnCount, maxOnScreen - activeObjects.Count); // Nicht mehr spawnen als max erlaubt

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject newObj = Instantiate(prefabs[Random.Range(0, prefabs.Count)], spawnParent);

            float randomScale = Random.Range(minMaxScale.x, minMaxScale.y);
            newObj.transform.localScale = new Vector2(Random.value < 0.5f ? randomScale : -randomScale, randomScale);

            float spawnY = Random.Range(ySpawnRange.x, ySpawnRange.y);
            float spawnX = Random.Range(-xSpawnRangeVariation.y, xSpawnRangeVariation.y);
            newObj.transform.position = new Vector3(spawnX, spawnY, 0);

            SpriteRenderer spriteRenderer = newObj.GetComponent<SpriteRenderer>(); // Zugriff auf SpriteRenderer optimieren
            spriteRenderer.color = colors[Random.Range(0, colors.Count)];

            WorldObject worldObjectScript = newObj.AddComponent<WorldObject>();
            worldObjectScript.speed = Random.Range(minMaxSpeed.x, minMaxSpeed.y);

            activeObjects.Add(worldObjectScript);
        }
    }
}

public class WorldObject : MonoBehaviour
{
    public float speed = 1;
}
