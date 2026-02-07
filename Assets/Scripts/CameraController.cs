using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player; // Das Ziel, dem die Kamera folgen soll
    public float yOffset; // Die feste Y-Offset-Position der Kamera
    public float xOffset; // Der X-Offset zwischen Kamera und Spieler
    public float followSpeed = 5f; // Wie schnell die Kamera dem Spieler folgt ("Drag"-Effekt)

    [HideInInspector]
    public bool isInit = false;

    public static CameraController instance;

    private void Awake()
    {
        instance = this;
    }

    public void init()
    {
        isInit = true;
    }

    public void lateUpdateCall()
    {
        // Zielposition der Kamera mit Offset
        Vector3 targetPosition = new Vector3(player.position.x + xOffset, yOffset, transform.position.z);

        // Interpoliere sanft zwischen der aktuellen Position der Kamera und der Zielposition
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }

    public static bool IsObjectVisible(GameObject obj)
    {
        if (obj == null)
            return false;

        Vector3 viewportPoint = Camera.main.WorldToViewportPoint(obj.transform.position);
        return viewportPoint.x > 0 && viewportPoint.x < 1 && viewportPoint.y > 0 && viewportPoint.y < 1 && viewportPoint.z > 0;
    }

    public static bool isOutsideLeftCameraEdge(GameObject obj, float xOffset = -7)
    {
        if (obj == null) return false;

        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();

        // Den linken Rand der Kamera in Weltkoordinaten berechnen
        float leftCameraEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).x + xOffset;

        // Den rechten Rand des SpriteRenderer in Weltkoordinaten berechnen
        float objectRightEdge = spriteRenderer.bounds.max.x;

        // Überprüfen, ob der rechte Rand des Objekts links vom linken Rand der Kamera ist
        return objectRightEdge < leftCameraEdge;
    }

    public static bool isInsideCameraView(GameObject obj)
    {
        if (obj == null) return false;

        SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
        float objectLeftEdge = spriteRenderer.bounds.min.x;
        float objectRightEdge = spriteRenderer.bounds.max.x;

        float leftCameraEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
        float rightCameraEdge = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

        return objectLeftEdge < rightCameraEdge && objectRightEdge > leftCameraEdge;

    }

}
