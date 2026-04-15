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

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask shootableMask;

    [Networked] public int        CurrentAmmo     { get; set; }
    [Networked] public int        ReserveAmmo     { get; set; }
    [Networked] public bool       IsRifleUnlocked { get; set; }
    [Networked] private TickTimer _reloadTimer    { get; set; }
    [Networked] private TickTimer _fireTimer      { get; set; }
    [Networked] public  int       CurrentSlot     { get; set; }
    [Networked] public  int       TeamID          { get; set; }
    [Networked] private float     _lastInputYaw   { get; set; }
    [Networked] private float     _lastInputPitch { get; set; }

    private WeaponData    _currentWeapon;
    private Transform     _currentFirePoint;
    private FPSController _fpsController;

    private int  _lastRenderedSlot  = -1;
    private int  _lastRenderedTeam  = -1;
    private bool _lastRifleUnlocked = false;

    private const float EYE_HEIGHT = 1.55f;

    public override void Spawned()
    {
        _fpsController = GetComponent<FPSController>();

        if (Object.HasStateAuthority)
        {
            var np = GetComponent<NetworkPlayer>();
            TeamID          = np != null ? np.Team : 0;
            IsRifleUnlocked = false;
            CurrentSlot     = 0;
            CurrentAmmo     = pistolData != null ? pistolData.magazineSize : 12;
            ReserveAmmo     = pistolData != null ? pistolData.reserveAmmo  : 36;
        }

        HideAllModels();

        // Fix weapon bounds visibility
        foreach (var m in new[] { ak47Model, m4a1Model, pistolModel })
            if (m != null)
                foreach (var r in m.GetComponentsInChildren<Renderer>())
                    r.allowOcclusionWhenDynamic = false;

        var smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null) smr.updateWhenOffscreen = true;

        // Fix: Set animation state ngay khi spawn cho local player
        // Không chờ Render() detect slot change để tránh delay 1 frame
        if (Object.HasInputAuthority)
        {
            // Default: pistol
            _fpsController?.SetWeaponType(1);
            if (pistolModel) pistolModel.SetActive(true);
        }
    }
