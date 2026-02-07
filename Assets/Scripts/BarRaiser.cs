using System.Collections.Generic;
using UnityEngine;

public class BarRaiser : MonoBehaviour
{
    public static BarRaiser instance;

    [Header("Prefabs")]
    public GameObject perfectPosMarkerPrefab;
    public GameObject perfectionFeedbackPrefab;
    public GameObject antiPerfectionFeedbackPrefab;
    public GameObject barPrefab;

    [Header("Growth")]
    public Vector2 minMaxGrowthSpeed = new(2f, 6f);

    [Header("Perfect Area (on NEXT platform)")]
    public float perfectAreaLength = 0.3f;
    public float perfectAreaGap = 0.03f;

    [Header("Debug")]
    public bool testMaxLength = false;
    [Range(0f, 1f)] public float testMaxLengthPercentage = 0.5f;

    [Header("Optional")]
    public bool showGrowFeedback = false;
    public GameObject tutorial;

    [HideInInspector] public bool firstTouch, barRaiseFailed, isInit;

    float growthSpeed, maxHeight;

    Platform curPlatform;                 // Plattform, auf der der aktuelle Bar hängt
    Transform curBarFeedback;             // detached feedback obj
    GameObject currentPerfectMarker;

    bool barIsRaising;
    bool canGrow;                         // <<< FIX: nur wenn true darf Grow passieren

    void Awake() => instance = this;
    public void init() => isInit = true;

    public void updateCall()
    {
        if (GameHandler.instance.isGameOver || barRaiseFailed || Input.touchCount == 0) return;

        firstTouch = true;
        if (tutorial) tutorial.SetActive(false);

        switch (Input.GetTouch(0).phase)
        {
            case TouchPhase.Began:
                if (!barIsRaising) CreateNewBar();
                break;

            case TouchPhase.Stationary:
            case TouchPhase.Moved:
                GrowBar();
                break;

            case TouchPhase.Ended:
                ReleaseBar();
                break;
        }
    }

    void CreateNewBar()
    {
        if (barIsRaising) return;

        Platform mount = curPlatform
            ? TryGetNextPlatform(curPlatform)
            : PlatformController.instance.getPlatforms()[0].GetComponent<Platform>();

        if (!mount || mount.barObject) return;
        if (!CameraController.isInsideCameraView(mount.gameObject)) return;

        Platform target = TryGetNextPlatform(mount);
        if (!target) return;

        InstantiateBar(mount);

        barIsRaising = true;
        canGrow = true; // <<< FIX: ab jetzt darf wachsen

        SetupAfterBarCreated(target);
    }

    void SetupAfterBarCreated(Platform target)
    {
        CreatePerfectMarker(target);
        SetMaxHeight(target);

        curBarFeedback = curPlatform.barObject.transform.GetChild(0);
        curBarFeedback.parent = null;
    }

    void InstantiateBar(Platform platform)
    {
        curPlatform = platform;

        Vector2 pos = platform.barMountPoint.transform.position;
        var bar = Instantiate(barPrefab).transform;

        bar.position = new Vector2(
            pos.x - bar.localScale.x / 2f,
            pos.y + bar.localScale.y / 2f
        );

        bar.SetParent(platform.barMountPoint.transform, true);
        platform.barObject = bar.GetComponent<Bar>();
    }

    void GrowBar()
    {
        if (!canGrow) return;                 // <<< FIX
        if (!curPlatform?.barObject) return;

        growthSpeed = Mathf.Lerp(minMaxGrowthSpeed.x, minMaxGrowthSpeed.y, GameHandler.instance.currentDifficulty);

        var t = curPlatform.barObject.transform;
        Vector2 s = t.localScale;

        float newH = s.y + growthSpeed * Time.deltaTime;
        if (newH >= maxHeight) return;

        float delta = newH - s.y;
        t.localScale = new Vector2(s.x, newH);
        t.position += new Vector3(0f, delta / 2f, 0f);

        if (showGrowFeedback)
        {
            float top = t.GetComponent<Collider2D>().bounds.max.y;
            curBarFeedback.position = new Vector2(t.position.x, top);
        }
    }

    void ReleaseBar()
    {
        if (!canGrow) return;                 // <<< FIX: doppelte Ended / Race safe
        canGrow = false;                      // <<< FIX: sofort sperren

        if (!curPlatform?.barObject) { barIsRaising = false; return; }

        curBarFeedback.SetParent(curPlatform.barObject.transform, true);
        curBarFeedback.gameObject.SetActive(false);

        barIsRaising = false;

        float accuracy = CalcAccuracy();
        GUI.instance.addAccuracy(Mathf.RoundToInt(accuracy));

        curPlatform.barObject.tipOverToTheRight(curPlatform);

        if (accuracy <= 0f)
        {
            DisableBar();
            curPlatform = IsFirstPlatform(curPlatform) ? null : GetPrevPlatform(curPlatform);
        }
    }

    // ---------- PERFECT: single source of truth ----------
    bool TryGetPerfectBand(Platform target, out float startX, out float width)
    {
        startX = 0f; width = 0f;
        if (!target) return false;

        var col = target.GetComponent<Collider2D>();
        if (!col) return false;

        float left = col.bounds.min.x;
        float right = col.bounds.max.x;

        startX = Mathf.Min(left + perfectAreaGap, right - 0.001f);

        float maxW = Mathf.Max(0.001f, right - startX);
        width = Mathf.Min(Mathf.Max(0.001f, perfectAreaLength), maxW);

        return true;
    }

