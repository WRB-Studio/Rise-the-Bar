using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;

public class GameplayTests
{
    private bool hadDistance;
    private bool hadAccuracy;
    private float savedDistance;
    private int savedAccuracy;
    private bool hadVibration;
    private int savedVibration;

    [UnitySetUp]
    public IEnumerator LoadGame()
    {
        hadDistance = PlayerPrefs.HasKey("distance");
        hadAccuracy = PlayerPrefs.HasKey("accuracy");
        savedDistance = PlayerPrefs.GetFloat("distance");
        savedAccuracy = PlayerPrefs.GetInt("accuracy");
        hadVibration = PlayerPrefs.HasKey(Haptics.PreferenceKey);
        savedVibration = PlayerPrefs.GetInt(Haptics.PreferenceKey);
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("MainWorld");
        yield return null;
        Call(GameHandler.instance, "OnApplicationFocus", true);
        Call(GameHandler.instance, "OnApplicationPause", false);
    }

    [UnityTearDown]
    public IEnumerator RestorePreferences()
    {
        if (hadDistance)
            PlayerPrefs.SetFloat("distance", savedDistance);
        else
            PlayerPrefs.DeleteKey("distance");
        if (hadAccuracy)
            PlayerPrefs.SetInt("accuracy", savedAccuracy);
        else
            PlayerPrefs.DeleteKey("accuracy");
        if (hadVibration)
            PlayerPrefs.SetInt(Haptics.PreferenceKey, savedVibration);
        else
            PlayerPrefs.DeleteKey(Haptics.PreferenceKey);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        yield return null;
    }

    [UnityTest]
    public IEnumerator TutorialDoesNotMoveOrAutoJump()
    {
        Rigidbody2D body = Player.instance.GetComponent<Rigidbody2D>();
        Vector2 start = body.position;
        yield return new WaitForSeconds(0.6f);
        Assert.That(body.position.x, Is.EqualTo(start.x).Within(0.02f));
        Assert.That(body.position.y, Is.LessThanOrEqualTo(start.y + 0.02f));
        Assert.That(BarRaiser.instance.firstTouch, Is.False);
        Assert.That(GameHandler.instance.ElapsedTime, Is.Zero);
        Assert.That(GameHandler.instance.Retries, Is.EqualTo(3));
        Assert.That(GUI.instance.perfectsForRetry, Is.EqualTo(2));
    }

    [Test]
    public void GeneratedPlatformsHaveConfiguredGapAndHeight()
    {
        var platforms = PlatformController.instance.getPlatforms();
        for (int i = 1; i < platforms.Count; i++)
        {
            Bounds previous = platforms[i - 1].GetComponent<Collider2D>().bounds;
            Bounds next = platforms[i].GetComponent<Collider2D>().bounds;
            Assert.That(next.min.x - previous.max.x, Is.InRange(1.999f, 2.201f));
            Assert.That(next.size.y, Is.EqualTo(PlatformController.instance.platformScaleY).Within(0.001f));
        }
    }

    [Test]
    public void OnlyConsecutivePerfectBarsAwardRetry()
    {
        GameHandler game = GameHandler.instance;
        game.RecordBar(200, true, false);
        game.RecordBar(100, false, false);
        game.RecordBar(200, true, false);
        Assert.That(game.Retries, Is.EqualTo(3));
        game.RecordBar(200, true, false);
        Assert.That(game.Retries, Is.EqualTo(4));
        game.RecordBar(0, false, true);
        Assert.That(game.PerfectStreak, Is.Zero);
        Assert.That(game.Retries, Is.EqualTo(3));
        Assert.That(game.Accuracy, Is.EqualTo(750));
    }

    [Test]
    public void PerfectComboAwardsCappedBonusesAndNormalHitsResetIt()
    {
        GameHandler game = GameHandler.instance;
        for (int combo = 1; combo <= 8; combo++)
        {
            game.RecordBar(200, true, false);
            Assert.That(game.PerfectStreak, Is.EqualTo(combo));
            Assert.That(game.LastComboBonus, Is.EqualTo(Mathf.Min((combo - 1) * 50, 250)));
        }
        Assert.That(game.Accuracy, Is.EqualTo(2850));
        game.RecordBar(100, false, false);
        Assert.That(game.PerfectStreak, Is.Zero);
        Assert.That(game.LastComboBonus, Is.Zero);
        game.RecordBar(200, true, false);
        Assert.That(game.LastBarPoints, Is.EqualTo(200));
        Assert.That(game.Accuracy, Is.EqualTo(3150));
    }

