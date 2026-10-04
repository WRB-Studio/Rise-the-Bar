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
    public float toleranceRadius = 1f;

    private readonly List<GameObject> spawnedStars = new List<GameObject>();
    private Camera mainCamera;
    private int initialAttempts;
    private const int MaxPositionAttempts = 20;
    public Color CurrentColor { get; private set; }
    private readonly List<SpriteRenderer> starRenderers = new List<SpriteRenderer>();
    private bool renderersInitialized;

    private void Awake()
    {
        CurrentColor = objectPrefab ? objectPrefab.GetComponent<SpriteRenderer>().color : Color.white;
        InitializeRenderers();
    }

    private void InitializeRenderers()
    {
        if (renderersInitialized)
            return;
        renderersInitialized = true;
        if (parent)
        {
            foreach (Transform child in parent)
            {
                if (child.TryGetComponent(out SpriteRenderer renderer))
                    starRenderers.Add(renderer);
            }
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
        initialAttempts = Mathf.Max(0, maxStars);
    }

    private void Update()
    {
        if (!mainCamera || !objectPrefab || !parent)
            return;

        float leftEdge = mainCamera.ViewportToWorldPoint(Vector3.zero).x - 12f;
        for (int i = spawnedStars.Count - 1; i >= 0; i--)
        {
            GameObject star = spawnedStars[i];
            if (!star || star.transform.position.x < leftEdge || spawnedStars.Count > maxStars)
            {
                if (star)
                {
                    starRenderers.Remove(star.GetComponent<SpriteRenderer>());
                    Destroy(star);
                }
                spawnedStars.RemoveAt(i);
            }
        }

        if (spawnedStars.Count >= Mathf.Max(0, maxStars))
            return;

        bool initial = initialAttempts > 0;
        if (initial)
            initialAttempts--;
        TrySpawnStar(initial);
    }

    private void TrySpawnStar(bool initial)
    {
        float left = mainCamera.ViewportToWorldPoint(Vector3.zero).x;
        float right = mainCamera.ViewportToWorldPoint(Vector3.right).x;
        for (int attempt = 0; attempt < MaxPositionAttempts; attempt++)
        {
            Vector3 position = new Vector3(
                initial ? Random.Range(left, right) : right + Random.Range(minMaxX.x, minMaxX.y),
                Random.Range(minMaxY.x, minMaxY.y), 0f);
            bool available = true;
            foreach (GameObject star in spawnedStars)
            {
                if (star && (star.transform.position - position).sqrMagnitude < toleranceRadius * toleranceRadius)
                {
                    available = false;
                    break;
                }
            }
            if (!available)
                continue;

            GameObject newStar = Instantiate(objectPrefab, position, Quaternion.identity, parent);
            float scale = Random.Range(minMaxScale.x, minMaxScale.y);
            newStar.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer renderer = newStar.GetComponent<SpriteRenderer>();
            renderer.color = CurrentColor;
            starRenderers.Add(renderer);
            spawnedStars.Add(newStar);
            return;
        }
    }

    public void SetColor(Color color)
    {
        InitializeRenderers();
        if (CurrentColor == color)
            return;
        CurrentColor = color;
        for (int i = starRenderers.Count - 1; i >= 0; i--)
        {
            if (starRenderers[i])
                starRenderers[i].color = color;
            else
                starRenderers.RemoveAt(i);
        }
    }
}
