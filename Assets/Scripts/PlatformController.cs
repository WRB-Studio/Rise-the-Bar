using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [Header("Platform props")]
    public GameObject platformExplosionPrefab;
    public GameObject platformPrefab;
    public int minPlatforms = 10;
    public Vector2 startPlatformPosition;
    public Vector2 minMaxPlatformLength;
    public float platformScaleY;
    public float platformPositionY;
    public Vector2 minMaxPlatformDistancePosition;
    public float platformDistancePercentVariation;
    public float removeDistance = 10f;

    [Header("RaiseBarTrigger")]
    public GameObject raiseBarTriggerPrefab;
    public float xOffset = 0.05f;

    private readonly List<GameObject> platforms = new List<GameObject>();
    private bool destructionStarted;
    [HideInInspector] public bool isInit;
    public static PlatformController instance;

    public Platform FirstPlatform => platforms.Count > 0 ? platforms[0].GetComponent<Platform>() : null;

    private void Awake() => instance = this;

    public void init()
    {
        if (isInit)
            return;

        for (int i = 0; i < Mathf.Max(2, minPlatforms); i++)
            CreatePlatform();
        isInit = true;
    }

    public void updateCallSlow()
    {
        if (GameHandler.instance.isGameOver)
            return;

        while (platforms.Count > 0 && CameraController.isOutsideLeftCameraEdge(platforms[0], -removeDistance))
        {
            Destroy(platforms[0]);
            platforms.RemoveAt(0);
        }

        while (platforms.Count < Mathf.Max(2, minPlatforms))
            CreatePlatform();
    }

    private void CreatePlatform()
    {
        GameObject platform = Instantiate(platformPrefab);
        float difficulty = GameHandler.instance.currentDifficulty;
        float minLength = Mathf.Lerp(minMaxPlatformLength.x, minMaxPlatformLength.y, difficulty);
        float maxLength = Mathf.Lerp(minMaxPlatformLength.y, minMaxPlatformLength.x, difficulty);
        float length = platforms.Count == 0
            ? minMaxPlatformLength.y
            : Random.Range(Mathf.Min(minLength, maxLength), Mathf.Max(minLength, maxLength));

        Vector3 scale = platform.transform.localScale;
        scale.x = length;
        scale.y = platformScaleY;
        platform.transform.localScale = scale;
        Vector2 position = startPlatformPosition;
        if (platforms.Count > 0)
        {
            Transform previous = platforms[platforms.Count - 1].transform;
            float gap = Mathf.Lerp(minMaxPlatformDistancePosition.x, minMaxPlatformDistancePosition.y, difficulty);
            gap = Random.Range(gap, gap * (1f + Mathf.Max(0f, platformDistancePercentVariation) / 100f));
            position = new Vector2(previous.position.x + previous.localScale.x / 2f + length / 2f + gap, platformPositionY);
        }
        platform.transform.position = position;

        Transform corner = platform.transform.Find("RightTopCorner");
        GameObject trigger = Instantiate(raiseBarTriggerPrefab);
        trigger.transform.position = new Vector2(corner.position.x - xOffset, corner.position.y);
        trigger.transform.SetParent(platform.transform, true);
        platform.GetComponent<Platform>().barMountPoint = trigger.GetComponent<BarMountPoint>();
        platforms.Add(platform);

        // New colliders must have current bounds before bar input is processed in this frame.
        Physics2D.SyncTransforms();
    }

    public IReadOnlyList<GameObject> getPlatforms() => platforms;

    public Platform GetNextPlatform(Platform current)
    {
        int index = current ? platforms.IndexOf(current.gameObject) : -1;
        return index >= 0 && index + 1 < platforms.Count ? platforms[index + 1].GetComponent<Platform>() : null;
    }

    public Platform GetPreviousPlatform(Platform current)
    {
        int index = current ? platforms.IndexOf(current.gameObject) : -1;
        return index > 0 ? platforms[index - 1].GetComponent<Platform>() : null;
    }

    public void gameOverDestruction()
    {
        if (destructionStarted)
            return;

        destructionStarted = true;
        GameHandler.instance.setIsGameOver();
        StartCoroutine(GameOverDestructionRoutine());
    }

    private IEnumerator GameOverDestructionRoutine()
    {
        yield return new WaitForSeconds(0.75f);
        Player.instance.GetComponent<Rigidbody2D>().simulated = false;

        for (int i = platforms.Count - 1; i >= 0; i--)
        {
            GameObject platform = platforms[i];
            yield return new WaitForSeconds(Random.Range(0.01f, 0.2f));
            if (!platform)
                continue;

            GameObject explosion = Instantiate(platformExplosionPrefab, platform.transform.position, Quaternion.identity);
            Destroy(explosion, 5f);
            ParticleSystem particles = explosion.GetComponent<ParticleSystem>();
            if (particles)
            {
                ParticleSystem.ShapeModule shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                Vector3 size = platform.GetComponent<Collider2D>().bounds.size;
                shape.scale = new Vector3(size.x, size.y, 1f);
            }
            platform.SetActive(false);
        }

        foreach (GameObject platform in platforms)
            Destroy(platform);
        platforms.Clear();

        yield return new WaitForSeconds(0.75f);
        Player.instance.die();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
