using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [HideInInspector] public float distance = 0;
    [HideInInspector] public float timer = 0;
    [HideInInspector] public int accuracy = 0;
    public int retries = 0;
    public int nSuccessAttemptsForRetry = 0;
    public int attemptsInRow = 0;

    private bool isNewHighscore = false;
    private Coroutine curVisualCountingRoutine;

    [HideInInspector]
    public bool isInit = false;

    public static GUI instance;


    private void Awake()
    {
        addAccuracy(0);
        instance = this;
    }

    public void init()
    {
        btnReplay.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });

        txtRetries.text = retries.ToString();

        isInit = true;
    }

    public void updateCall()
    {
        if (!BarRaiser.instance.firstTouch)
            return;

        updateTimer();

        updateTraveledDistance(Vector2.Distance(Player.instance.startPoint, Player.instance.transform.position));

    }

    public void showGameOver()
    {
        isNewHighscore = SaveLoadManager.saveWhenNewHighscore(distance, accuracy);
        StartCoroutine(showHighscoreAnimationRoutine());
        StartCoroutine(skipHighscoreRoutine());
    }

    public void updateTraveledDistance(float value)
    {
        distance = value;
        txtTraveledDistance.text = Mathf.RoundToInt(value) + " m";
    }

    public void updateTimer()
    {
        if (GameHandler.instance.isGameOver)
            return;
        timer += Time.deltaTime;
    }

    public void addAccuracy(int value)
    {
        txtAccuracy.text = accuracy.ToString();
        if(curVisualCountingRoutine != null)
            StopCoroutine(curVisualCountingRoutine);
            
        accuracy += value;
        curVisualCountingRoutine = StartCoroutine(visualCountingRoutine(value));
    }

    public void addRetries(int value)
    {
        retries += value;
        txtRetries.text = retries.ToString();
    }

    public void addPerfectBarAttempt(bool isSuccess)
    {
        if (isSuccess)
        {
            attemptsInRow++;

            if (attemptsInRow % nSuccessAttemptsForRetry == 0)
            {
                addRetries(1);
            }
        }
        else
        {
            attemptsInRow = 0;
            addRetries(-1);
        }
    }

    private IEnumerator visualCountingRoutine(int value)
    {
        float duration = 1.0f;
        int startValue = accuracy;
        int endValue = accuracy + value;
        float time = 0;
        int tmpValue = accuracy;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            tmpValue = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, t));
            txtAccuracy.text = accuracy.ToString();
            yield return null;
        }

        txtAccuracy.text = accuracy.ToString();
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
            distance,
            countDuration,
            2,
            " m"));
        yield return new WaitForSeconds(countDuration + 0.1f);

        gameOverAccuracy.gameObject.SetActive(true);
        StartCoroutine(countToTargetRoutine(
            gameOverAccuracy.transform.GetChild(1).GetComponent<Text>(),
            accuracy,
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
        float highscoreValue = SaveLoadManager.calculateHighscore(distance, accuracy);
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

        if (targetValue != 0)
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


}
