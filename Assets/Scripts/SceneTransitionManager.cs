using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    Canvas _canvas;
    Image _fadeImage;
    bool _isTransitioning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateCanvas();
    }

    void CreateCanvas()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(transform, false);
        _fadeImage = imgObj.AddComponent<Image>();
        _fadeImage.color = new Color(0f, 0f, 0f, 0f); // Transparente por defecto
        _fadeImage.raycastTarget = false;

        RectTransform rt = _fadeImage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }

    public void LoadScene(string sceneName)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        _isTransitioning = true;
        _fadeImage.raycastTarget = true; // Bloquea clicks

        // Fade Out (hacia negro)
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 2f; // Medio segundo
            _fadeImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t));
            yield return null;
        }

        // Cargar escena
        PostProcessingSetup.PrepareForSceneReload();
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync(sceneName);

        // Pequeña pausa
        yield return new WaitForSecondsRealtime(0.1f);

        // Fade In (hacia transparente)
        t = 1f;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime * 2f;
            _fadeImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t));
            yield return null;
        }

        _fadeImage.raycastTarget = false;
        _isTransitioning = false;
    }
}