    void CreatePerfectMarker(Platform target)
    {
        if (currentPerfectMarker) Destroy(currentPerfectMarker);
        if (!TryGetPerfectBand(target, out float startX, out float width)) return;

        float centerX = startX + width * 0.5f;
        float y = target.transform.position.y;

        Vector3 prefabScale = perfectPosMarkerPrefab.transform.localScale;

        currentPerfectMarker = Instantiate(perfectPosMarkerPrefab);
        currentPerfectMarker.transform.SetParent(target.transform, false);

        Vector3 worldPos = new(centerX, y, 0f);
        currentPerfectMarker.transform.localPosition = target.transform.InverseTransformPoint(worldPos);

        float px = Mathf.Max(0.0001f, target.transform.lossyScale.x);
        float py = Mathf.Max(0.0001f, target.transform.lossyScale.y);

        Vector3 s = currentPerfectMarker.transform.localScale;
        s.x = width / px;
        s.y = prefabScale.y / py;
        currentPerfectMarker.transform.localScale = s;
    }

    void GetPerfectRange(out float min, out float max)
    {
        min = max = 0f;

        Platform mount = curPlatform;
        Platform target = TryGetNextPlatform(mount);
        if (!mount || !target) return;

        float mountRight = mount.GetComponent<Collider2D>().bounds.max.x;
        float targetLeft = target.GetComponent<Collider2D>().bounds.min.x;

        float gap = Mathf.Abs(targetLeft - mountRight);

        float startLen = gap + perfectAreaGap;
        float endLen = startLen;

        if (TryGetPerfectBand(target, out _, out float bandW))
            endLen = startLen + bandW;

        float targetRight = target.GetComponent<Collider2D>().bounds.max.x;
        float maxAllowed = Mathf.Max(0.001f, targetRight - mountRight);

        min = startLen;
        max = Mathf.Min(endLen, maxAllowed);
    }

    float CalcAccuracy()
    {
        float actual = curPlatform.barObject.transform.localScale.y;
        GetPerfectRange(out float min, out float max);

        if (actual < min)
        {
            AudioController.playReleaseBarFailed();
            Destroy(Instantiate(antiPerfectionFeedbackPrefab, curBarFeedback.position, Quaternion.identity), 1f);

            GUI.instance.addPerfectBarAttempt(false);

            if (GUI.instance.retries <= 0)
            {
                barRaiseFailed = true;
                PlatformController.instance.gameOverDestruction();
            }

            return 0f;
        }

        if (actual <= max)
        {
            var hinge = curPlatform.barObject.GetComponent<HingeJoint2D>();
            var limits = hinge.limits;
            limits.max = 90;
            hinge.limits = limits;

            AudioController.playReleaseBarPerfect();
            Destroy(Instantiate(perfectionFeedbackPrefab, curBarFeedback.position, Quaternion.identity), 1f);

            GUI.instance.addPerfectBarAttempt(true);
            return 200f;
        }

        AudioController.playReleaseBar();

        float excess = actual - max;
        float scale = Mathf.Max(0.001f, (max - min) + 0.5f);

        return Mathf.Max(100f * Mathf.Pow(0.5f, excess / scale), 0f);
    }

    void DisableBar()
    {
        var hinge = curPlatform.barObject.GetComponent<HingeJoint2D>();
        hinge.enabled = false;
        curPlatform.barObject.GetComponent<Collider2D>().enabled = false;

        Destroy(curPlatform.barObject, 5f);
        curPlatform.barObject = null;
    }

    void SetMaxHeight(Platform target)
    {
        Platform mount = curPlatform;
        if (!mount || !target) { maxHeight = 0f; return; }

        float mountRight = mount.GetComponent<Collider2D>().bounds.max.x;
        float targetLeft = target.GetComponent<Collider2D>().bounds.min.x;
        float gap = Mathf.Abs(targetLeft - mountRight);

        float targetWidth = target.GetComponent<Collider2D>().bounds.size.x;

        float perfectMin = gap + perfectAreaGap;
        float maxLen = gap + targetWidth * 0.75f;

        if (testMaxLength)
        {
            Player.instance.minMaxIdleSpeed = Vector2.zero;
            Player.instance.minMaxNormalSpeed = Vector2.zero;
            maxHeight = perfectMin + ((maxLen - perfectMin) * testMaxLengthPercentage);
        }
        else
        {
            maxHeight = maxLen;
        }
    }

    // ---------- platform nav ----------
    Platform TryGetNextPlatform(Platform current)
    {
        if (!current) return null;

        var platforms = PlatformController.instance.getPlatforms();
        int i = platforms.IndexOf(current.gameObject);

        return (i >= 0 && i + 1 < platforms.Count)
            ? platforms[i + 1].GetComponent<Platform>()
            : null;
    }

    Platform GetPrevPlatform(Platform current)
    {
        var platforms = PlatformController.instance.getPlatforms();
        int i = platforms.IndexOf(current.gameObject);
        return i > 0 ? platforms[i - 1].GetComponent<Platform>() : null;
    }

    bool IsFirstPlatform(Platform current)
    {
        var platforms = PlatformController.instance.getPlatforms();
        return platforms.IndexOf(current.gameObject) == 0;
    }
}
