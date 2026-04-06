using UnityEngine;
using Fusion;
using RiotStrike.Data;

public class WeaponController : NetworkBehaviour
{
    [Header("Weapon Data")]
    [SerializeField] private WeaponData ak47Data;
    [SerializeField] private WeaponData m4a1Data;
    [SerializeField] private WeaponData pistolData;

    [Header("Fire Points")]
    [SerializeField] private Transform ak47FirePoint;
    [SerializeField] private Transform m4a1FirePoint;
    [SerializeField] private Transform pistolFirePoint;

    [Header("Weapon Models")]
    [SerializeField] private GameObject ak47Model;
    [SerializeField] private GameObject m4a1Model;
    [SerializeField] private GameObject pistolModel;

    [Header("References")]
    [SerializeField] private GameObject muzzleFlashVFX;
    [SerializeField] private Camera     fpsCamera;

    [Networked] public int        CurrentAmmo     { get; set; }
    [Networked] public int        ReserveAmmo     { get; set; }
    [Networked] public bool       IsRifleUnlocked { get; set; }
    [Networked] private TickTimer _reloadTimer    { get; set; }
    [Networked] private TickTimer _fireTimer      { get; set; }
    [Networked] private int       _currentSlot    { get; set; }
    [Networked] public int        TeamID          { get; set; }

    private WeaponData    _currentWeapon;
    private Transform     _currentFirePoint;
    private FPSController _fpsController;
    private bool          _equipped   = false;
    private int           _lastTeamID = -1;
    private bool          _lastRifleUnlocked = false;

    public override void Spawned()
    {
        _fpsController = GetComponent<FPSController>();
        HideAllModels();
        _fpsController?.SetWeaponType(1);

        if (Object.HasStateAuthority)
        {
            var np = GetComponent<NetworkPlayer>();
            TeamID = np != null ? np.Team : 0;
            IsRifleUnlocked = false;
        }
    }

    public override void Render()
    {
        if (!Object.HasInputAuthority) return;

        // Equip lần đầu khi TeamID sync về
        if (!_equipped && TeamID != _lastTeamID)
        {
            _lastTeamID = TeamID;
            _equipped   = true;
            EquipPistol();
        }

        // Auto switch sang rifle khi phase unlock
        if (IsRifleUnlocked && !_lastRifleUnlocked)
        {
            _lastRifleUnlocked = true;
            EquipRifle();
            Debug.Log("[WeaponController] Auto switched to Rifle!");
        }
    }

    public void EquipPistol()
    {
        _currentWeapon    = pistolData;
        _currentFirePoint = pistolFirePoint;
        _currentSlot      = 0;

        if (Object.HasStateAuthority)
        {
            CurrentAmmo = pistolData != null ? pistolData.magazineSize : 12;
            ReserveAmmo = pistolData != null ? pistolData.reserveAmmo  : 36;
        }

        HideAllModels();
        if (pistolModel) pistolModel.SetActive(true);
        _fpsController?.SetWeaponType(1);
    }

    public void EquipRifle()
    {
        if (!IsRifleUnlocked) return; // guard — không equip nếu chưa unlock

        bool isAK         = TeamID == 0;
        _currentWeapon    = isAK ? ak47Data     : m4a1Data;
        _currentFirePoint = isAK ? ak47FirePoint : m4a1FirePoint;
        _currentSlot      = 1;

        if (Object.HasStateAuthority)
        {
            CurrentAmmo = _currentWeapon != null ? _currentWeapon.magazineSize : 30;
            ReserveAmmo = _currentWeapon != null ? _currentWeapon.reserveAmmo  : 90;
        }

        HideAllModels();
        if (isAK) { if (ak47Model) ak47Model.SetActive(true); }
        else      { if (m4a1Model) m4a1Model.SetActive(true); }
        _fpsController?.SetWeaponType(0);
    }

    void HideAllModels()
    {
        if (ak47Model)   ak47Model.SetActive(false);
        if (m4a1Model)   m4a1Model.SetActive(false);
        if (pistolModel) pistolModel.SetActive(false);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority) return;
        if (!GetInput(out NetworkInputData input)) return;
        if (_currentWeapon == null) return;

        // Switch weapon — chỉ cho phép nếu rifle đã unlock
        if (input.SwitchToRifle && IsRifleUnlocked && _currentSlot != 1)
            EquipRifle();
        if (input.SwitchToPistol && _currentSlot != 0)
            EquipPistol();

        // Reload complete
        if (_reloadTimer.Expired(Runner))
        {
            _reloadTimer = default;
            int needed  = _currentWeapon.magazineSize - CurrentAmmo;
            int take    = Mathf.Min(needed, ReserveAmmo);
            CurrentAmmo += take;
            ReserveAmmo -= take;
        }

        bool isReloading = !_reloadTimer.ExpiredOrNotRunning(Runner);

        // Fire
        // Lock bắn khi trong safe zone hoặc chưa bắt đầu game
        bool canFire = SafeZoneManager.instance == null || SafeZoneManager.instance.CanFire();

        if (canFire && input.Fire && _fireTimer.ExpiredOrNotRunning(Runner)
            && !isReloading && CurrentAmmo > 0)
        {
            Fire();
            _fireTimer = TickTimer.CreateFromSeconds(Runner, 1f / _currentWeapon.fireRate);
        }

        // Reload
        if (input.Reload && _reloadTimer.ExpiredOrNotRunning(Runner)
            && CurrentAmmo < _currentWeapon.magazineSize && ReserveAmmo > 0)
        {
            _reloadTimer = TickTimer.CreateFromSeconds(Runner, _currentWeapon.reloadTime);
            _fpsController?.TriggerReload();
        }
    }

    void Fire()
    {
        CurrentAmmo--;
        _fpsController?.TriggerFire();

        if (muzzleFlashVFX != null && _currentFirePoint != null)
        {
            muzzleFlashVFX.transform.SetPositionAndRotation(
                _currentFirePoint.position, _currentFirePoint.rotation);
            muzzleFlashVFX.SetActive(true);
            Invoke(nameof(HideMuzzleFlash), 0.05f);
        }

        if (!Runner.IsForward) return;

        Camera cam = fpsCamera != null ? fpsCamera : Camera.main;
        if (cam == null) return;

        var ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        if (Physics.Raycast(ray, out RaycastHit hit, _currentWeapon.range))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);
            var health = hit.collider.GetComponentInParent<PlayerHealth>();
            if (health != null)
            {
                bool isHeadshot = hit.collider.CompareTag("Head");
                int dmg = isHeadshot
                    ? _currentWeapon.damage * _currentWeapon.headshotMultiplier
                    : _currentWeapon.damage;

                // Pass killerRef để GameManager track kill
                RPC_ApplyDamage(health.Object, dmg, Runner.LocalPlayer);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_ApplyDamage(NetworkObject target, int damage, PlayerRef killerRef)
    {
        var health = target.GetComponent<PlayerHealth>();
        if (health == null) return;
        health.TakeDamage(damage, killerRef);
        target.GetComponent<FPSController>()?.TriggerHit();
    }

    void HideMuzzleFlash() => muzzleFlashVFX?.SetActive(false);

    public void OnRifleUnlocked()
    {
        if (Object.HasStateAuthority)
            IsRifleUnlocked = true;
        Debug.Log("[WeaponController] Rifle unlocked!");
    }
}