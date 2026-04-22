using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    private WeaponAudio   _weaponAudio;
    private Animator      _animator;
    private FPSController _fpsController;
    private bool          _ready;

    // WeaponType tracking: lưu giá trị mong muốn, áp dụng lại mỗi frame nếu chưa sync
    // Mặc định = 1 (Pistol) để tránh Rifle idle khi khởi động
    private int _targetWeaponType  = 1;
    private int _appliedWeaponType = -1; // -1 = chưa áp dụng

    void Awake()
    {
        // Khởi tạo sớm trong Awake để kịp trước khi Fusion gọi Spawned()
        _animator      = GetComponentInChildren<Animator>();
        _fpsController = GetComponent<FPSController>();

        _weaponAudio = GetComponent<WeaponAudio>();
        if (_weaponAudio == null)
            _weaponAudio = GetComponentInChildren<WeaponAudio>();
        if (_weaponAudio == null)
            Debug.LogWarning($"[PlayerAnimatorController] WeaponAudio not found on {gameObject.name}.");
    }

    void Update()
    {
        if (!_ready)
        {
            if (_fpsController == null || _fpsController.Object == null || !_fpsController.Object.IsValid)
                return;
            _ready = true;
        }

        // Lazy-init nếu Animator chưa sẵn sàng trong Awake
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
            return;
        }

        // Animation jitter fix:
        // Vấn đề: "Any State → RifleIdle/PistolIdle" trong Animator Controller được Unity
        // đánh giá LẠI mỗi frame. Nếu condition WeaponType==0 và ta đang ở RifleIdle,
        // và "Can Transition To Self = true" (Unity mặc định), nó restart animation mỗi frame → jitter.
        //
        // Fix: Set WeaponType = -1 (không khớp bất kỳ condition nào) rồi dùng CrossFade để
        // đặt thẳng vào state đúng. Any State transitions không còn condition thỏa mãn → không fire.
        // Trigger-based transitions (Fire, Death, Hit, Reload) vẫn hoạt động bình thường.
        if (_appliedWeaponType != _targetWeaponType)
        {
            _appliedWeaponType = _targetWeaponType;
            _animator.SetInteger("WeaponType", -1);           // tắt Any State conditions
            string idle = _targetWeaponType == 0 ? "RifleIdle" : "PistolIdle";
            _animator.CrossFade(idle, 0.1f);                  // force vào đúng state, không qua Any State
        }

        float forward = Mathf.Abs(_fpsController.LastForward) < 0.001f ? 0f : _fpsController.LastForward;
        float strafe  = Mathf.Abs(_fpsController.LastStrafe)  < 0.001f ? 0f : _fpsController.LastStrafe;

        _animator.SetFloat("Forward", forward, 0.1f, Time.deltaTime);
        _animator.SetFloat("Strafe",  strafe,  0.1f, Time.deltaTime);
        _animator.SetBool("Crouch",   _fpsController.IsCrouching);
    }

    public void TriggerHit()   => _animator?.SetTrigger("Hit");
    public void TriggerDeath() => _animator?.SetTrigger("Death");

    public void TriggerRespawn()
    {
        if (_animator == null) return;
        _animator.ResetTrigger("Death");
        // CrossFade về "Stand" trên Base Layer (layer 0) — thoát khỏi Death state
        _animator.CrossFade("Stand", 0.15f, 0);
        // Force weapon idle update trên WeaponLayer ở frame tiếp theo
        _appliedWeaponType = -1;
    }

    public void TriggerFire()
    {
        _animator?.SetTrigger("Fire");
        _weaponAudio?.PlayFireSound();
    }

    public void TriggerReload()
    {
        _animator?.SetTrigger("Reload");
        _weaponAudio?.PlayReloadSound();
    }

    public void SetWeaponType(int type)
    {
        _targetWeaponType  = type;
        _appliedWeaponType = -1; // force CrossFade trong Update() frame tiếp theo
        // Không gọi SetInteger ở đây — Update() sẽ set -1 + CrossFade để tránh jitter
    }
}
