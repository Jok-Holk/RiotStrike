using System.Collections;
using Fusion;
using UnityEngine;

public class SafeZoneManager : NetworkBehaviour
{
    public static SafeZoneManager instance;

    [Networked] private float _countdown  { get; set; }
    [Networked] public  bool  GameStarted { get; set; }

    private GameObject[] _barriersA;
    private GameObject[] _barriersB;

    private bool _localLeftSafeZone  = false;
    private bool _isInSafeZone       = false;
    private int  _localTeam          = -1;
    private bool _hasSafeZones       = false;
    private bool _barriersDeactivated = false;

    private ChangeDetector _changeDetector;

    const string LAYER_BARRIER_A = "BarrierA";
    const string LAYER_BARRIER_B = "BarrierB";
    const string LAYER_TEAM_A    = "TeamA";
    const string LAYER_TEAM_B    = "TeamB";

    public override void Spawned()
    {
        instance = this;
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        _barriersA = GameObject.FindGameObjectsWithTag("BarrierA");
        _barriersB = GameObject.FindGameObjectsWithTag("BarrierB");

        _hasSafeZones = (_barriersA.Length > 0 || _barriersB.Length > 0);

        if (_hasSafeZones)
        {
            SetBarrierLayers();
            _isInSafeZone = true;
        }
        else
        {
            _isInSafeZone = false;
            Debug.Log("[SafeZoneManager] Không có barrier → bắn được ngay.");
        }

        if (Object.HasStateAuthority)
        {
            if (_hasSafeZones)
            {
                // Ưu tiên RoomPlayerData, fallback GameConfig (static, lưu trước scene load)
                int waitTime = RoomPlayerData.instance != null
                    ? RoomPlayerData.instance.WaitTime
                    : GameConfig.WaitTime;
                _countdown  = waitTime;
                GameStarted = false;
                Debug.Log($"[SafeZoneManager] WaitTime = {waitTime}s");
            }
            else
            {
                _countdown  = 0f;
                GameStarted = true;
            }
        }

        // Late join: game đã started → deactivate barriers ngay
        // Không guard bằng _hasSafeZones để đảm bảo cả trường hợp layer chưa tạo
        if (GameStarted)
            DeactivateBarriers();

        if (_hasSafeZones)
            StartCoroutine(InitLocalPlayer());
    }

    void SetBarrierLayers()
    {
        int layerA = LayerMask.NameToLayer(LAYER_BARRIER_A);
        int layerB = LayerMask.NameToLayer(LAYER_BARRIER_B);
        if (layerA == -1 || layerB == -1)
        {
            // Layer chưa tạo → không assign layer, nhưng GIỮ _hasSafeZones = true
            // để barriers vẫn được deactivate khi game start.
            // Camera culling sẽ không hoạt động, nhưng tốt hơn là để tường vô hình chặn đạn mãi mãi.
            Debug.LogWarning("[SafeZoneManager] Layer BarrierA/BarrierB chưa tạo → bỏ qua camera culling, barriers vẫn được deactivate khi game start.");
            return;
        }
        foreach (var b in _barriersA) if (b != null) b.layer = layerA;
        foreach (var b in _barriersB) if (b != null) b.layer = layerB;
    }

