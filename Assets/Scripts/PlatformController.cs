using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private List<GameObject> platforms = new List<GameObject>();

    [Header("RaiseBarTrigger")]
    public GameObject raiseBarTriggerPrefab;
    public float xOffset = 0.05f;

    private Camera mainCamera;

    [HideInInspector]
    public bool isInit = false;

    public static PlatformController instance;


    private void Awake()
    {
        instance = this;
    }

    public void init()
    {
        mainCamera = Camera.main;

        for (int i = 0; i < minPlatforms; i++)
            createPlatform();

        isInit = true;
    }

    public void updateCallSlow()
    {
        if (GameHandler.instance.isGameOver)
            return;

        //remove platforms out of left range and create new on right side.
        if (verifyAndRemovePlatforms())
            createPlatform();
    }

    private bool verifyAndRemovePlatforms()
    {
        //remove platforms out of left range
        GameObject[] tmpPlatforms = platforms.ToArray();
        for (int i = 0; i < tmpPlatforms.Length; i++)
        {
            if (CameraController.isOutsideLeftCameraEdge(tmpPlatforms[i]))
            {
                removePlatform(tmpPlatforms[i]);
                return true;
            }
        }

        return false;
    }

    private GameObject createPlatform()
    {
        GameObject newPlatform = Instantiate(platformPrefab);

        newPlatform = calculateAndSetPositionAndScale(newPlatform);

        createRaiseBarTrigger(newPlatform);

        platforms.Add(newPlatform);

        return newPlatform;
    }

    private GameObject calculateAndSetPositionAndScale(GameObject platform)
    {
        float randomLength;
        float randomDistancePosition;

        Vector2 randomScale;
        Vector2 randomPosition;

        if (platforms.Count > 0) //first element
        {
            //Calculate random length and height scale
            float minLenghthCalcByDifficulty = Mathf.Lerp(minMaxPlatformLength.x, minMaxPlatformLength.y, GameHandler.instance.currentDifficulty);
            float maxLenghthCalcByDifficulty = Mathf.Lerp(minMaxPlatformLength.y, minMaxPlatformLength.x, GameHandler.instance.currentDifficulty);
            randomLength = Random.Range(minLenghthCalcByDifficulty, maxLenghthCalcByDifficulty);
            randomScale = new Vector2(randomLength, platformScaleY);

            //Calculate random space between last platform and new platform.
            float lastPlatformPositionX = platforms[platforms.Count - 1].transform.position.x;
            float platformDistanceWithoutSpace = platforms[platforms.Count - 1].transform.localScale.x / 2 + platform.transform.localScale.x / 2;
            float randomSpace = Mathf.Lerp(minMaxPlatformDistancePosition.x, minMaxPlatformDistancePosition.y, GameHandler.instance.currentDifficulty);
            randomSpace = Random.Range(randomSpace, randomSpace + (randomSpace / 100 * platformDistancePercentVariation));
            randomDistancePosition = lastPlatformPositionX + platformDistanceWithoutSpace + randomSpace;

            randomPosition = new Vector2(randomDistancePosition, platformPositionY);
        }
        else
        {
            randomLength = Random.Range(minMaxPlatformLength.y, minMaxPlatformLength.y);
            randomScale = new Vector2(randomLength, platform.transform.localScale.y);

            randomPosition = startPlatformPosition;
        }

        platform.transform.localScale = randomScale;
        platform.transform.position = randomPosition;

        return platform;
    }

    private void createRaiseBarTrigger(GameObject parentPlatform)
    {
        Transform raiseBarTriggerPoint = parentPlatform.transform.Find("RightTopCorner");
        GameObject newTriggerPoint = Instantiate(raiseBarTriggerPrefab);
        newTriggerPoint.transform.position = new Vector2(raiseBarTriggerPoint.position.x - xOffset, raiseBarTriggerPoint.position.y);
        newTriggerPoint.transform.parent = parentPlatform.transform;

        parentPlatform.GetComponent<Platform>().barMountPoint = newTriggerPoint.GetComponent<BarMountPoint>();
    }

    private void removePlatform(GameObject platform)
    {
        platforms.Remove(platform);
        Destroy(platform);
    }

    public List<GameObject> getPlatforms()
    {
        return platforms;
    }

    public void gameOverDestruction()
    {
        StartCoroutine(gameOverDestructionRoutine());        
    }

    private IEnumerator gameOverDestructionRoutine()
    {
        GameHandler.instance.setIsGameOver();

        yield return new WaitForSeconds(0.75f);
        Player.instance.GetComponent<Rigidbody2D>().simulated = false;

        List<GameObject> reversedPlatforms = platforms;
        reversedPlatforms.Reverse();
        for (int i = 0; i < reversedPlatforms.Count; i++)
        {
            GameObject platform = reversedPlatforms[i];

            yield return new WaitForSeconds(Random.Range(0.01f, 0.2f));

            GameObject newPlatformExplosion = Instantiate(platformExplosionPrefab, platform.transform.position, Quaternion.identity);
            Destroy(newPlatformExplosion, 5f);

            ParticleSystem particleSystem = newPlatformExplosion.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                ParticleSystem.ShapeModule shapeModule = particleSystem.shape;
                shapeModule.shapeType = ParticleSystemShapeType.Box; // Setzt den Shape-Typ auf Rectangle
                Collider2D platformCollider = platform.GetComponent<Collider2D>();
                shapeModule.scale = new Vector3(platformCollider.bounds.size.x, platformCollider.bounds.size.y, 1); // Z Tiefe muss definiert werden, z.B. auf 1, da es sich um ein 2D-Objekt handelt
            }

            platform.SetActive(false);
        }

        for (int i = 0; i < platforms.Count; i++)
        {
            GameObject platform = platforms[i];
            platforms.Remove(platform);
            Destroy(platform);
        }

        yield return new WaitForSeconds(0.75f);

        Player.instance.die();
    }

}
