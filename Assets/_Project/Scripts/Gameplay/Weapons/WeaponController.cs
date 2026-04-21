using UnityEngine;
using Fusion;
using RiotStrike.Data;

/// Team 0 = XANH = M4A1
/// Team 1 = ĐỎ = AK47u
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

    [Header("Muzzle Flash (child của FirePoint)")]
    [SerializeField] private GameObject muzzleFlashAK47;
    [SerializeField] private GameObject muzzleFlashM4;
    [SerializeField] private GameObject muzzleFlashPistol;

    [Header("Raycast")]
    [SerializeField] private LayerMask shootableMask;

    [Header("FPV Weapon Position (local relative to camera — chỉnh trong Inspector)")]
    [SerializeField] private Vector3 fpvOffset   = new Vector3(0.15f, -0.22f, 0.35f);
    [SerializeField] private Vector3 fpvRotation = Vector3.zero;

    [Networked] public  int        CurrentAmmo            { get; set; }
    [Networked] public  int        ReserveAmmo            { get; set; }
    [Networked] public  bool       IsRifleUnlocked        { get; set; }
    [Networked] private TickTimer  _reloadTimer           { get; set; }
    [Networked] private TickTimer  _fireTimer             { get; set; }
    [Networked] public  int        CurrentSlot            { get; set; }
    [Networked] public  int        TeamID                 { get; set; }
    [Networked] private float      _lastInputYaw          { get; set; }
    [Networked] private float      _lastInputPitch        { get; set; }
    [Networked] public  int        NetworkedSlotForRemote { get; set; }

    private WeaponData    _currentWeapon;
    private Transform     _currentFirePoint;
    private FPSController _fpsController;
    private Transform     _fpvHolder; // runtime holder — child của FPSCamera, giữ weapon cố định trước mặt

    private bool _lastRifleUnlocked  = false;
    private ChangeDetector _changeDetector;
    private int  _appliedVisualSlot = -1; // -1=chưa apply, 0=pistol, 1=rifle

    private const float EYE_HEIGHT = 1.55f;

    public override void Spawned()
    {
        _fpsController = GetComponent<FPSController>();

        if (Object.HasStateAuthority)
        {
            var np = GetComponent<NetworkPlayer>();
            TeamID                 = np != null ? np.Team : 0;
            IsRifleUnlocked        = false;
            CurrentSlot            = 0;
            NetworkedSlotForRemote = 0;
            CurrentAmmo = pistolData != null ? pistolData.magazineSize : 12;
            // Fallback 36 nếu pistolData.reserveAmmo chưa được gán trong ScriptableObject (mặc định = 0)
            int pistolReserve = pistolData != null ? pistolData.reserveAmmo : 0;
            ReserveAmmo = pistolReserve > 0 ? pistolReserve : 36;
            if (pistolReserve == 0) Debug.LogWarning("[WC] pistolData.reserveAmmo = 0 → dùng fallback 36. Vào Inspector gán giá trị đúng cho WeaponData ScriptableObject!");
        }

        // ChangeDetector cho Render() — SnapshotFrom chỉ fire khi state THẬT SỰ thay đổi
        // (không fire trong resimulation → hết jitter model)
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);

        HideAllMuzzleFlash();

        foreach (var m in new[] { ak47Model, m4a1Model, pistolModel })
            if (m != null)
                foreach (var r in m.GetComponentsInChildren<Renderer>())
                    r.allowOcclusionWhenDynamic = false;

        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            smr.updateWhenOffscreen = true;

        HideAllModels();

        if (Object.HasInputAuthority)
        {
            _fpsController?.SetWeaponType(1); // WeaponType 1 = Pistol animator
            _currentFirePoint = pistolFirePoint;
            _currentWeapon    = pistolData;

            // FPV holder: reparent weapon models vào child của camera
            // → súng luôn đứng cố định trước mặt, không theo xương tay nữa
            SetupFPVHolder();

            if (pistolModel) pistolModel.SetActive(true);
            _appliedVisualSlot = 0; // Đánh dấu pistol đã show → ChangeDetector sẽ skip ApplyPistolVisual lần đầu
        }
        else
        {
            ShowModelRemote(NetworkedSlotForRemote);
        }
    }

    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            // Local player: kiểm tra trực tiếp mỗi frame thay vì ChangeDetector
            // ChangeDetector có thể miss transition trong một số trường hợp Fusion → jitter
            // Logic: so sánh _appliedVisualSlot với target, chỉ apply khi cần
            int targetSlot = (CurrentSlot == 1 && IsRifleUnlocked) ? 1 : 0;
            if (_appliedVisualSlot != targetSlot)
            {
                if (targetSlot == 1) ApplyRifleVisual();
                else                 ApplyPistolVisual();
            }

            // Auto-equip rifle khi rifle mở khóa (1 lần duy nhất)
            if (IsRifleUnlocked && !_lastRifleUnlocked)
            {
                _lastRifleUnlocked = true;
                RPC_RequestSlotChange(1);
            }
        }
        else
        {
            // Remote player: dùng ChangeDetector để update model khi slot thay đổi
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                if (change == nameof(NetworkedSlotForRemote))
                    ShowModelRemote(NetworkedSlotForRemote);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        // InputAuthority (người chơi local) nhấn R → gửi RPC trực tiếp lên StateAuthority (host).
        // Đây là cách đáng tin cậy nhất — không phụ thuộc vào GetInput trên host
        // hay HasStateAuthority gate (vốn block Player:2 client từ xử lý FixedUpdateNetwork).
        // Runner.IsForward: chỉ gọi RPC trong forward tick, không gọi lại trong resimulation
        if (input.Reload && Object.HasInputAuthority && Runner.IsForward)
        {
            Debug.Log($"[WC] R pressed → gọi RPC_RequestReload | ammo={CurrentAmmo} reserve={ReserveAmmo}");
            RPC_RequestReload();
        }

        if (!Object.HasStateAuthority) return;

        _lastInputYaw   = input.Yaw;
        _lastInputPitch = input.Pitch;

        if (_currentWeapon == null) RefreshWeaponReference();

        // Switch weapon
        if (input.SwitchToRifle && IsRifleUnlocked && CurrentSlot != 1)
        {
            CurrentSlot = 1; NetworkedSlotForRemote = 1;
            RefreshWeaponReference();
            CurrentAmmo = _currentWeapon?.magazineSize ?? 30;
            int rifleReserve = _currentWeapon?.reserveAmmo ?? 0;
            ReserveAmmo = rifleReserve > 0 ? rifleReserve : 90; // fallback nếu ScriptableObject chưa gán
        }
        if (input.SwitchToPistol && CurrentSlot != 0)
        {
            CurrentSlot = 0; NetworkedSlotForRemote = 0;
            RefreshWeaponReference();
            CurrentAmmo = _currentWeapon?.magazineSize ?? 12;
            int pistolR = _currentWeapon?.reserveAmmo ?? 0;
            ReserveAmmo = pistolR > 0 ? pistolR : 36;
        }

        if (_currentWeapon == null) return;

        // Reload complete
        if (_reloadTimer.Expired(Runner))
        {
            _reloadTimer = default;
            // Dùng fallback nếu magazineSize chưa config trong ScriptableObject
            int magSize = (_currentWeapon.magazineSize > 0) ? _currentWeapon.magazineSize : (CurrentSlot == 1 ? 30 : 12);
            int needed  = magSize - CurrentAmmo;
            if (needed > 0)
            {
                int take = Mathf.Min(needed, ReserveAmmo);
                CurrentAmmo += take;
                ReserveAmmo -= take;
                Debug.Log($"[WC] Reload complete! ammo={CurrentAmmo}/{magSize} reserve={ReserveAmmo}");
            }
        }

        bool isReloading = !_reloadTimer.ExpiredOrNotRunning(Runner);
        bool canFire     = SafeZoneManager.instance == null || SafeZoneManager.instance.CanFire();

        // Log chỉ 1 lần mỗi giây tránh spam
        if (!canFire && input.Fire && Runner.IsForward)
            Debug.Log($"[WC] CanFire=false | GameStarted={SafeZoneManager.instance?.GameStarted} | HasSafeZones check SafeZoneManager");

        if (canFire && input.Fire
            && _fireTimer.ExpiredOrNotRunning(Runner)
            && !isReloading && CurrentAmmo > 0)
        {
            CurrentAmmo--;
            _fireTimer = TickTimer.CreateFromSeconds(Runner, 1f / _currentWeapon.fireRate);
            ServerFire();
        }

        if (input.Reload && Runner.IsForward && _currentWeapon != null)
        {
            bool timerOK    = _reloadTimer.ExpiredOrNotRunning(Runner);
            bool needAmmo   = CurrentAmmo < _currentWeapon.magazineSize;
            bool hasReserve = ReserveAmmo > 0;
            Debug.Log($"[WC] R pressed → timerOK={timerOK} needAmmo={needAmmo}({CurrentAmmo}/{_currentWeapon.magazineSize}) hasReserve={hasReserve}({ReserveAmmo}) weapon={_currentWeapon.name}");
        }
        else if (input.Reload && Runner.IsForward && _currentWeapon == null)
        {
            Debug.LogWarning("[WC] R pressed nhưng _currentWeapon = NULL! Kiểm tra WeaponData ScriptableObject đã gán chưa.");
        }

        if (input.Reload && _reloadTimer.ExpiredOrNotRunning(Runner))
        {
            // Dùng fallback nếu ScriptableObject chưa config (tránh magazineSize=0 → never reload)
            int inlineMag = _currentWeapon.magazineSize > 0 ? _currentWeapon.magazineSize : (CurrentSlot == 1 ? 30 : 12);
            float inlineRT = _currentWeapon.reloadTime > 0 ? _currentWeapon.reloadTime : 2f;
            if (CurrentAmmo < inlineMag && ReserveAmmo > 0)
            {
                _reloadTimer = TickTimer.CreateFromSeconds(Runner, inlineRT);
                RPC_NotifyReload();
                Debug.Log($"[WC] Inline reload started | ammo={CurrentAmmo}/{inlineMag} reserve={ReserveAmmo} time={inlineRT}s");
            }
        }
    }

    void ServerFire()
    {
        RPC_NotifyFire();
        if (!Runner.IsForward) return;

        Vector3 eyePos  = transform.position + Vector3.up * EYE_HEIGHT;
        Vector3 forward = Quaternion.Euler(_lastInputPitch, _lastInputYaw, 0f) * Vector3.forward;
        var ray = new Ray(eyePos, forward);
        Debug.DrawLine(ray.origin, ray.origin + forward * _currentWeapon.range, Color.yellow, 0.5f);

        RaycastHit[] hits = (shootableMask.value == 0)
            ? Physics.RaycastAll(ray, _currentWeapon.range)
            : Physics.RaycastAll(ray, _currentWeapon.range, shootableMask);

        if (hits == null || hits.Length == 0) return;
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger) continue;

            // ── Bỏ qua mọi collider thuộc về chính player đang bắn ──────────────
            // Dùng IsChildOf để bắt cả CharacterController (trên root), weapon model,
            // và bất kỳ collider con nào — dù PlayerHealth có null hay không.
            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
            {
                Debug.Log($"[WC] Skip self-collider: {hit.collider.gameObject.name}");
                continue;
            }

            var health = hit.collider.GetComponentInParent<PlayerHealth>(true); // true = include inactive

            if (health == null)
            {
                // Vật thể rắn không phải player → chặn tia, dừng
                Debug.Log($"[WC] Blocked by non-player: {hit.collider.gameObject.name}");
                break;
            }

            // Bỏ qua nếu cùng InputAuthority (safety net kép cho trường hợp hierarchy lạ)
            if (health.Object != null && health.Object.InputAuthority == Object.InputAuthority)
            {
                Debug.Log($"[WC] Skip same-authority: {hit.collider.gameObject.name}");
                continue;
            }

            // ── Chạm player hợp lệ ────────────────────────────────────────────────
            bool isHeadshot = hit.collider.CompareTag("Head");
            int  dmg        = isHeadshot
                ? _currentWeapon.damage * _currentWeapon.headshotMultiplier
                : _currentWeapon.damage;

            Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);
            Debug.Log($"[WC] Hit enemy: {hit.collider.gameObject.name} | dmg={dmg} headshot={isHeadshot}");

            health.RPC_TakeDamage(dmg, Object.InputAuthority);
            hit.collider.GetComponentInParent<FPSController>()?.TriggerHit();
            break;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestSlotChange(int slot)
    {
        if (slot == 1 && !IsRifleUnlocked) return;
        CurrentSlot = slot; NetworkedSlotForRemote = slot;
        RefreshWeaponReference();
        if (_currentWeapon != null)
        {
            CurrentAmmo = _currentWeapon.magazineSize > 0 ? _currentWeapon.magazineSize : (slot == 1 ? 30 : 12);
            int r = _currentWeapon.reserveAmmo;
            ReserveAmmo = r > 0 ? r : (slot == 1 ? 90 : 36);
            Debug.Log($"[WC] SlotChange → slot={slot} ammo={CurrentAmmo} reserve={ReserveAmmo} weapon={_currentWeapon.name}");
        }
    }

    /// Reload RPC — InputAuthority gửi trực tiếp lên StateAuthority.
    /// Đây là con đường đáng tin cậy nhất để reload hoạt động trong AutoHostOrClient,
    /// bỏ qua mọi vấn đề về GetInput trên host và HasStateAuthority gate.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestReload()
    {
        if (_currentWeapon == null) RefreshWeaponReference();
        if (_currentWeapon == null) return;

        bool timerOK    = _reloadTimer.ExpiredOrNotRunning(Runner);
        int  magSize    = _currentWeapon.magazineSize > 0 ? _currentWeapon.magazineSize : (CurrentSlot == 1 ? 30 : 12);

        // Fallback nếu ReserveAmmo hoặc magazineSize chưa config trong ScriptableObject
        if (ReserveAmmo <= 0)
        {
            int raw = _currentWeapon.reserveAmmo;
            ReserveAmmo = raw > 0 ? raw : (CurrentSlot == 1 ? 90 : 36);
            Debug.LogWarning($"[WC] ReserveAmmo was 0 → auto-fix to {ReserveAmmo}. Gán reserveAmmo trong WeaponData Inspector!");
        }

        Debug.Log($"[WC] RPC_RequestReload → timerOK={timerOK} ammo={CurrentAmmo}/{magSize} reserve={ReserveAmmo}");

        if (!timerOK) return;
        if (CurrentAmmo >= magSize) return;
        if (ReserveAmmo <= 0) return;

        float rt = _currentWeapon.reloadTime > 0 ? _currentWeapon.reloadTime : 2f;
        _reloadTimer = TickTimer.CreateFromSeconds(Runner, rt);
        RPC_NotifyReload();
        Debug.Log($"[WC] Reload started! reloadTime={rt}s");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_NotifyFire()
    {
        var flash = GetCurrentMuzzleFlash();
        if (flash != null) { flash.SetActive(false); flash.SetActive(true); }
        // Gọi TriggerFire() cho mọi client — WeaponAudio.PlayFireSound() tự xử lý
        // spatialBlend (0=2D cho local, 1=3D cho remote) để remote player nghe được súng nhau
        _fpsController?.TriggerFire();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_NotifyReload()
    {
        // Tất cả client đều chạy animation reload (kể cả remote thấy nhau reload)
        _fpsController?.TriggerReload();
    }

    void ApplyPistolVisual()
    {
        if (_appliedVisualSlot == 0) return; // đã đang hiện pistol → không làm gì
        _appliedVisualSlot = 0;

        _currentWeapon    = pistolData;
        _currentFirePoint = pistolFirePoint;
        HideAllModels();
        if (pistolModel) pistolModel.SetActive(true);
        _fpsController?.SetWeaponType(1);
    }

    void ApplyRifleVisual()
    {
        if (_appliedVisualSlot == 1) return; // đã đang hiện rifle → không làm gì
        _appliedVisualSlot = 1;

        // Team 0 = XANH = M4A1 | Team 1 = ĐỎ = AK47u
        bool isAK         = TeamID == 1;
        _currentWeapon    = isAK ? ak47Data    : m4a1Data;
        _currentFirePoint = isAK ? ak47FirePoint : m4a1FirePoint;
        HideAllModels();
        if (isAK) { if (ak47Model) ak47Model.SetActive(true); }
        else      { if (m4a1Model) m4a1Model.SetActive(true); }
        _fpsController?.SetWeaponType(0);
    }

    void ShowModelRemote(int slot)
    {
        bool isAK = TeamID == 1; // đỏ = AK
        HideAllModels();
        if (slot == 0)
        {
            if (pistolModel) pistolModel.SetActive(true);
        }
        else
        {
            if (isAK) { if (ak47Model) ak47Model.SetActive(true); }
            else      { if (m4a1Model) m4a1Model.SetActive(true); }
        }
    }

    void HideAllModels()
    {
        if (ak47Model)   ak47Model.SetActive(false);
        if (m4a1Model)   m4a1Model.SetActive(false);
        if (pistolModel) pistolModel.SetActive(false);
    }

    void HideAllMuzzleFlash()
    {
        if (muzzleFlashAK47)   muzzleFlashAK47.SetActive(false);
        if (muzzleFlashM4)     muzzleFlashM4.SetActive(false);
        if (muzzleFlashPistol) muzzleFlashPistol.SetActive(false);
    }

    GameObject GetCurrentMuzzleFlash()
    {
        if (CurrentSlot == 0) return muzzleFlashPistol;
        return TeamID == 1 ? muzzleFlashAK47 : muzzleFlashM4; // đỏ=AK, xanh=M4
    }

    void RefreshWeaponReference()
    {
        if (CurrentSlot == 0)
        {
            _currentWeapon    = pistolData;
            _currentFirePoint = pistolFirePoint;
        }
        else
        {
            bool isAK         = TeamID == 1;
            _currentWeapon    = isAK ? ak47Data    : m4a1Data;
            _currentFirePoint = isAK ? ak47FirePoint : m4a1FirePoint;
        }
    }

    public void OnRifleUnlocked()
    {
        if (!Object.HasStateAuthority) return;
        IsRifleUnlocked = true;
    }

    public int CurrentSlotID => CurrentSlot;

    /// Tạo FPV holder là child của FPSCamera, reparent tất cả weapon model vào đó.
    /// Kết quả: súng luôn hiện ở vị trí cố định trước mặt camera, không theo bone tay.
    void SetupFPVHolder()
    {
        var fpsCamera = GetComponentInChildren<FPSCamera>();
        if (fpsCamera == null)
        {
            Debug.LogWarning("[WC] Không tìm thấy FPSCamera để setup FPV holder.");
            return;
        }

        _fpvHolder = new GameObject("FPVWeaponHolder").transform;
        _fpvHolder.SetParent(fpsCamera.transform, false);
        _fpvHolder.localPosition = fpvOffset;
        _fpvHolder.localRotation = Quaternion.Euler(fpvRotation);
        _fpvHolder.localScale    = Vector3.one;

        foreach (var model in new[] { ak47Model, m4a1Model, pistolModel })
        {
            if (model == null) continue;
            bool wasActive = model.activeSelf;
            model.transform.SetParent(_fpvHolder, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale    = Vector3.one;
            model.SetActive(wasActive);
        }

        Debug.Log("[WC] FPV weapon holder created — chỉnh fpvOffset trong Inspector nếu lệch.");
    }
}