    [Test]
    public void ComboScoreDoesNotOverflowAndCancelledBarsKeepCombo()
    {
        GameHandler game = GameHandler.instance;
        game.RecordBar(int.MaxValue, true, false);
        game.RecordBar(200, true, false);
        Assert.That(game.Accuracy, Is.EqualTo(int.MaxValue));
        Call(BarRaiser.instance, "CreateNewBar");
        BarRaiser.instance.CancelInput();
        Assert.That(game.PerfectStreak, Is.EqualTo(2));
        game.RecordBar(0, false, true);
        Assert.That(game.PerfectStreak, Is.Zero);
        Assert.That(game.LastComboBonus, Is.Zero);
    }

    [Test]
    public void PerfectSoundRisesWithComboIsCappedAndResets()
    {
        AudioController audio = AudioController.instance;
        Assert.That(audio.releaseBarSound.clip, Is.Not.Null);
        Assert.That(audio.releaseBarPerfectSound.clip, Is.Not.EqualTo(audio.releaseBarSound.clip));
        Assert.That(audio.releaseBarFailed.clip, Is.Not.EqualTo(audio.releaseBarSound.clip));
        AudioController.playReleaseBarPerfect(1);
        float basePitch = audio.releaseBarPerfectSound.pitch;
        AudioController.playReleaseBarPerfect(3);
        Assert.That(audio.releaseBarPerfectSound.pitch, Is.GreaterThan(basePitch));
        AudioController.playReleaseBarPerfect(100);
        Assert.That(audio.releaseBarPerfectSound.pitch, Is.EqualTo(Mathf.Min(3f, basePitch * audio.maximumComboPitchMultiplier)).Within(0.001f));
        AudioController.playReleaseBarPerfect(1);
        Assert.That(audio.releaseBarPerfectSound.pitch, Is.EqualTo(basePitch));
    }

    [UnityTest]
    public IEnumerator PerfectFeedbackShowsActualBonusAndDoesNotBlockNextBar()
    {
        Player.instance.minMaxNormalSpeed = Vector2.zero;
        Player.instance.minMaxIdleSpeed = Vector2.zero;
        GameHandler.instance.RecordBar(200, true, false);
        Assert.That(Call(BarRaiser.instance, "CreateNewBar"), Is.True);
        Platform mount = PlatformController.instance.FirstPlatform;
        Platform target = PlatformController.instance.GetNextPlatform(mount);
        mount.barObject.SetLength(target.GetComponent<Collider2D>().bounds.min.x + BarRaiser.instance.perfectAreaGap
            + BarRaiser.instance.perfectAreaLength / 2f - mount.barMountPoint.transform.position.x);
        Call(BarRaiser.instance, "ReleaseBar");
        HitFeedbackUI feedback = GUI.instance.HitFeedback;
        Assert.That(GameHandler.instance.Accuracy, Is.EqualTo(450));
        Assert.That(feedback.PerfectPoints.text, Does.Contain("+250 pts"));
        Assert.That(feedback.PerfectTitle.gameObject.activeInHierarchy, Is.True);
        Assert.That(feedback.PerfectTitle.raycastTarget, Is.False);
        Assert.That(feedback.PerfectTitle.GetComponentInParent<CanvasGroup>().blocksRaycasts, Is.False);
        Assert.That(Call(BarRaiser.instance, "CreateNewBar"), Is.True);
        yield return new WaitForSeconds(1.1f);
        Assert.That(feedback.PerfectTitle.gameObject.activeInHierarchy, Is.False);
        Assert.That(feedback.ComboText.text, Is.EqualTo("COMBO x2"));
        Assert.That(feedback.ComboText.gameObject.activeInHierarchy, Is.True);
        GameHandler.instance.RecordBar(100, false, false);
        yield return null;
        Assert.That(feedback.ComboText.gameObject.activeInHierarchy, Is.False);
        feedback.ShowPerfect(3, 300, target.transform.position);
        yield return new WaitForSeconds(0.6f);
        feedback.ShowPerfect(4, 350, target.transform.position);
        yield return new WaitForSeconds(0.4f);
        Assert.That(feedback.PerfectTitle.gameObject.activeInHierarchy, Is.True);
        Assert.That(feedback.PerfectPoints.text, Does.Contain("+350 pts"));
        yield return new WaitForSeconds(0.6f);
        Assert.That(feedback.PerfectTitle.gameObject.activeInHierarchy, Is.False);
    }

