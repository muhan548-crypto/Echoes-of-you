using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PostProcessingSetup : MonoBehaviour
{
    static PostProcessingSetup _instance;

#if UNITY_POST_PROCESSING_STACK_V2
    static GameObject _volumeGo;
    static PostProcessProfile _runtimeProfile;
    static PostProcessResources _cachedResources;

    Coroutine _setupRoutine;
    bool _loggedMissingResources;

    public static PostProcessProfile RuntimeProfile => _runtimeProfile;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSetup()
    {
        EnsureInstance();
        _instance?.ScheduleSetup();
    }

    public static void PrepareForSceneReload()
    {
        if (_instance == null)
            return;

        _instance.CleanupRuntimeObjects(true);
    }

    static void EnsureInstance()
    {
        if (_instance != null)
            return;

        GameObject go = new GameObject("PostProcessingSetup");
        _instance = go.AddComponent<PostProcessingSetup>();
        DontDestroyOnLoad(go);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _instance?.ScheduleSetup();
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ScheduleSetup();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

#if UNITY_POST_PROCESSING_STACK_V2
        if (_setupRoutine != null)
        {
            StopCoroutine(_setupRoutine);
            _setupRoutine = null;
        }
#endif
    }

    void OnDestroy()
    {
        if (_instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        CleanupRuntimeObjects(false);
        _instance = null;
    }

    void ScheduleSetup()
    {
#if UNITY_POST_PROCESSING_STACK_V2
        if (_setupRoutine != null)
            StopCoroutine(_setupRoutine);

        _setupRoutine = StartCoroutine(SetupWhenCameraReady());
#endif
    }

#if UNITY_POST_PROCESSING_STACK_V2
    IEnumerator SetupWhenCameraReady()
    {
        CleanupRuntimeObjects(false);

        for (int frame = 0; frame < 40; frame++)
        {
            Camera cameraRef = Camera.main;
            if (cameraRef != null)
            {
                SetupPostProcessing(cameraRef);
                _setupRoutine = null;
                yield break;
            }

            yield return null;
        }

        _setupRoutine = null;
    }

    void SetupPostProcessing(Camera cameraRef)
    {
        if (cameraRef == null)
            return;

        if (!TryResolveResources(out PostProcessResources resources))
        {
            DisablePostProcessing(cameraRef);
            return;
        }

        PostProcessLayer layer = cameraRef.GetComponent<PostProcessLayer>();
        if (layer == null)
            layer = cameraRef.gameObject.AddComponent<PostProcessLayer>();

        layer.volumeTrigger = cameraRef.transform;
        layer.volumeLayer = LayerMask.GetMask("Default");
        layer.stopNaNPropagation = true;
        layer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        layer.Init(resources);
        layer.enabled = true;

        if (_volumeGo != null)
            Destroy(_volumeGo);
        if (_runtimeProfile != null)
            Destroy(_runtimeProfile);

        _volumeGo = new GameObject("GlobalPostProcessVolume");
        PostProcessVolume volume = _volumeGo.AddComponent<PostProcessVolume>();
        volume.isGlobal = true;
        volume.priority = 1f;

        _runtimeProfile = ScriptableObject.CreateInstance<PostProcessProfile>();

        Bloom bloom = _runtimeProfile.AddSettings<Bloom>();
        bloom.enabled.value = true;
        bloom.intensity.value = 0.85f;
        bloom.intensity.overrideState = true;
        bloom.threshold.value = 1.2f;
        bloom.threshold.overrideState = true;
        bloom.softKnee.value = 0.45f;
        bloom.softKnee.overrideState = true;
        bloom.diffusion.value = 6f;
        bloom.diffusion.overrideState = true;

        ColorGrading grading = _runtimeProfile.AddSettings<ColorGrading>();
        grading.enabled.value = true;
        grading.gradingMode.value = GradingMode.LowDefinitionRange;
        grading.gradingMode.overrideState = true;
        grading.lift.value = new Vector4(-0.08f, -0.06f, 0.02f, 0f);
        grading.lift.overrideState = true;
        grading.gamma.value = new Vector4(0f, 0f, 0.02f, -0.02f);
        grading.gamma.overrideState = true;
        grading.gain.value = new Vector4(0.01f, 0.01f, 0.02f, 0f);
        grading.gain.overrideState = true;
        grading.saturation.value = -28f;
        grading.saturation.overrideState = true;
        grading.contrast.value = 22f;
        grading.contrast.overrideState = true;

        Vignette vignette = _runtimeProfile.AddSettings<Vignette>();
        vignette.enabled.value = true;
        vignette.intensity.value = 0.33f;
        vignette.intensity.overrideState = true;
        vignette.smoothness.value = 0.92f;
        vignette.smoothness.overrideState = true;
        vignette.color.value = Color.black;
        vignette.color.overrideState = true;

        AmbientOcclusion ambientOcclusion = _runtimeProfile.AddSettings<AmbientOcclusion>();
        ambientOcclusion.enabled.value = false;
        ambientOcclusion.enabled.overrideState = true;

        ChromaticAberration ca = _runtimeProfile.AddSettings<ChromaticAberration>();
        ca.enabled.value = true;
        ca.intensity.value = 0.08f;
        ca.intensity.overrideState = true;

        LensDistortion ld = _runtimeProfile.AddSettings<LensDistortion>();
        ld.enabled.value = true;
        ld.intensity.value = 0f;
        ld.intensity.overrideState = true;

        volume.profile = _runtimeProfile;
    }

    void DisablePostProcessing(Camera cameraRef)
    {
        if (cameraRef != null)
        {
            PostProcessLayer layer = cameraRef.GetComponent<PostProcessLayer>();
            if (layer != null)
                layer.enabled = false;
        }

        CleanupRuntimeObjects(false);
    }

    bool TryResolveResources(out PostProcessResources resources)
    {
        resources = _cachedResources;
        if (resources != null)
            return true;

        PostProcessResources[] loadedResources = Resources.FindObjectsOfTypeAll<PostProcessResources>();
        if (loadedResources != null)
        {
            for (int i = 0; i < loadedResources.Length; i++)
            {
                if (loadedResources[i] != null)
                {
                    _cachedResources = loadedResources[i];
                    resources = _cachedResources;
                    return true;
                }
            }
        }

#if UNITY_EDITOR
        _cachedResources = AssetDatabase.LoadAssetAtPath<PostProcessResources>(
            "Library/PackageCache/com.unity.postprocessing@141166b789e3/PostProcessing/PostProcessResources.asset");
        if (_cachedResources == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:PostProcessResources");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                _cachedResources = AssetDatabase.LoadAssetAtPath<PostProcessResources>(path);
                if (_cachedResources != null)
                    break;
            }
        }
#endif

        resources = _cachedResources;
        if (resources != null)
            return true;

        if (!_loggedMissingResources)
        {
            _loggedMissingResources = true;
            Debug.LogWarning("PostProcessingSetup: PostProcessResources no encontrado. Se desactiva el post process para evitar NullReferenceException.");
        }

        return false;
    }

    void CleanupRuntimeObjects(bool disableSceneLayers)
    {
        if (_setupRoutine != null)
        {
            StopCoroutine(_setupRoutine);
            _setupRoutine = null;
        }

        if (disableSceneLayers)
        {
            PostProcessLayer[] layers = FindObjectsOfType<PostProcessLayer>(true);
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null)
                    continue;

                layers[i].enabled = false;
                Destroy(layers[i]);
            }
        }

        if (_volumeGo != null)
        {
            Destroy(_volumeGo);
            _volumeGo = null;
        }

        if (_runtimeProfile != null)
        {
            Destroy(_runtimeProfile);
            _runtimeProfile = null;
        }
    }
#else
    void ScheduleSetup() { }
    void CleanupRuntimeObjects(bool disableSceneLayers) { }
#endif
}
