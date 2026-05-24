using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controlador del menú principal usando UI Toolkit.
/// Diseño "ESTADO DEL SISTEMA COGNITIVO" con side nav.
/// Requiere un UIDocument component en el mismo GameObject.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] string firstLevelScene = "Level_01";

    [Header("Debug")]
    [Tooltip("Si es true, carga Level_01 automáticamente sin esperar input")]
    [SerializeField] bool autoStartGame = false;

    [Header("Background Textures (Optional - will load from Resources/UI if not assigned)")]
    [SerializeField] Texture2D defaultBg;
    [SerializeField] Texture2D newGameBg;
    [SerializeField] Texture2D continueBg;
    [SerializeField] Texture2D settingsBg;
    [SerializeField] Texture2D exitBg;

    UIDocument _doc;
    VisualElement _root;
    VisualElement _menuBg;
    Label _heroTitle;

    // Panels
    VisualElement _settingsPanel;
    VisualElement _levelSelectPanel;

    // Main Menu Buttons
    Button _btnNewGame;
    Button _btnContinue;
    Button _btnLevels;
    Button _btnSettings;
    Button _btnExit;
    Button _activeNavButton;

    // Settings Controls - Audio
    Slider _sldMaster;
    Slider _sldMusic;
    Slider _sldSfx;
    Label _lblMasterVal;
    Label _lblMusicVal;
    Label _lblSfxVal;

    // Settings Controls - Visuals
    DropdownField _resDropdown;
    Toggle _fullscreenToggle;
    Toggle _vsyncToggle;
    DropdownField _scaleDropdown;

    // Settings Controls - Neural
    Button _btnSensLow;
    Button _btnSensMed;
    Button _btnSensHigh;
    Label _lblSensVal;
    Slider _sensitivitySlider;
    Label _lblCamSensVal;

    // Fog Density & Echo Opacity Settings
    Slider _sldFog;
    Label _lblFogVal;
    Slider _sldEcho;
    Label _lblEchoVal;

    List<Resolution> _filteredResolutions;

    void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        if (_doc == null || _doc.rootVisualElement == null) return;
        _root = _doc.rootVisualElement;

        // Apply saved UI scale immediately
        ApplySavedUIScale();

        // Background & Hero
        _menuBg = _root.Q("menu-bg");
        _heroTitle = _root.Q<Label>("hero-title");

        // Panels
        _settingsPanel = _root.Q("settings-panel");
        _levelSelectPanel = _root.Q("level-select-panel");

        // Side nav buttons
        _btnNewGame = _root.Q<Button>("nav-newgame");
        _btnContinue = _root.Q<Button>("nav-continue");
        _btnLevels = _root.Q<Button>("nav-levels");
        _btnSettings = _root.Q<Button>("nav-settings");
        _btnExit = _root.Q<Button>("nav-exit");

        // Setup hover behaviors
        SetupHoverCallbacks();

        // Bind panel actions
        RegisterButtonClick("nav-newgame", PlayGame);
        RegisterButtonClick("nav-continue", ShowLevelSelect);
        RegisterButtonClick("nav-levels", ShowMain);
        RegisterButtonClick("nav-settings", ShowSettings);
        RegisterButtonClick("nav-exit", QuitGame);

        // Settings panel bindings
        _sldMaster = _root.Q<Slider>("sld-master");
        _sldMusic = _root.Q<Slider>("sld-music");
        _sldSfx = _root.Q<Slider>("sld-sfx");

        _lblMasterVal = _root.Q<Label>("lbl-master-val");
        _lblMusicVal = _root.Q<Label>("lbl-music-val");
        _lblSfxVal = _root.Q<Label>("lbl-sfx-val");

        _resDropdown = _root.Q<DropdownField>("ResolutionDropdown");
        _fullscreenToggle = _root.Q<Toggle>("FullscreenToggle");
        _vsyncToggle = _root.Q<Toggle>("VsyncToggle");
        _scaleDropdown = _root.Q<DropdownField>("ScaleDropdown");

        _btnSensLow = _root.Q<Button>("btn-sens-low");
        _btnSensMed = _root.Q<Button>("btn-sens-med");
        _btnSensHigh = _root.Q<Button>("btn-sens-high");
        _lblSensVal = _root.Q<Label>("lbl-sens-val");

        _sensitivitySlider = _root.Q<Slider>("SensitivitySlider");
        _lblCamSensVal = _root.Q<Label>("lbl-cam-sens-val");

        _sldFog = _root.Q<Slider>("sld-fog");
        _lblFogVal = _root.Q<Label>("lbl-fog-val");
        _sldEcho = _root.Q<Slider>("sld-echo");
        _lblEchoVal = _root.Q<Label>("lbl-echo-val");

        // Bind settings buttons
        RegisterButtonClick("btn-restore-defaults", RestoreFactoryDefaults);
        RegisterButtonClick("btn-settings-back", DiscardSettings);
        RegisterButtonClick("btn-settings-apply", ApplySettings);
        RegisterButtonClick("btn-levels-back", ShowMain);

        // Dynamic dashboard computation
        int completedLevels = GameProgress.GetCompletedCount();
        int totalLevels = GameProgress.TotalLevels;
        float completionRatio = totalLevels > 0 ? (float)completedLevels / totalLevels : 0f;

        float stability = 0.20f + 0.80f * completionRatio;
        float coherence = 0.10f + 0.90f * completionRatio;
        float progress = completionRatio;

        // Stability Elements
        var lblStabilityVal = _root.Q<Label>("lbl-stat-stability-val");
        var barStabilityFill = _root.Q("bar-stat-stability-fill");
        var lblStabilityDesc = _root.Q<Label>("lbl-stat-stability-desc");
        if (lblStabilityVal != null) lblStabilityVal.text = stability.ToString("F2");
        if (barStabilityFill != null) barStabilityFill.style.width = Length.Percent(stability * 100f);
        if (lblStabilityDesc != null)
        {
            if (completedLevels == 0) lblStabilityDesc.text = "Neural cohesion initializing...";
            else if (completedLevels == totalLevels) lblStabilityDesc.text = "Neural cohesion fully synchronized.";
            else lblStabilityDesc.text = "Neural alignment in progress...";
        }

        // Coherence Elements
        var lblCoherenceVal = _root.Q<Label>("lbl-stat-coherence-val");
        var barCoherenceFill = _root.Q("bar-stat-coherence-fill");
        var lblCoherenceDesc = _root.Q<Label>("lbl-stat-coherence-desc");
        if (lblCoherenceVal != null) lblCoherenceVal.text = coherence.ToString("F2");
        if (barCoherenceFill != null) barCoherenceFill.style.width = Length.Percent(coherence * 100f);
        if (lblCoherenceDesc != null)
        {
            if (completedLevels == 0) lblCoherenceDesc.text = "Fragment resonance is currently unstable.";
            else if (completedLevels == totalLevels) lblCoherenceDesc.text = "Fragment resonance stable.";
            else lblCoherenceDesc.text = "Intermittent signal synchronization.";
        }

        // Narrative Progress Elements
        var lblProgressVal = _root.Q<Label>("lbl-stat-progress-val");
        var barProgressFill = _root.Q("bar-stat-progress-fill");
        var lblProgressDesc = _root.Q<Label>("lbl-stat-progress-desc");
        if (lblProgressVal != null) lblProgressVal.text = progress.ToString("F2");
        if (barProgressFill != null) barProgressFill.style.width = Length.Percent(progress * 100f);
        if (lblProgressDesc != null)
        {
            if (completedLevels == 0) lblProgressDesc.text = "Deep memory nodes still inaccessible.";
            else if (completedLevels == totalLevels) lblProgressDesc.text = "All memory nodes restored.";
            else lblProgressDesc.text = "Memory fragments beginning to re-align.";
        }

        // Level select buttons and styles
        for (int i = 1; i <= 6; i++)
        {
            int levelNum = i;
            string sceneName = $"Level_{levelNum:D2}";
            string btnName = $"btn-level-{levelNum:D2}";
            var btn = _root.Q<Button>(btnName);
            if (btn != null)
            {
                bool isUnlocked = GameProgress.IsSceneUnlocked(sceneName);
                bool isCompleted = GameProgress.IsSceneCompleted(sceneName);
                
                // Clear any leftover styles
                btn.RemoveFromClassList("level-button--locked");
                btn.RemoveFromClassList("level-button--completed");

                if (!isUnlocked)
                {
                    btn.AddToClassList("level-button--locked");
                    btn.SetEnabled(false);
                }
                else
                {
                    btn.SetEnabled(true);
                    if (isCompleted)
                    {
                        btn.AddToClassList("level-button--completed");
                    }
                    btn.clicked += () => LoadLevel(sceneName);
                }
            }
        }

        // Try load textures dynamically if not assigned
        LoadFallbackTextures();

        InitializeSettings();
        ShowMain();
    }

    void Start()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        Time.timeScale = 1f;

        // Debug auto-start
        if (autoStartGame)
        {
            Invoke(nameof(AutoStart), 0.5f);
        }
    }

    void AutoStart()
    {
        LoadLevel(firstLevelScene);
    }

    void RegisterButtonClick(string name, System.Action action)
    {
        var btn = _root.Q<Button>(name);
        if (btn != null)
        {
            btn.clicked += action;
        }
        else
        {
            var el = _root.Q(name);
            if (el != null)
                el.RegisterCallback<ClickEvent>(_ => action());
        }
    }

    // --- Hover Background & Title Swap ---

    void SetupHoverCallbacks()
    {
        if (_btnNewGame != null)
        {
            _btnNewGame.RegisterCallback<MouseEnterEvent>(_ => OnNavHover(_btnNewGame, newGameBg, "NEURAL SYNC"));
            _btnNewGame.RegisterCallback<MouseLeaveEvent>(_ => OnNavHoverLeave(_btnNewGame));
        }
        if (_btnContinue != null)
        {
            _btnContinue.RegisterCallback<MouseEnterEvent>(_ => OnNavHover(_btnContinue, continueBg, "MEMORY CORE"));
            _btnContinue.RegisterCallback<MouseLeaveEvent>(_ => OnNavHoverLeave(_btnContinue));
        }
        if (_btnLevels != null)
        {
            _btnLevels.RegisterCallback<MouseEnterEvent>(_ => OnNavHover(_btnLevels, defaultBg, "STABILITY"));
            _btnLevels.RegisterCallback<MouseLeaveEvent>(_ => OnNavHoverLeave(_btnLevels));
        }
        if (_btnSettings != null)
        {
            _btnSettings.RegisterCallback<MouseEnterEvent>(_ => OnNavHover(_btnSettings, settingsBg, "CALIBRATION"));
            _btnSettings.RegisterCallback<MouseLeaveEvent>(_ => OnNavHoverLeave(_btnSettings));
        }
        if (_btnExit != null)
        {
            _btnExit.RegisterCallback<MouseEnterEvent>(_ => OnNavHover(_btnExit, exitBg, "VOID MAP"));
            _btnExit.RegisterCallback<MouseLeaveEvent>(_ => OnNavHoverLeave(_btnExit));
        }
    }

    void OnNavHover(Button btn, Texture2D bgTex, string title)
    {
        if (bgTex != null && _menuBg != null)
        {
            _menuBg.style.backgroundImage = new StyleBackground(bgTex);
        }

        if (_heroTitle != null)
        {
            _heroTitle.text = title;
        }

        btn.AddToClassList("nav-item--active");
    }

    void OnNavHoverLeave(Button btn)
    {
        if (defaultBg != null && _menuBg != null)
        {
            _menuBg.style.backgroundImage = new StyleBackground(defaultBg);
        }

        if (_heroTitle != null)
        {
            _heroTitle.text = "ISOLATED";
        }

        // Keep active selection styling only on the active nav button
        if (btn != _activeNavButton)
        {
            btn.RemoveFromClassList("nav-item--active");
        }
    }

    void SetActiveNav(Button activeBtn)
    {
        _activeNavButton = activeBtn;
        _btnNewGame?.RemoveFromClassList("nav-item--active");
        _btnContinue?.RemoveFromClassList("nav-item--active");
        _btnLevels?.RemoveFromClassList("nav-item--active");
        _btnSettings?.RemoveFromClassList("nav-item--active");
        _btnExit?.RemoveFromClassList("nav-item--active");
        activeBtn?.AddToClassList("nav-item--active");
    }

    void LoadFallbackTextures()
    {
        if (defaultBg == null) defaultBg = Resources.Load<Texture2D>("UI/Images/void_fog_bg");
        if (newGameBg == null) newGameBg = Resources.Load<Texture2D>("UI/Images/menu_background");
        if (continueBg == null) continueBg = Resources.Load<Texture2D>("UI/Images/fragmenting_silhouette");
        if (settingsBg == null) settingsBg = Resources.Load<Texture2D>("UI/Images/void_fog_bg");
        if (exitBg == null) exitBg = Resources.Load<Texture2D>("UI/Images/menu_background");

        // Try load from Assets path if Resources fails
#if UNITY_EDITOR
        if (defaultBg == null) defaultBg = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Images/void_fog_bg.png");
        if (newGameBg == null) newGameBg = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Images/menu_background.png");
        if (continueBg == null) continueBg = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Images/fragmenting_silhouette.png");
        if (settingsBg == null) settingsBg = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Images/void_fog_bg.png");
        if (exitBg == null) exitBg = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Images/menu_background.png");
#endif
    }

    // --- Panel Switching ---

    void ShowMain()
    {
        _settingsPanel?.AddToClassList("hidden");
        _levelSelectPanel?.AddToClassList("hidden");
        SetActiveNav(_btnLevels);
    }

    void ShowSettings()
    {
        _levelSelectPanel?.AddToClassList("hidden");
        _settingsPanel?.RemoveFromClassList("hidden");
        SetActiveNav(_btnSettings);
        LoadCurrentSettingsIntoUI();
    }

    void ShowLevelSelect()
    {
        _settingsPanel?.AddToClassList("hidden");
        _levelSelectPanel?.RemoveFromClassList("hidden");
        SetActiveNav(_btnContinue);
    }

    // --- Actions ---

    void PlayGame()
    {
        LoadLevel(firstLevelScene);
    }

    void LoadLevel(string levelName)
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(levelName);
        }
        else
        {
            PostProcessingSetup.PrepareForSceneReload();
            UnityEngine.SceneManagement.SceneManager.LoadScene(levelName);
        }
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- Settings & Calibration logic ---

    void InitializeSettings()
    {
        // UI Scale dropdown setup
        if (_scaleDropdown != null)
        {
            _scaleDropdown.choices = new List<string> { "Normal", "Large", "Extra Large" };
            _scaleDropdown.value = PlayerPrefs.GetString("UIScale", "Normal");
        }

        // Neural presets
        if (_btnSensLow != null) _btnSensLow.clicked += () => SelectSensitivityPreset("Low", 0.5f);
        if (_btnSensMed != null) _btnSensMed.clicked += () => SelectSensitivityPreset("Medium", 1.0f);
        if (_btnSensHigh != null) _btnSensHigh.clicked += () => SelectSensitivityPreset("High", 2.0f);

        // Sliders change updates labels
        if (_sldMaster != null) _sldMaster.RegisterValueChangedCallback(evt => UpdateLabel(_lblMasterVal, evt.newValue));
        if (_sldMusic != null) _sldMusic.RegisterValueChangedCallback(evt => UpdateLabel(_lblMusicVal, evt.newValue));
        if (_sldSfx != null) _sldSfx.RegisterValueChangedCallback(evt => UpdateLabel(_lblSfxVal, evt.newValue));
        if (_sensitivitySlider != null) _sensitivitySlider.RegisterValueChangedCallback(evt => UpdateSensitivityLabel(evt.newValue));
        if (_sldFog != null) _sldFog.RegisterValueChangedCallback(evt => UpdateFogLabel(evt.newValue));
        if (_sldEcho != null) _sldEcho.RegisterValueChangedCallback(evt => UpdateLabel(_lblEchoVal, evt.newValue));

        // Resolutions
        if (_resDropdown != null)
        {
            Resolution[] resolutions = Screen.resolutions;
            _filteredResolutions = new List<Resolution>();
            List<string> options = new List<string>();
            int currentResIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                _filteredResolutions.Add(resolutions[i]);
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                    currentResIndex = i;
            }

            _resDropdown.choices = options;
            if (options.Count > 0)
            {
                _resDropdown.index = Mathf.Clamp(currentResIndex, 0, options.Count - 1);
            }
        }

        LoadCurrentSettingsIntoUI();
    }

    void LoadCurrentSettingsIntoUI()
    {
        if (_sldMaster != null) _sldMaster.value = PlayerPrefs.GetFloat("MasterVolume", AudioListener.volume);
        if (_sldMusic != null) _sldMusic.value = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
        if (_sldSfx != null) _sldSfx.value = PlayerPrefs.GetFloat("SfxVolume", 0.72f);

        if (_lblMasterVal != null) UpdateLabel(_lblMasterVal, _sldMaster.value);
        if (_lblMusicVal != null) UpdateLabel(_lblMusicVal, _sldMusic.value);
        if (_lblSfxVal != null) UpdateLabel(_lblSfxVal, _sldSfx.value);

        if (_fullscreenToggle != null) _fullscreenToggle.value = Screen.fullScreen;
        if (_vsyncToggle != null) _vsyncToggle.value = QualitySettings.vSyncCount > 0;

        if (_scaleDropdown != null) _scaleDropdown.value = PlayerPrefs.GetString("UIScale", "Normal");

        float currentSens = PlayerPrefs.GetFloat("CameraSensitivity", 1f);
        if (_sensitivitySlider != null) _sensitivitySlider.value = currentSens;
        UpdateSensitivityLabel(currentSens);

        if (_sldFog != null) _sldFog.value = PlayerPrefs.GetFloat("FogDensity", RenderSettings.fog ? RenderSettings.fogDensity : 0.035f);
        if (_sldEcho != null) _sldEcho.value = PlayerPrefs.GetFloat("EchoOpacity", 0.6f);

        if (_lblFogVal != null) UpdateFogLabel(_sldFog.value);
        if (_lblEchoVal != null) UpdateLabel(_lblEchoVal, _sldEcho.value);

        // Preset button highlights based on sensitivity value
        if (Mathf.Approximately(currentSens, 0.5f)) SelectSensitivityPresetUI("Low");
        else if (Mathf.Approximately(currentSens, 2.0f)) SelectSensitivityPresetUI("High");
        else SelectSensitivityPresetUI("Medium");
    }

    void UpdateLabel(Label lbl, float val)
    {
        if (lbl != null) lbl.text = Mathf.RoundToInt(val * 100f) + "%";
    }

    void UpdateFogLabel(float val)
    {
        if (_lblFogVal != null) _lblFogVal.text = val.ToString("F3");
    }

    void UpdateSensitivityLabel(float val)
    {
        if (_lblCamSensVal != null) _lblCamSensVal.text = val.ToString("F1");
    }

    void SelectSensitivityPreset(string name, float value)
    {
        if (_sensitivitySlider != null) _sensitivitySlider.value = value;
        if (_lblSensVal != null) _lblSensVal.text = name;
        SelectSensitivityPresetUI(name);
    }

    void SelectSensitivityPresetUI(string activePreset)
    {
        _btnSensLow?.RemoveFromClassList("preset-button--active");
        _btnSensMed?.RemoveFromClassList("preset-button--active");
        _btnSensHigh?.RemoveFromClassList("preset-button--active");

        if (activePreset == "Low") _btnSensLow?.AddToClassList("preset-button--active");
        else if (activePreset == "High") _btnSensHigh?.AddToClassList("preset-button--active");
        else _btnSensMed?.AddToClassList("preset-button--active");

        if (_lblSensVal != null) _lblSensVal.text = activePreset;
    }

    void ApplySettings()
    {
        // 1. Audio
        float master = _sldMaster != null ? _sldMaster.value : 0.84f;
        float music = _sldMusic != null ? _sldMusic.value : 0.6f;
        float sfx = _sldSfx != null ? _sldSfx.value : 0.72f;

        AudioListener.volume = master;
        PlayerPrefs.SetFloat("MasterVolume", master);
        PlayerPrefs.SetFloat("MusicVolume", music);
        PlayerPrefs.SetFloat("SfxVolume", sfx);

        // Broadcast audio settings
        var levelCtrl = FindAnyObjectByType<LevelRuntimeController>();
        if (levelCtrl != null) levelCtrl.SendMessage("ApplySavedAudioSettings", SendMessageOptions.DontRequireReceiver);

        // 2. Visuals
        if (_fullscreenToggle != null) Screen.fullScreen = _fullscreenToggle.value;
        if (_vsyncToggle != null) QualitySettings.vSyncCount = _vsyncToggle.value ? 1 : 0;

        if (_resDropdown != null && _filteredResolutions != null && _resDropdown.index >= 0 && _resDropdown.index < _filteredResolutions.Count)
        {
            Resolution res = _filteredResolutions[_resDropdown.index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        }

        // 3. UI Scale
        if (_scaleDropdown != null)
        {
            string oldScale = PlayerPrefs.GetString("UIScale", "Normal");
            string newScale = _scaleDropdown.value;
            PlayerPrefs.SetString("UIScale", newScale);

            if (oldScale != newScale)
            {
                ApplySavedUIScale();
                // Broadcast live UI Scale update to any other open UI document
                var allDocs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
                foreach (var doc in allDocs)
                {
                    if (doc != _doc && doc.gameObject != gameObject)
                    {
                        doc.gameObject.SendMessage("ApplySavedUIScale", SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
        }

        // 4. Sensitivity
        float sens = _sensitivitySlider != null ? _sensitivitySlider.value : 1.0f;
        PlayerPrefs.SetFloat("CameraSensitivity", sens);

        // Broadcast camera sensitivity
        var cam = FindAnyObjectByType<ThirdPersonCamera>();
        if (cam != null) cam.SendMessage("ApplySavedSensitivity", SendMessageOptions.DontRequireReceiver);

        // 5. Fog Density
        float fogDensity = _sldFog != null ? _sldFog.value : 0.035f;
        PlayerPrefs.SetFloat("FogDensity", fogDensity);
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fog = fogDensity > 0f;

        // 6. Echo Opacity
        float echoOpacity = _sldEcho != null ? _sldEcho.value : 0.6f;
        PlayerPrefs.SetFloat("EchoOpacity", echoOpacity);

        // Broadcast echo opacity
        var allRecorders = FindObjectsByType<EchoPlayback>(FindObjectsSortMode.None);
        foreach (var playback in allRecorders)
        {
            playback.SendMessage("ApplySavedEchoOpacity", SendMessageOptions.DontRequireReceiver);
        }

        PlayerPrefs.Save();
        ShowMain();
    }

    void DiscardSettings()
    {
        ShowMain();
    }

    void RestoreFactoryDefaults()
    {
        if (_sldMaster != null) _sldMaster.value = 0.84f;
        if (_sldMusic != null) _sldMusic.value = 0.60f;
        if (_sldSfx != null) _sldSfx.value = 0.72f;

        if (_fullscreenToggle != null) _fullscreenToggle.value = true;
        if (_vsyncToggle != null) _vsyncToggle.value = true;

        if (_scaleDropdown != null) _scaleDropdown.value = "Normal";

        if (_sldFog != null) _sldFog.value = 0.035f;
        if (_sldEcho != null) _sldEcho.value = 0.60f;

        SelectSensitivityPreset("Medium", 1.0f);
    }

    // Apply scaling styles to root UXML element
    public void ApplySavedUIScale()
    {
        if (_root == null) return;

        string scale = PlayerPrefs.GetString("UIScale", "Normal");
        _root.RemoveFromClassList("scale-large");
        _root.RemoveFromClassList("scale-xl");

        if (scale == "Large")
        {
            _root.AddToClassList("scale-large");
        }
        else if (scale == "Extra Large")
        {
            _root.AddToClassList("scale-xl");
        }
    }
}
