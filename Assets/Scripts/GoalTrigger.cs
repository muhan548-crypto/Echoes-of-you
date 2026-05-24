using System;
using UnityEngine;

public class GoalTrigger : MonoBehaviour, IResettableLevelObject
{
    [SerializeField] string displayName = "Memoria";
    [SerializeField] PressurePlate pressurePlate;
    [SerializeField] DoorController doorController;
    [SerializeField] bool usePlatePressedState = true;
    [SerializeField] bool useDoorOpenState;
    [SerializeField] bool accumulateOnce = true;

    bool _satisfied;

    public string DisplayName => displayName;
    public bool IsSatisfied => _satisfied;

    public event Action<GoalTrigger, bool> SatisfactionChanged;

    void Awake()
    {
        if (pressurePlate == null)
            pressurePlate = GetComponent<PressurePlate>();
        if (doorController == null)
            doorController = GetComponent<DoorController>();
    }

    void OnEnable()
    {
        if (pressurePlate != null)
            pressurePlate.PressedChanged += OnPlateChanged;
        if (doorController != null)
            doorController.DoorStateChanged += OnDoorChanged;
    }

    void Start()
    {
        RefreshFromSources();
    }

    void OnDisable()
    {
        if (pressurePlate != null)
            pressurePlate.PressedChanged -= OnPlateChanged;
        if (doorController != null)
            doorController.DoorStateChanged -= OnDoorChanged;
    }

    public void MarkSatisfied()
    {
        SetSatisfied(true);
    }

    public void ResetLevelState()
    {
        _satisfied = false;
        SatisfactionChanged?.Invoke(this, false);
        RefreshFromSources();
    }

    void OnPlateChanged(bool pressed)
    {
        if (usePlatePressedState)
            SetSatisfied(pressed);
    }

    void OnDoorChanged(bool open)
    {
        if (useDoorOpenState)
            SetSatisfied(open);
    }

    void RefreshFromSources()
    {
        if (useDoorOpenState && doorController != null)
        {
            SetSatisfied(doorController.IsOpen);
            return;
        }

        if (usePlatePressedState && pressurePlate != null)
            SetSatisfied(pressurePlate.IsPressed);
    }

    void SetSatisfied(bool value)
    {
        if (accumulateOnce && _satisfied && !value)
            return;

        if (_satisfied == value)
            return;

        _satisfied = value;
        SatisfactionChanged?.Invoke(this, _satisfied);
    }
}
