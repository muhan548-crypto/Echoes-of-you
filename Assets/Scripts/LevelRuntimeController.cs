using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelRuntimeController : MonoBehaviour
{
    [Header("Copy")]
    [SerializeField] string objectiveText = "Llega a la salida.";
    [SerializeField] string introLine = "";
    [SerializeField] string completionLine = "";

    [Header("Reset")]
    [SerializeField] bool allowSoftReset = true;
    [SerializeField] bool allowHardReset = true;
    [SerializeField] float hardResetHoldSeconds = 0.5f;

    GameHUD _hud;
    EchoRecorder _recorder;
    GameStateController _gameState;
    float _hardResetHold;
    bool _completed;
    bool _restartRequested;

    public static LevelRuntimeController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _gameState = FindAnyObjectByType<GameStateController>();
        if (_gameState == null)
            _gameState = new GameObject("GameStateController").AddComponent<GameStateController>();
    }

    void Start()
    {
        // Ocultar y bloquear el cursor para una mejor experiencia de gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _hud = FindAnyObjectByType<GameHUD>();
        _recorder = FindAnyObjectByType<EchoRecorder>();

        if (_hud != null)
        {
            _hud.SetObjective(ResolveObjective());

            if (!string.IsNullOrEmpty(introLine))
                _hud.ShowToast(introLine, new Color(0.95f, 0.96f, 0.98f, 1f), 2.6f);
                
            CheckAndShowProgressiveHint();
        }
    }
    
    void CheckAndShowProgressiveHint()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        int deaths = PlayerPrefs.GetInt("Deaths_" + sceneName, 0);
        
        if (deaths == 0) return;

        string hint = "";
        if (sceneName == "Level_01")
            hint = deaths == 1 ? "Pisa el boton y usa la grabadora." : "Graba un eco tuyo pisando el boton, luego cruza la puerta.";
        else if (sceneName == "Level_02")
            hint = deaths == 1 ? "Inicia la grabacion cerca de la puerta." : "Tu eco puede correr hacia el boton por ti. Graba al reves.";
        else if (sceneName == "Level_03")
            hint = deaths == 1 ? "Tu y tu eco deben trabajar en secuencia." : "Mientras el eco sostiene una puerta, avanza para la siguiente.";
        else if (sceneName == "Level_04")
            hint = deaths == 1 ? "La sincronizacion debe ser exacta." : "Abrete la puerta central con el boton, graba, corre.";
        else if (sceneName == "Level_05")
            hint = deaths == 1 ? "A veces hay que sacrificarse." : "Tirate y oprime el boton. Luego deten la grabacion.";
        else if (sceneName == "Level_06")
            hint = deaths == 1 ? "Cada version tuya importa." : "Usa dos ecos, cada uno sosteniendo una puerta.";

        if (!string.IsNullOrEmpty(hint))
            _hud.ShowToast("Pista: " + hint, new Color(0.5f, 0.8f, 1f, 1f), 4.5f);
    }

    void Update()
    {
        if (_completed)
            return;

        if (allowSoftReset && Input.GetKeyDown(KeyCode.Q))
            SoftReset();

        if (!allowHardReset)
            return;

        if (Input.GetKey(KeyCode.T))
        {
            _hardResetHold += Time.unscaledDeltaTime;

            if (_hardResetHold >= hardResetHoldSeconds)
                RequestRestart(0f);
        }
        else
        {
            _hardResetHold = 0f;
        }
    }

    public void SoftReset()
    {
        if (_restartRequested || _completed)
            return;

        _recorder ??= FindAnyObjectByType<EchoRecorder>();
        _hud ??= FindAnyObjectByType<GameHUD>();

        _recorder?.ClearAllEchoes(false);

        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IResettableLevelObject resettable)
                resettable.ResetLevelState();
        }

        _hud?.ShowToast("Ecos limpiados", new Color(0.48f, 0.94f, 0.78f, 1f), 1.1f);
        _hud?.SetObjective(ResolveObjective());
    }

    public void SetObjective(string nextObjective)
    {
        objectiveText = nextObjective;
        _hud ??= FindAnyObjectByType<GameHUD>();
        _hud?.SetObjective(nextObjective);
    }

    public void HandlePlayerDeath(Vector3 position, float restartDelay = 1.2f)
    {
        if (_completed || _restartRequested)
            return;

        string sceneName = SceneManager.GetActiveScene().name;
        int deaths = PlayerPrefs.GetInt("Deaths_" + sceneName, 0);
        PlayerPrefs.SetInt("Deaths_" + sceneName, deaths + 1);
        PlayerPrefs.Save();

        _completed = true;
        _hud ??= FindAnyObjectByType<GameHUD>();
        _hud?.ShowToast("Reiniciando memoria...", new Color(1f, 0.43f, 0.43f, 1f), restartDelay);
        _gameState?.NotifyPlayerDeath(position);
        RequestRestart(restartDelay);
    }

    public void OnLevelCompleted(Vector3 position, string toastOverride = "")
    {
        if (_completed)
            return;

        _completed = true;
        _hud ??= FindAnyObjectByType<GameHUD>();
        _hud?.SetObjective("Memoria restaurada.");

        string toast = !string.IsNullOrEmpty(toastOverride) ? toastOverride : completionLine;
        if (!string.IsNullOrEmpty(toast))
            _hud?.ShowToast(toast, new Color(1f, 0.83f, 0.42f, 1f), 1.8f);

        _gameState?.NotifyLevelCompleted(position);
    }

    public void RequestRestart(float delaySeconds)
    {
        if (_restartRequested)
            return;

        _restartRequested = true;
        _gameState ??= FindAnyObjectByType<GameStateController>();
        if (_gameState != null)
        {
            _gameState.RequestSceneRestart(delaySeconds);
            return;
        }

        PostProcessingSetup.PrepareForSceneReload();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void PrepareForSceneReload()
    {
        _recorder ??= FindAnyObjectByType<EchoRecorder>();
        _recorder?.ClearAllEchoes(false);

        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IResettableLevelObject resettable)
                resettable.ResetLevelState();
        }
    }

    string ResolveObjective()
    {
        LevelGoal goal = FindAnyObjectByType<LevelGoal>();
        return goal != null ? goal.ObjectiveText : objectiveText;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
