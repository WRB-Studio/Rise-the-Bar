using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    [Header("Ingame music settings")]
    public AudioSource ingameMusic;
    public Vector2 ingamePitchRange;
    public float ingamePitchDuration;

    [Header("GameOver music settings")]
    public AudioSource gameOverMusic;
    public Vector2 gameOverPitchRange;
    public float gameOverPitchDuration;

    [Header("Sounds")]
    public AudioSource releaseBarSound;
    public AudioSource releaseBarPerfectSound;
    public AudioSource releaseBarFailed;
    public AudioSource jumpSound;
    public AudioSource dieSound;
    public AudioSource newHighscore;
    public AudioSource countingSound;
    public float countingSoundDelay;

    public static AudioController instance;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        playIngameMusic();
    }

    public static void playIngameMusic()
    {
        instance.gameOverMusic.Stop();

        instance.ingameMusic.Play();
        instance.StartCoroutine(instance.ChangePitchOverTime(instance.ingameMusic, instance.ingamePitchRange, instance.ingamePitchDuration));
    }

    public static void playGameOverMusic()
    {
        instance.StartCoroutine(instance.ChangePitchOverTime(instance.gameOverMusic, instance.gameOverPitchRange, instance.gameOverPitchDuration));
    }

    public IEnumerator ChangePitchOverTime(AudioSource audioSource, Vector2 pitchRange, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            audioSource.pitch = Mathf.Lerp(pitchRange.x, pitchRange.y, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        audioSource.pitch = pitchRange.y; // Stelle sicher, dass der Ziel-Pitch am Ende erreicht wird
    }

    public static void playJump()
    {
        instance.jumpSound.Play();
    }

    public static void playReleaseBar()
    {
        instance.releaseBarSound.Play();
    }

    public static void playReleaseBarPerfect()
    {
        instance.releaseBarPerfectSound.Play();
    }

    public static void playReleaseBarFailed()
    {
        instance.releaseBarFailed.Play();
    }

    public static void playDie()
    {
        instance.dieSound.Play();
    }

    public static void playNewHighscore()
    {
        instance.newHighscore.Play();
    }

    public static void playCounting(float duration)
    {
        instance.StartCoroutine(instance.playCountingRoutine(duration));
    }

    private IEnumerator playCountingRoutine(float duration)
    {
        yield return null;

        while (duration > 0)
        {
            instance.countingSound.Play();
            yield return new WaitForSeconds(countingSoundDelay);
            duration -= countingSoundDelay; // Reduziere duration um die Verzögerung nach jedem Durchgang
        }
    }
}