public override void Render()
    {
        if (!Object.HasInputAuthority) return;

        bool slotChanged = CurrentSlot != _lastRenderedSlot;
        bool teamChanged = TeamID      != _lastRenderedTeam;

        if (slotChanged || teamChanged || _lastRenderedSlot == -1)
        {
            _lastRenderedSlot = CurrentSlot;
            _lastRenderedTeam = TeamID;

            if (CurrentSlot == 1 && IsRifleUnlocked)
                ApplyRifleVisual();
            else
                ApplyPistolVisual();
        }

        if (IsRifleUnlocked && !_lastRifleUnlocked)
        {
            _lastRifleUnlocked = true;
            RPC_RequestSlotChange(1);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;
        if (!Object.HasStateAuthority) return;

        _lastInputYaw   = input.Yaw;
        _lastInputPitch = input.Pitch;

        if (_currentWeapon == null) RefreshWeaponReference();

        if (input.SwitchToRifle && IsRifleUnlocked && CurrentSlot != 1)
        {
            CurrentSlot = 1;
            RefreshWeaponReference();
            CurrentAmmo = _currentWeapon != null ? _currentWeapon.magazineSize : 30;
            ReserveAmmo = _currentWeapon != null ? _currentWeapon.reserveAmmo  : 90;
        }
        if (input.SwitchToPistol && CurrentSlot != 0)
        {
            CurrentSlot = 0;
            RefreshWeaponReference();
            CurrentAmmo = _currentWeapon != null ? _currentWeapon.magazineSize : 12;
            ReserveAmmo = _currentWeapon != null ? _currentWeapon.reserveAmmo  : 36;
        }

        if (_currentWeapon == null) return;

        if (_reloadTimer.Expired(Runner))
        {
            _reloadTimer = default;
            int needed = _currentWeapon.magazineSize - CurrentAmmo;
            int take   = Mathf.Min(needed, ReserveAmmo);
            CurrentAmmo += take;
            ReserveAmmo -= take;
        }

        bool isReloading = !_reloadTimer.ExpiredOrNotRunning(Runner);
        bool canFire     = SafeZoneManager.instance == null || SafeZoneManager.instance.CanFire();

        if (!canFire)
        {
            // Debug: log lý do không bắn được
            if (input.Fire && Runner.IsForward)
                Debug.Log($"[WeaponController] CanFire=false. SafeZone={SafeZoneManager.instance?.CanFire()} GameStarted={SafeZoneManager.instance?.GameStarted}");
        }

        if (canFire && input.Fire
            && _fireTimer.ExpiredOrNotRunning(Runner)
            && !isReloading
            && CurrentAmmo > 0)
        {
            CurrentAmmo--;
            _fireTimer = TickTimer.CreateFromSeconds(Runner, 1f / _currentWeapon.fireRate);
            ServerFire();
        }

        if (input.Reload
            && _reloadTimer.ExpiredOrNotRunning(Runner)
            && CurrentAmmo < _currentWeapon.magazineSize
            && ReserveAmmo > 0)
        {
_reloadTimer = TickTimer.CreateFromSeconds(Runner, _currentWeapon.reloadTime);
            RPC_NotifyReload();
        }
    }

    void ServerFire()
    {
        RPC_NotifyFire();
        if (!Runner.IsForward) return;

        Vector3 eyePos  = transform.position + Vector3.up * EYE_HEIGHT;
        Vector3 forward = Quaternion.Euler(_lastInputPitch, _lastInputYaw, 0f) * Vector3.forward;
        var ray = new Ray(eyePos, forward);

        // Debug: luôn vẽ ray để xác nhận hướng bắn
        Debug.DrawLine(ray.origin, ray.origin + forward * _currentWeapon.range, Color.yellow, 1f);

        if (shootableMask == 0)
        {
            // Không có layer nào được chọn — dùng Physics.DefaultRaycastLayers
            Debug.LogWarning("[WeaponController] shootableMask = Nothing! Tự dùng DefaultRaycastLayers.");
            if (!Physics.Raycast(ray, out RaycastHit hitDefault, _currentWeapon.range)) return;
            ProcessHit(hitDefault);
        }
        else
        {
            if (!Physics.Raycast(ray, out RaycastHit hit, _currentWeapon.range, shootableMask)) return;
            ProcessHit(hit);
        }
    }

    void ProcessHit(RaycastHit hit)
    {
        Debug.DrawLine(new Ray(transform.position + Vector3.up * EYE_HEIGHT,
            Quaternion.Euler(_lastInputPitch, _lastInputYaw, 0f) * Vector3.forward).origin, hit.point, Color.red, 1f);
        Debug.Log($"[WeaponController] Hit: {hit.collider.name} layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");

        var health = hit.collider.GetComponentInParent<PlayerHealth>();
        if (health == null) return;
        if (health.Object.InputAuthority == Object.InputAuthority) return;

        bool isHeadshot = hit.collider.CompareTag("Head");
        int dmg = isHeadshot
            ? _currentWeapon.damage * _currentWeapon.headshotMultiplier
            : _currentWeapon.damage;

        health.TakeDamage(dmg, Object.InputAuthority);
        hit.collider.GetComponentInParent<FPSController>()?.TriggerHit();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestSlotChange(int slot)
    {
        if (slot == 1 && !IsRifleUnlocked) return;
        CurrentSlot = slot;
        RefreshWeaponReference();
        if (_currentWeapon != null)
        {
            CurrentAmmo = _currentWeapon.magazineSize;
            ReserveAmmo = _currentWeapon.reserveAmmo;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_NotifyFire()
    {
        if (muzzleFlashVFX != null && _currentFirePoint != null)
        {
            muzzleFlashVFX.transform.SetPositionAndRotation(
                _currentFirePoint.position, _currentFirePoint.rotation);
            muzzleFlashVFX.SetActive(true);
            Invoke(nameof(HideMuzzleFlash), 0.05f);
        }
        if (Object.HasInputAuthority)
            _fpsController?.TriggerFire();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
void RPC_NotifyReload()
    {
        if (Object.HasInputAuthority)
            _fpsController?.TriggerReload();
    }

    void HideMuzzleFlash() => muzzleFlashVFX?.SetActive(false);

    void RefreshWeaponReference()
    {
        if (CurrentSlot == 0)
        {
            _currentWeapon    = pistolData;
            _currentFirePoint = pistolFirePoint;
        }
        else
        {
            bool isAK         = TeamID == 0;
            _currentWeapon    = isAK ? ak47Data     : m4a1Data;
            _currentFirePoint = isAK ? ak47FirePoint : m4a1FirePoint;
        }
    }

    void ApplyPistolVisual()
    {
        _currentWeapon    = pistolData;
        _currentFirePoint = pistolFirePoint;
        HideAllModels();
        if (pistolModel) pistolModel.SetActive(true);
        _fpsController?.SetWeaponType(1);
    }

    void ApplyRifleVisual()
    {
        bool isAK         = TeamID == 0;
        _currentWeapon    = isAK ? ak47Data     : m4a1Data;
        _currentFirePoint = isAK ? ak47FirePoint : m4a1FirePoint;
        HideAllModels();
        if (isAK) { if (ak47Model) ak47Model.SetActive(true); }
    }
    void HideAllModels()
    {
        if (ak47Model)   ak47Model.SetActive(false);
        if (m4a1Model)   m4a1Model.SetActive(false);
        if (pistolModel) pistolModel.SetActive(false);
    }

    public void OnRifleUnlocked()
    {
        if (!Object.HasStateAuthority) return;
        IsRifleUnlocked = true;
    }

    public int CurrentSlotID => CurrentSlot;
    }
