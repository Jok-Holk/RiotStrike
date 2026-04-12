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

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask shootableMask;

    // --- Networked state (chỉ StateAuthority write) ---
    [Networked] public int        CurrentAmmo     { get; set; }
    [Networked] public int        ReserveAmmo     { get; set; }
    [Networked] public bool       IsRifleUnlocked { get; set; }
    [Networked] private TickTimer _reloadTimer    { get; set; }
    [Networked] private TickTimer _fireTimer      { get; set; }
    [Networked] public  int       CurrentSlot     { get; set; } // 0=Pistol 1=Rifle — networked để render sync
    [Networked] public  int       TeamID          { get; set; }

    // Local visual state (không cần networked)
    private WeaponData    _currentWeapon;
    private Transform     _currentFirePoint;
    private FPSController _fpsController;

    // Render-side change detection
    private int  _lastRenderedSlot      = -1;
    private int  _lastRenderedTeam      = -1;
    private bool _lastRifleUnlocked     = false;

    public override void Spawned()
    {
        _fpsController = GetComponent<FPSController>();

        if (Object.HasStateAuthority)
        {
            var np = GetComponent<NetworkPlayer>();
            TeamID          = np != null ? np.Team : 0;
            IsRifleUnlocked = false;
            CurrentSlot     = 0; // bắt đầu với pistol
            CurrentAmmo     = pistolData != null ? pistolData.magazineSize : 12;
            ReserveAmmo     = pistolData != null ? pistolData.reserveAmmo  : 36;
        }

        HideAllModels();
    }

    public override void Render()
    {
        if (!Object.HasInputAuthority) return;

        // Equip visual khi slot hoặc team thay đổi
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

        // Auto switch sang rifle khi vừa unlock
        if (IsRifleUnlocked && !_lastRifleUnlocked)
        {
            _lastRifleUnlocked = true;
            Debug.Log("[WeaponController] Rifle unlocked — auto switching");
            RPC_RequestSlotChange(1);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;
        if (!Object.HasStateAuthority) return; // chỉ host xử lý weapon logic

        if (_currentWeapon == null)
            RefreshWeaponReference();

        // Switch weapon request từ input
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
        bool canFire     = SafeZoneManager.instance == null || SafeZoneManager.instance.CanFire();

        // Fire
        if (canFire && input.Fire
            && _fireTimer.ExpiredOrNotRunning(Runner)
            && !isReloading
            && CurrentAmmo > 0)
        {
            CurrentAmmo--;
            _fireTimer = TickTimer.CreateFromSeconds(Runner, 1f / _currentWeapon.fireRate);
            ServerFire();
        }

        // Reload request
        if (input.Reload
            && _reloadTimer.ExpiredOrNotRunning(Runner)
            && CurrentAmmo < _currentWeapon.magazineSize
            && ReserveAmmo > 0)
        {
            _reloadTimer = TickTimer.CreateFromSeconds(Runner, _currentWeapon.reloadTime);
            RPC_NotifyReload();
        }
    }

    // Chỉ chạy trên StateAuthority — raycast + damage authoritative
    void ServerFire()
    {
        // Notify visual/audio cho local player qua RPC
        RPC_NotifyFire();

        // Raycast chỉ 1 lần trên forward tick
        if (!Runner.IsForward) return;

        Camera cam = fpsCamera != null ? fpsCamera : Camera.main;
        if (cam == null) return;

        var ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        if (!Physics.Raycast(ray, out RaycastHit hit, _currentWeapon.range, shootableMask)) return;

        Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);

        var health = hit.collider.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        // Jangan tembak diri sendiri
        if (health.Object.InputAuthority == Object.InputAuthority) return;

        bool isHeadshot = hit.collider.CompareTag("Head");
        int dmg = isHeadshot
            ? _currentWeapon.damage * _currentWeapon.headshotMultiplier
            : _currentWeapon.damage;

        health.TakeDamage(dmg, Object.InputAuthority);
        hit.collider.GetComponentInParent<FPSController>()?.TriggerHit();
    }

    // InputAuthority → StateAuthority: yêu cầu đổi slot
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

    // StateAuthority → All: thông báo fire để play visual + audio
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_NotifyFire()
    {
        // Visual
        if (muzzleFlashVFX != null && _currentFirePoint != null)
        {
            muzzleFlashVFX.transform.SetPositionAndRotation(
                _currentFirePoint.position, _currentFirePoint.rotation);
            muzzleFlashVFX.SetActive(true);
            Invoke(nameof(HideMuzzleFlash), 0.05f);
        }

        // Animation + audio (chỉ local player cần TriggerFire — remote thấy qua animator sync)
        if (Object.HasInputAuthority)
            _fpsController?.TriggerFire();
    }

    // StateAuthority → All: thông báo reload để play animation + audio
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_NotifyReload()
    {
        if (Object.HasInputAuthority)
            _fpsController?.TriggerReload();
    }

    void HideMuzzleFlash() => muzzleFlashVFX?.SetActive(false);

    // Cập nhật _currentWeapon và _currentFirePoint theo slot + team hiện tại
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
            _currentWeapon    = isAK ? ak47Data    : m4a1Data;
            _currentFirePoint = isAK ? ak47FirePoint : m4a1FirePoint;
        }
    }

    // Apply visual chỉ trên InputAuthority (local player)
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
        bool isAK = TeamID == 0;
        _currentWeapon    = isAK ? ak47Data    : m4a1Data;
        _currentFirePoint = isAK ? ak47FirePoint : m4a1FirePoint;
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

    public void OnRifleUnlocked()
    {
        if (!Object.HasStateAuthority) return;
        IsRifleUnlocked = true;
        // CurrentSlot sẽ được switch qua Render() → RPC_RequestSlotChange
        Debug.Log("[WeaponController] Rifle unlocked!");
    }

    // Accessor cho WeaponAudio
    public int CurrentSlotID => CurrentSlot;
}
