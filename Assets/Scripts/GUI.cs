using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class GUI : MonoBehaviour
{
    [Header("Ingame GUI elements")]
    public GameObject panelTop;
    public Text txtAccuracy;
    public Text txtTraveledDistance;
    public Text txtRetries;

    [Header("GameOver GUI elements")]
    public Transform gameOverPanel;
    public Button btnReplay;
    public GameObject gameOverTraveled;
    public GameObject gameOverAccuracy;
    public GameObject seperator;
    public float countDuration;

    public Text txtHighscoreTitle;
    public GameObject highscore;
    public GameObject bestHighscore;
    public GameObject newHighscore;
    public Text txtNewHighscoreValue;
    public float countDurationHighscore;
    public float gameOverAnimationSpeed;

    [FormerlySerializedAs("retries")] public int startingRetries = 3;
    [FormerlySerializedAs("nSuccessAttemptsForRetry")] public int perfectsForRetry = 2;

    private bool isNewHighscore = false;
    private Coroutine curVisualCountingRoutine;
    private int displayedAccuracy;
    private int accuracyTarget;
    private int displayedRetries = -1;
    private int displayedDistance = -1;
    private bool gameOverShown;

    [HideInInspector]
    public bool isInit = false;

    public static GUI instance;


    private void Awake()
    {
        instance = this;
    }

    public void init()
    {
        if (isInit)
            return;
        btnReplay.onClick.AddListener(Replay);
        Canvas canvas = panelTop.GetComponentInParent<Canvas>();
        foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(true))
        {
            if (!graphic.GetComponentInParent<Selectable>(true))
                graphic.raycastTarget = false;
        }
        txtAccuracy.text = "0";
        updateCall();

        isInit = true;
    }

    public void updateCall()
    {
        GameHandler game = GameHandler.instance;
        int distance = Mathf.RoundToInt(game.Distance);
        if (distance != displayedDistance)
        {
            displayedDistance = distance;
            txtTraveledDistance.text = distance + " m";
        }
        if (game.Retries != displayedRetries)
        {
            displayedRetries = game.Retries;
            txtRetries.text = displayedRetries.ToString();
        }
        if (game.Accuracy != accuracyTarget)
        {
            accuracyTarget = game.Accuracy;
            if (curVisualCountingRoutine != null)
                StopCoroutine(curVisualCountingRoutine);
            curVisualCountingRoutine = StartCoroutine(visualCountingRoutine(displayedAccuracy, accuracyTarget));
        }
    }

    public void showGameOver()
    {
        if (gameOverShown)
            return;
        gameOverShown = true;
        updateCall();
        isNewHighscore = SaveLoadManager.saveWhenNewHighscore(GameHandler.instance.Distance, GameHandler.instance.Accuracy);
        StartCoroutine(showHighscoreAnimationRoutine());
        StartCoroutine(skipHighscoreRoutine());
    }

    private void Replay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator visualCountingRoutine(int startValue, int endValue)
    {
        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime;
            displayedAccuracy = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, time));
            txtAccuracy.text = displayedAccuracy.ToString();
            yield return null;
        }
        displayedAccuracy = endValue;
        txtAccuracy.text = endValue.ToString();
        curVisualCountingRoutine = null;
    }

    private IEnumerator showHighscoreAnimationRoutine()
    {
        gameOverTraveled.SetActive(false);
        gameOverAccuracy.SetActive(false);
        seperator.SetActive(false);

        txtHighscoreTitle.gameObject.SetActive(false);
        highscore.SetActive(false);
        bestHighscore.SetActive(false);
        newHighscore.SetActive(false);
        txtNewHighscoreValue.gameObject.SetActive(false);
        btnReplay.gameObject.SetActive(false);

        AudioController.playGameOverMusic();

        //Show GameOver panel
        yield return new WaitForSeconds(1.5f);
        panelTop.SetActive(false);
        gameOverPanel.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.2f);

        //show count animations
        gameOverTraveled.gameObject.SetActive(true);
        StartCoroutine(countToTargetRoutine(
            gameOverTraveled.transform.GetChild(1).GetComponent<Text>(),
            GameHandler.instance.Distance,
            countDuration,
            2,
            " m"));
        yield return new WaitForSeconds(countDuration + 0.1f);

        gameOverAccuracy.gameObject.SetActive(true);
        StartCoroutine(countToTargetRoutine(
            gameOverAccuracy.transform.GetChild(1).GetComponent<Text>(),
            GameHandler.instance.Accuracy,
            countDuration,
            0,
            " Pts."));

        yield return new WaitForSeconds(countDuration + 0.1f + gameOverAnimationSpeed);
        seperator.SetActive(true);
        yield return new WaitForSeconds(gameOverAnimationSpeed);

        //show highscore
        txtHighscoreTitle.gameObject.SetActive(true);
        yield return new WaitForSeconds(gameOverAnimationSpeed);

        highscore.gameObject.SetActive(true);
        float highscoreValue = SaveLoadManager.calculateHighscore(GameHandler.instance.Distance, GameHandler.instance.Accuracy);
        StartCoroutine(countToTargetRoutine(
            highscore.transform.GetChild(1).GetComponent<Text>(),
            highscoreValue,
            countDurationHighscore));

        if (highscoreValue != 0)
            yield return new WaitForSeconds(countDurationHighscore);

        if (isNewHighscore)
        {
            AudioController.playNewHighscore();
            highscore.gameObject.SetActive(false);
            txtNewHighscoreValue.text = highscoreValue.ToString("F0");
            txtNewHighscoreValue.gameObject.SetActive(true);
            newHighscore.SetActive(isNewHighscore);
        }
        else
        {
            bestHighscore.gameObject.SetActive(true);
            StartCoroutine(countToTargetRoutine(
               bestHighscore.transform.GetChild(1).GetComponent<Text>(),
               SaveLoadManager.getLastHighscore(),
               0.5f));
        }

        yield return new WaitForSeconds(gameOverAnimationSpeed);
        btnReplay.gameObject.SetActive(true);
    }

    private IEnumerator skipHighscoreRoutine()
    {
        yield return new WaitForSeconds(1);

        while (true)
        {
            if (Input.anyKeyDown)
            {
                break;
            }

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                break;
            }

            yield return null;
        }

        btnReplay.gameObject.SetActive(true);
    }

    private IEnumerator countToTargetRoutine(Text textElement, float targetValue, float duration, int decimalPlaces = 0, string suffix = "")
    {
        float startValue = 0;
        float currentValue = startValue;
        float elapsedTime = 0;

        string format = "F" + decimalPlaces;

        if (targetValue != 0 && duration > 0f)
        {
            AudioController.playCounting(duration);
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                currentValue = Mathf.Lerp(startValue, targetValue, elapsedTime / duration);
                textElement.text = currentValue.ToString(format) + suffix;
                yield return null;
            }
        }

        textElement.text = targetValue.ToString(format) + suffix;
    }

    private void OnDestroy()
    {
        if (btnReplay)
            btnReplay.onClick.RemoveListener(Replay);
        if (instance == this)
            instance = null;
    }
}
