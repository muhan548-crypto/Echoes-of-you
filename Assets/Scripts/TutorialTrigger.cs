using UnityEngine;

/// <summary>
/// Zona trigger que muestra un mensaje de tutorial cuando el jugador entra.
/// Se activa una sola vez por zona.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("Mensaje")]
    [SerializeField] string message = "Usa WASD para moverte";
    [SerializeField] string hint = "";
    [SerializeField] float displayDuration = 5f;

    [Header("Configuración")]
    [SerializeField] bool oneShot = true;
    [SerializeField] string playerTag = "Player";

    bool _triggered;

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered && oneShot) return;
        if (!other.CompareTag(playerTag)) return;

        _triggered = true;

        GameHUD hud = FindAnyObjectByType<GameHUD>();
        if (hud == null)
            return;

        string combined = string.IsNullOrEmpty(hint)
            ? message
            : $"{message}\n{hint}";

        hud.SetPrompt(combined, displayDuration);
    }
}
