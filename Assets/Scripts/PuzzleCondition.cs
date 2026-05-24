using System;
using UnityEngine;

/// <summary>
/// Define condiciones para desbloquear algo (puertas, eventos, salidas).
/// Configurable desde el Inspector. Soporta: temporizadores, contadores, secuencias.
/// 
/// USO:
/// 1. Añade a un GameObject
/// 2. Configura el tipo de condición
/// 3. Arrastra las PressurePlates relevantes
/// 4. Conecta el evento OnConditionMet a lo que quieras activar
/// </summary>
public class PuzzleCondition : MonoBehaviour
{
    public enum ConditionType
    {
        AllPlatesSimultaneous,  // Todos los botones presionados al mismo tiempo
        AnyPlateOnce,           // Cualquier botón activado una vez
        SequentialOrder,        // Botones en orden específico (plates[0], luego [1], etc.)
        TimedHold,              // Mantener un botón por X segundos
        PlateCount              // Activar N botones cualquiera de la lista
    }

    [Header("Condition Setup")]
    public ConditionType type = ConditionType.AllPlatesSimultaneous;
    public PressurePlate[] plates;

    [Header("Parameters")]
    [Tooltip("Para TimedHold: segundos que debe mantenerse presionado")]
    public float holdDuration = 3f;
    [Tooltip("Para PlateCount: cuántos botones deben estar activos")]
    public int requiredCount = 1;

    [Header("Feedback")]
    public string progressMessage = "";
    public string successMessage = "Condicion cumplida!";
    public string failMessage = "";

    [Header("State")]
    [SerializeField] bool _isMet = false;
    public bool IsMet => _isMet;

    public event Action<bool> ConditionChanged;

    // Sequential tracking
    int _sequenceIndex = 0;
    // Timed tracking
    float _holdTimer = 0f;
    bool _holdActive = false;

    void Start()
    {
        if (plates == null) return;

        for (int i = 0; i < plates.Length; i++)
        {
            if (plates[i] != null)
            {
                int idx = i;
                plates[i].PressedChanged += (pressed) => OnPlateChanged(idx, pressed);
            }
        }
    }

    void Update()
    {
        if (type == ConditionType.TimedHold && _holdActive && !_isMet)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= holdDuration)
            {
                SetMet(true);
            }
        }
    }

    void OnPlateChanged(int plateIndex, bool pressed)
    {
        switch (type)
        {
            case ConditionType.AllPlatesSimultaneous:
                EvaluateAll();
                break;

            case ConditionType.AnyPlateOnce:
                if (pressed) SetMet(true);
                break;

            case ConditionType.SequentialOrder:
                EvaluateSequence(plateIndex, pressed);
                break;

            case ConditionType.TimedHold:
                if (pressed)
                {
                    _holdActive = true;
                    _holdTimer = 0f;
                }
                else
                {
                    _holdActive = false;
                    _holdTimer = 0f;
                    if (!string.IsNullOrEmpty(failMessage))
                        ShowToast(failMessage, new Color(1f, 0.5f, 0.4f));
                }
                break;

            case ConditionType.PlateCount:
                EvaluateCount();
                break;
        }
    }

    void EvaluateAll()
    {
        bool allPressed = true;
        foreach (var p in plates)
        {
            if (p != null && !p.IsPressed)
            {
                allPressed = false;
                break;
            }
        }
        SetMet(allPressed);
    }

    void EvaluateSequence(int plateIndex, bool pressed)
    {
        if (!pressed) return;
        if (_isMet) return;

        if (plateIndex == _sequenceIndex)
        {
            _sequenceIndex++;
            if (!string.IsNullOrEmpty(progressMessage))
                ShowToast($"{progressMessage} ({_sequenceIndex}/{plates.Length})", new Color(0.5f, 0.8f, 1f));

            if (_sequenceIndex >= plates.Length)
                SetMet(true);
        }
        else
        {
            // Wrong order - reset
            _sequenceIndex = 0;
            if (!string.IsNullOrEmpty(failMessage))
                ShowToast(failMessage, new Color(1f, 0.5f, 0.4f));
        }
    }

    void EvaluateCount()
    {
        int count = 0;
        foreach (var p in plates)
        {
            if (p != null && p.IsPressed) count++;
        }
        SetMet(count >= requiredCount);
    }

    void SetMet(bool met)
    {
        if (_isMet == met) return;
        _isMet = met;
        ConditionChanged?.Invoke(_isMet);

        if (_isMet && !string.IsNullOrEmpty(successMessage))
            ShowToast(successMessage, new Color(0.48f, 0.94f, 0.78f));
    }

    void ShowToast(string msg, Color color)
    {
        GameHUD hud = FindAnyObjectByType<GameHUD>();
        hud?.ShowToast(msg, color, 1.5f);
    }

    public void ResetCondition()
    {
        _isMet = false;
        _sequenceIndex = 0;
        _holdTimer = 0f;
        _holdActive = false;
        ConditionChanged?.Invoke(false);
    }
}
