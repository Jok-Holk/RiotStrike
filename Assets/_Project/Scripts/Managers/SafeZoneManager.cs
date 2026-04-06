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

    private bool _localStarted     = false;
    private bool _localLeftSafeZone = false;
    private bool _isInSafeZone     = true;
    private int  _localTeam        = -1;

    // Layer names
    const string LAYER_BARRIER_A = "BarrierA";
    const string LAYER_BARRIER_B = "BarrierB";
    const string LAYER_TEAM_A    = "TeamA";
    const string LAYER_TEAM_B    = "TeamB";

    public override void Spawned()
    {
        instance = this;

        _barriersA = GameObject.FindGameObjectsWithTag("BarrierA");
        _barriersB = GameObject.FindGameObjectsWithTag("BarrierB");

        SetBarrierLayers();

        if (Object.HasStateAuthority)
        {
            int waitTime = RoomPlayerData.instance != null
                ? RoomPlayerData.instance.WaitTime : 10;
            _countdown  = waitTime;
            GameStarted = false;
        }

        StartCoroutine(InitLocalPlayer());
    }

    void SetBarrierLayers()
    {
        int layerA = LayerMask.NameToLayer(LAYER_BARRIER_A);
        int layerB = LayerMask.NameToLayer(LAYER_BARRIER_B);

        foreach (var b in _barriersA)
            if (b != null) b.layer = layerA;
        foreach (var b in _barriersB)
            if (b != null) b.layer = layerB;
    }

    IEnumerator InitLocalPlayer()
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        while (runner == null || RoomPlayerData.instance == null)
        {
            runner = FindFirstObjectByType<NetworkRunner>();
            yield return null;
        }

        // Tìm team local player
        foreach (var slot in RoomPlayerData.instance.GetOccupied())
        {
            if (slot.PlayerRef == runner.LocalPlayer)
            {
                _localTeam = slot.Team;
                break;
            }
        }

        // Set player layer
        yield return new WaitForSeconds(0.5f); // chờ player spawn
        SetLocalPlayerLayer();

        // Setup camera culling — ẩn barrier team mình khỏi camera local
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
            SetLayerRecursive(fps.gameObject, layer);
            break;
        }
    }

    void SetupCameraCulling()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // Ẩn barrier team mình khỏi camera — team A không thấy BarrierA
        string myBarrierLayer = _localTeam == 0 ? LAYER_BARRIER_A : LAYER_BARRIER_B;
        int myBarrierMask     = 1 << LayerMask.NameToLayer(myBarrierLayer);

        // Bỏ layer barrier team mình khỏi culling mask
        cam.cullingMask &= ~myBarrierMask;

        Debug.Log($"[SafeZone] Camera culling set, team={_localTeam}, hidden layer={myBarrierLayer}");
    }

    void ShowMyBarrierOnCamera()
    {
        // Khi ra khỏi safe zone → thêm lại barrier team mình vào culling mask
        // (thấy tường của mình từ ngoài — nhưng không đi vào được)
        var cam = Camera.main;
        if (cam == null) return;

        string myBarrierLayer = _localTeam == 0 ? LAYER_BARRIER_A : LAYER_BARRIER_B;
        int myBarrierMask     = 1 << LayerMask.NameToLayer(myBarrierLayer);
        cam.cullingMask |= myBarrierMask;

        Debug.Log($"[SafeZone] Barrier visible again after leaving safe zone");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (GameStarted) return;

        _countdown -= Runner.DeltaTime;
        if (_countdown <= 0f)
        {
            _countdown  = 0f;
            GameStarted = true;
        }
    }

    public override void Render()
    {
        // Hết countdown — game bắt đầu
        if (GameStarted && !_localStarted && _localTeam >= 0)
        {
            _localStarted = true;
            Debug.Log("[SafeZone] Game started!");
        }
    }

    // Gọi từ SafeZoneDetector khi player ra khỏi thảm
    public void OnPlayerLeftSafeZone(int teamID)
    {
        if (_localTeam != teamID) return;
        if (_localLeftSafeZone) return; // chỉ xử lý 1 lần

        _localLeftSafeZone = true;
        _isInSafeZone      = false;

        // Thấy barrier team mình từ ngoài
        ShowMyBarrierOnCamera();

        Debug.Log($"[SafeZone] Player left safe zone permanently");
    }

    // Gọi từ SafeZoneDetector OnTriggerStay/Enter
    public void SetLocalPlayerInSafeZone(bool inZone)
    {
        if (_localLeftSafeZone) return; // đã ra rồi không cho vào lại
        _isInSafeZone = inZone;
    }

    // WeaponController gọi để check có được bắn không
    public bool CanFire()
    {
        if (!GameStarted) return false;       // chưa bắt đầu
        if (_isInSafeZone) return false;      // đang trong safe zone
        return true;
    }

    public float GetCountdown() => Mathf.Max(0, _countdown);

    void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}