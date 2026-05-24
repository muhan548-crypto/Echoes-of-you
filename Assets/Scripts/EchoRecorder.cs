using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grabación por mantener R: hasta 6 s o al soltar. Genera un eco que repite el bucle.
/// Q limpia todos los ecos.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class EchoRecorder : MonoBehaviour
{
    [Header("Prefab y límites")]
    [SerializeField] GameObject echoPrefab;
    [SerializeField] Transform echoSpawnRoot;
    [SerializeField] int maxEchoes = 2;
    [SerializeField] float maxRecordSeconds = 6f;
    [SerializeField] float minRecordSeconds = 0.35f;

    [Header("HUD (opcional)")]
    [SerializeField] GameHUD hud;

    readonly List<RecordFrame> _frames = new List<RecordFrame>();
    readonly List<EchoPlayback> _echoes = new List<EchoPlayback>();

    Animator _anim;
    bool _recording;
    float _recordStartTime;
    float _lastRecordDuration;

    public bool IsRecording => _recording;
    public int EchoCount => _echoes.Count;
    public int MaxEchoes => maxEchoes;
    public float MaxRecordSeconds => maxRecordSeconds;
    public float RecordingElapsed => _recording ? Mathf.Min(Time.time - _recordStartTime, maxRecordSeconds) : 0f;
    public float LastClipDuration => _lastRecordDuration;
    public bool HasEchoes => _echoes.Count > 0;

    /// <summary>Fired when an echo is created. Arg = current echo count.</summary>
    public event Action<int> EchoCreated;
    /// <summary>Fired when all echoes are cleared.</summary>
    public event Action EchoesCleared;
    /// <summary>Fired when recording starts.</summary>
    public event Action RecordingStarted;
    /// <summary>Fired when recording stops (even if too short).</summary>
    public event Action<bool> RecordingStopped;

    void Awake()
    {
        if (hud == null)
            hud = UnityEngine.Object.FindAnyObjectByType<GameHUD>();
        if (echoSpawnRoot == null)
            echoSpawnRoot = transform;
        _anim = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        RefreshHud();
    }

    void Update()
    {
        bool hold = Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.R);

        if (hold && !_recording)
            StartRecording();

        if (!hold && _recording)
            StopRecordingAndSpawn();

        if (_recording && Time.time - _recordStartTime >= maxRecordSeconds)
            StopRecordingAndSpawn();

        if (_anim != null && _anim.runtimeAnimatorController != null)
            _anim.SetBool("IsRecording", _recording);

        RefreshHud();
    }

    void FixedUpdate()
    {
        if (!_recording)
            return;

        float elapsed = Time.time - _recordStartTime;
        _frames.Add(new RecordFrame(elapsed, transform.position, transform.rotation));

        if (_frames.Count == 1)
        {
            // Duplicar primer fotograma en t=0 para interpolación estable al inicio
            _frames.Insert(0, new RecordFrame(0f, _frames[0].position, _frames[0].rotation));
        }
    }

    void StartRecording()
    {
        _recording = true;
        _recordStartTime = Time.time;
        _frames.Clear();
        if (_anim == null) _anim = GetComponentInChildren<Animator>();
        if (_anim != null && _anim.runtimeAnimatorController != null) _anim.SetBool("IsRecording", true);
        RecordingStarted?.Invoke();
        GameStateController.Instance?.SetRecording(true, transform.position, transform.up);
        hud?.SetPrompt("Suelta R para crear un eco", 1.6f);
        RefreshHud();
    }

    void StopRecordingAndSpawn()
    {
        if (!_recording)
            return;

        _recording = false;
        float elapsed = Time.time - _recordStartTime;
        _lastRecordDuration = elapsed;
        GameStateController.Instance?.SetRecording(false, transform.position, transform.up);

        GameFeelController.Instance?.PlayRecordStop(transform.position);

        if (elapsed < minRecordSeconds || _frames.Count < 2)
        {
            _frames.Clear();
            RecordingStopped?.Invoke(false);
            hud?.ShowToast("Grabacion muy corta", new Color(1f, 0.43f, 0.43f, 1f), 1.1f);
            hud?.SetEchoState("Error");
            GameFeelController.Instance?.PlaySoftError(transform.position);
            RefreshHud();
            return;
        }

        if (echoPrefab == null)
        {
            Debug.LogError("EchoRecorder: asigna echoPrefab.");
            _frames.Clear();
            hud?.ShowToast("Falta el prefab del eco", new Color(1f, 0.43f, 0.43f, 1f), 1.2f);
            return;
        }

        TrimEchoesIfNeeded();

        GameObject instance = Instantiate(echoPrefab, echoSpawnRoot.position, echoSpawnRoot.rotation);
        instance.tag = "Echo";
        var playback = instance.GetComponent<EchoPlayback>();
        if (playback == null)
            playback = instance.AddComponent<EchoPlayback>();

        float duration = Mathf.Max(elapsed, 0.05f);
        playback.BeginPlayback(_frames, duration);
        _echoes.Add(playback);

        _frames.Clear();

        RecordingStopped?.Invoke(true);
        EchoCreated?.Invoke(_echoes.Count);
        hud?.ShowToast("Eco creado", new Color(0.48f, 0.94f, 0.78f, 1f), 1.25f);
        GameFeelController.Instance?.PlayEchoSpawn(instance.transform.position);

        RefreshHud();
    }

    void TrimEchoesIfNeeded()
    {
        while (_echoes.Count >= maxEchoes)
        {
            EchoPlayback oldest = _echoes[0];
            _echoes.RemoveAt(0);
            if (oldest != null)
                Destroy(oldest.gameObject);
        }
    }

    public void ClearAllEchoes(bool showFeedback = true)
    {
        _recording = false;
        _frames.Clear();
        _lastRecordDuration = 0f;
        GameStateController.Instance?.SetRecording(false, transform.position, transform.up);

        for (int i = 0; i < _echoes.Count; i++)
        {
            if (_echoes[i] != null)
                Destroy(_echoes[i].gameObject);
        }

        _echoes.Clear();
        EchoesCleared?.Invoke();
        if (showFeedback)
            hud?.ShowToast("Ecos limpiados", new Color(0.48f, 0.94f, 0.78f, 1f), 1f);
        RefreshHud();
    }

    void RefreshHud()
    {
        if (hud == null)
            return;

        hud.SetEchoCount(_echoes.Count, maxEchoes);
        hud.SetRecording(_recording, _recording ? RecordingElapsed / Mathf.Max(0.01f, maxRecordSeconds) : 0f);
        hud.SetEchoState(_recording ? "Grabando" : (_echoes.Count > 0 ? "Reproduciendo" : "Listo"));
    }
}
