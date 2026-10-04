using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
    private readonly Color perfectColor = new Color(0.25f, 1f, 0.65f);
    private readonly Color comboColor = new Color(1f, 0.82f, 0.32f);

    public void Initialize(Text template)
    {
        font = template.font;
        ComboText = CreateText("Combo", transform, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(600f, 50f), 28);
        ComboText.color = comboColor;
        ComboText.gameObject.SetActive(false);

        var root = new GameObject("PerfectFeedback", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        perfectGroup = root.GetComponent<CanvasGroup>();
        perfectGroup.blocksRaycasts = false;
        perfectGroup.interactable = false;
        PerfectTitle = CreateText("PerfectTitle", rect, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(750f, 95f), 60);
        PerfectTitle.fontStyle = FontStyle.Bold;
        PerfectPoints = CreateText("PerfectPoints", rect, new Vector2(0.5f, 0.72f), new Vector2(0f, -60f), new Vector2(750f, 50f), 28);
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
        ComboText.gameObject.SetActive(displayedCombo >= 2 && !game.isGameOver);
        VibrationButton.gameObject.SetActive(!BarRaiser.instance.firstTouch || game.isGameOver);
        if (game.isGameOver && perfectGroup.gameObject.activeSelf)
        {
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);
            animationRoutine = null;
            perfectGroup.gameObject.SetActive(false);
        }
    }

    public void ShowPerfect(int combo, int points)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        PerfectTitle.text = "PERFECT!";
        PerfectTitle.color = Color.Lerp(perfectColor, comboColor, Mathf.Clamp01((combo - 1) / 5f));
        PerfectPoints.text = combo > 1 ? "COMBO x" + combo + "  |  +" + points + " pts" : "+" + points + " pts";
        perfectGroup.gameObject.SetActive(true);
        animationRoutine = StartCoroutine(AnimatePerfect(combo));
    }

    private IEnumerator AnimatePerfect(int combo)
    {
        float elapsed = 0f;
        float peakScale = 1.08f + Mathf.Min(Mathf.Max(0, combo - 1), 5) * 0.03f;
        while (elapsed < 0.9f)
        {
            float scale = elapsed < 0.12f
                ? Mathf.Lerp(0.75f, peakScale, elapsed / 0.12f)
                : Mathf.Lerp(peakScale, 1f, Mathf.Clamp01((elapsed - 0.12f) / 0.2f));
            PerfectTitle.rectTransform.localScale = Vector3.one * scale;
            float rise = Mathf.Clamp01(elapsed / 0.9f) * 20f;
            PerfectTitle.rectTransform.anchoredPosition = new Vector2(0f, rise);
            PerfectPoints.rectTransform.anchoredPosition = new Vector2(0f, -60f + rise);
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
    }
}
