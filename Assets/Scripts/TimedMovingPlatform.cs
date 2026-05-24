using UnityEngine;

/// <summary>
/// Plataforma que se mueve entre dos posiciones locales mientras la placa asociada esté presionada.
/// </summary>
public class TimedMovingPlatform : MonoBehaviour, IResettableLevelObject
{
    public PressurePlate plate;
    public Vector3 inactiveLocal = Vector3.zero;
    public Vector3 activeLocal = new Vector3(0f, 0f, 6f);
    public float travelSpeed = 2.5f;
    [Tooltip("Multiplier applied to travelSpeed when returning to inactive state. High value = drops instantly.")]
    public float returnMultiplier = 8f;
    [Tooltip("If true, the platform will return to inactiveLocal using the returnMultiplier speed.")]
    public bool fastReturn = false;

    bool _wasActive;
    Transform _t;
    Vector3 _target;

    void Awake()
    {
        _t = transform;
    }

    void Start()
    {
        if (plate != null)
            plate.PressedChanged += OnPlate;

        RefreshTarget();
    }

    void OnDestroy()
    {
        if (plate != null)
            plate.PressedChanged -= OnPlate;
    }

    void OnPlate(bool pressed)
    {
        RefreshTarget();

        if (pressed && !_wasActive)
        {
            if (TutorialHUD.Instance != null)
                TutorialHUD.Instance.ShowMessage("Puente activo", "", 1.2f);
        }
        _wasActive = pressed;
    }

    void RefreshTarget()
    {
        _target = plate != null && plate.IsPressed ? activeLocal : inactiveLocal;
    }

    void Update()
    {
        float speed = (_target == inactiveLocal && fastReturn) ? (travelSpeed * returnMultiplier) : travelSpeed;
        _t.localPosition = Vector3.MoveTowards(_t.localPosition, _target, speed * Time.deltaTime);
    }

    public void ResetLevelState()
    {
        _target = inactiveLocal;
        if (_t != null)
            _t.localPosition = inactiveLocal;
    }
}
