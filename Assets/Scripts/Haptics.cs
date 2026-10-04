using UnityEngine;

public static class Haptics
{
    public const string PreferenceKey = "vibrationEnabled";

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(PreferenceKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(PreferenceKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static void PlayPerfect()
    {
        if (!Enabled)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                activity.Call("runOnUiThread", new AndroidJavaRunnable(PlayAndroidFeedback));
        }
        catch (AndroidJavaException exception)
        {
            Debug.LogWarning("Haptic feedback unavailable: " + exception.Message);
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void PlayAndroidFeedback()
    {
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var window = activity.Call<AndroidJavaObject>("getWindow"))
            using (var view = window.Call<AndroidJavaObject>("getDecorView"))
            using (var constants = new AndroidJavaClass("android.view.HapticFeedbackConstants"))
                view.Call<bool>("performHapticFeedback", constants.GetStatic<int>("CLOCK_TICK"));
        }
        catch (AndroidJavaException exception)
        {
            Debug.LogWarning("Haptic feedback unavailable: " + exception.Message);
        }
    }
#endif
}
