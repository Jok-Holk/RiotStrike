using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    // Không cần SerializeField — tự tìm trong Start() để tránh missing link
    private WeaponAudio   _weaponAudio;
    private Animator      _animator;
    private FPSController _fpsController;
    private bool          _ready;

    void Start()
    {
        _animator      = GetComponentInChildren<Animator>();
        _fpsController = GetComponent<FPSController>();

        // Auto-find WeaponAudio — fix lỗi "missing link" mà team báo cáo
        // WeaponAudio phải gắn trên cùng GameObject với PlayerAnimatorController
        _weaponAudio = GetComponent<WeaponAudio>();
        if (_weaponAudio == null)
            _weaponAudio = GetComponentInChildren<WeaponAudio>();
        if (_weaponAudio == null)
            Debug.LogWarning($"[PlayerAnimatorController] WeaponAudio not found on {gameObject.name}. Audio sẽ không hoạt động.");
    }

    void Update()
    {
        if (!_ready)
        {
            if (_fpsController == null || _fpsController.Object == null || !_fpsController.Object.IsValid)
                return;
            _ready = true;
        }

        if (_animator == null) return;

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

    public void SetWeaponType(int type) => _animator?.SetInteger("WeaponType", type);
}
