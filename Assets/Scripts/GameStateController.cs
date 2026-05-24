using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateController : MonoBehaviour
{
    public enum GameFlowState
    {
        Exploration,
        Recording,
        PlayerDead,
        LevelCompleted,
        Restarting
    }

    public enum ImportantEventType
    {
        GoalStep,
        GoalReady,
        LevelCompleted,
        PlayerDeath
    }

    public static GameStateController Instance { get; private set; }

    public GameFlowState CurrentState { get; private set; } = GameFlowState.Exploration;

    bool _restartQueued;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetRecording(bool recording, Vector3 focusPoint, Vector3 up)
    {
        SetState(recording ? GameFlowState.Recording : GameFlowState.Exploration, focusPoint, up, false);
    }

    public void NotifyGoalStep(Vector3 focusPoint)
    {
        GameFeelController.Instance?.PlayPlatePress(focusPoint);
        FocusCameraEvent(focusPoint, 0.22f, 0.3f, 48f);
    }

    public void NotifyGoalReady(Vector3 focusPoint)
    {
        GameFeelController.Instance?.PlayPuzzleSolved(focusPoint);
        FocusCameraEvent(focusPoint, 0.42f, 0.9f, 50f);
    }

    public void NotifyPlayerDeath(Vector3 focusPoint)
    {
        SetState(GameFlowState.PlayerDead, focusPoint, Vector3.up, true);
        FocusCameraEvent(focusPoint, 0.35f, 0.5f, 45f);
    }

    public void NotifyLevelCompleted(Vector3 focusPoint)
    {
        SetState(GameFlowState.LevelCompleted, focusPoint, Vector3.up, true);
        FocusCameraEvent(focusPoint, 0.55f, 1.1f, 52f);
    }

    public void RequestSceneRestart(float delaySeconds = 0f)
    {
        if (_restartQueued)
            return;

        StartCoroutine(RestartSceneRoutine(delaySeconds));
    }

    IEnumerator RestartSceneRoutine(float delaySeconds)
    {
        _restartQueued = true;
        SetState(GameFlowState.Restarting, Vector3.zero, Vector3.up, false);

        PostProcessingSetup.PrepareForSceneReload();
        LevelRuntimeController.Instance?.PrepareForSceneReload();

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (delaySeconds > 0f)
            yield return new WaitForSecondsRealtime(delaySeconds);

        yield return null;

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    void FocusCameraEvent(Vector3 focusPoint, float weight, float holdSeconds, float pulseFov)
    {
        ThirdPersonCamera legacyCamera = ThirdPersonCamera.ResolveActive();
        if (legacyCamera != null)
        {
            legacyCamera.RequestEventFocus(focusPoint, weight, holdSeconds, pulseFov);
            return;
        }

        FixedPuzzleCameraController fixedCamera = FixedPuzzleCameraController.ResolveActive();
        fixedCamera?.RequestEventFocus(focusPoint, weight, holdSeconds, pulseFov);
    }

    void SetState(GameFlowState nextState, Vector3 focusPoint, Vector3 up, bool react)
    {
        if (CurrentState == nextState)
            return;

        CurrentState = nextState;
        if (!react)
            return;

        switch (nextState)
        {
            case GameFlowState.Recording:
                GameFeelController.Instance?.PlayRecordStart(focusPoint, up);
                break;
            case GameFlowState.PlayerDead:
                GameFeelController.Instance?.PlayPlayerDeath(focusPoint);
                break;
            case GameFlowState.LevelCompleted:
                GameFeelController.Instance?.PlayPuzzleSolved(focusPoint);
                break;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
