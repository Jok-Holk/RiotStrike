using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed   = 4f;
    [SerializeField] private float runSpeed    = 7f;
    [SerializeField] private float crouchSpeed = 2f;

    [Header("Crouch")]
    [SerializeField] private float standHeight           = 2f;
    [SerializeField] private float crouchHeight          = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [SerializeField] private float gravity = -20f;

    [Networked] public float NetworkedYaw     { get; set; }
    [Networked] public float NetworkedForward { get; set; }
    [Networked] public float NetworkedStrafe  { get; set; }
    [Networked] private bool _isCrouching    { get; set; }

    private Vector3             _velocity;
    private CharacterController _cc;
    private FPSCamera           _fpsCamera;
    private float               _targetHeight;
    private bool                _hasLandedOnce;

    // Local cache — dùng cho FootstepAudio và animator local player
    public float LastForward { get; private set; }
    public float LastStrafe  { get; private set; }
    public bool  IsCrouching => _isCrouching;

    public void InitSpawnPosition(Vector3 pos)
    {
        if (_cc == null) _cc = GetComponent<CharacterController>();
        _cc.enabled        = false;
        transform.position = pos;
        _cc.enabled        = true;
        _velocity          = new Vector3(0f, -2f, 0f);
        _hasLandedOnce     = false;
    }

    public override void Spawned()
    {
        _cc           = GetComponent<CharacterController>();
        _fpsCamera    = GetComponentInChildren<FPSCamera>();
        _targetHeight = standHeight;
        _velocity     = new Vector3(0f, -2f, 0f);
        _hasLandedOnce = false;

        _fpsCamera?.Initialize(Object.HasInputAuthority);

        // CC cần enable trên cả InputAuthority (client tự chạy physics local)
        // và StateAuthority (host chạy physics authoritative)
        _cc.enabled = Object.HasInputAuthority || Object.HasStateAuthority;

        if (Object.HasInputAuthority)
        {
            var ip = FindFirstObjectByType<InputProvider>();
            if (ip != null) ip.SetCamera(_fpsCamera);

            foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        if (FindFirstObjectByType<PlayerDebugLogger>() == null)
            new GameObject("PlayerDebugLogger").AddComponent<PlayerDebugLogger>();
        PlayerDebugLogger.Register(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        PlayerDebugLogger.Unregister(this);
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        NetworkedYaw = input.Yaw;

        // Cả InputAuthority lẫn StateAuthority đều chạy movement
        // Fusion reconcile phía host, client chạy prediction
        transform.rotation = Quaternion.Euler(0f, NetworkedYaw, 0f);
        HandleCrouch(input);
        HandleMovement(input);
        ApplyGravity();

        // Sync animation state qua network để remote player thấy đúng
        NetworkedForward = input.MoveDirection.y;
        NetworkedStrafe  = input.MoveDirection.x;

        // Cache local để FootstepAudio dùng
        LastForward = input.MoveDirection.y;
        LastStrafe  = input.MoveDirection.x;
    }

    public override void Render()
    {
        if (!Object.HasInputAuthority)
        {
            // Remote player: lerp rotation + cập nhật LastForward/Strafe từ networked
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.Euler(0f, NetworkedYaw, 0f),
                Time.deltaTime * 25f);

            LastForward = NetworkedForward;
            LastStrafe  = NetworkedStrafe;
        }
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
        if (!_hasLandedOnce)
        {
            _velocity.y = -2f;
            _cc.Move(_velocity * Runner.DeltaTime);
            if (_cc.isGrounded) _hasLandedOnce = true;
            return;
        }
        if (_cc.isGrounded && _velocity.y < 0)
            _velocity.y = -2f;
        _velocity.y += gravity * Runner.DeltaTime;
        _cc.Move(_velocity * Runner.DeltaTime);
    }

    public void TriggerHit()    => GetComponent<PlayerAnimatorController>()?.TriggerHit();
    public void TriggerDeath()  => GetComponent<PlayerAnimatorController>()?.TriggerDeath();
    public void TriggerFire()   => GetComponent<PlayerAnimatorController>()?.TriggerFire();
    public void TriggerReload() => GetComponent<PlayerAnimatorController>()?.TriggerReload();
    public void SetWeaponType(int type) => GetComponent<PlayerAnimatorController>()?.SetWeaponType(type);
}
