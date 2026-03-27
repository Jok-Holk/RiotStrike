using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed   = 4f;
    [SerializeField] private float runSpeed    = 7f;
    [SerializeField] private float crouchSpeed = 2f;
    // Jump đã bỏ

    [Header("Crouch")]
    [SerializeField] private float standHeight         = 2f;
    [SerializeField] private float crouchHeight        = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [SerializeField] private float gravity = -20f;

    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public float   NetworkedYaw      { get; set; }
    [Networked] private bool   _isCrouching      { get; set; }

    private Vector3   _velocity;
    private CharacterController _cc;
    private FPSCamera _fpsCamera;
    private float     _targetHeight;
    private bool      _warped;

    public float LastForward  { get; private set; }
    public float LastStrafe   { get; private set; }
    public bool  IsCrouching  => _isCrouching;

    public void InitSpawnPosition(Vector3 pos)
    {
        if (_cc == null) _cc = GetComponent<CharacterController>();
        _cc.enabled = false;
        transform.position = pos;
        _cc.enabled = true;
        NetworkedPosition = pos;
        NetworkedYaw      = 0f;
        _velocity = new Vector3(0f, -2f, 0f);
    }

    public override void Spawned()
    {
        _cc        = GetComponent<CharacterController>();
        _fpsCamera = GetComponentInChildren<FPSCamera>();
        _targetHeight = standHeight;
        _warped    = false;

        if (_velocity == Vector3.zero)
            _velocity = new Vector3(0f, -2f, 0f);

        _fpsCamera?.Initialize(Object.HasInputAuthority);

        if (Object.HasStateAuthority)
        {
            NetworkedPosition = transform.position;
            NetworkedYaw      = transform.eulerAngles.y;
        }

        _cc.enabled = Object.HasInputAuthority;

        if (Object.HasInputAuthority)
        {
            var ip = FindFirstObjectByType<InputProvider>();
            if (ip != null) ip.SetCamera(_fpsCamera);
        }

        if (FindFirstObjectByType<PlayerDebugLogger>() == null)
            new GameObject("PlayerDebugLogger").AddComponent<PlayerDebugLogger>();
        PlayerDebugLogger.Register(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        PlayerDebugLogger.Unregister(this);
    }

    void Update()
    {
        if (!_warped && Object.HasInputAuthority && !Object.HasStateAuthority)
        {
            if (NetworkedPosition != Vector3.zero)
            {
                _cc.enabled = false;
                transform.position = NetworkedPosition;
                _cc.enabled = true;
                _velocity = new Vector3(0f, -2f, 0f);
                _warped   = true;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority) return;
        if (!GetInput(out NetworkInputData input)) return;

        NetworkedYaw       = input.Yaw;
        transform.rotation = Quaternion.Euler(0f, NetworkedYaw, 0f);

        if (Runner.IsForward)
        {
            HandleCrouch(input);
            HandleMovement(input);
            // Jump đã bỏ
            _fpsCamera?.ApplyInput(input, Runner.DeltaTime);
        }

        ApplyGravity();

        LastForward = input.MoveDirection.y;
        LastStrafe  = input.MoveDirection.x;

        if (Object.HasStateAuthority)
            NetworkedPosition = transform.position;
        else if (Runner.IsForward)
            RPC_SyncTransform(transform.position, input.Yaw);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SyncTransform(Vector3 pos, float yaw)
    {
        NetworkedPosition = pos;
        NetworkedYaw      = yaw;
    }

    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            _fpsCamera?.ApplyVisual();
            return;
        }

        if (NetworkedPosition == Vector3.zero) return;

        float dist = Vector3.Distance(transform.position, NetworkedPosition);
        transform.position = dist > 3f
            ? NetworkedPosition
            : Vector3.Lerp(transform.position, NetworkedPosition, Time.deltaTime * 25f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(0f, NetworkedYaw, 0f),
            Time.deltaTime * 25f);
    }

    void HandleCrouch(NetworkInputData input)
    {
        if (input.Crouch != _isCrouching)
        {
            _isCrouching  = input.Crouch;
            _targetHeight = _isCrouching ? crouchHeight : standHeight;
        }
        _cc.height = Mathf.Lerp(_cc.height, _targetHeight, Runner.DeltaTime * crouchTransitionSpeed);
    }

    void HandleMovement(NetworkInputData input)
    {
        bool isSprinting = input.Sprint && !_isCrouching && input.MoveDirection.y > 0;
        float speed      = _isCrouching ? crouchSpeed : (isSprinting ? runSpeed : walkSpeed);
        Vector2 dir      = input.MoveDirection;
        if (dir.magnitude > 1f) dir.Normalize();
        Vector3 move = transform.right * dir.x + transform.forward * dir.y;
        _cc.Move(move * speed * Runner.DeltaTime);
    }

    void ApplyGravity()
    {
        if (_cc.isGrounded && _velocity.y < 0)
            _velocity.y = -2f;
        _velocity.y += gravity * Runner.DeltaTime;
        _cc.Move(_velocity * Runner.DeltaTime);
    }

    // ─── Animation triggers ───────────────────────────────────────────────────
    public void TriggerHit()    => GetComponent<PlayerAnimatorController>()?.TriggerHit();
    public void TriggerDeath()  => GetComponent<PlayerAnimatorController>()?.TriggerDeath();
    public void TriggerFire()   => GetComponent<PlayerAnimatorController>()?.TriggerFire();
    public void TriggerReload() => GetComponent<PlayerAnimatorController>()?.TriggerReload();
    public void SetWeaponType(int type) => GetComponent<PlayerAnimatorController>()?.SetWeaponType(type);
}