    [UnityTest]
    public IEnumerator PerfectFeedbackStaysVisibleForTargetsOutsideScreenAndFollowsCamera()
    {
        GameHandler.instance.enabled = false;
        Camera camera = Camera.main;
        HitFeedbackUI feedback = GUI.instance.HitFeedback;
        RectTransform panel = feedback.PerfectTitle.GetComponentInParent<CanvasGroup>(true).GetComponent<RectTransform>();
        var corners = new Vector3[4];
        float depth = camera.WorldToScreenPoint(Vector3.zero).z;
        Vector3 rightTarget = camera.ViewportToWorldPoint(new Vector3(1.5f, 0.25f, depth));
        Vector2[] targetViewports = { new Vector2(1.5f, 0.25f), new Vector2(-0.5f, 0.25f), new Vector2(0.8f, 1.5f) };
        foreach (Vector2 viewport in targetViewports)
        {
            feedback.ShowPerfect(6, 450, camera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth)));
            yield return new WaitForSeconds(0.15f);
            panel.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, corner);
                Assert.That(screen.x, Is.InRange(Screen.safeArea.xMin, Screen.safeArea.xMax));
                Assert.That(screen.y, Is.InRange(Screen.safeArea.yMin, Screen.safeArea.yMax));
            }
            feedback.PerfectTitle.rectTransform.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 local = panel.InverseTransformPoint(corner);
                Assert.That(local.x, Is.InRange(panel.rect.xMin, panel.rect.xMax));
                Assert.That(local.y, Is.InRange(panel.rect.yMin, panel.rect.yMax));
            }
        }
        feedback.ShowPerfect(6, 450, rightTarget);
        yield return null;
        float clampedX = panel.position.x;
        camera.transform.position += Vector3.right * camera.orthographicSize * camera.aspect * 1.6f;
        yield return null;
        Assert.That(panel.position.x, Is.LessThan(clampedX - 20f));
    }

    [UnityTest]
    public IEnumerator PerfectFeedbackAppearsAboveVisibleTargetInsteadOfScreenCenter()
    {
        GameHandler.instance.enabled = false;
        Camera camera = Camera.main;
        float depth = camera.WorldToScreenPoint(Vector3.zero).z;
        Vector3 hitPosition = camera.ViewportToWorldPoint(new Vector3(0.75f, 0.3f, depth));
        HitFeedbackUI feedback = GUI.instance.HitFeedback;
        feedback.ShowPerfect(3, 300, hitPosition);
        yield return new WaitForSeconds(0.15f);
        RectTransform panel = feedback.PerfectTitle.GetComponentInParent<CanvasGroup>().GetComponent<RectTransform>();
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, panel.position);
        Vector3 targetScreen = camera.WorldToScreenPoint(hitPosition);
        Assert.That(screen.x, Is.EqualTo(targetScreen.x).Within(2f));
        Assert.That(screen.y, Is.GreaterThan(targetScreen.y + 10f));
        Assert.That(feedback.PerfectTitle.color, Is.EqualTo(Color.white));
        UnityEngine.UI.Image background = panel.GetComponent<UnityEngine.UI.Image>();
        Assert.That(background.raycastTarget, Is.False);
    }

    [UnityTest]
    public IEnumerator VibrationSettingCanBeClickedAndSurvivesReplay()
    {
        PlayerPrefs.DeleteKey(Haptics.PreferenceKey);
        Assert.That(Haptics.Enabled, Is.False);
        UnityEngine.UI.Button button = GUI.instance.HitFeedback.VibrationButton;
        Canvas.ForceUpdateCanvases();
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null,
                button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits.Count, Is.GreaterThan(0));
        GameObject clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        Assert.That(clickTarget, Is.EqualTo(button.gameObject));
        ExecuteEvents.Execute(clickTarget, pointer, ExecuteEvents.pointerClickHandler);
        Assert.That(Haptics.Enabled, Is.True);
        yield return SceneManager.LoadSceneAsync("MainWorld");
        yield return null;
        Assert.That(Haptics.Enabled, Is.True);
        Assert.That(GUI.instance.HitFeedback.VibrationButton.GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("Vibration: On"));
        GUI.instance.HitFeedback.VibrationButton.onClick.Invoke();
        Assert.That(Haptics.Enabled, Is.False);
    }

    [Test]
    public void DisabledRetryBonusDoesNotDivideByZero()
    {
        GUI.instance.perfectsForRetry = 0;
        GameHandler.instance.RecordBar(200, true, false);
        Assert.That(GameHandler.instance.Retries, Is.EqualTo(3));
    }

    [Test]
    public void HorizontalDistanceIgnoresJumpHeightAndDoesNotGoBackwards()
    {
        BarRaiser.instance.firstTouch = true;
        Rigidbody2D body = Player.instance.GetComponent<Rigidbody2D>();
        body.position = Player.instance.startPoint + new Vector2(10f, 100f);
        Physics2D.SyncTransforms();
        Call(GameHandler.instance, "UpdateDistance");
        Assert.That(GameHandler.instance.Distance, Is.EqualTo(10f).Within(0.001f));
        body.position = Player.instance.startPoint + new Vector2(5f, 0f);
        Call(GameHandler.instance, "UpdateDistance");
        Assert.That(GameHandler.instance.Distance, Is.EqualTo(10f).Within(0.001f));
    }

    [Test]
    public void ExistingHighscoreIsKeptUnlessRunIsBetter()
    {
        PlayerPrefs.SetFloat("distance", 10f);
        PlayerPrefs.SetInt("accuracy", 200);
        Assert.That(SaveLoadManager.saveWhenNewHighscore(20f, 50), Is.False);
        Assert.That(SaveLoadManager.saveWhenNewHighscore(10f, 200), Is.False);
        Assert.That(PlayerPrefs.GetFloat("distance"), Is.EqualTo(10f));
        Assert.That(SaveLoadManager.saveWhenNewHighscore(20f, 200), Is.True);
        Assert.That(SaveLoadManager.getLastHighscore(), Is.EqualTo(400));
    }

    [Test]
    public void DecorativeGraphicsDoNotBlockGameInput()
    {
        Canvas canvas = GUI.instance.panelTop.GetComponentInParent<Canvas>();
        foreach (UnityEngine.UI.Graphic graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        {
            if (!graphic.GetComponentInParent<UnityEngine.UI.Selectable>(true))
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
        }
    }

    [Test]
    public void GrowthClampsAtMaximumLength()
    {
        Call(BarRaiser.instance, "CreateNewBar");
        BarRaiser.instance.minMaxGrowthSpeed = new Vector2(100000f, 100000f);
        Call(BarRaiser.instance, "GrowBar");
        float maximum = (float)Field(BarRaiser.instance, "maxHeight");
        Assert.That(PlatformController.instance.FirstPlatform.barObject.Length, Is.EqualTo(maximum).Within(0.001f));
    }

    [Test]
    public void CancelledBarDoesNotSpendRetryAndCanBeRecreated()
    {
        Assert.That(Call(BarRaiser.instance, "CreateNewBar"), Is.True);
        Platform mount = PlatformController.instance.FirstPlatform;
        BarRaiser.instance.CancelInput();
        Assert.That(mount.barObject, Is.Null);
        Assert.That(GameHandler.instance.Retries, Is.EqualTo(3));
        Assert.That(Call(BarRaiser.instance, "CreateNewBar"), Is.True);
    }

    [Test]
    public void BarGrowthKeepsPivotFixedAndPerfectMarkerMatchesScore()
    {
        Assert.That(Call(BarRaiser.instance, "CreateNewBar"), Is.True);
        Platform mount = PlatformController.instance.FirstPlatform;
        Platform target = PlatformController.instance.GetNextPlatform(mount);
        float perfectTip = target.GetComponent<Collider2D>().bounds.min.x + BarRaiser.instance.perfectAreaGap + BarRaiser.instance.perfectAreaLength / 2f;
        Bar bar = mount.barObject;
        float bottom = bar.transform.position.y - bar.Length / 2f;
        bar.SetLength(perfectTip - mount.barMountPoint.transform.position.x);
        Assert.That(bar.transform.position.y - bar.Length / 2f, Is.EqualTo(bottom).Within(0.001f));
        GameObject marker = (GameObject)Field(BarRaiser.instance, "currentPerfectMarker");
        Assert.That(marker.transform.position.x, Is.EqualTo(perfectTip).Within(0.001f));
        Call(BarRaiser.instance, "ReleaseBar");
        Assert.That(GameHandler.instance.Accuracy, Is.EqualTo(200));
        Assert.That(bar.GetComponent<Rigidbody2D>().simulated, Is.True);
    }

    [UnityTest]
    public IEnumerator PerfectBridgeCanBeCrossedUsingPhysicsMovement()
    {
        Player.instance.minMaxNormalSpeed = new Vector2(2f, 2f);
        Player.instance.minMaxIdleSpeed = Vector2.zero;
        GameHandler.instance.maxDifficultyAfterSec = float.MaxValue;
        Call(BarRaiser.instance, "CreateNewBar");
        Platform mount = PlatformController.instance.FirstPlatform;
        Platform target = PlatformController.instance.GetNextPlatform(mount);
        float targetLeft = target.GetComponent<Collider2D>().bounds.min.x;
        mount.barObject.SetLength(targetLeft + BarRaiser.instance.perfectAreaGap + BarRaiser.instance.perfectAreaLength / 2f - mount.barMountPoint.transform.position.x);
        Call(BarRaiser.instance, "ReleaseBar");
        yield return new WaitForSeconds(6f);
        Assert.That(GameHandler.instance.isGameOver, Is.False);
        Assert.That(Player.instance.GetComponent<Rigidbody2D>().position.x, Is.GreaterThan(targetLeft));
    }

    [UnityTest]
    public IEnumerator LandedBridgeStaysInPlaceWhenNextBarIsRaised()
    {
        Player.instance.minMaxNormalSpeed = Vector2.zero;
        Player.instance.minMaxIdleSpeed = Vector2.zero;
        Assert.That(Call(BarRaiser.instance, "CreateNewBar"), Is.True);
        Bar bridge = PlatformController.instance.FirstPlatform.barObject;
        Platform targetPlatform = PlatformController.instance.GetNextPlatform(PlatformController.instance.FirstPlatform);
        float targetLeft = targetPlatform.GetComponent<Collider2D>().bounds.min.x;
        bridge.SetLength(targetLeft + 0.01f - PlatformController.instance.FirstPlatform.barMountPoint.transform.position.x);
        Call(BarRaiser.instance, "ReleaseBar");
        float deadline = Time.time + 6f;
        while (!bridge.getHasFallen() && Time.time < deadline)
            yield return null;
        Assert.That(bridge.getHasFallen(), Is.True);
        Assert.That(bridge.GetComponent<Collider2D>().bounds.center.y,
            Is.GreaterThanOrEqualTo(PlatformController.instance.FirstPlatform.GetComponent<Collider2D>().bounds.max.y - 0.1f));
        Vector2 landedPosition = bridge.GetComponent<Rigidbody2D>().position;
        float landedAngle = bridge.GetComponent<Rigidbody2D>().rotation;

        Assert.That(Call(BarRaiser.instance, "CreateNewBar"), Is.True);
        Player.instance.minMaxNormalSpeed = new Vector2(2f, 2f);
        GameHandler.instance.maxDifficultyAfterSec = float.MaxValue;
        for (int i = 0; i < 30; i++)
        {
            Call(BarRaiser.instance, "GrowBar");
            yield return null;
        }
        yield return new WaitForSeconds(4f);
        Assert.That(GameHandler.instance.isGameOver, Is.False);
        Assert.That(Player.instance.Position.x, Is.GreaterThan(targetLeft));
        Assert.That(Vector2.Distance(bridge.GetComponent<Rigidbody2D>().position, landedPosition), Is.LessThan(0.03f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(landedAngle, bridge.GetComponent<Rigidbody2D>().rotation)), Is.LessThan(1f));
    }

    [UnityTest]
    public IEnumerator ReplayButtonReceivesPointerAndStartsFreshRun()
    {
        GameHandler previousGame = GameHandler.instance;
        previousGame.RecordBar(200, true, false);
        previousGame.RecordBar(200, true, false);
        PlatformController.instance.gameOverDestruction();
        yield return new WaitForSeconds(1.6f);
        UnityEngine.UI.Button button = GUI.instance.btnReplay;
        button.gameObject.SetActive(true);
        yield return null;
        Canvas.ForceUpdateCanvases();
        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, button.GetComponent<RectTransform>().position),
            button = PointerEventData.InputButton.Left
        };
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits.Count, Is.GreaterThan(0), "Replay button cannot be hit by a pointer.");
        GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        Assert.That(target, Is.EqualTo(button.gameObject));
        ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        yield return null;
        yield return null;
        Assert.That(GameHandler.instance, Is.Not.SameAs(previousGame));
        Assert.That(GameHandler.instance.isGameOver, Is.False);
        Assert.That(GameHandler.instance.Accuracy, Is.Zero);
        Assert.That(GameHandler.instance.PerfectStreak, Is.Zero);
        Assert.That(GameHandler.instance.LastComboBonus, Is.Zero);
        Assert.That(BarRaiser.instance.firstTouch, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [UnityTest]
    public IEnumerator AccuracyAnimationEndsAtActualScoreAfterInterruptedCounting()
    {
        GameHandler.instance.RecordBar(200, true, false);
        GUI.instance.updateCall();
        yield return new WaitForSeconds(0.2f);
        Assert.That(int.Parse(GUI.instance.txtAccuracy.text), Is.InRange(1, 199));
        GameHandler.instance.RecordBar(100, false, false);
        GUI.instance.updateCall();
        yield return new WaitForSeconds(1.2f);
        Assert.That(GUI.instance.txtAccuracy.text, Is.EqualTo("300"));
    }

    [UnityTest]
    public IEnumerator FailedBarDestroysWholeObject()
    {
        Call(BarRaiser.instance, "CreateNewBar");
        GameObject bar = PlatformController.instance.FirstPlatform.barObject.gameObject;
        Call(BarRaiser.instance, "ReleaseBar");
        Assert.That(GameHandler.instance.Retries, Is.EqualTo(2));
        yield return new WaitForSeconds(5.2f);
        Assert.That(bar == null, Is.True);
    }

    [UnityTest]
    public IEnumerator GameOverDestroysAllPlatformsAndFreezesResults()
    {
        BarRaiser.instance.firstTouch = true;
        GameHandler game = GameHandler.instance;
        game.RecordBar(200, true, false);
        PlatformController.instance.gameOverDestruction();
        PlatformController.instance.gameOverDestruction();
        float finalDistance = game.Distance;
        Player.instance.GetComponent<Rigidbody2D>().position += new Vector2(50f, -10f);
        game.RecordBar(200, true, false);
        yield return new WaitForSeconds(4f);
        Assert.That(PlatformController.instance.getPlatforms().Count, Is.Zero);
        Assert.That(game.Distance, Is.EqualTo(finalDistance));
        Assert.That(game.Accuracy, Is.EqualTo(200));
    }

    [Test]
    public void PauseCancelsInputAndRestoresTimeScale()
    {
        Call(BarRaiser.instance, "CreateNewBar");
        Call(GameHandler.instance, "OnApplicationPause", true);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(PlatformController.instance.FirstPlatform.barObject, Is.Null);
        Call(GameHandler.instance, "OnApplicationPause", false);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(GameHandler.instance.Retries, Is.EqualTo(3));
    }

    [UnityTest]
    public IEnumerator CloudsAreDestroyedAfterLeavingView()
    {
        WorldObjectController clouds = Object.FindFirstObjectByType<WorldObjectController>();
        IList objects = (IList)Field(clouds, "activeObjects");
        Assert.That(objects.Count, Is.GreaterThan(0));
        Transform cloud = (Transform)objects[0].GetType().GetField("transform").GetValue(objects[0]);
        cloud.position = Vector3.left * 1000f;
        Call(clouds, "Update");
        yield return null;
        Assert.That(cloud == null, Is.True);
    }

    [UnityTest]
    public IEnumerator CrowdedStarSpawningIsBoundedAndNewStarsUseCurrentColor()
    {
        StarsController stars = Object.FindFirstObjectByType<StarsController>(FindObjectsInactive.Include);
        stars.gameObject.SetActive(true);
        yield return null;
        stars.enabled = false;
        IList spawned = (IList)Field(stars, "spawnedStars");
        foreach (GameObject star in spawned)
            Object.Destroy(star);
        spawned.Clear();
        stars.minMaxX = Vector2.zero;
        stars.minMaxY = Vector2.zero;
        stars.SetColor(Color.red);
        for (int i = 0; i < 100; i++)
            Call(stars, "TrySpawnStar", false);
        Assert.That(spawned.Count, Is.EqualTo(1));
        Assert.That(((GameObject)spawned[0]).GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.red));
    }

    private static object Call(object target, string name, params object[] args)
    {
        return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }

    private static object Field(object target, string name)
    {
        return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
