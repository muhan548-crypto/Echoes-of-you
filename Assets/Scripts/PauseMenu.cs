using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Menú de pausa usando UI Toolkit.
/// Requiere un UIDocument component en el mismo GameObject con PauseMenuUI.uxml asignado.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PauseMenu : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] string hubSceneName = "Level_07";
    [SerializeField] string mainMenuScene = "MainMenu";

    bool _paused;
    UIDocument _doc;
    VisualElement _pauseRoot;
    VisualElement _pauseNav;
    VisualElement _settingsPanel;

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

        _pauseRoot = _doc.rootVisualElement.Q("pause-root");
        _pauseNav = _doc.rootVisualElement.Q("pause-nav");
        _settingsPanel = _doc.rootVisualElement.Q("pause-settings-panel");

        // Apply saved UI scale immediately
        ApplySavedUIScale();

        // Wire main menu buttons
        var btnResume = _pauseRoot?.Q<Button>("btn-resume");
        if (btnResume != null) btnResume.clicked += Resume;

        var btnRecalibrate = _pauseRoot?.Q<Button>("btn-recalibrate");
        if (btnRecalibrate != null) btnRecalibrate.clicked += () => {
            Resume();
            PostProcessingSetup.PrepareForSceneReload();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        };

        var btnSettings = _pauseRoot?.Q<Button>("btn-settings");
        if (btnSettings != null) btnSettings.clicked += ShowSettings;

        System.Action hubAction = () =>
        {
            Resume();
            PostProcessingSetup.PrepareForSceneReload();
            SceneManager.LoadScene(hubSceneName);
        };
        var btnHub = _pauseRoot?.Q<Button>("btn-hub");
        if (btnHub != null) btnHub.clicked += hubAction;

        System.Action menuAction = () =>
        {
            Resume();
            PostProcessingSetup.PrepareForSceneReload();
            SceneManager.LoadScene(mainMenuScene);
        };
        var btnMainMenu = _pauseRoot?.Q<Button>("btn-mainmenu");
        if (btnMainMenu != null) btnMainMenu.clicked += menuAction;

        var btnVoid = _pauseRoot?.Q<Button>("btn-void");
        if (btnVoid != null) btnVoid.clicked += menuAction;

        // Wire calibration panel controls
        _sldMaster = _doc.rootVisualElement.Q<Slider>("p-sld-master");
        _sldMusic = _doc.rootVisualElement.Q<Slider>("p-sld-music");
        _sldSfx = _doc.rootVisualElement.Q<Slider>("p-sld-sfx");

        _lblMasterVal = _doc.rootVisualElement.Q<Label>("p-lbl-master-val");
        _lblMusicVal = _doc.rootVisualElement.Q<Label>("p-lbl-music-val");
        _lblSfxVal = _doc.rootVisualElement.Q<Label>("p-lbl-sfx-val");

        _resDropdown = _doc.rootVisualElement.Q<DropdownField>("p-ResolutionDropdown");
        _fullscreenToggle = _doc.rootVisualElement.Q<Toggle>("p-FullscreenToggle");
        _vsyncToggle = _doc.rootVisualElement.Q<Toggle>("p-VsyncToggle");
        _scaleDropdown = _doc.rootVisualElement.Q<DropdownField>("p-ScaleDropdown");

        _btnSensLow = _doc.rootVisualElement.Q<Button>("p-btn-sens-low");
        _btnSensMed = _doc.rootVisualElement.Q<Button>("p-btn-sens-med");
        _btnSensHigh = _doc.rootVisualElement.Q<Button>("p-btn-sens-high");
        _lblSensVal = _doc.rootVisualElement.Q<Label>("p-lbl-sens-val");

        _sensitivitySlider = _doc.rootVisualElement.Q<Slider>("p-SensitivitySlider");
        _lblCamSensVal = _doc.rootVisualElement.Q<Label>("p-lbl-cam-sens-val");

        _sldFog = _doc.rootVisualElement.Q<Slider>("p-sld-fog");
        _lblFogVal = _doc.rootVisualElement.Q<Label>("p-lbl-fog-val");
        _sldEcho = _doc.rootVisualElement.Q<Slider>("p-sld-echo");
        _lblEchoVal = _doc.rootVisualElement.Q<Label>("p-lbl-echo-val");

        // Bind settings buttons
        var btnRestoreDefaults = _doc.rootVisualElement.Q<Button>("p-btn-restore-defaults");
        if (btnRestoreDefaults != null) btnRestoreDefaults.clicked += RestoreFactoryDefaults;

        var btnSettingsBack = _doc.rootVisualElement.Q<Button>("p-btn-settings-back");
        if (btnSettingsBack != null) btnSettingsBack.clicked += DiscardSettings;

        var btnSettingsApply = _doc.rootVisualElement.Q<Button>("p-btn-settings-apply");
        if (btnSettingsApply != null) btnSettingsApply.clicked += ApplySettings;

        InitializeSettings();

        // Start hidden
        if (_pauseRoot != null)
            _pauseRoot.AddToClassList("hidden");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_paused)
            {
                // If settings are open, close them and return to main pause nav rather than resuming gameplay
                if (_settingsPanel != null && !_settingsPanel.ClassListContains("hidden"))
                {
                    DiscardSettings();
                }
                else
                {
                    Resume();
                }
            }
            else
            {
                Pause();
            }
        }
    }

    void Pause()
    {
        _paused = true;
        Time.timeScale = 0f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        _pauseRoot?.RemoveFromClassList("hidden");
        ShowPauseNav();
    }

    void Resume()
    {
        _paused = false;
        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        _pauseRoot?.AddToClassList("hidden");
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    // --- Panel switching inside Pause Menu ---

    void ShowPauseNav()
    {
        _settingsPanel?.AddToClassList("hidden");
        _pauseNav?.RemoveFromClassList("hidden");
    }

    void ShowSettings()
    {
        _pauseNav?.AddToClassList("hidden");
        _settingsPanel?.RemoveFromClassList("hidden");
        LoadCurrentSettingsIntoUI();
    }

    // --- Settings & Calibration logic ---

    void InitializeSettings()
    {
        // UI Scale setup
        if (_scaleDropdown != null)
        {
            _scaleDropdown.choices = new List<string> { "Normal", "Large", "Extra Large" };
            _scaleDropdown.value = PlayerPrefs.GetString("UIScale", "Normal");
        }

        // Sensitivity presets
        if (_btnSensLow != null) _btnSensLow.clicked += () => SelectSensitivityPreset("Low", 0.5f);
        if (_btnSensMed != null) _btnSensMed.clicked += () => SelectSensitivityPreset("Medium", 1.0f);
        if (_btnSensHigh != null) _btnSensHigh.clicked += () => SelectSensitivityPreset("High", 2.0f);

        // Slider value labels
        if (_sldMaster != null) _sldMaster.RegisterValueChangedCallback(evt => UpdateLabel(_lblMasterVal, evt.newValue));
        if (_sldMusic != null) _sldMusic.RegisterValueChangedCallback(evt => UpdateLabel(_lblMusicVal, evt.newValue));
        if (_sldSfx != null) _sldSfx.RegisterValueChangedCallback(evt => UpdateLabel(_lblSfxVal, evt.newValue));
        if (_sensitivitySlider != null) _sensitivitySlider.RegisterValueChangedCallback(evt => UpdateSensitivityLabel(evt.newValue));
        if (_sldFog != null) _sldFog.RegisterValueChangedCallback(evt => UpdateFogLabel(evt.newValue));
        if (_sldEcho != null) _sldEcho.RegisterValueChangedCallback(evt => UpdateLabel(_lblEchoVal, evt.newValue));

        // Resolutions dropdown
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
        ShowPauseNav();
    }

    void DiscardSettings()
    {
        ShowPauseNav();
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
