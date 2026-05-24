using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class HubPortal : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] string sceneName = "Level_01";
    [SerializeField] string displayName = "Primer Rastro";
    [SerializeField] string memoryLine = "Primero recuerdas.";

    [Header("Visuals")]
    [SerializeField] Renderer[] visualRenderers;
    [SerializeField] Light portalLight;
    [SerializeField] Color lockedColor = new Color(0.38f, 0.24f, 0.28f, 1f);
    [SerializeField] Color unlockedColor = new Color(0.16f, 0.85f, 1f, 1f);
    [SerializeField] Color completedColor = new Color(1f, 0.82f, 0.45f, 1f);

    GameHUD _hud;
    bool _playerInside;
    bool _isUnlocked;
    bool _isCompleted;

    void Awake()
    {
        var colliderRef = GetComponent<Collider>();
        colliderRef.isTrigger = true;

        if (GetComponent<Rigidbody>() == null)
        {
            var body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        if (visualRenderers == null || visualRenderers.Length == 0)
            visualRenderers = GetComponentsInChildren<Renderer>(true);
    }

    void Start()
    {
        _hud = FindAnyObjectByType<GameHUD>();
        RefreshState();
    }

    void Update()
    {
        if (!_playerInside)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_isUnlocked)
            {
                PostProcessingSetup.PrepareForSceneReload();
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                _hud?.ShowToast("Recuerdo bloqueado", new Color(1f, 0.43f, 0.43f, 1f), 1.2f);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerInside = true;
        _hud ??= FindAnyObjectByType<GameHUD>();
        RefreshState();
        ShowPrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerInside = false;
        _hud?.ClearPrompt();
    }

    void RefreshState()
    {
        _isUnlocked = GameProgress.IsSceneUnlocked(sceneName);
        _isCompleted = GameProgress.IsSceneCompleted(sceneName);

        Color targetColor = _isCompleted ? completedColor : (_isUnlocked ? unlockedColor : lockedColor);

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            Renderer rendererRef = visualRenderers[i];
            if (rendererRef == null)
                continue;

            Material[] mats = rendererRef.materials;
            for (int matIndex = 0; matIndex < mats.Length; matIndex++)
            {
                if (mats[matIndex] == null)
                    continue;

                mats[matIndex].color = targetColor;

                if (mats[matIndex].HasProperty("_EmissionColor"))
                {
                    mats[matIndex].EnableKeyword("_EMISSION");
                    mats[matIndex].SetColor("_EmissionColor", targetColor * (_isUnlocked ? 1.5f : 0.25f));
                }
            }
        }

        if (portalLight != null)
        {
            portalLight.color = targetColor;
            portalLight.intensity = _isUnlocked ? 4.5f : 1.2f;
            portalLight.range = _isUnlocked ? 8f : 5f;
        }
    }

    void ShowPrompt()
    {
        if (_hud == null)
            return;

        if (_isUnlocked)
        {
            string prefix = _isCompleted ? "Repetir" : "Entrar";
            _hud.SetPrompt($"E - {prefix} en {displayName}\n{memoryLine}", 0f);
        }
        else
        {
            _hud.SetPrompt($"{displayName}\nBloqueado hasta restaurar el recuerdo anterior.", 0f);
        }
    }
}
