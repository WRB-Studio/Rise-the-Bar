using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarsController : MonoBehaviour
{
    public GameObject objectPrefab;
    public Transform parent;
    public int maxStars = 10;
    public Vector2 minMaxX;
    public Vector2 minMaxY;
    public Vector2 minMaxScale;
    public float toleranceRadius = 1.0f; // Der Toleranzradius um bereits bestehende Objekte

    private bool starterStarsIsInit = false;

    private List<GameObject> spawnedStars = new List<GameObject>();

    void Start()
    {
        StartCoroutine(startSpawnStarsRoutine());
    }

    private void Update()
    {
        if (!starterStarsIsInit)
            return;

        float leftEdgeOfCamera = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).x - 12;

        // Entferne alle Sterne, die links von der Kamera sind
        for (int i = spawnedStars.Count - 1; i >= 0; i--)
        {
            if (spawnedStars[i] != null && spawnedStars[i].transform.position.x < leftEdgeOfCamera)
            {
                Destroy(spawnedStars[i]);
                spawnedStars.RemoveAt(i);
            }
        }

        if(spawnedStars.Count < maxStars)
            StartCoroutine(instantiateStar());
    }

    private IEnumerator instantiateStar()
    {
        yield return null;
        
        GameObject newStar = Instantiate(objectPrefab, parent);
        float randomScale = Random.Range(minMaxScale.x, minMaxScale.y);
        newStar.transform.localScale = new Vector2(randomScale, randomScale);

        // Finde eine freie Position
        Vector3 spawnPosition;
        bool freePosition = false;
        do
        {
            yield return null;

            float spawnY = Random.Range(minMaxY.x, minMaxY.y);
            float spawnX = getCameraRightXBorder() + Random.Range(minMaxX.x, minMaxX.y);
            spawnPosition = new Vector3(spawnX, spawnY, 0);
            freePosition = true;

            foreach (GameObject star in spawnedStars)
            {
                if (Vector3.Distance(star.transform.position, spawnPosition) < toleranceRadius)
                {
                    freePosition = false;
                    break;
                }
            }
        } while (!freePosition);

        newStar.transform.position = spawnPosition;
        spawnedStars.Add(newStar);
    }

    private IEnumerator startSpawnStarsRoutine()
    {
        Camera mainCamera = Camera.main; // Kamera nur einmal abrufen
        int retries = 20; // Maximale Anzahl an Versuchen für jeden Stern

        while (spawnedStars.Count < maxStars)
        {
            GameObject newStar = Instantiate(objectPrefab, parent);
            float randomScale = Random.Range(minMaxScale.x, minMaxScale.y);
            newStar.transform.localScale = new Vector2(randomScale, randomScale);

            bool freePos = false;
            Vector3 spawnPosition = Vector2.zero;
            for (int i = 0; i < retries && !freePos; i++)
            {
                float spawnY = Random.Range(minMaxY.x, minMaxY.y);
                float spawnX = Random.Range(-getCameraRightXBorder(), getCameraRightXBorder());
                spawnPosition = new Vector3(spawnX, spawnY, 0);

                freePos = true; // Start with the assumption that the position is free
                foreach (GameObject star in spawnedStars)
                {
                    if (Vector3.Distance(star.transform.position, spawnPosition) < toleranceRadius)
                    {
                        freePos = false; // Found a star too close, the position is not free
                        break;
                    }
                }
            }

            if (freePos)
            {
                newStar.transform.position = spawnPosition;
                spawnedStars.Add(newStar);
            }
            else
            {
                Destroy(newStar); // Wenn nach allen Versuchen keine freie Position gefunden wurde
            }

            yield return null; // Eine kurze Pause zwischen den Spawn-Versuchen
        }

        starterStarsIsInit = true;
    }

    private float getCameraRightXBorder()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera.orthographic)
        {
            // Für orthographische Kameras
            return mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect;
        }
        else
        {
            // Für perspektivische Kameras
            return mainCamera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;
        }
    }
}
