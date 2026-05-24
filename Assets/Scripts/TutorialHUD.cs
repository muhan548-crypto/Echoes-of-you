using UnityEngine;

/// <summary>
/// Tutorial HUD mejorado: muestra objetivo del nivel al iniciar,
/// mensajes contextuales con fade, y hint de controles en nivel 1.
/// Singleton accesible via TutorialHUD.Instance.
/// Rutas a GameHUD usando UI Toolkit en lugar de OnGUI legacy.
/// </summary>
public class TutorialHUD : MonoBehaviour
{
    [Header("Mensajes por nivel")]
    [SerializeField] string[] levelObjectives = new string[]
    {
        "",                                                           // Level 1 — diseño enseña
        "",                                                           // Level 2 — diseño enseña
        "",                                                           // Level 3 — diseño enseña
        "",                                                           // Level 4 — diseño enseña
        "",                                                           // Level 5
        ""                                                            // Level 6
    };

    [Header("Diseño (Obsoleto — Controlado por USS)")]
    [SerializeField] Color bgColor = new Color(0.016f, 0.039f, 0.055f, 0.62f);
    [SerializeField] Color textColor = new Color(0f, 0.851f, 1f, 1f);           // #00D9FF
    [SerializeField] Color hintColor = new Color(0.6f, 0.6f, 0.7f, 1f);
    [SerializeField] Color objectiveColor = new Color(0.945f, 0.965f, 0.98f, 1f); // #F1F6FA
    [SerializeField] float fadeSpeed = 3f;

    [Header("Objetivo del nivel")]
    [SerializeField] float objectiveShowDuration = 3f;
    [SerializeField] bool showObjectiveOnStart = false;

    public static TutorialHUD Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (showObjectiveOnStart)
        {
            int sceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            int levelIndex = sceneIndex;
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName.Contains("01") || sceneName.Contains("_1")) levelIndex = 0;
            else if (sceneName.Contains("02") || sceneName.Contains("_2")) levelIndex = 1;
            else if (sceneName.Contains("03") || sceneName.Contains("_3")) levelIndex = 2;
            else if (sceneName.Contains("04") || sceneName.Contains("_4")) levelIndex = 3;
            else if (sceneName.Contains("05") || sceneName.Contains("_5")) levelIndex = 4;
            else if (sceneName.Contains("06") || sceneName.Contains("_6")) levelIndex = 5;
            else levelIndex = -1;

            if (levelIndex >= 0 && levelIndex < levelObjectives.Length)
            {
                ShowObjective(levelObjectives[levelIndex], objectiveShowDuration);
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Mostrar un mensaje de tutorial (bottom-center).</summary>
    public void ShowMessage(string message, string hint = "", float duration = 5f)
    {
        GameHUD hud = FindAnyObjectByType<GameHUD>();
        if (hud != null)
        {
            string combined = string.IsNullOrEmpty(hint)
                ? message
                : $"{message}\n{hint}";
            hud.SetPrompt(combined, duration);
        }
    }

    /// <summary>Show objective strip at top-center.</summary>
    public void ShowObjective(string text, float duration = 5f)
    {
        GameHUD hud = FindAnyObjectByType<GameHUD>();
        if (hud != null)
        {
            hud.SetObjective(text);
        }
    }

    /// <summary>Ocultar el mensaje actual.</summary>
    public void HideMessage()
    {
        GameHUD hud = FindAnyObjectByType<GameHUD>();
        if (hud != null)
        {
            hud.ClearPrompt();
        }
    }
}
