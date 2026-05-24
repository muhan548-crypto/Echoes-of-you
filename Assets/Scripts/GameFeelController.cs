using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

/// <summary>
/// Sistema de Game Feel con jerarquía de intensidad.
/// Separa feedback visual, audio, cámara y tiempo.
/// Cada evento tiene un peso diferente para evitar que todo se sienta igual.
/// </summary>
public class GameFeelController : MonoBehaviour
{
    public static GameFeelController Instance { get; private set; }

    // --- Intensidad ---
    public enum Intensity { Low, Medium, High, Critical }

    [Header("Particulas")]
    [SerializeField] ParticleSystem jumpEffectPrefab;
    [SerializeField] ParticleSystem landingEffectPrefab;
    [SerializeField] ParticleSystem gravityShiftEffectPrefab;
    [SerializeField] ParticleSystem puzzleSolvedEffectPrefab;
    [SerializeField] ParticleSystem recordEffectPrefab;
    [SerializeField] ParticleSystem echoSpawnEffectPrefab;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip jumpClip;
    [SerializeField] AudioClip landingClip;
    [SerializeField] AudioClip gravityShiftClip;
    [SerializeField] AudioClip puzzleSolvedClip;
    [SerializeField] AudioClip recordClip;
    [SerializeField] AudioClip recordStopClip;
    [SerializeField] AudioClip echoSpawnClip;
    [SerializeField] AudioClip softErrorClip;
    [SerializeField] AudioClip platePressClip;
    [SerializeField] AudioClip doorMoveClip;
    [SerializeField] AudioClip playerDeathClip;
    [SerializeField] float defaultVolume = 1f;

    [Header("Camera Shake")]
    [SerializeField] CameraShake cameraShake;
    [SerializeField] ThirdPersonCamera gameplayCamera;
    [SerializeField] FixedPuzzleCameraController fixedGameplayCamera;
    [SerializeField] float jumpShake = 0.08f;
    [SerializeField] float landingShake = 0.18f;
    [SerializeField] float gravityShake = 0.28f;
    [SerializeField] float puzzleSolvedShake = 0.22f;
    [SerializeField] float recordShake = 0.06f;
    [SerializeField] float echoSpawnShake = 0.1f;

    [Header("Slow Motion")]
    [SerializeField] float slowMotionScale = 0.3f;
    [SerializeField] float slowMotionDuration = 0.15f;
    float _slowMotionTimer;
    float _slowMotionTarget = 1f;

    [Header("FOV Pulse")]
    [SerializeField] float baseFOV = 50f;
    float _fovPulseTarget;
    float _fovPulseTimer;
    float _fovPulseDuration;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (cameraShake == null)
            cameraShake = GetComponent<CameraShake>();

