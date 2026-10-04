using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    private const string DistanceKey = "distance";
    private const string AccuracyKey = "accuracy";

    public static bool saveWhenNewHighscore(float distance, int accuracy)
    {
        if (calculateHighscore(distance, accuracy) <= getLastHighscore())
            return false;

        PlayerPrefs.SetFloat(DistanceKey, distance);
        PlayerPrefs.SetInt(AccuracyKey, accuracy);
        PlayerPrefs.Save();
        return true;
    }

    public static int calculateHighscore(float distance, int accuracy)
    {
        if (float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0f || accuracy <= 0)
            return 0;
        double score = (double)distance * accuracy / 10d;
        return (int)System.Math.Min(int.MaxValue, System.Math.Round(score, System.MidpointRounding.ToEven));
    }

    public static int getLastHighscore()
    {
        return calculateHighscore(PlayerPrefs.GetFloat(DistanceKey, 0f), PlayerPrefs.GetInt(AccuracyKey, 0));
    }
}
