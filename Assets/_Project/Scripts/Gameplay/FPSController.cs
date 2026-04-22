using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float crouchSpeed = 2f;

    [Header("Crouch")]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [SerializeField] private float gravity = -20f;

    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public float NetworkedYaw { get; set; }
    [Networked] public float NetworkedForward { get; set; }
    [Networked] public float NetworkedStrafe { get; set; }
    [Networked] private bool _isCrouching { get; set; }

    private Vector3 _velocity;
    private CharacterController _cc;
    private FPSCamera _fpsCamera;
    private float _targetHeight;
    private bool _hasLandedOnce;

    public float LastForward { get; private set; }
    public float LastStrafe { get; private set; }
    public bool IsCrouching => _isCrouching;
    public bool IsGrounded  => _cc != null && _cc.isGrounded;

    // IsDead = true khi player chết (set qua RPC_OnDied → TriggerDeath).
    // FixedUpdateNetwork và WeaponController đọc flag này để khóa input.
    public bool IsDead { get; private set; } = false;

    public void InitSpawnPosition(Vector3 pos)
    {
        if (_cc == null) _cc = GetComponent<CharacterController>();
        _cc.enabled = false;
        transform.position = pos;
        _cc.enabled = true;
        NetworkedPosition = pos;
        _velocity = new Vector3(0f, -2f, 0f);
        _hasLandedOnce = false;
    }

    public override void Spawned()
    {
        _cc = GetComponent<CharacterController>();
        _fpsCamera = GetComponentInChildren<FPSCamera>();
        _targetHeight = standHeight;
        _velocity = new Vector3(0f, -2f, 0f);
        _hasLandedOnce = false;

        _fpsCamera?.Initialize(Object.HasInputAuthority);
        _cc.enabled = Object.HasStateAuthority;
        NetworkedPosition = transform.position;

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

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        bool gameEnded = GameManager.instance != null && GameManager.instance.GameEnded;
        bool locked    = gameEnded || IsDead;  // khóa input khi chết hoặc endgame

        NetworkedYaw     = input.Yaw;           // camera xoay vẫn ok (nhìn UI / deathcam)
        NetworkedForward = locked ? 0f : input.MoveDirection.y;
        NetworkedStrafe  = locked ? 0f : input.MoveDirection.x;
        LastForward = NetworkedForward;
        LastStrafe  = NetworkedStrafe;

        if (!Object.HasStateAuthority) return;

        transform.rotation = Quaternion.AngleAxis(NetworkedYaw, Vector3.up);
        HandleCrouch(input);
        if (!locked) HandleMovement(input);
        ApplyGravity();
        NetworkedPosition = transform.position;
    }

    public override void Render()
    {
        if (Object.HasStateAuthority)
        {
            LastForward = NetworkedForward;
            LastStrafe = NetworkedStrafe;
            return;
        }

        transform.position = Vector3.Lerp(transform.position, NetworkedPosition, Time.deltaTime * 30f);

        if (!Object.HasInputAuthority)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.AngleAxis(NetworkedYaw, Vector3.up),
                Time.deltaTime * 25f);
        }

        LastForward = NetworkedForward;
        LastStrafe = NetworkedStrafe;
    }

    void HandleCrouch(NetworkInputData input)
    {
        if (input.Crouch != _isCrouching)
        {
            _isCrouching = input.Crouch;
            _targetHeight = _isCrouching ? crouchHeight : standHeight;
        }
        _cc.height = Mathf.Lerp(_cc.height, _targetHeight, Runner.DeltaTime * crouchTransitionSpeed);
    }

    void HandleMovement(NetworkInputData input)
    {
        bool isSprinting = input.Sprint && !_isCrouching && input.MoveDirection.y > 0;
        float speed = _isCrouching ? crouchSpeed : (isSprinting ? runSpeed : walkSpeed);
        Vector2 dir = input.MoveDirection;
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
        if (_cc.isGrounded && _velocity.y < 0) _velocity.y = -2f;
        _velocity.y += gravity * Runner.DeltaTime;
        _cc.Move(_velocity * Runner.DeltaTime);
    }

    public void TriggerHit()    => GetComponent<PlayerAnimatorController>()?.TriggerHit();
    public void TriggerFire()   => GetComponent<PlayerAnimatorController>()?.TriggerFire();
    public void TriggerReload() => GetComponent<PlayerAnimatorController>()?.TriggerReload();
    public void SetWeaponType(int type) => GetComponent<PlayerAnimatorController>()?.SetWeaponType(type);

    /// Gọi qua RPC_OnDied → chạy trên mọi client. Khóa movement + bật animation chết.
    public void TriggerDeath()
    {
        IsDead = true;
        GetComponent<PlayerAnimatorController>()?.TriggerDeath();
        Debug.Log($"[FPS] TriggerDeath — IsDead=true on {gameObject.name}");
    }

    /// Gọi qua RPC_OnRespawned → chạy trên mọi client. Mở khóa movement + reset animation về Stand.
    public void TriggerRespawn()
    {
        IsDead = false;
        GetComponent<PlayerAnimatorController>()?.TriggerRespawn();
        Debug.Log($"[FPS] TriggerRespawn — IsDead=false on {gameObject.name}");
    }
}