        if (gameplayCamera == null)
            gameplayCamera = GetComponent<ThirdPersonCamera>();
        if (gameplayCamera == null)
            gameplayCamera = ThirdPersonCamera.ResolveActive();
        if (fixedGameplayCamera == null)
            fixedGameplayCamera = GetComponent<FixedPuzzleCameraController>();
        if (fixedGameplayCamera == null)
            fixedGameplayCamera = FixedPuzzleCameraController.ResolveActive();
    }

    void Update()
    {
        // Slow motion recovery
        if (_slowMotionTimer > 0f)
        {
            _slowMotionTimer -= Time.unscaledDeltaTime;
            if (_slowMotionTimer <= 0f)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
        }

        // FOV pulse recovery (para Cinemachine, controlado indirectamente)
        if (_fovPulseTimer > 0f)
        {
            _fovPulseTimer -= Time.unscaledDeltaTime;
        }

        UpdatePostProcessingAndCameraEffects();
    }

    void UpdatePostProcessingAndCameraEffects()
    {
        EchoRecorder recorder = Object.FindAnyObjectByType<EchoRecorder>();
        bool isRecording = recorder != null && recorder.IsRecording;
        bool hasEchoes = recorder != null && recorder.EchoCount > 0;

        float targetCA = 0.08f;
        float targetLD = 0f;

        if (isRecording)
        {
            // Aberración y distorsión tipo lente de seguridad / grabación
            targetCA = 0.38f + Mathf.PingPong(Time.unscaledTime * 5f, 0.08f);
            targetLD = -18f + Mathf.PingPong(Time.unscaledTime * 10f, 4f);
        }
        else if (hasEchoes)
        {
            // Aberración media y distorsión sutil que pulsa como un latido
            targetCA = 0.22f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.04f;
            targetLD = -6f + Mathf.Sin(Time.unscaledTime * 3f) * 3f;

            // Pulso/respiración de cámara durante la presencia de ecos
            float breatheFov = baseFOV + Mathf.Sin(Time.unscaledTime * 3f) * 1.5f;
            RequestCameraPulse(breatheFov, 0.05f);
        }

#if UNITY_POST_PROCESSING_STACK_V2
        PostProcessProfile profile = PostProcessingSetup.RuntimeProfile;
        if (profile != null)
        {
            if (profile.TryGetSettings<ChromaticAberration>(out var ca))
            {
                ca.intensity.value = Mathf.MoveTowards(ca.intensity.value, targetCA, Time.unscaledDeltaTime * 1.5f);
            }
            if (profile.TryGetSettings<LensDistortion>(out var ld))
            {
                ld.intensity.value = Mathf.MoveTowards(ld.intensity.value, targetLD, Time.unscaledDeltaTime * 60f);
            }
        }
#endif
    }

    void OnDestroy()
    {
        // Asegurar que timeScale se restaura si el objeto se destruye
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            Instance = null;
        }
    }

    // ═══════════════════════════════════════════
    // EVENTOS DE JUEGO — cada uno con peso distinto
    // ═══════════════════════════════════════════

    // Low: salto básico
    public void PlayJump(Vector3 position, Vector3 up)
    {
        SpawnEffect(jumpEffectPrefab, position, up);
        PlayClip3D(jumpClip, position, defaultVolume * 0.7f);
        cameraShake?.AddShake(jumpShake);
    }

    // Medium: aterrizaje proporcional al impacto
    public void PlayLanding(Vector3 position, Vector3 up, float impactSpeed)
    {
        SpawnEffect(landingEffectPrefab, position, up);
        float vol = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(impactSpeed / 12f));
        PlayClip3D(landingClip, position, defaultVolume * vol);
        cameraShake?.AddShake(Mathf.Clamp01(landingShake + impactSpeed * 0.015f));
    }

    // High: cambio de gravedad
    public void PlayGravityShift(Vector3 position, Vector3 up)
    {
        SpawnEffect(gravityShiftEffectPrefab, position, up);
        PlayClip3D(gravityShiftClip, position, defaultVolume);
        cameraShake?.AddShake(gravityShake);
        ApplySlowMotion(0.4f, 0.12f);
    }

    // Critical: puzzle resuelto — máximo feedback
    public void PlayPuzzleSolved(Vector3 position)
    {
        SpawnEffect(puzzleSolvedEffectPrefab, position, Vector3.up);
        PlayClip3D(puzzleSolvedClip, position, defaultVolume * 1.2f);
        cameraShake?.AddShake(puzzleSolvedShake);
        RequestCameraPulse(50f, 0.35f);
        ApplySlowMotion(slowMotionScale, 0.2f);
    }

    // Medium: inicio de grabación
    public void PlayRecordStart(Vector3 position, Vector3 up)
    {
        SpawnEffect(recordEffectPrefab, position, up);
        PlayClip3D(recordClip, position, defaultVolume * 0.9f);
        cameraShake?.AddShake(recordShake);
        RequestCameraPulse(48f, 0.2f);
    }

    // Low: fin de grabación
    public void PlayRecordStop(Vector3 position)
    {
        PlayClip3D(recordStopClip, position, defaultVolume * 0.9f);
    }

    // High: eco creado — momento clave, slow motion breve
    public void PlayEchoSpawn(Vector3 position)
    {
        SpawnEffect(echoSpawnEffectPrefab, position, Vector3.up);
        PlayClip3D(echoSpawnClip, position, defaultVolume * 1.1f);
        cameraShake?.AddShake(echoSpawnShake * 1.5f);
        RequestCameraPulse(47f, 0.25f);
        ApplySlowMotion(slowMotionScale, slowMotionDuration);
    }

    // Low: error suave
    public void PlaySoftError(Vector3 position)
    {
        PlayClip3D(softErrorClip, position, defaultVolume * 0.6f);
    }

    // Low: placa presionada
    public void PlayPlatePress(Vector3 position)
    {
        PlayClip3D(platePressClip, position, defaultVolume * 0.85f);
        cameraShake?.AddShake(0.04f);
    }

    // Medium: puerta moviéndose
    public void PlayDoorMove(Vector3 position)
    {
        PlayClip3D(doorMoveClip, position, defaultVolume * 0.9f);
        cameraShake?.AddShake(0.06f);
    }

    // High: muerte del jugador
    public void PlayPlayerDeath(Vector3 position)
    {
        PlayClip3D(playerDeathClip, position, defaultVolume * 1.2f);
        cameraShake?.AddShake(0.35f);
        ApplySlowMotion(0.2f, 0.3f);
    }

    // ═══════════════════════════════════════════
    // SUBSISTEMAS INTERNOS
    // ═══════════════════════════════════════════

    // Slow motion controlado — breve, no rompe gameplay
    void ApplySlowMotion(float scale, float duration)
    {
        Time.timeScale = Mathf.Clamp(scale, 0.1f, 1f);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        _slowMotionTimer = duration;
    }

    void RequestCameraPulse(float targetFov, float holdSeconds)
    {
        if (gameplayCamera == null)
            gameplayCamera = ThirdPersonCamera.ResolveActive();
        if (fixedGameplayCamera == null)
            fixedGameplayCamera = FixedPuzzleCameraController.ResolveActive();

        if (gameplayCamera != null)
        {
            gameplayCamera.RequestFovPulse(targetFov, holdSeconds);
            return;
        }

        fixedGameplayCamera?.RequestFovPulse(targetFov, holdSeconds);
    }

    // Partículas — spawn y auto-destroy
    void SpawnEffect(ParticleSystem prefab, Vector3 position, Vector3 up)
    {
        if (prefab == null)
            return;

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, up.normalized);
        ParticleSystem instance = Instantiate(prefab, position, rotation);
        float lifetime = instance.main.duration + instance.main.startLifetime.constantMax;
        Destroy(instance.gameObject, Mathf.Max(1f, lifetime + 0.5f));
    }

    // Audio 3D — usa PlayClipAtPoint para espacialidad real
    void PlayClip3D(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null)
            return;

        // PlayClipAtPoint crea un AudioSource temporal con spatialBlend=1 (3D)
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }
}
