using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class HitFeedbackUI : MonoBehaviour
{
    public Text ComboText { get; private set; }
    public Text PerfectTitle { get; private set; }
    public Text PerfectPoints { get; private set; }
    public Button VibrationButton { get; private set; }

    private Font font;
    private CanvasGroup perfectGroup;
    private Text vibrationLabel;
    private Coroutine animationRoutine;
    private int displayedCombo = -1;
    private GameObject comboBadge;
    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform perfectRect;
    private Camera gameCamera;
    private Vector3 targetPosition;
    private float rise;
    private Sprite roundedPanelSprite;
    private readonly Color comboColor = new Color(1f, 0.82f, 0.32f);

    public void Initialize(Text template)
    {
        font = template.font;
        canvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();
        gameCamera = Camera.main;
        roundedPanelSprite = CreateRoundedPanelSprite();
        comboBadge = new GameObject("ComboBadge", typeof(RectTransform), typeof(Image));
        RectTransform badgeRect = comboBadge.GetComponent<RectTransform>();
        badgeRect.SetParent(transform, false);
        badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0.5f, 1f);
        badgeRect.anchoredPosition = new Vector2(0f, -112f);
        badgeRect.sizeDelta = new Vector2(200f, 44f);
        Image badgeImage = comboBadge.GetComponent<Image>();
        badgeImage.sprite = roundedPanelSprite;
        badgeImage.type = Image.Type.Sliced;
        badgeImage.color = new Color(0.04f, 0.06f, 0.1f, 0.76f);
        badgeImage.raycastTarget = false;
        ComboText = CreateText("Combo", badgeRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190f, 40f), 24);
        ComboText.color = comboColor;
        comboBadge.SetActive(false);

        var root = new GameObject("PerfectFeedback", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        perfectRect = root.GetComponent<RectTransform>();
        perfectRect.SetParent(transform, false);
        perfectRect.anchorMin = perfectRect.anchorMax = canvasRect.pivot;
        perfectRect.pivot = new Vector2(0.5f, 0f);
        perfectRect.sizeDelta = new Vector2(340f, 110f);
        Image background = root.GetComponent<Image>();
        background.sprite = roundedPanelSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color(0.04f, 0.06f, 0.1f, 0.72f);
        background.raycastTarget = false;
        perfectGroup = root.GetComponent<CanvasGroup>();
        perfectGroup.blocksRaycasts = false;
        perfectGroup.interactable = false;
        PerfectTitle = CreateText("PerfectTitle", perfectRect, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(310f, 45f), 32);
        PerfectTitle.fontStyle = FontStyle.Bold;
        PerfectPoints = CreateText("PerfectPoints", perfectRect, new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(310f, 32f), 22);
        PerfectPoints.color = comboColor;
        root.SetActive(false);

        var buttonObject = new GameObject("VibrationSetting", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(transform, false);
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-28f, 28f);
        buttonRect.sizeDelta = new Vector2(250f, 60f);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.06f, 0.09f, 0.16f, 0.9f);
        VibrationButton = buttonObject.GetComponent<Button>();
        VibrationButton.targetGraphic = image;
        VibrationButton.onClick.AddListener(ToggleVibration);
        vibrationLabel = CreateText("Label", buttonRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 56f), 22);
        UpdateVibrationLabel();
    }

    private static Sprite CreateRoundedPanelSprite()
    {
        const int size = 64;
        const int radius = 16;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "RoundedFeedbackPanel",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                var center = new Vector2(Mathf.Clamp(point.x, radius, size - radius), Mathf.Clamp(point.y, radius, size - radius));
                float alpha = Mathf.Clamp01(radius + 0.5f - Vector2.Distance(point, center));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private Text CreateText(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        Shadow shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private void Update()
    {
        GameHandler game = GameHandler.instance;
        if (!game || !game.IsInitialized)
            return;

        if (displayedCombo != game.PerfectStreak)
        {
            displayedCombo = game.PerfectStreak;
            ComboText.text = "COMBO x" + displayedCombo;
        }
        comboBadge.SetActive(displayedCombo >= 2 && !game.isGameOver);
        VibrationButton.gameObject.SetActive(!BarRaiser.instance.firstTouch || game.isGameOver);
        if (game.isGameOver && perfectGroup.gameObject.activeSelf)
        {
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);
            animationRoutine = null;
            perfectGroup.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (perfectGroup.gameObject.activeSelf)
            PositionFeedback();
    }

    private void PositionFeedback()
    {
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Rect pixels = canvas.pixelRect;
        if (!uiCamera || !uiCamera.targetTexture)
        {
            Rect safeArea = Screen.safeArea;
            pixels = Rect.MinMaxRect(Mathf.Max(pixels.xMin, safeArea.xMin), Mathf.Max(pixels.yMin, safeArea.yMin),
                Mathf.Min(pixels.xMax, safeArea.xMax), Mathf.Min(pixels.yMax, safeArea.yMax));
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pixels.min, uiCamera, out Vector2 minimum);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pixels.max, uiCamera, out Vector2 maximum);
        const float margin = 20f;
        const float hudClearance = 140f;
        float availableWidth = Mathf.Max(1f, maximum.x - minimum.x - margin * 2f);
        float availableHeight = Mathf.Max(1f, maximum.y - minimum.y - margin * 2f - hudClearance);
        float scale = Mathf.Min(1f, Mathf.Min(availableWidth / perfectRect.sizeDelta.x, availableHeight / perfectRect.sizeDelta.y));
        perfectRect.localScale = Vector3.one * scale;
        Vector2 size = perfectRect.sizeDelta * scale;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, gameCamera.WorldToScreenPoint(targetPosition),
            uiCamera, out Vector2 desired);
        desired.y += 48f + rise;
        desired.x = Mathf.Clamp(desired.x, minimum.x + margin + size.x / 2f, maximum.x - margin - size.x / 2f);
        desired.y = Mathf.Clamp(desired.y, minimum.y + margin, maximum.y - margin - hudClearance - size.y);
        perfectRect.anchoredPosition = desired;
    }

    public void ShowPerfect(int combo, int points, Vector3 position)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        PerfectTitle.text = "PERFECT!";
        targetPosition = position;
        PerfectPoints.text = combo > 1 ? "x" + combo + "  |  +" + points + " pts" : "+" + points + " pts";
        perfectGroup.gameObject.SetActive(true);
        animationRoutine = StartCoroutine(AnimatePerfect(combo));
        PositionFeedback();
    }

    private IEnumerator AnimatePerfect(int combo)
    {
        float elapsed = 0f;
        float peakScale = 1.04f + Mathf.Min(Mathf.Max(0, combo - 1), 5) * 0.005f;
        while (elapsed < 0.9f)
        {
            float scale = elapsed < 0.12f
                ? Mathf.Lerp(0.75f, peakScale, elapsed / 0.12f)
                : Mathf.Lerp(peakScale, 1f, Mathf.Clamp01((elapsed - 0.12f) / 0.2f));
            PerfectTitle.rectTransform.localScale = Vector3.one * scale;
            rise = Mathf.Clamp01(elapsed / 0.9f) * 12f;
            perfectGroup.alpha = elapsed < 0.45f ? 1f : 1f - (elapsed - 0.45f) / 0.45f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        perfectGroup.gameObject.SetActive(false);
        animationRoutine = null;
    }

    private void ToggleVibration()
    {
        Haptics.Enabled = !Haptics.Enabled;
        UpdateVibrationLabel();
        Haptics.PlayPerfect();
    }

    private void UpdateVibrationLabel()
    {
        vibrationLabel.text = Haptics.Enabled ? "Vibration: On" : "Vibration: Off";
    }

    private void OnDestroy()
    {
        if (VibrationButton)
            VibrationButton.onClick.RemoveListener(ToggleVibration);
        if (roundedPanelSprite)
        {
            Destroy(roundedPanelSprite.texture);
            Destroy(roundedPanelSprite);
        }
    }
}
