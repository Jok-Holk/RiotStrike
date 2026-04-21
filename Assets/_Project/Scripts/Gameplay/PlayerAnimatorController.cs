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

        // Chỉ set WeaponType khi giá trị thay đổi.
        // Gọi SetInteger mỗi frame với cùng giá trị vẫn trigger transition evaluation trong Unity Animator,
        // gây ra "Any State → RifleIdle" tự loop lại (nếu Can Transition To Self = true) → jitter.
        // Guard này ngăn transition thừa. Nếu jitter vẫn xảy ra → trong Animator Controller Unity,
        // kiểm tra các transition từ Any State: tắt "Can Transition To Self" và "Has Exit Time".
        if (_appliedWeaponType != _targetWeaponType)
        {
            _animator.SetInteger("WeaponType", _targetWeaponType);
            _appliedWeaponType = _targetWeaponType;
        }

        float forward = Mathf.Abs(_fpsController.LastForward) < 0.001f ? 0f : _fpsController.LastForward;
        float strafe  = Mathf.Abs(_fpsController.LastStrafe)  < 0.001f ? 0f : _fpsController.LastStrafe;

        _animator.SetFloat("Forward", forward, 0.1f, Time.deltaTime);
        _animator.SetFloat("Strafe",  strafe,  0.1f, Time.deltaTime);
        _animator.SetBool("Crouch",   _fpsController.IsCrouching);
    }

    public void TriggerHit()   => _animator?.SetTrigger("Hit");
    public void TriggerDeath() => _animator?.SetTrigger("Death");

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
        _appliedWeaponType = -1; // force re-apply next Update()
        _animator?.SetInteger("WeaponType", type);
    }
}
