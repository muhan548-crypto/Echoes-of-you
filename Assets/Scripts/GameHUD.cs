using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HUD de gameplay usando UI Toolkit.
/// Muestra indicador de grabación, estado del eco, objetivo, toasts y timeline.
/// Requiere un UIDocument component con GameHUDUI.uxml asignado.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class GameHUD : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] bool showHUD = true;

    UIDocument _doc;
    VisualElement _root;

    // Record panel elements
    VisualElement _recordPanel;
    VisualElement _recordDot;
    VisualElement _echoIcon;
    VisualElement _recordBarBg;
    VisualElement _recordBarFill;
    Label _echoLabel;

    // HUD bars
    VisualElement _stabilityFill;
    VisualElement _recallFill;

    // Text elements
    Label _objectiveText;
    Label _toastText;
    Label _echoStateLabel;

    // Footer status
    VisualElement _recordingStatus;
    VisualElement _playbackStatus;

    // --- Protocol & Schematic Elements ---
    VisualElement _protocolStatus;
    Label _protocolTitle;
    Label _protocolSubtitle;

    VisualElement _modePanel;
    Label _modeTitle;
    Label _modeDesc;
    Label _modeKey;

    VisualElement _nodeLeft;
    VisualElement _nodeRight;
    VisualElement _keyPromptArea;
    Label _keyPromptLabel;

    // Timeline elements
    VisualElement _timeline;
    VisualElement _timelineFill;
    VisualElement _timelinePlayhead;
    Label _timelineStart;
    Label _timelineEnd;

    // State
    int _echoCurrent;
    int _echoMax;
    bool _recording;
    float _recordNorm;
    string _echoState = "";

    string _objective = "";
    string _prompt = "";
    bool _promptSticky;
    float _promptUntil;
    string _toast = "";
    Color _toastColor;
    float _toastUntil;

    void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        if (_doc == null || _doc.rootVisualElement == null) return;
        _root = _doc.rootVisualElement;

        // Apply saved UI scale immediately
        ApplySavedUIScale();

        // Query all elements
        _recordPanel = _root.Q("hud-record-panel");
        _recordDot = _root.Q("hud-record-dot");
        _echoIcon = _root.Q("hud-echo-icon");
        _recordBarBg = _root.Q("hud-record-bar-bg");
        _recordBarFill = _root.Q("hud-record-bar-fill");
        _echoLabel = _root.Q<Label>("hud-echo-label");

        _stabilityFill = _root.Q("hud-stability-fill");
        _recallFill = _root.Q("hud-recall-fill");

        _objectiveText = _root.Q<Label>("hud-objective-text");
        _toastText = _root.Q<Label>("hud-toast-text");
        _echoStateLabel = _root.Q<Label>("hud-echo-state-label");

        _recordingStatus = _root.Q("hud-recording-status");
        _playbackStatus = _root.Q("hud-playback-status");

        // Protocol & Schematic Elements
        _protocolStatus = _root.Q("protocol-status");
        _protocolTitle = _root.Q<Label>("protocol-title");
        _protocolSubtitle = _root.Q<Label>("protocol-subtitle");

        _modePanel = _root.Q("mode-panel");
        _modeTitle = _root.Q<Label>("mode-title");
        _modeDesc = _root.Q<Label>("mode-desc");
        _modeKey = _root.Q<Label>("mode-key");

        _nodeLeft = _root.Q("node-left");
        _nodeRight = _root.Q("node-right");
        _keyPromptArea = _root.Q("key-prompt-area");
        _keyPromptLabel = _root.Q<Label>("key-prompt-label");

        // Timeline elements
        _timeline = _root.Q("timeline");
        _timelineFill = _root.Q("timeline-fill");
        _timelinePlayhead = _root.Q("timeline-playhead");
        _timelineStart = _root.Q<Label>("timeline-start");
        _timelineEnd = _root.Q<Label>("timeline-end");
    }

    // --- Public API (compatible with existing code) ---

    public void SetEchoCount(int current, int max)
    {
        _echoCurrent = Mathf.Max(0, current);
        _echoMax = Mathf.Max(0, max);
    }

    public void SetRecording(bool recording, float normalizedTime01)
    {
        _recording = recording;
        _recordNorm = Mathf.Clamp01(normalizedTime01);
    }

    public void SetEchoState(string state) { _echoState = state ?? string.Empty; }

    public void SetObjective(string objective) { _objective = objective ?? string.Empty; }

    public void SetPrompt(string prompt, float duration = 2.4f)
    {
        _prompt = prompt ?? string.Empty;
        _promptSticky = duration <= 0f;
        _promptUntil = _promptSticky ? float.PositiveInfinity : Time.unscaledTime + duration;
    }

    public void ClearPrompt()
    {
        _prompt = string.Empty;
        _promptSticky = false;
        _promptUntil = 0f;
    }

    public void ShowToast(string message, Color color, float duration = 1.5f)
    {
        _toast = message ?? string.Empty;
        _toastColor = color;
        _toastUntil = Time.unscaledTime + Mathf.Max(0.1f, duration);
    }

    void Update()
    {
        if (!showHUD || _root == null) return;

        // Expire prompt / toast
        if (!_promptSticky && Time.unscaledTime > _promptUntil) _prompt = string.Empty;
        if (Time.unscaledTime > _toastUntil) _toast = string.Empty;

        UpdateRecordPanel();
        UpdateObjective();
        UpdateToast();
        UpdateEchoState();
        UpdateFooterStatus();
        UpdateSchematicHUD();
    }

    void UpdateRecordPanel()
    {
        bool showPanel = _recording || _echoCurrent > 0 || _echoMax > 0;

        if (_recordPanel == null) return;

        if (showPanel)
            _recordPanel.RemoveFromClassList("hidden");
        else
            _recordPanel.AddToClassList("hidden");

        if (!showPanel) return;

        if (_recording)
        {
            Show(_recordDot);
            Show(_recordBarBg);
            Hide(_echoIcon);
            Hide(_echoLabel);

            float pulse = Mathf.Sin(Time.unscaledTime * 4f) * 0.3f + 0.7f;
            if (_recordDot != null)
                _recordDot.style.opacity = pulse;

            if (_recordBarFill != null)
                _recordBarFill.style.width = Length.Percent(_recordNorm * 100f);
        }
        else
        {
            Hide(_recordDot);
            Hide(_recordBarBg);
            Show(_echoIcon);
            Show(_echoLabel);

            if (_echoLabel != null)
                _echoLabel.text = $"{_echoCurrent}/{_echoMax}";
        }
    }

    void UpdateObjective()
    {
        if (_objectiveText != null)
            _objectiveText.text = _objective;
    }

    void UpdateToast()
    {
        if (_toastText == null) return;

        string txt = !string.IsNullOrEmpty(_toast) ? _toast : _prompt;
        Color col = !string.IsNullOrEmpty(_toast) ? _toastColor : new Color(0.1f, 0.5f, 1f, 0.6f);

        _toastText.text = txt;
        _toastText.style.color = new StyleColor(col);
    }

    void UpdateEchoState()
    {
        if (_echoStateLabel == null) return;
        _echoStateLabel.text = _echoState;

        if (!string.IsNullOrEmpty(_echoState))
        {
            string lower = _echoState.ToLowerInvariant();
            Color stateColor;
            if (lower.Contains("grab"))
                stateColor = new Color(0.9f, 0.15f, 0.15f, 1f);
            else if (lower.Contains("repro") || lower.Contains("eco"))
                stateColor = new Color(0.5f, 0.2f, 1f, 1f);
            else
                stateColor = new Color(0.8f, 0.8f, 0.8f, 1f);

            _echoStateLabel.style.color = new StyleColor(stateColor);
        }
    }

    void UpdateFooterStatus()
    {
        if (_recordingStatus != null)
        {
            if (_recording)
            {
                _recordingStatus.RemoveFromClassList("footer-item--inactive");
                _recordingStatus.AddToClassList("footer-item--active");
            }
            else
            {
                _recordingStatus.RemoveFromClassList("footer-item--active");
                _recordingStatus.AddToClassList("footer-item--inactive");
            }
        }

        if (_playbackStatus != null)
        {
            if (_echoCurrent > 0 && !_recording)
            {
                _playbackStatus.RemoveFromClassList("footer-item--inactive");
                _playbackStatus.AddToClassList("footer-item--active");
            }
            else
            {
                _playbackStatus.RemoveFromClassList("footer-item--active");
                _playbackStatus.AddToClassList("footer-item--inactive");
            }
        }
    }

    // --- Schematic / Timeline HUD updates ---

    void UpdateSchematicHUD()
    {
        // 1. Dynamic Mode Panel based on recording / playback state
        if (_modePanel != null)
        {
            _modePanel.RemoveFromClassList("hidden");
            if (_recording)
            {
                if (_modeTitle != null) _modeTitle.text = "GRABACIÓN COGNITIVA";
                if (_modeDesc != null) _modeDesc.text = "Registrando línea de tiempo...";
                if (_modeKey != null) _modeKey.text = "R";
            }
            else if (_echoCurrent > 0)
            {
                if (_modeTitle != null) _modeTitle.text = "SINCRONIZACIÓN ECO";
                if (_modeDesc != null) _modeDesc.text = "Reproduciendo secuencia temporal.";
                if (_modeKey != null) _modeKey.text = "E";
            }
            else
            {
                if (_modeTitle != null) _modeTitle.text = "NEURAL LISTO";
                if (_modeDesc != null) _modeDesc.text = "Buscando anclas de memoria.";
                if (_modeKey != null) _modeKey.text = "R";
            }
        }

        // 2. Timeline Progress Bar
        if (_timeline != null)
        {
            float fillPct = _recordNorm * 100f;
            if (_timelineFill != null)
                _timelineFill.style.width = Length.Percent(fillPct);

            if (_timelinePlayhead != null)
                _timelinePlayhead.style.left = Length.Percent(fillPct);

            // Display dynamic time stamps (seconds)
            float totalSecs = 10f; // assuming 10 second maximum record length
            float currentSecs = _recordNorm * totalSecs;
            if (_timelineStart != null)
                _timelineStart.text = $"00:00:{currentSecs:00.00}";
            if (_timelineEnd != null)
                _timelineEnd.text = $"00:00:{totalSecs:00.00}";
        }

        // 3. Floating Schematic Nodes & Prompts
        if (_recording)
        {
            _nodeLeft?.RemoveFromClassList("hidden");
            _nodeRight?.AddToClassList("hidden");
            _keyPromptArea?.AddToClassList("hidden");
        }
        else if (_echoCurrent > 0)
        {
            _nodeLeft?.AddToClassList("hidden");
            _nodeRight?.RemoveFromClassList("hidden");
            _keyPromptArea?.AddToClassList("hidden");
        }
        else
        {
            // If near puzzle plate or objective, show key prompt area
            if (!string.IsNullOrEmpty(_prompt) && (_prompt.Contains("placa") || _prompt.Contains("puerta") || _prompt.Contains("botón")))
            {
                _keyPromptArea?.RemoveFromClassList("hidden");
                if (_keyPromptLabel != null)
                {
                    if (_prompt.Contains("ESPACIO")) _keyPromptLabel.text = "ESPACIO";
                    else if (_prompt.Contains("E")) _keyPromptLabel.text = "E";
                    else if (_prompt.Contains("R")) _keyPromptLabel.text = "R";
                    else _keyPromptLabel.text = "ACCION";
                }
            }
            else
            {
                _keyPromptArea?.AddToClassList("hidden");
            }
            _nodeLeft?.AddToClassList("hidden");
            _nodeRight?.AddToClassList("hidden");
        }
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

    // --- Helpers ---

    void Show(VisualElement el) { el?.RemoveFromClassList("hidden"); }
    void Hide(VisualElement el) { el?.AddToClassList("hidden"); }
}
