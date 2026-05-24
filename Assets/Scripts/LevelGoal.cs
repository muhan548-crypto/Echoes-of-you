using System.Collections.Generic;
using UnityEngine;

public class LevelGoal : MonoBehaviour, IResettableLevelObject
{
    [SerializeField] string objectiveText = "Activa las memorias y alcanza la salida.";
    [SerializeField] string readyPrompt = "Salida desbloqueada.";
    [SerializeField] string completionToast = "Recuerdo restaurado.";
    [SerializeField] LevelExit[] linkedExits;
    [SerializeField] GoalTrigger[] triggers;
    [SerializeField] bool autoCollectChildTriggers = true;
    [SerializeField] int requiredTriggerCount;

    readonly HashSet<GoalTrigger> _satisfied = new HashSet<GoalTrigger>();

    bool _ready;

    public static LevelGoal Instance { get; private set; }
    public bool IsReady => _ready;
    public string ObjectiveText => objectiveText;
    public string CompletionToast => completionToast;
    public int SatisfiedCount => _satisfied.Count;
    public int RequiredCount => Mathf.Max(1, requiredTriggerCount > 0 ? requiredTriggerCount : triggers != null ? triggers.Length : 1);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (autoCollectChildTriggers)
            triggers = GetComponentsInChildren<GoalTrigger>(true);

        if (linkedExits == null || linkedExits.Length == 0)
            linkedExits = FindObjectsOfType<LevelExit>(true);

        for (int i = 0; i < linkedExits.Length; i++)
        {
            if (linkedExits[i] != null)
                linkedExits[i].BindGoal(this);
        }
    }

    void OnEnable()
    {
        if (triggers == null)
            return;

        for (int i = 0; i < triggers.Length; i++)
        {
            if (triggers[i] != null)
                triggers[i].SatisfactionChanged += OnTriggerChanged;
        }
    }

    void Start()
    {
        RebuildProgress();
        LevelRuntimeController.Instance?.SetObjective(ComposeObjective());
    }

    void OnDisable()
    {
        if (triggers == null)
            return;

        for (int i = 0; i < triggers.Length; i++)
        {
            if (triggers[i] != null)
                triggers[i].SatisfactionChanged -= OnTriggerChanged;
        }
    }

    public bool CanComplete()
    {
        return IsReady;
    }

    public string GetLockedMessage()
    {
        return ComposeObjective();
    }

    public string GetCompletionToast()
    {
        return string.IsNullOrEmpty(completionToast) ? "Recuerdo restaurado." : completionToast;
    }

    public void ResetLevelState()
    {
        _ready = false;
        _satisfied.Clear();
        RebuildProgress();
        LevelRuntimeController.Instance?.SetObjective(ComposeObjective());
    }

    void OnTriggerChanged(GoalTrigger trigger, bool satisfied)
    {
        if (trigger == null)
            return;

        if (satisfied)
            _satisfied.Add(trigger);
        else
            _satisfied.Remove(trigger);

        if (satisfied)
            GameStateController.Instance?.NotifyGoalStep(trigger.transform.position);

        EvaluateState(trigger.transform.position);
    }

    void RebuildProgress()
    {
        _satisfied.Clear();
        if (triggers != null)
        {
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] != null && triggers[i].IsSatisfied)
                    _satisfied.Add(triggers[i]);
            }
        }

        Vector3 focusPoint = transform.position;
        if (linkedExits != null)
        {
            for (int i = 0; i < linkedExits.Length; i++)
            {
                if (linkedExits[i] != null)
                {
                    focusPoint = linkedExits[i].transform.position;
                    break;
                }
            }
        }

        EvaluateState(focusPoint);
    }

    void EvaluateState(Vector3 focusPoint)
    {
        bool wasReady = _ready;
        _ready = SatisfiedCount >= RequiredCount;
        
        Debug.Log($"[QA LevelGoal] Progreso: {SatisfiedCount} / {RequiredCount} | Listo: {_ready}");

        if (linkedExits != null)
        {
            for (int i = 0; i < linkedExits.Length; i++)
            {
                if (linkedExits[i] != null)
                    linkedExits[i].SetUnlocked(_ready);
            }
        }

        LevelRuntimeController.Instance?.SetObjective(ComposeObjective());

        if (!_ready || wasReady)
            return;

        GameHUD hud = FindAnyObjectByType<GameHUD>();
        hud?.ShowToast(readyPrompt, new Color(1f, 0.83f, 0.42f, 1f), 1.35f);
        GameStateController.Instance?.NotifyGoalReady(focusPoint);
    }

    string ComposeObjective()
    {
        if (_ready)
            return objectiveText + " Ve a la salida.";

        return $"{objectiveText} ({SatisfiedCount}/{RequiredCount})";
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
