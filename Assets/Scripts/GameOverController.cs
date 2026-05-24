using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Pantalla de Game Over / muerte usando UI Toolkit.
/// Se activa externamente vía Show() desde LevelRuntimeController o GameStateController.
/// Requiere un UIDocument component con GameOverUI.uxml asignado.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class GameOverController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] string hubSceneName = "Level_07";
    [SerializeField] string mainMenuScene = "MainMenu";

    [Header("Messages")]
    [SerializeField] string[] deathMessages = new string[]
    {
        "La señal se ha perdido...",
        "Un eco se desvanece en la oscuridad.",
        "La memoria se fragmenta.",
        "El vacío reclama otro fragmento.",
        "La conexión se ha interrumpido."
    };

    [SerializeField] string[] deathPoems = new string[]
    {
        "Los ecos se disuelven en el vacío...",
        "Cada caída es un recuerdo que se pierde.",
        "En la quietud, la mente se reinicia.",
        "Lo que fue, será otra vez... quizás.",
        "El silencio entre los ecos es infinito."
    };

    public static GameOverController Instance { get; private set; }

    UIDocument _doc;
    VisualElement _gameoverRoot;
    Label _titleLabel;
    Label _subtitleLabel;
    Label _poemLabel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        if (_doc == null || _doc.rootVisualElement == null) return;

        _gameoverRoot = _doc.rootVisualElement.Q("gameover-root");
        _titleLabel = _doc.rootVisualElement.Q<Label>("gameover-title");
        _subtitleLabel = _doc.rootVisualElement.Q<Label>("gameover-subtitle");
        _poemLabel = _doc.rootVisualElement.Q<Label>("gameover-poem");

        // Apply saved UI scale immediately
        ApplySavedUIScale();

        // Wire buttons
        _gameoverRoot?.Q<Button>("btn-retry")?.RegisterCallback<ClickEvent>(_ => Retry());
        _gameoverRoot?.Q<Button>("btn-go-hub")?.RegisterCallback<ClickEvent>(_ => GoToHub());
        _gameoverRoot?.Q<Button>("btn-go-menu")?.RegisterCallback<ClickEvent>(_ => GoToMenu());
        _gameoverRoot?.Q<Button>("btn-exit")?.RegisterCallback<ClickEvent>(_ => GoToMenu());

        // Start hidden
        _gameoverRoot?.AddToClassList("hidden");
    }

    /// <summary>
    /// Muestra la pantalla de game over con mensajes aleatorios.
    /// </summary>
    public void Show(string customTitle = null, string customSubtitle = null)
    {
        if (_gameoverRoot == null) return;

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        Time.timeScale = 0f;

        if (_titleLabel != null)
            _titleLabel.text = customTitle ?? "MEMORIA FRAGMENTADA";

        if (_subtitleLabel != null)
            _subtitleLabel.text = customSubtitle ?? deathMessages[Random.Range(0, deathMessages.Length)];

        if (_poemLabel != null)
            _poemLabel.text = deathPoems[Random.Range(0, deathPoems.Length)];

        _gameoverRoot.RemoveFromClassList("hidden");
    }

    /// <summary>
    /// Oculta la pantalla de game over.
    /// </summary>
    public void Hide()
    {
        _gameoverRoot?.AddToClassList("hidden");
    }

    void Retry()
    {
        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
        PostProcessingSetup.PrepareForSceneReload();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToHub()
    {
        Time.timeScale = 1f;
        PostProcessingSetup.PrepareForSceneReload();
        SceneManager.LoadScene(hubSceneName);
    }

    void GoToMenu()
    {
        Time.timeScale = 1f;
        PostProcessingSetup.PrepareForSceneReload();
        SceneManager.LoadScene(mainMenuScene);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        Time.timeScale = 1f;
    }

    public void ApplySavedUIScale()
    {
        VisualElement root = _doc?.rootVisualElement;
        if (root == null) return;

        string scale = PlayerPrefs.GetString("UIScale", "Normal");
        root.RemoveFromClassList("scale-large");
        root.RemoveFromClassList("scale-xl");

        if (scale == "Large")
        {
            root.AddToClassList("scale-large");
        }
        else if (scale == "Extra Large")
        {
            root.AddToClassList("scale-xl");
        }
    }
}
