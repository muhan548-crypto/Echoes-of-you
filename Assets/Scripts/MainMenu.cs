using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador del menú principal. Busca botones por nombre y los conecta en runtime.
/// </summary>
public class MainMenu : MonoBehaviour
{
    public CanvasGroup mainPanel;
    public CanvasGroup levelSelectPanel;
    public CanvasGroup settingsPanel;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        WireButtons();
        ShowMain();
    }

    void WireButtons()
    {
        // Wire all buttons by name
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons)
        {
            string btnName = btn.gameObject.name;
            btn.onClick.RemoveAllListeners();

            switch (btnName)
            {
                case "Btn_Play":
                    btn.onClick.AddListener(PlayGame);
                    break;
                case "Btn_LevelSelect":
                    btn.onClick.AddListener(ShowLevelSelect);
                    break;
                case "Btn_Settings":
                    btn.onClick.AddListener(ShowSettings);
                    break;
                case "Btn_Exit":
                    btn.onClick.AddListener(QuitGame);
                    break;
                case "Btn_Back":
                    btn.onClick.AddListener(ShowMain);
                    break;
                case "Btn_Level01":
                    btn.onClick.AddListener(() => LoadLevel("Level_01"));
                    break;
                case "Btn_Level02":
                    btn.onClick.AddListener(() => LoadLevel("Level_02"));
                    break;
                case "Btn_Level03":
                    btn.onClick.AddListener(() => LoadLevel("Level_03"));
                    break;
                case "Btn_Level04":
                    btn.onClick.AddListener(() => LoadLevel("Level_04"));
                    break;
                case "Btn_Level05":
                    btn.onClick.AddListener(() => LoadLevel("Level_05"));
                    break;
                case "Btn_Level06":
                    btn.onClick.AddListener(() => LoadLevel("Level_06"));
                    break;
            }
        }

        // Wire settings if they exist
        Dropdown resDrop = FindComponentByName<Dropdown>("ResolutionDropdown");
        Toggle fsTog = FindComponentByName<Toggle>("FullscreenToggle");
        Toggle vsTog = FindComponentByName<Toggle>("VsyncToggle");
        Slider volSld = FindComponentByName<Slider>("VolumeSlider");
        Slider sensSld = FindComponentByName<Slider>("SensitivitySlider");

        if (resDrop != null && fsTog != null && vsTog != null && volSld != null && sensSld != null)
            InitializeSettings(resDrop, fsTog, vsTog, volSld, sensSld);
    }

    T FindComponentByName<T>(string objName) where T : Component
    {
        T[] all = GetComponentsInChildren<T>(true);
        foreach (T c in all)
        {
            if (c.gameObject.name == objName)
                return c;
        }
        return null;
    }

    public void ShowMain() { SwitchTo(mainPanel); }
    public void ShowLevelSelect() { SwitchTo(levelSelectPanel); }
    public void ShowSettings() { SwitchTo(settingsPanel); }

    void SwitchTo(CanvasGroup target)
    {
        if (mainPanel != null) mainPanel.gameObject.SetActive(target == mainPanel);
        if (levelSelectPanel != null) levelSelectPanel.gameObject.SetActive(target == levelSelectPanel);
        if (settingsPanel != null) settingsPanel.gameObject.SetActive(target == settingsPanel);
    }

    public void PlayGame()
    {
        LoadLevel("Level_01");
    }

    public void LoadLevel(string levelName)
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

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- SETTINGS ---
    List<Resolution> _filteredResolutions;

    public void InitializeSettings(Dropdown resDropdown, Toggle fullscreenToggle, Toggle vsyncToggle, Slider masterSlider, Slider sensSlider)
    {
        Resolution[] resolutions = Screen.resolutions;
        _filteredResolutions = new List<Resolution>();
        resDropdown.ClearOptions();

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
        resDropdown.AddOptions(options);
        resDropdown.value = currentResIndex;
        resDropdown.RefreshShownValue();
        resDropdown.onValueChanged.AddListener(SetResolution);

        fullscreenToggle.isOn = Screen.fullScreen;
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
        vsyncToggle.onValueChanged.AddListener(SetVSync);

        masterSlider.value = AudioListener.volume;
        masterSlider.onValueChanged.AddListener(SetMasterVolume);

        sensSlider.value = PlayerPrefs.GetFloat("CameraSensitivity", 1f);
        sensSlider.onValueChanged.AddListener(SetCameraSensitivity);
    }

    public void SetResolution(int resIndex)
    {
        if (_filteredResolutions != null && resIndex >= 0 && resIndex < _filteredResolutions.Count)
        {
            Resolution res = _filteredResolutions[resIndex];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        }
    }

    public void SetFullscreen(bool isFullscreen) { Screen.fullScreen = isFullscreen; }
    public void SetVSync(bool isVsync) { QualitySettings.vSyncCount = isVsync ? 1 : 0; }
    public void SetMasterVolume(float vol) { AudioListener.volume = vol; }
    public void SetCameraSensitivity(float sens) { PlayerPrefs.SetFloat("CameraSensitivity", sens); PlayerPrefs.Save(); }
}
