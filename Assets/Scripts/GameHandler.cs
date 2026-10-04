using UnityEngine;

public class GameHandler : MonoBehaviour
{
    public float maxDifficultyAfterSec = 30f;
    public float currentDifficulty { get; private set; }
    public bool isGameOver { get; private set; }
    public bool IsInitialized { get; private set; }
    public float ElapsedTime { get; private set; }
    public float Distance { get; private set; }
    public int Accuracy { get; private set; }
    public int Retries { get; private set; }
    public int PerfectStreak { get; private set; }

    public static GameHandler instance;

    private bool applicationPaused;
    private bool applicationUnfocused;
    private bool timePaused;
    private float previousTimeScale;

    private void Awake()
    {
        instance = this;
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        Retries = Mathf.Max(0, GUI.instance.startingRetries);
        PlatformController.instance.init();
        CameraController.instance.init();
        BarRaiser.instance.init();
        Player.instance.init();
        GUI.instance.init();
        IsInitialized = true;
    }

    private void Update()
    {
        if (!IsInitialized || isGameOver || timePaused)
            return;

        if (BarRaiser.instance.firstTouch)
        {
            ElapsedTime += Time.deltaTime;
            UpdateDistance();
            currentDifficulty = maxDifficultyAfterSec > 0f
                ? Mathf.Clamp01(ElapsedTime / maxDifficultyAfterSec)
                : 1f;
        }

        PlatformController.instance.updateCallSlow();
        BarRaiser.instance.updateCall();
        Player.instance.updateCall();
        GUI.instance.updateCall();
    }

    private void LateUpdate()
    {
        if (IsInitialized && !timePaused)
            CameraController.instance.lateUpdateCall();
    }

    public void RecordBar(int points, bool isPerfect, bool failed)
    {
        if (isGameOver)
            return;

        Accuracy = (int)System.Math.Min(int.MaxValue, (long)Accuracy + Mathf.Max(0, points));
        PerfectStreak = isPerfect && !failed ? PerfectStreak + 1 : 0;
        int requiredPerfects = GUI.instance.perfectsForRetry;
        if (isPerfect && !failed && requiredPerfects > 0 && PerfectStreak % requiredPerfects == 0)
            Retries++;

        if (failed)
        {
            Retries = Mathf.Max(0, Retries - 1);
            if (Retries == 0)
                PlatformController.instance.gameOverDestruction();
        }
    }

    private void UpdateDistance()
    {
        if (BarRaiser.instance.firstTouch)
            Distance = Mathf.Max(Distance, Player.instance.Position.x - Player.instance.startPoint.x);
    }

    public void setIsGameOver()
    {
        if (isGameOver)
            return;

        UpdateDistance();
        isGameOver = true;
        BarRaiser.instance.CancelInput();
        GUI.instance.showGameOver();
    }

    private void OnApplicationPause(bool paused)
    {
        applicationPaused = paused;
        UpdatePauseState();
    }

    private void OnApplicationFocus(bool focused)
    {
        applicationUnfocused = !focused;
        UpdatePauseState();
    }

    private void UpdatePauseState()
    {
        bool shouldPause = applicationPaused || applicationUnfocused;
        if (timePaused == shouldPause)
            return;

        timePaused = shouldPause;
        if (shouldPause)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            if (BarRaiser.instance)
                BarRaiser.instance.CancelInput();
        }
        else
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = false;
        }
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        if (timePaused)
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = false;
        }
        instance = null;
    }
}
