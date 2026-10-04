using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    public float yOffset;
    public float xOffset;
    public float followSpeed = 5f;

    [HideInInspector] public bool isInit;
    public static CameraController instance;
    private Camera mainCamera;

    private void Awake()
    {
        instance = this;
        mainCamera = GetComponent<Camera>();
    }

    public void init() => isInit = true;

    public void lateUpdateCall()
    {
        if (!player)
            return;
        Vector3 target = new Vector3(player.position.x + xOffset, yOffset, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
    }

    public static bool isOutsideLeftCameraEdge(GameObject obj, float xOffset = -7f)
    {
        if (!obj || !instance || !instance.mainCamera || !obj.TryGetComponent(out SpriteRenderer renderer))
            return false;
        float leftEdge = instance.mainCamera.ViewportToWorldPoint(Vector3.zero).x + xOffset;
        return renderer.bounds.max.x < leftEdge;
    }

    public static bool isInsideCameraView(GameObject obj)
    {
        if (!obj || !instance || !instance.mainCamera || !obj.TryGetComponent(out SpriteRenderer renderer))
            return false;
        float left = instance.mainCamera.ViewportToWorldPoint(Vector3.zero).x;
        float right = instance.mainCamera.ViewportToWorldPoint(Vector3.right).x;
        return renderer.bounds.min.x < right && renderer.bounds.max.x > left;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
