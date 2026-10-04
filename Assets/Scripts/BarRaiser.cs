using UnityEngine;
using UnityEngine.EventSystems;

public class BarRaiser : MonoBehaviour
{
    public static BarRaiser instance;

    [Header("Prefabs")]
    public GameObject perfectPosMarkerPrefab;
    public GameObject perfectionFeedbackPrefab;
    public GameObject antiPerfectionFeedbackPrefab;
    public GameObject barPrefab;

    [Header("Growth")]
    public Vector2 minMaxGrowthSpeed = new Vector2(2f, 6f);

    [Header("Perfect Area (on NEXT platform)")]
    public float perfectAreaLength = 0.3f;
    public float perfectAreaGap = 0.03f;

    [Header("Debug")]
    public bool testMaxLength;
    [Range(0f, 1f)] public float testMaxLengthPercentage = 0.5f;

    [Header("Optional")]
    public bool showGrowFeedback;
    public GameObject tutorial;

    [HideInInspector] public bool firstTouch, isInit;
    private float maxHeight;
    private Platform curPlatform;
    private Transform curBarFeedback;
    private GameObject currentPerfectMarker;
    private bool barIsRaising;
    private int activeFingerId = -1;

    private void Awake() => instance = this;
    public void init() => isInit = true;

    public void updateCall()
    {
        if (!isInit || GameHandler.instance.isGameOver)
            return;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (activeFingerId == -1 && touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;
                if (CreateNewBar())
                    activeFingerId = touch.fingerId;
            }

            if (touch.fingerId != activeFingerId)
                continue;

            switch (touch.phase)
            {
                case TouchPhase.Stationary:
                case TouchPhase.Moved:
                    GrowBar();
                    break;
                case TouchPhase.Ended:
                    ReleaseBar();
                    activeFingerId = -1;
                    break;
                case TouchPhase.Canceled:
                    CancelInput();
                    break;
            }
            return;
        }

        if (activeFingerId >= 0)
            CancelInput();

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.touchCount > 0)
            return;
        if (Input.GetMouseButtonDown(0) && activeFingerId == -1)
        {
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
                return;
            if (CreateNewBar())
                activeFingerId = -2;
        }
        else if (activeFingerId == -2 && Input.GetMouseButtonUp(0))
        {
            ReleaseBar();
            activeFingerId = -1;
        }
        else if (activeFingerId == -2 && Input.GetMouseButton(0))
        {
            GrowBar();
        }
