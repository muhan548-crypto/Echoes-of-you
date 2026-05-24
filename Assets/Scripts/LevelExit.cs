using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Marca el nivel como completado y carga el siguiente destino.
/// Usa SceneTransitionManager para fade suave cuando está disponible.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LevelExit : MonoBehaviour
{
    public string nextSceneName;
    public bool loadNextBuildIndex = true;
    public float delaySeconds = 1.35f;

    [Header("Completion Copy")]
    [SerializeField] string completionToast = "";
    [SerializeField] string lockedToast = "Completa el puzzle antes de salir.";

    bool _triggered;
    bool _isUnlocked = true;
    LevelGoal _goal;
    Collider _collider;
    Renderer[] _renderers;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
        _renderers = GetComponentsInChildren<Renderer>(true);

        // Ensure Rigidbody exists for reliable trigger detection
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[LevelExit] OnTriggerEnter: {other.name} tag={other.tag}");

        if (!other.CompareTag("Player"))
            return;

        if (!_isUnlocked || (_goal != null && !_goal.CanComplete()))
        {
            GameHUD hud = FindAnyObjectByType<GameHUD>();
            string message = _goal != null ? _goal.GetLockedMessage() : lockedToast;
            Debug.Log($"[LevelExit] LOCKED: {message} | unlocked={_isUnlocked} goal={_goal != null} canComplete={_goal?.CanComplete()}");
            hud?.ShowToast(message, new Color(1f, 0.43f, 0.43f, 1f), 1.25f);
            GameFeelController.Instance?.PlaySoftError(transform.position);
            return;
        }

        if (_triggered)
            return;

        _triggered = true;
        Debug.Log($"[LevelExit] TRIGGERED! Loading: {nextSceneName}");

        string sceneName = SceneManager.GetActiveScene().name;
        GameProgress.MarkSceneCompleted(sceneName);

        string toast = !string.IsNullOrEmpty(completionToast)
            ? completionToast
            : (_goal != null ? _goal.GetCompletionToast() : ResolveCompletionToast(sceneName));
        LevelRuntimeController.Instance?.OnLevelCompleted(transform.position, toast);
        Invoke(nameof(LoadNext), delaySeconds);
    }

    void OnTriggerStay(Collider other)
    {
        // Fallback: if OnTriggerEnter was missed, try on stay
        if (!_triggered)
            OnTriggerEnter(other);
    }

    public void BindGoal(LevelGoal goal)
    {
        _goal = goal;
        bool unlocked = goal == null || goal.IsReady;
        Debug.Log($"[LevelExit] BindGoal: goal={goal != null} isReady={goal?.IsReady} => unlocked={unlocked}");
        SetUnlocked(unlocked);
    }

    public void SetUnlocked(bool unlocked)
    {
        _isUnlocked = unlocked;
        Debug.Log($"[LevelExit] SetUnlocked({unlocked})");
        UpdateVisualState();
    }

    void LoadNext()
    {
        Debug.Log($"[LevelExit] LoadNext => target={nextSceneName}");
        PostProcessingSetup.PrepareForSceneReload();

        string target = !string.IsNullOrEmpty(nextSceneName) ? nextSceneName : null;

        if (string.IsNullOrEmpty(target) && loadNextBuildIndex)
        {
            int next = SceneManager.GetActiveScene().buildIndex + 1;
            if (next < SceneManager.sceneCountInBuildSettings)
                target = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(next));
            else
                target = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(0));
        }

        if (string.IsNullOrEmpty(target))
        {
            Debug.LogWarning("[LevelExit] No target scene. Returning to MainMenu.");
            target = "MainMenu";
        }

        // Use SceneTransitionManager for smooth fade when available
        SceneTransitionManager stm = SceneTransitionManager.Instance;
        if (stm != null)
        {
            stm.LoadScene(target);
        }
        else
        {
            SceneManager.LoadScene(target);
        }
    }

    static string ResolveCompletionToast(string sceneName)
    {
        switch (sceneName)
        {
            case "Level_01": return "Primero recuerdas.";
            case "Level_02": return "Luego pruebas.";
            case "Level_03": return "Dos decisiones se sostienen.";
            case "Level_04": return "El orden cambia el camino.";
            case "Level_05": return "La precision revela el patron.";
            case "Level_06": return "Tu identidad vuelve al centro.";
            default: return "Recuerdo restaurado.";
        }
    }

    void UpdateVisualState()
    {
        if (_renderers == null)
            return;

        Color tint = _isUnlocked
            ? new Color(0.4f, 0.7f, 1f, 1f)
            : new Color(0.4f, 0.48f, 0.58f, 0.75f);

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer rendererRef = _renderers[i];
            if (rendererRef == null)
                continue;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            rendererRef.GetPropertyBlock(block);
            block.SetColor("_Color", tint);
            block.SetColor("_EmissionColor", _isUnlocked ? tint * 2.5f : tint * 0.3f);
            rendererRef.SetPropertyBlock(block);
        }
    }
}

