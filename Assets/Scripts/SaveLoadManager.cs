using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager instance;

    private void Awake()
    {
        instance = this;
    }

    public static bool saveWhenNewHighscore(float distance, int accuracy)
    {
        if (isNewHighscore(distance, accuracy))
        {
            PlayerPrefs.SetFloat("distance", distance);
            PlayerPrefs.SetInt("accuracy", accuracy);

            PlayerPrefs.Save();

            return true;
        }

        return false;
    }

    public static int calculateHighscore(float distance, int accuracy)
    {
        return Mathf.RoundToInt(distance * accuracy / 10f);
    }

    public static int getLastHighscore()
    {
        return calculateHighscore(loadDistance(), loadAccuracy());
    }

    private static bool isNewHighscore(float distance, int accuracy)
    {
        float currentHighScore = calculateHighscore(distance, accuracy);
        int lastHighScore = getLastHighscore();

        if (currentHighScore > lastHighScore)
            return true;

        return false;
    }

    private static float loadDistance()
    {
        return PlayerPrefs.GetFloat("distance", 0);
    }

    private static int loadAccuracy()
    {
        return PlayerPrefs.GetInt("accuracy", 0);
    }
}