#endif
    }

    private bool CreateNewBar()
    {
        if (barIsRaising)
            return false;

        PlatformController platforms = PlatformController.instance;
        Platform mount = curPlatform ? platforms.GetNextPlatform(curPlatform) : platforms.FirstPlatform;
        if (!mount || mount.barObject || !CameraController.isInsideCameraView(mount.gameObject))
            return false;

        Platform target = platforms.GetNextPlatform(mount);
        if (!target)
            return false;

        curPlatform = mount;
        Transform bar = Instantiate(barPrefab).transform;
        Vector2 pivot = mount.barMountPoint.transform.position;
        bar.position = new Vector2(pivot.x - bar.localScale.x / 2f, pivot.y + bar.localScale.y / 2f);
        bar.SetParent(mount.barMountPoint.transform, true);
        mount.barObject = bar.GetComponent<Bar>();
        barIsRaising = true;
        firstTouch = true;
        if (tutorial)
            tutorial.SetActive(false);

        CreatePerfectMarker(target);
        SetMaxHeight(target);
        if (bar.childCount > 0)
        {
            curBarFeedback = bar.GetChild(0);
            curBarFeedback.SetParent(null, true);
            curBarFeedback.gameObject.SetActive(showGrowFeedback);
            UpdateGrowFeedback();
        }
        return true;
    }

    private void GrowBar()
    {
        if (!barIsRaising || !curPlatform || !curPlatform.barObject)
            return;

        float growthSpeed = Mathf.Lerp(minMaxGrowthSpeed.x, minMaxGrowthSpeed.y, GameHandler.instance.currentDifficulty);
        Bar bar = curPlatform.barObject;
        bar.SetLength(Mathf.Min(bar.Length + growthSpeed * Time.deltaTime, maxHeight));
        UpdateGrowFeedback();
    }

    private void UpdateGrowFeedback()
    {
        if (curBarFeedback && curPlatform && curPlatform.barObject)
        {
            Transform bar = curPlatform.barObject.transform;
            curBarFeedback.position = new Vector2(bar.position.x, bar.position.y + curPlatform.barObject.Length / 2f);
        }
    }

    private void ReleaseBar()
    {
        if (!barIsRaising || !curPlatform || !curPlatform.barObject)
            return;

        barIsRaising = false;
        float accuracy = CalcAccuracy();
        RestoreGrowFeedback();
        if (accuracy > 0f)
        {
            HingeJoint2D hinge = curPlatform.barObject.GetComponent<HingeJoint2D>();
            JointAngleLimits2D limits = hinge.limits;
            limits.max = 90f;
            hinge.limits = limits;
        }
        curPlatform.barObject.tipOverToTheRight(curPlatform);
        if (accuracy == 200f)
            ShowFeedback(perfectionFeedbackPrefab);
        else if (accuracy <= 0f)
            ShowFeedback(antiPerfectionFeedbackPrefab);
        if (accuracy <= 0f)
        {
            DisableBar();
            curPlatform = PlatformController.instance.GetPreviousPlatform(curPlatform);
        }
        GameHandler.instance.RecordBar(Mathf.RoundToInt(accuracy), accuracy == 200f, accuracy <= 0f);
        if (accuracy == 200f)
        {
            AudioController.playReleaseBarPerfect(GameHandler.instance.PerfectStreak);
            Haptics.PlayPerfect();
            Platform target = PlatformController.instance.GetNextPlatform(curPlatform);
            TryGetPerfectBand(target, out float startX, out float width);
            Vector3 hitPosition = new Vector3(startX + width / 2f, target.GetComponent<Collider2D>().bounds.max.y, 0f);
            GUI.instance.HitFeedback.ShowPerfect(GameHandler.instance.PerfectStreak, GameHandler.instance.LastBarPoints, hitPosition);
        }
        else if (accuracy <= 0f)
        {
            AudioController.playReleaseBarFailed();
        }
        else
            AudioController.playReleaseBar();
    }

    public void CancelInput()
    {
        activeFingerId = -1;
        if (!barIsRaising)
            return;

        barIsRaising = false;
        RestoreGrowFeedback();
        if (curPlatform)
        {
            if (curPlatform.barObject)
            {
                Destroy(curPlatform.barObject.gameObject);
                curPlatform.barObject = null;
            }
            curPlatform = PlatformController.instance.GetPreviousPlatform(curPlatform);
        }
        if (currentPerfectMarker)
            Destroy(currentPerfectMarker);
    }

    private void RestoreGrowFeedback()
    {
        if (!curBarFeedback)
            return;
        if (curPlatform && curPlatform.barObject)
        {
            curBarFeedback.SetParent(curPlatform.barObject.transform, true);
            curBarFeedback.gameObject.SetActive(false);
        }
        else
            Destroy(curBarFeedback.gameObject);
        curBarFeedback = null;
    }

    private bool TryGetPerfectBand(Platform target, out float startX, out float width)
    {
        startX = width = 0f;
        if (!target)
            return false;

        Bounds bounds = target.GetComponent<Collider2D>().bounds;
        startX = Mathf.Min(bounds.min.x + Mathf.Max(0f, perfectAreaGap), bounds.max.x - 0.001f);
        width = Mathf.Min(Mathf.Max(0.001f, perfectAreaLength), Mathf.Max(0.001f, bounds.max.x - startX));
        return true;
    }

    private void CreatePerfectMarker(Platform target)
    {
        if (currentPerfectMarker)
            Destroy(currentPerfectMarker);
        if (!perfectPosMarkerPrefab || !TryGetPerfectBand(target, out float startX, out float width))
            return;

        currentPerfectMarker = Instantiate(perfectPosMarkerPrefab);
        currentPerfectMarker.transform.position = new Vector3(startX + width / 2f, target.transform.position.y, 0f);
        Vector3 scale = perfectPosMarkerPrefab.transform.localScale;
        scale.x = width;
        currentPerfectMarker.transform.localScale = scale;
        currentPerfectMarker.transform.SetParent(target.transform, true);
    }

    private float CalcAccuracy()
    {
        Platform target = PlatformController.instance.GetNextPlatform(curPlatform);
        if (!TryGetPerfectBand(target, out float startX, out float width))
            return 0f;

        float pivotX = curPlatform.barMountPoint.transform.position.x;
        float actual = curPlatform.barObject.Length;
        float minimum = startX - pivotX;
        float maximum = minimum + width;
        if (actual < target.GetComponent<Collider2D>().bounds.min.x - pivotX)
        {
            return 0f;
        }

        if (actual >= minimum && actual <= maximum)
        {
            return 200f;
        }

        float excess = Mathf.Max(0f, actual - maximum);
        return 100f * Mathf.Pow(0.5f, excess / Mathf.Max(0.001f, width + 0.5f));
    }

    private void ShowFeedback(GameObject prefab)
    {
        if (prefab)
        {
            Vector3 position = curBarFeedback ? curBarFeedback.position : curPlatform.barObject.transform.position;
            Destroy(Instantiate(prefab, position, Quaternion.identity), 1f);
        }
    }

    private void DisableBar()
    {
        Bar bar = curPlatform.barObject;
        bar.GetComponent<HingeJoint2D>().enabled = false;
        bar.GetComponent<Collider2D>().enabled = false;
        Destroy(bar.gameObject, 5f);
        curPlatform.barObject = null;
    }

    private void SetMaxHeight(Platform target)
    {
        Bounds bounds = target.GetComponent<Collider2D>().bounds;
        float gap = bounds.min.x - curPlatform.barMountPoint.transform.position.x;
        float maxLength = gap + bounds.size.x * 0.75f;
        maxHeight = testMaxLength
            ? Mathf.Lerp(gap + perfectAreaGap, maxLength, testMaxLengthPercentage)
            : maxLength;
    }

    private void OnDestroy()
    {
        if (curBarFeedback)
            Destroy(curBarFeedback.gameObject);
        if (instance == this)
            instance = null;
    }
}