    IEnumerator InitLocalPlayer()
    {
        // Chờ RoomPlayerData tối đa 5 giây
        var runner = FindFirstObjectByType<NetworkRunner>();
        float timeout = 5f;
        while ((runner == null || RoomPlayerData.instance == null) && timeout > 0f)
        {
            runner = FindFirstObjectByType<NetworkRunner>();
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Lấy team từ RoomPlayerData (ưu tiên)
        if (RoomPlayerData.instance != null && runner != null)
        {
            foreach (var slot in RoomPlayerData.instance.GetOccupied())
                if (slot.PlayerRef == runner.LocalPlayer) { _localTeam = slot.Team; break; }
        }

        // Fallback: đọc Team từ NetworkPlayer của local player
        if (_localTeam == -1)
        {
            foreach (var np in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
            {
                if (np.Object != null && np.Object.HasInputAuthority)
                {
                    _localTeam = np.Team;
                    Debug.Log($"[SafeZoneManager] Fallback team từ NetworkPlayer: {_localTeam}");
                    break;
                }
            }
        }

        // Last resort: default team 0 để không bị kẹt
        if (_localTeam == -1)
        {
            _localTeam = 0;
            Debug.LogWarning("[SafeZoneManager] Không xác định được team → mặc định team 0.");
        }

        yield return new WaitForSeconds(0.5f);
        SetLocalPlayerLayer();
        SetupCameraCulling();
    }

    void SetLocalPlayerLayer()
    {
        foreach (var fps in FindObjectsByType<FPSController>(FindObjectsSortMode.None))
        {
            if (!fps.Object.HasInputAuthority) continue;
            int layer = _localTeam == 0
                ? LayerMask.NameToLayer(LAYER_TEAM_A)
                : LayerMask.NameToLayer(LAYER_TEAM_B);
            if (layer == -1) { Debug.LogWarning("[SafeZoneManager] Team layer chưa tạo."); return; }
            SetLayerRecursive(fps.gameObject, layer);
            break;
        }
    }

    void SetupCameraCulling()
    {
        var cam = Camera.main;
        if (cam == null) return;
        string myBarrierLayer = _localTeam == 0 ? LAYER_BARRIER_A : LAYER_BARRIER_B;
        int idx = LayerMask.NameToLayer(myBarrierLayer);
        if (idx == -1) return;
        cam.cullingMask &= ~(1 << idx);
    }

    void ShowMyBarrierOnCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        string myBarrierLayer = _localTeam == 0 ? LAYER_BARRIER_A : LAYER_BARRIER_B;
        int idx = LayerMask.NameToLayer(myBarrierLayer);
        if (idx == -1) return;
        cam.cullingMask |= (1 << idx);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !_hasSafeZones || GameStarted) return;
        _countdown -= Runner.DeltaTime;
        if (_countdown <= 0f) { _countdown = 0f; GameStarted = true; }
    }

    public override void Render()
    {
        // Detect khi GameStarted flip true → deactivate barriers trên mọi client
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(GameStarted) && GameStarted)
            {
                Debug.Log("[SafeZoneManager] GameStarted → Deactivate barriers!");
                DeactivateBarriers();
            }
        }
    }

    /// Deactivate barriers trên local client để player có thể đi ra.
    void DeactivateBarriers()
    {
        if (_barriersDeactivated) return;
        _barriersDeactivated = true;

        // Pass 1: Deactivate theo tag (tìm trong Spawned())
        foreach (var b in _barriersA) if (b != null) b.SetActive(false);
        foreach (var b in _barriersB) if (b != null) b.SetActive(false);

        // Pass 2: Deactivate theo layer để bắt barriers không có tag hoặc tag sai.
        // Camera culling ẩn barrier visually nhưng collider vẫn chặn raycast →
        // phải SetActive(false) để thực sự tắt cả render lẫn physics.
        int layerA = LayerMask.NameToLayer(LAYER_BARRIER_A);
        int layerB = LayerMask.NameToLayer(LAYER_BARRIER_B);
        if (layerA != -1 || layerB != -1)
        {
            foreach (var go in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if ((layerA != -1 && go.layer == layerA) || (layerB != -1 && go.layer == layerB))
                    go.SetActive(false);
            }
        }

        // Pass 3: Tắt trực tiếp tất cả Collider trên barriers đã tìm được
        // để đảm bảo không còn block raycast dù SetActive bị fail vì lý do nào đó.
        foreach (var b in _barriersA)
            if (b != null) foreach (var col in b.GetComponentsInChildren<Collider>()) col.enabled = false;
        foreach (var b in _barriersB)
            if (b != null) foreach (var col in b.GetComponentsInChildren<Collider>()) col.enabled = false;

        Debug.Log("[SafeZoneManager] Barriers deactivated (tag + layer + collider disabled).");
    }

    public void OnPlayerLeftSafeZone(int teamID)
    {
        // SafeZoneDetector đã lọc đúng team (np.Team == teamID) trước khi gọi hàm này
        // → không cần check _localTeam nữa (tránh bị block khi _localTeam = -1)
        if (_localLeftSafeZone) return;
        _localLeftSafeZone = true;
        _isInSafeZone      = false;
        ShowMyBarrierOnCamera();
        Debug.Log($"[SafeZoneManager] Team {teamID} player rời safe zone vĩnh viễn.");
    }

    public void SetLocalPlayerInSafeZone(bool inZone)
    {
        if (_localLeftSafeZone) return;
        _isInSafeZone = inZone;
    }

    public bool CanFire()
    {
        // GameStarted là [Networked] — nhất quán ngay trong FixedUpdateNetwork
        // Không dùng _barriersDeactivated (set trong Render → trễ 1 frame so với FUN)
        if (!GameStarted) return false;
        return true; // game started → hết safe zone restriction, bắn tự do
    }

    public float GetCountdown() => Mathf.Max(0, _countdown);

    void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
