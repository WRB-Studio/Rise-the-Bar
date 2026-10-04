using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    public Color startBgColor;
    public Color endBgColor;
    public Color startStarColor;
    public Color endStarColor;
    public float colorEndDistance;

    private Camera mainCamera;
    private StarsController stars;

    private void Start()
    {
        mainCamera = Camera.main;
        stars = FindFirstObjectByType<StarsController>(FindObjectsInactive.Include);
        UpdateColors();
    }

    private void Update() => UpdateColors();

    private void UpdateColors()
    {
        if (!mainCamera || !GameHandler.instance)
            return;
        float progress = colorEndDistance > 0f ? GameHandler.instance.Distance / colorEndDistance : 1f;
        mainCamera.backgroundColor = Color.Lerp(startBgColor, endBgColor, progress);
        if (stars)
            stars.SetColor(Color.Lerp(startStarColor, endStarColor, progress));
    }
}
