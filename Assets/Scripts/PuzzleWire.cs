using System;
using UnityEngine;

/// <summary>
/// Componente de diseño: Arrastra PressurePlates y DoorControllers en el Inspector
/// para definir qué botones abren qué puertas. Soporta lógica AND/OR.
/// 
/// USO:
/// 1. Añade este componente a cualquier GameObject en tu nivel
/// 2. En el Inspector, agrega entries al array "Connections"
/// 3. Arrastra los PressurePlates y DoorControllers deseados
/// 4. Configura si la puerta necesita TODOS los botones (AND) o solo UNO (OR)
/// 5. Marca "latchOpen" si la puerta debe quedarse abierta permanentemente
/// </summary>
public class PuzzleWire : MonoBehaviour, IResettableLevelObject
{
    [Serializable]
    public class Connection
    {
        [Tooltip("La puerta que será controlada")]
        public DoorController door;

        [Tooltip("Los botones que controlan esta puerta")]
        public PressurePlate[] plates;

        [Tooltip("AND = todos los botones deben estar presionados. OR = cualquiera abre la puerta.")]
        public LogicMode logic = LogicMode.AND;

        [Tooltip("Si es true, la puerta se queda abierta después de abrirse por primera vez")]
        public bool latchOpen = false;

        [Tooltip("Delay antes de que la puerta responda (segundos)")]
        public float responseDelay = 0f;

        [Header("Feedback")]
        [Tooltip("Mensaje que aparece cuando la puerta se abre")]
        public string openMessage = "";
        [Tooltip("Mensaje que aparece cuando la puerta se cierra")]
        public string closeMessage = "";
    }

    public enum LogicMode { AND, OR }

    [Header("Puzzle Connections")]
    [Tooltip("Define las conexiones entre botones y puertas")]
    public Connection[] connections;

    bool[] _doorLatched;

    void Start()
    {
        if (connections == null) return;
        _doorLatched = new bool[connections.Length];

        for (int i = 0; i < connections.Length; i++)
        {
            Connection conn = connections[i];
            if (conn.door == null || conn.plates == null) continue;

            // Override the door's built-in plates with our custom wiring
            conn.door.latchOpen = conn.latchOpen;

            int index = i; // Capture for closure
            foreach (PressurePlate plate in conn.plates)
            {
                if (plate != null)
                    plate.PressedChanged += (_) => EvaluateConnection(index);
            }

            EvaluateConnection(i);
        }
    }

    void EvaluateConnection(int index)
    {
        if (index < 0 || index >= connections.Length) return;
        Connection conn = connections[index];
        if (conn.door == null || conn.plates == null) return;
        if (_doorLatched[index]) return; // Already latched open

        bool shouldOpen;
        if (conn.logic == LogicMode.AND)
        {
            shouldOpen = true;
            foreach (PressurePlate plate in conn.plates)
            {
                if (plate != null && !plate.IsPressed)
                {
                    shouldOpen = false;
                    break;
                }
            }
        }
        else // OR
        {
            shouldOpen = false;
            foreach (PressurePlate plate in conn.plates)
            {
                if (plate != null && plate.IsPressed)
                {
                    shouldOpen = true;
                    break;
                }
            }
        }

        if (conn.responseDelay > 0f && shouldOpen)
        {
            StartCoroutine(DelayedDoorAction(index, shouldOpen, conn.responseDelay));
            return;
        }

        ApplyDoorState(index, shouldOpen);
    }

    System.Collections.IEnumerator DelayedDoorAction(int index, bool open, float delay)
    {
        yield return new WaitForSeconds(delay);
        ApplyDoorState(index, open);
    }

    void ApplyDoorState(int index, bool open)
    {
        Connection conn = connections[index];
        if (conn.door == null) return;

        // Delegate all collider/renderer management to DoorController
        conn.door.SetOpenState(open);

        if (open && conn.latchOpen)
            _doorLatched[index] = true;

        // Show feedback messages
        if (open && !string.IsNullOrEmpty(conn.openMessage))
        {
            GameHUD hud = FindAnyObjectByType<GameHUD>();
            hud?.ShowToast(conn.openMessage, new Color(0.48f, 0.94f, 0.78f, 1f), 1.5f);
        }
        else if (!open && !string.IsNullOrEmpty(conn.closeMessage))
        {
            GameHUD hud = FindAnyObjectByType<GameHUD>();
            hud?.ShowToast(conn.closeMessage, new Color(1f, 0.6f, 0.4f, 1f), 1.5f);
        }
    }

    public void ResetLevelState()
    {
        if (connections == null || _doorLatched == null) return;

        for (int i = 0; i < _doorLatched.Length; i++)
        {
            _doorLatched[i] = false;
        }

        // Re-evaluate all connections so doors go back to their correct state
        for (int i = 0; i < connections.Length; i++)
        {
            if (connections[i].door != null)
                connections[i].door.SetOpenState(false);
        }
    }
}
