using System;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FixedPuzzleCameraController : MonoBehaviour
{
    [Header("Scene References")]
    public CinemachineVirtualCamera virtualCamera;
    public CinemachineTargetGroup targetGroup;
    public Transform followTarget;
    public Transform playerFocus;
    public Transform goalFocus;
    public Transform eventFocus;

    [Header("Weights")]
    public float playerWeight = 1.35f;
    public float goalWeight = 0.52f;
    public float eventBlendSharpness = 10f;

    [Header("Lens")]
    public float baseFov = 46f;
    public float fovDamping = 8f;

    float _currentEventWeight;
    float _requestedEventWeight;
    float _eventFocusUntil;
    float _pulseTargetFov;
    float _pulseUntil;
    Vector3 _eventWorldPoint;

    public static FixedPuzzleCameraController ResolveActive()
    {
        Camera main = Camera.main;
        if (main != null && main.TryGetComponent(out FixedPuzzleCameraController controller))
            return controller;

        return FindAnyObjectByType<FixedPuzzleCameraController>();
    }

    void Awake()
    {
        CacheSceneReferences();

        if (virtualCamera != null)
        {
            var lens = virtualCamera.m_Lens;
            if (baseFov <= 0f)
                baseFov = lens.FieldOfView;

            lens.FieldOfView = baseFov;
            virtualCamera.m_Lens = lens;
        }

        _pulseTargetFov = baseFov;
    }

    void LateUpdate()
    {
        CacheSceneReferences();
        if (virtualCamera == null || targetGroup == null)
            return;

        if (followTarget != null && virtualCamera.Follow != followTarget)
            virtualCamera.Follow = followTarget;
        if (virtualCamera.LookAt != targetGroup.transform)
            virtualCamera.LookAt = targetGroup.transform;

        if (eventFocus != null)
            eventFocus.position = _eventWorldPoint;

        float distanceFactor = 0f;
        if (followTarget != null && goalFocus != null)
            distanceFactor = Mathf.Clamp01(Vector3.Distance(followTarget.position, goalFocus.position) / 18f);

        float desiredGoalWeight = goalFocus != null
            ? Mathf.Lerp(goalWeight * 0.8f, goalWeight * 1.4f, distanceFactor)
            : 0f;
        float desiredEventWeight = Time.unscaledTime < _eventFocusUntil ? _requestedEventWeight : 0f;
        _currentEventWeight = Mathf.Lerp(
            _currentEventWeight,
            desiredEventWeight,
            DampingFactor(eventBlendSharpness, Time.unscaledDeltaTime));

        SetMemberWeight(playerFocus, playerWeight, 0.6f);
        SetMemberWeight(goalFocus, desiredGoalWeight, 1.4f);
        SetMemberWeight(eventFocus, _currentEventWeight, 1.8f);

        float desiredFov = Time.unscaledTime < _pulseUntil ? _pulseTargetFov : baseFov;
        var lens = virtualCamera.m_Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, desiredFov, DampingFactor(fovDamping, Time.unscaledDeltaTime));
        virtualCamera.m_Lens = lens;
    }

    public void RequestEventFocus(Vector3 worldPoint, float weight = 0.35f, float holdSeconds = 0.45f, float pulseFov = 50f)
    {
        _eventWorldPoint = worldPoint;
        _requestedEventWeight = Mathf.Clamp(weight, 0.15f, 1f);
        _eventFocusUntil = Time.unscaledTime + Mathf.Max(0.05f, holdSeconds);
        RequestFovPulse(pulseFov, holdSeconds);
    }

    public void RequestFovPulse(float temporaryFov, float holdSeconds = 0.25f)
    {
        _pulseTargetFov = Mathf.Max(38f, temporaryFov);
        _pulseUntil = Time.unscaledTime + Mathf.Max(0.05f, holdSeconds);
    }

    void CacheSceneReferences()
    {
        if (virtualCamera == null)
            virtualCamera = FindAnyObjectByType<CinemachineVirtualCamera>();

        if (targetGroup == null)
            targetGroup = FindAnyObjectByType<CinemachineTargetGroup>();

        if (followTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                followTarget = player.transform;
        }

        if (playerFocus == null && followTarget != null)
            playerFocus = followTarget.Find("CameraFocus");

        if (goalFocus == null)
        {
            LevelExit exitRef = FindAnyObjectByType<LevelExit>();
            if (exitRef != null)
            {
                Transform goalMarker = exitRef.transform.parent != null
                    ? exitRef.transform.parent.Find("GoalFocus")
                    : null;
                goalFocus = goalMarker != null ? goalMarker : exitRef.transform;
            }
        }

        if (eventFocus == null)
        {
            GameObject focusObject = new GameObject("CameraEventFocus");
            if (targetGroup != null)
                focusObject.transform.SetParent(targetGroup.transform, false);
            else
                focusObject.transform.SetParent(transform, false);

            eventFocus = focusObject.transform;
            eventFocus.position = goalFocus != null ? goalFocus.position : transform.position;
            _eventWorldPoint = eventFocus.position;
        }
    }

    void SetMemberWeight(Transform member, float weight, float radius)
    {
        if (targetGroup == null || member == null)
            return;

        int index = EnsureMember(member, radius);
        if (index < 0)
            return;

        CinemachineTargetGroup.Target[] members = targetGroup.m_Targets;
        CinemachineTargetGroup.Target entry = members[index];
        entry.target = member;
        entry.radius = radius;
        entry.weight = Mathf.Max(0f, weight);
        members[index] = entry;
        targetGroup.m_Targets = members;
    }

    int EnsureMember(Transform member, float radius)
    {
        if (targetGroup == null || member == null)
            return -1;

        CinemachineTargetGroup.Target[] members = targetGroup.m_Targets ?? Array.Empty<CinemachineTargetGroup.Target>();
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i].target == member)
                return i;
        }

        Array.Resize(ref members, members.Length + 1);
        members[members.Length - 1] = new CinemachineTargetGroup.Target
        {
            target = member,
            weight = 0f,
            radius = radius
        };
        targetGroup.m_Targets = members;
        return members.Length - 1;
    }

    static float DampingFactor(float sharpness, float deltaTime)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0f, sharpness) * deltaTime);
    }
}
