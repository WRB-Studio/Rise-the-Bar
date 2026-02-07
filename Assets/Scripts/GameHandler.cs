using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameHandler : MonoBehaviour
{
    public float maxDifficultyAfterSec = 30f;
    public float currentDifficulty = 0;

    public bool isGameOver = false;

    private bool gameIsInitialized = false;

    public static GameHandler instance;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        PlatformController.instance.init();
        CameraController.instance.init();
        BarRaiser.instance.init();
        GUI.instance.init();
        Player.instance.init();

        gameIsInitialized = true;
    }

    private void Update()
    {
        if (!gameIsInitialized)
            return;

        PlatformController.instance.updateCallSlow();
        BarRaiser.instance.updateCall();
        GUI.instance.updateCall();
        Player.instance.updateCall();

        updateDifficulty();
    }

    private void LateUpdate()
    {
        if (!gameIsInitialized)
            return;
        CameraController.instance.lateUpdateCall();
    }

    private void updateDifficulty()
    {
        float elapsedTime = GUI.instance.timer;
        float maxDifficultyTime = maxDifficultyAfterSec;
        currentDifficulty = Mathf.Clamp01(elapsedTime / maxDifficultyTime);
    }

    public void setIsGameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;
        GUI.instance.showGameOver();
    }

}
