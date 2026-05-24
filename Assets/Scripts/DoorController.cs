using System;
using UnityEngine;

/// <summary>
/// When all linked plates are pressed, the door opens and emits a one-shot
/// puzzle solved feedback event. If plates are released again, the door closes.
/// </summary>
public class DoorController : MonoBehaviour, IResettableLevelObject
{
    public PressurePlate[] plates;
    public bool latchOpen = true;
    public bool invertLogic = false;

    Collider _col;
    Renderer[] _renderers;
    bool _isOpen;

    public bool IsOpen => _isOpen;

    public event Action<bool> DoorStateChanged;

    void Start()
    {
        _col = GetComponent<Collider>();
        _renderers = GetComponentsInChildren<Renderer>();

        if (plates != null)
        {
            foreach (PressurePlate plate in plates)
            {
                if (plate != null)
                    plate.PressedChanged += OnPlateChanged;
            }
        }

        UpdateState();
    }

    void OnDestroy()
    {
        if (plates == null)
            return;

        foreach (PressurePlate plate in plates)
        {
            if (plate != null)
                plate.PressedChanged -= OnPlateChanged;
        }
    }

    void OnPlateChanged(bool _)
    {
        UpdateState();
    }

    void UpdateState()
    {
        bool shouldOpen = true;
        if (plates == null || plates.Length == 0)
        {
            shouldOpen = false;
        }
        else
        {
            foreach (PressurePlate plate in plates)
            {
                if (plate != null)
                {
                    bool checkState = plate.IsPressed;
                    if (invertLogic) checkState = !checkState;

                    if (!checkState)
                    {
                        shouldOpen = false;
                        break;
                    }
                }
            }
        }

        if (latchOpen && _isOpen)
            shouldOpen = true;

        if (_isOpen != shouldOpen)
        {
            _isOpen = shouldOpen;
            DoorStateChanged?.Invoke(_isOpen);

            GameFeelController.Instance?.PlayDoorMove(transform.position);

            if (_isOpen)
            {
                if (TutorialHUD.Instance != null)
                    TutorialHUD.Instance.ShowMessage("Puerta abierta", "", 1.5f);
            }
        }

        ApplyVisualState();
    }

    /// <summary>
    /// Force the door into a specific open/closed state. Used by PuzzleWire
    /// and other external controllers. Recursively disables/enables ALL
    /// colliders in children so invisible child geometry can't block players.
    /// </summary>
    public void SetOpenState(bool open)
    {
        _isOpen = open;
        ApplyVisualState();
    }

    void ApplyVisualState()
    {
        // Disable ALL colliders recursively (root + children)
        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider c in allColliders)
        {
            if (c != null)
                c.enabled = !_isOpen;
        }

        if (_renderers == null)
            return;

        foreach (Renderer renderer in _renderers)
        {
            if (renderer != null)
                renderer.enabled = !_isOpen;
        }
    }

    public void ResetLevelState()
    {
        _isOpen = false;
        UpdateState();
    }
}
