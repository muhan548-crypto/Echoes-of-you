using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    const string AnimatorParamSpeed = "Speed";
    const string AnimatorParamIsGrounded = "IsGrounded";
    const string AnimatorParamIsRecording = "IsRecording";
    const string AnimatorLegacyGrounded = "Grounded";
    const string AnimatorLegacyFalling = "Falling";
    const string AnimatorLegacyJump = "Jump";

    [Header("Movimiento")]
    public float moveSpeed = 8f;
    public float sprintMultiplier = 1.6f;
    public float acceleration = 45f;
    public float deceleration = 50f;
    public float rotationSharpness = 20f;
    [Range(0.1f, 1f)]
    public float airControlFactor = 0.85f;

    [Header("Salto / Gravedad")]
    public float jumpHeight = 3.2f;
    public float gravityStrength = 28f;
    public Vector3 defaultGravityDirection = Vector3.down;
    public float groundedStickForce = 5f;
    public float gravityBlendSpeed = 9f;
    public float fallGravityMultiplier = 2.0f;
    public bool alignToGroundNormal = true;

    [Header("Jump Assist")]
    public float jumpBufferTime = 0.2f;
    public float coyoteTime = 0.2f;

    [Header("Deteccion de suelo")]
    public Transform groundCheck;
    public float groundProbeRadius = 0.24f;
    public float groundProbeDistance = 0.38f;
    public LayerMask groundMask = (1 << 6); // Solo layer Ground — incluir Default causa que el SphereCast detecte al propio jugador

    [Header("Failsafe")]
    public float voidHeight = -15f;

    CharacterController _controller;
    Transform _cam;
    Animator _anim;
    Rigidbody _rb;
    EchoRecorder _echoRecorder;
    Transform _visualRoot;
    Transform _modelRoot;

    readonly List<GravityZone> _gravityZones = new List<GravityZone>();

    Vector3 _planarVelocity;
    Vector3 _verticalVelocity;
    Vector3 _currentGravity;
    Vector3 _targetGravity;
    Vector3 _currentUp = Vector3.up;
    Vector3 _lastFacing = Vector3.forward;
    bool _grounded;
    bool _wasGrounded;
    bool _isDead;
    bool _jumpedThisFrame;
    bool _isFalling;

    public enum PlayerAnimationState
    {
        Idle = 0,
        Run = 1,
        Jump = 2,
        Recording = 3
    }

    // Jump buffer & coyote
    float _jumpBufferTimer;
    float _coyoteTimer;

    public Vector3 UpAxis => _currentUp;
    public Vector3 GravityDirection => _currentGravity.sqrMagnitude > 0.0001f ? _currentGravity.normalized : Vector3.down;
    public Vector3 GravityVector => _currentGravity;
    public bool IsGrounded => _grounded;
    public PlayerAnimationState CurrentAnimationState { get; private set; }

    void Awake()
    {
        gameObject.tag = "Player";
        _controller = GetComponent<CharacterController>();
        TryGetComponent(out _rb);
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }

        EnsureGroundCheck();
        EnsureVisualAnimator();
        EnsureCameraFocus();
        RefreshCameraReference();

        _anim = GetComponentInChildren<Animator>();
        _echoRecorder = GetComponent<EchoRecorder>();
        _targetGravity = SafeGravity(defaultGravityDirection, gravityStrength);
        _currentGravity = _targetGravity;
        _currentUp = -_currentGravity.normalized;

        Vector3 initialForward = Vector3.ProjectOnPlane(transform.forward, _currentUp);
        if (initialForward.sqrMagnitude < 0.001f)
            initialForward = Vector3.ProjectOnPlane(transform.right, _currentUp);
        if (initialForward.sqrMagnitude < 0.001f)
            initialForward = Vector3.Cross(transform.right, _currentUp);

        _lastFacing = initialForward.normalized;
        transform.rotation = Quaternion.LookRotation(_lastFacing, _currentUp);
    }

    void Update()
    {
        if (_isDead)
            return;

        if (_cam == null || (Camera.main != null && _cam != Camera.main.transform))
            RefreshCameraReference();

        if (transform.position.y < voidHeight)
        {
            Die();
            return;
        }

        EnsureGroundCheck();
        UpdateTargetGravity();
        BlendGravity(Time.deltaTime);

        _wasGrounded = _grounded;
        GroundProbe preMoveProbe = ProbeGround(_currentUp);
        _grounded = preMoveProbe.isGrounded || _controller.isGrounded;

        // Coyote time: start timer when leaving ground (not from jumping)
        if (_wasGrounded && !_grounded && !_jumpedThisFrame)
            _coyoteTimer = coyoteTime;
        if (_coyoteTimer > 0f)
            _coyoteTimer -= Time.deltaTime;

        // Jump buffer: count down
        if (_jumpBufferTimer > 0f)
            _jumpBufferTimer -= Time.deltaTime;

        float downwardSpeed = Mathf.Max(0f, Vector3.Dot(_verticalVelocity, GravityDirection));
        if (_grounded && downwardSpeed > 0f)
            _verticalVelocity = GravityDirection * groundedStickForce;

        Vector3 movementUp = ResolveMovementUp(preMoveProbe);
        HandleMovementInput(movementUp);
        HandleJumpInput(movementUp);

        if (!_grounded || _jumpedThisFrame)
        {
            float gravMul = 1f;
            float downSpeed = Vector3.Dot(_verticalVelocity, GravityDirection);
            if (downSpeed > 0f && !_jumpedThisFrame)
                gravMul = fallGravityMultiplier;
            _verticalVelocity += _currentGravity * gravMul * Time.deltaTime;
        }

        _isFalling = !_grounded && Vector3.Dot(_verticalVelocity, GravityDirection) > 0.5f;

        Vector3 motion = (_planarVelocity + _verticalVelocity) * Time.deltaTime;
        _controller.Move(motion);

        GroundProbe postMoveProbe = ProbeGround(movementUp);
        // No re-groundear en el frame del salto o mientras nos movemos hacia arriba (evita cancelar el salto instantáneamente)
        if (!_jumpedThisFrame)
        {
            bool movingUp = Vector3.Dot(_verticalVelocity, GravityDirection) < -0.1f;
            if (!movingUp)
            {
                _grounded = postMoveProbe.isGrounded || _controller.isGrounded;
                if (_grounded)
                    _verticalVelocity = GravityDirection * groundedStickForce;
            }
            else
            {
                _grounded = false;
            }
        }

        if (!_wasGrounded && _grounded)
        {
            GameFeelController.Instance?.PlayLanding(transform.position, movementUp, downwardSpeed);
        }

        UpdateOrientation(postMoveProbe, Time.deltaTime);
        UpdateAnimator();

        _jumpedThisFrame = false;
    }

    public void RegisterGravityZone(GravityZone zone)
    {
        if (zone == null || _gravityZones.Contains(zone))
            return;

        _gravityZones.Add(zone);
    }

    public void UnregisterGravityZone(GravityZone zone)
    {
        if (zone == null)
            return;

        _gravityZones.Remove(zone);
    }

    public void ForceGravity(Vector3 worldDirection, float strength = -1f, bool playFeedback = true)
    {
        Vector3 nextGravity = SafeGravity(worldDirection, strength > 0f ? strength : gravityStrength);
        if (playFeedback && Vector3.Angle(_targetGravity, nextGravity) > 8f)
            GameFeelController.Instance?.PlayGravityShift(transform.position, -nextGravity.normalized);

        _targetGravity = nextGravity;
    }

    public void Teleport(Vector3 worldPosition, Quaternion worldRotation)
    {
        _controller.enabled = false;
        transform.SetPositionAndRotation(worldPosition, worldRotation);
        _planarVelocity = Vector3.zero;
        _verticalVelocity = Vector3.zero;
        _currentUp = transform.up;
        _currentGravity = -_currentUp * gravityStrength;
        _targetGravity = _currentGravity;
        _controller.enabled = true;
    }

    void HandleMovementInput(Vector3 movementUp)
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 cameraForward = Vector3.ProjectOnPlane(transform.forward, movementUp).normalized;
        Vector3 cameraRight = Vector3.Cross(movementUp, cameraForward).normalized;

        if (_cam != null)
        {
            cameraForward = Vector3.ProjectOnPlane(_cam.forward, movementUp).normalized;
            if (cameraForward.sqrMagnitude < 0.001f)
                cameraForward = Vector3.ProjectOnPlane(_cam.up, movementUp).normalized;

            cameraRight = Vector3.ProjectOnPlane(_cam.right, movementUp).normalized;
            if (cameraRight.sqrMagnitude < 0.001f)
                cameraRight = Vector3.Cross(movementUp, cameraForward).normalized;
        }

        // Failsafe: si la camara queda alineada con el eje vertical o sin MainCamera,
        // garantizamos una base ortonormal para no perder completamente el movimiento.
        if (cameraForward.sqrMagnitude < 0.001f)
            cameraForward = Vector3.ProjectOnPlane(_lastFacing, movementUp).normalized;
        if (cameraForward.sqrMagnitude < 0.001f)
            cameraForward = Vector3.ProjectOnPlane(Vector3.forward, movementUp).normalized;
        if (cameraForward.sqrMagnitude < 0.001f)
            cameraForward = Vector3.ProjectOnPlane(Vector3.right, movementUp).normalized;

        if (cameraRight.sqrMagnitude < 0.001f)
            cameraRight = Vector3.Cross(movementUp, cameraForward).normalized;
        if (cameraRight.sqrMagnitude < 0.001f)
            cameraRight = Vector3.ProjectOnPlane(Vector3.right, movementUp).normalized;

        Vector3 desiredDirection = cameraForward * input.z + cameraRight * input.x;
        desiredDirection = Vector3.ProjectOnPlane(desiredDirection, movementUp);
        if (desiredDirection.sqrMagnitude > 1f)
            desiredDirection.Normalize();

        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
        Vector3 desiredVelocity = desiredDirection * speed;

        _planarVelocity = Vector3.ProjectOnPlane(_planarVelocity, movementUp);
        float sharpness = desiredVelocity.sqrMagnitude > 0.001f ? acceleration : deceleration;
        if (!_grounded)
            sharpness *= airControlFactor;
        _planarVelocity = DampVector(_planarVelocity, desiredVelocity, sharpness, Time.deltaTime);
    }

    void HandleJumpInput(Vector3 movementUp)
    {
        // Variable jump height: cut upward velocity if button released
        if (Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.Space))
        {
            if (Vector3.Dot(_verticalVelocity, movementUp) > 0f)
            {
                _verticalVelocity -= movementUp * (Vector3.Dot(_verticalVelocity, movementUp) * 0.5f);
            }
        }

        // Buffer the jump input
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            _jumpBufferTimer = jumpBufferTime;

        // Can jump if: (grounded OR coyote active) AND (buffer active)
        bool canJump = (_grounded || _coyoteTimer > 0f) && _jumpBufferTimer > 0f;

        if (!canJump)
            return;

        // Consume both timers
        _jumpBufferTimer = 0f;
        _coyoteTimer = 0f;

        float jumpSpeed = Mathf.Sqrt(2f * gravityStrength * jumpHeight);
        _verticalVelocity = movementUp * jumpSpeed;
        _grounded = false;
        _jumpedThisFrame = true;

        if (HasAnimatorParameter(AnimatorLegacyJump, AnimatorControllerParameterType.Trigger))
            _anim.SetTrigger(AnimatorLegacyJump);

        GameFeelController.Instance?.PlayJump(transform.position, movementUp);
    }

    void UpdateOrientation(GroundProbe probe, float deltaTime)
    {
        Vector3 desiredUp = -GravityDirection;
        if (alignToGroundNormal && probe.hit.collider != null)
            desiredUp = probe.hit.normal;

        float upBlend = DampingFactor(gravityBlendSpeed, deltaTime);
        _currentUp = Vector3.Slerp(_currentUp, desiredUp.normalized, upBlend).normalized;

        Vector3 facing = Vector3.ProjectOnPlane(_planarVelocity, _currentUp);
        if (facing.sqrMagnitude > 0.001f)
            _lastFacing = facing.normalized;
        else
            _lastFacing = Vector3.ProjectOnPlane(transform.forward, _currentUp).normalized;

        if (_lastFacing.sqrMagnitude < 0.001f)
            _lastFacing = Vector3.Cross(transform.right, _currentUp).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(_lastFacing, _currentUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, DampingFactor(rotationSharpness, deltaTime));
    }

    void UpdateAnimator()
    {
        if (_anim == null || _anim.runtimeAnimatorController == null)
            return;

        Vector3 flatVelocity = Vector3.ProjectOnPlane(_controller.velocity, _currentUp);
        bool isRecording = _echoRecorder != null && _echoRecorder.IsRecording;
        CurrentAnimationState = ResolveAnimationState(flatVelocity.magnitude, isRecording);

        SetAnimatorFloatIfExists(AnimatorParamSpeed, flatVelocity.magnitude);
        
        Vector3 localVelocity = transform.InverseTransformDirection(_controller.velocity);
        SetAnimatorFloatIfExists("VelocityX", localVelocity.x);
        SetAnimatorFloatIfExists("VelocityZ", localVelocity.z);
        
        SetAnimatorBoolIfExists(AnimatorParamIsGrounded, _grounded);
        SetAnimatorBoolIfExists(AnimatorParamIsRecording, isRecording);
        SetAnimatorBoolIfExists(AnimatorLegacyGrounded, _grounded);
        SetAnimatorBoolIfExists(AnimatorLegacyFalling, _isFalling);

        if (HasAnimatorParameter("State", AnimatorControllerParameterType.Int))
            _anim.SetInteger("State", (int)CurrentAnimationState);
    }

    void UpdateTargetGravity()
    {
        GravityZone strongestZone = null;
        int highestPriority = int.MinValue;

        for (int i = _gravityZones.Count - 1; i >= 0; i--)
        {
            GravityZone zone = _gravityZones[i];
            if (zone == null)
            {
                _gravityZones.RemoveAt(i);
                continue;
            }

            if (zone.Priority >= highestPriority)
            {
                highestPriority = zone.Priority;
                strongestZone = zone;
            }
        }

        Vector3 nextGravity = strongestZone != null
            ? strongestZone.GetGravityVector()
            : SafeGravity(defaultGravityDirection, gravityStrength);

        if (Vector3.Angle(_targetGravity, nextGravity) > 8f || Mathf.Abs(_targetGravity.magnitude - nextGravity.magnitude) > 0.5f)
            GameFeelController.Instance?.PlayGravityShift(transform.position, -nextGravity.normalized);

        _targetGravity = nextGravity;
    }

    void BlendGravity(float deltaTime)
    {
        Vector3 currentDirection = _currentGravity.sqrMagnitude > 0.0001f ? _currentGravity.normalized : Vector3.down;
        Vector3 targetDirection = _targetGravity.normalized;
        float blend = DampingFactor(gravityBlendSpeed, deltaTime);
        Vector3 blendedDirection = Vector3.Slerp(currentDirection, targetDirection, blend).normalized;
        float blendedStrength = Mathf.Lerp(_currentGravity.magnitude, _targetGravity.magnitude, blend);
        _currentGravity = blendedDirection * blendedStrength;
    }

    Vector3 ResolveMovementUp(GroundProbe probe)
    {
        if (alignToGroundNormal && probe.hit.collider != null)
            return probe.hit.normal;

        return _currentUp;
    }

    GroundProbe ProbeGround(Vector3 probeUp)
    {
        Vector3 normalizedUp = probeUp.sqrMagnitude > 0.001f ? probeUp.normalized : transform.up;
        Vector3 origin = groundCheck != null
            ? groundCheck.position + normalizedUp * groundProbeRadius
            : transform.position + normalizedUp * groundProbeRadius;

        RaycastHit hit;
        bool grounded = Physics.SphereCast(
            origin,
            groundProbeRadius,
            -normalizedUp,
            out hit,
            groundProbeDistance + groundProbeRadius,
            groundMask,
            QueryTriggerInteraction.Ignore);

        if (grounded)
            grounded = hit.distance <= groundProbeDistance + 0.02f;

        // Excluir colisiones con el propio jugador (failsafe si groundMask incluye Default)
        if (grounded && hit.collider != null && hit.collider.transform.IsChildOf(transform))
            grounded = false;

        return new GroundProbe
        {
            isGrounded = grounded,
            hit = hit
        };
    }

    void EnsureGroundCheck()
    {
        if (groundCheck == null)
        {
            var gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform, false);
            groundCheck = gc.transform;
        }

        float localYOffset = -((_controller.height * 0.5f) - _controller.radius + 0.02f);
        groundCheck.localPosition = new Vector3(0f, localYOffset, 0f);
        groundCheck.localRotation = Quaternion.identity;
    }

    void RefreshCameraReference()
    {
        if (Camera.main != null)
            _cam = Camera.main.transform;
    }

    void EnsureVisualAnimator()
    {
        Transform visualRoot = transform.Find("PlayerVisual");
        if (visualRoot == null)
        {
            GameObject root = new GameObject("PlayerVisual");
            root.transform.SetParent(transform, false);
            visualRoot = root.transform;
        }

        _visualRoot = visualRoot;
        _visualRoot.localPosition = Vector3.zero;
        _visualRoot.localRotation = Quaternion.identity;
        _visualRoot.localScale = Vector3.one;

        if (visualRoot.childCount == 0)
            CreateFallbackVisual(visualRoot);

        _modelRoot = visualRoot.GetChild(0);

        Animator visualAnimator = _modelRoot.GetComponent<Animator>();
        if (visualAnimator == null)
            visualAnimator = _modelRoot.GetComponentInChildren<Animator>(true);
        if (visualAnimator != null)
        {
            visualAnimator.applyRootMotion = false;
            visualAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            visualAnimator.updateMode = AnimatorUpdateMode.Normal;
        }

        SkinnedMeshRenderer[] renderers = _modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].updateWhenOffscreen = true;

        _anim = visualAnimator;
    }

    void CreateFallbackVisual(Transform visualRoot)
    {
        _anim = GetComponentInChildren<Animator>();

        // Fallback capsule if no model found
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "FallbackCapsule";
        capsule.transform.SetParent(visualRoot, false);
        capsule.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        capsule.transform.localRotation = Quaternion.identity;
        capsule.transform.localScale = new Vector3(0.8f, 1.05f, 0.8f);

        Collider capsuleCollider = capsule.GetComponent<Collider>();
        if (capsuleCollider != null)
            Destroy(capsuleCollider);

        Renderer rendererRef = capsule.GetComponent<Renderer>();
        if (rendererRef != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.87f, 0.91f, 0.96f, 1f);
            rendererRef.sharedMaterial = material;
        }
    }

    void Die()
    {
        _isDead = true;
        _controller.enabled = false;
        
        if (LevelRuntimeController.Instance != null)
        {
            LevelRuntimeController.Instance.HandlePlayerDeath(transform.position, 1.2f);
        }
        else
        {
            StartCoroutine(FallbackRestart());
        }
    }

    System.Collections.IEnumerator FallbackRestart()
    {
        yield return new WaitForSecondsRealtime(1.2f);
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }



    static Vector3 SafeGravity(Vector3 direction, float strength)
    {
        Vector3 fallback = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.down;
        return fallback * Mathf.Max(0.01f, strength);
    }

    static float DampingFactor(float sharpness, float deltaTime)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0f, sharpness) * deltaTime);
    }

    void EnsureCameraFocus()
    {
        Transform focus = transform.Find("CameraFocus");
        if (focus == null)
        {
            GameObject focusObject = new GameObject("CameraFocus");
            focusObject.transform.SetParent(transform, false);
            focus = focusObject.transform;
        }

        float focusHeight = Mathf.Clamp(_controller.height * 0.68f, 1.05f, 1.3f);
        focus.localPosition = new Vector3(0f, focusHeight, 0.08f);
        focus.localRotation = Quaternion.identity;
        focus.localScale = Vector3.one;
    }

    void AlignModelToFeet()
    {
        if (_modelRoot == null || !TryGetModelBounds(out Bounds bounds))
            return;

        Vector3 up = transform.up.sqrMagnitude > 0.001f ? transform.up.normalized : Vector3.up;
        Vector3 lateralOffset = Vector3.ProjectOnPlane(bounds.center - transform.position, up);
        _modelRoot.position -= lateralOffset;

        if (!TryGetModelBounds(out bounds))
            return;

        Vector3 currentFeet = bounds.center - up * bounds.extents.y;
        float feetDelta = Vector3.Dot(transform.position - currentFeet, up);
        _modelRoot.position += up * feetDelta;
    }

    bool TryGetModelBounds(out Bounds bounds)
    {
        bounds = default;
        if (_modelRoot == null)
            return false;

        Renderer[] renderers = _modelRoot.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererRef = renderers[i];
            if (rendererRef == null)
                continue;

            if (!found)
            {
                bounds = rendererRef.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(rendererRef.bounds);
            }
        }

        return found;
    }

    PlayerAnimationState ResolveAnimationState(float speed, bool isRecording)
    {
        if (isRecording)
            return PlayerAnimationState.Recording;
        if (!_grounded || _jumpedThisFrame || _isFalling)
            return PlayerAnimationState.Jump;
        if (speed > 0.15f)
            return PlayerAnimationState.Run;
        return PlayerAnimationState.Idle;
    }

    bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (_anim == null)
            return false;

        AnimatorControllerParameter[] parameters = _anim.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == parameterType && parameters[i].name == parameterName)
                return true;
        }

        return false;
    }

    void SetAnimatorBoolIfExists(string parameterName, bool value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            _anim.SetBool(parameterName, value);
    }

    void SetAnimatorFloatIfExists(string parameterName, float value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
            _anim.SetFloat(parameterName, value);
    }

    static Vector3 DampVector(Vector3 current, Vector3 target, float sharpness, float deltaTime)
    {
        return Vector3.Lerp(current, target, DampingFactor(sharpness, deltaTime));
    }

    struct GroundProbe
    {
        public bool isGrounded;
        public RaycastHit hit;
    }
}
