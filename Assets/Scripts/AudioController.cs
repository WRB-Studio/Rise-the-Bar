using System.Collections;
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

    [Header("Perfect combo sound")]
    [Min(0f)] public float comboPitchStep = 0.06f;
    [Min(1f)] public float maximumComboPitchMultiplier = 1.3f;

    public static AudioController instance;
    private Coroutine musicRoutine;
    private Coroutine countingRoutine;
    private AudioSource currentMusic;
    private float perfectBasePitch;

    private void Awake()
    {
        instance = this;
        perfectBasePitch = releaseBarPerfectSound ? releaseBarPerfectSound.pitch : 1f;
    }
    private void Start() => playIngameMusic();

    public static void playIngameMusic()
    {
        if (instance)
            instance.StartMusic(instance.ingameMusic, instance.ingamePitchRange, instance.ingamePitchDuration);
    }

    public static void playGameOverMusic()
    {
        if (instance)
            instance.StartMusic(instance.gameOverMusic, instance.gameOverPitchRange, instance.gameOverPitchDuration);
    }

    private void StartMusic(AudioSource source, Vector2 pitchRange, float duration)
    {
        if (!source)
            return;
        if (musicRoutine != null)
            StopCoroutine(musicRoutine);
        if (currentMusic && currentMusic != source)
            currentMusic.Stop();
        currentMusic = source;
        source.pitch = pitchRange.x;
        if (!source.isPlaying)
            source.Play();
        musicRoutine = StartCoroutine(ChangePitchOverTime(source, pitchRange, duration));
    }

    public IEnumerator ChangePitchOverTime(AudioSource source, Vector2 pitchRange, float duration)
    {
        float elapsed = 0f;
        while (source && elapsed < duration)
        {
            source.pitch = Mathf.Lerp(pitchRange.x, pitchRange.y, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (source)
            source.pitch = pitchRange.y;
    }

    private static void Play(AudioSource source)
    {
        if (source)
            source.Play();
    }

    public static void playJump() => Play(instance ? instance.jumpSound : null);
    public static void playReleaseBar() => Play(instance ? instance.releaseBarSound : null);
    public static void playReleaseBarPerfect(int combo = 1)
    {
        if (!instance || !instance.releaseBarPerfectSound)
            return;
        float multiplier = Mathf.Min(Mathf.Max(1f, instance.maximumComboPitchMultiplier),
            1f + Mathf.Max(0, combo - 1) * Mathf.Max(0f, instance.comboPitchStep));
        instance.releaseBarPerfectSound.pitch = Mathf.Clamp(instance.perfectBasePitch * multiplier, 0.1f, 3f);
        Play(instance.releaseBarPerfectSound);
    }
    public static void playReleaseBarFailed() => Play(instance ? instance.releaseBarFailed : null);
    public static void playDie() => Play(instance ? instance.dieSound : null);
    public static void playNewHighscore() => Play(instance ? instance.newHighscore : null);

    public static void playCounting(float duration)
    {
        if (!instance || !instance.countingSound || duration <= 0f)
            return;
        if (instance.countingRoutine != null)
            instance.StopCoroutine(instance.countingRoutine);
        instance.countingRoutine = instance.StartCoroutine(instance.PlayCountingRoutine(duration));
    }

    private IEnumerator PlayCountingRoutine(float duration)
    {
        float interval = Mathf.Max(0.01f, countingSoundDelay);
        WaitForSeconds delay = new WaitForSeconds(interval);
        while (duration > 0f)
        {
            countingSound.Play();
            yield return delay;
            duration -= interval;
        }
        countingRoutine = null;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
