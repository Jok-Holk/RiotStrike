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

    private bool _localStarted      = false;
    private bool _localLeftSafeZone = false;
    private bool _isInSafeZone      = true;
    private int  _localTeam         = -1;

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

        Debug.Log($"[SafeZone] BarrierA layer={layerA}, BarrierB layer={layerB}");

        if (layerA == -1 || layerB == -1)
        {
            Debug.LogError("[SafeZone] Barrier layers chưa được tạo hoặc tên không khớp!");
            return;
        }

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

            Debug.Log($"[SafeZone] Local player team={_localTeam}, layer={layer}");

            if (layer == -1)
            {
                Debug.LogError("[SafeZone] Team layer chưa được tạo hoặc tên không khớp!");
                return;
            }

            SetLayerRecursive(fps.gameObject, layer);
            break;
        }
    }

    void SetupCameraCulling()
    {
        var cam = Camera.main;
        if (cam == null) return;

        string myBarrierLayer = _localTeam == 0 ? LAYER_BARRIER_A : LAYER_BARRIER_B;
        int layerIndex = LayerMask.NameToLayer(myBarrierLayer);

        if (layerIndex == -1)
        {
            Debug.LogError("[SafeZone] Barrier layer chưa được tạo hoặc tên không khớp!");
            return;
        }

        int myBarrierMask = 1 << layerIndex;

        cam.cullingMask &= ~myBarrierMask;

        Debug.Log($"[SafeZone] Camera culling set, team={_localTeam}, hidden layer={myBarrierLayer}");
    }

    void ShowMyBarrierOnCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        string myBarrierLayer = _localTeam == 0 ? LAYER_BARRIER_A : LAYER_BARRIER_B;
        int layerIndex = LayerMask.NameToLayer(myBarrierLayer);

        if (layerIndex == -1)
        {
            Debug.LogError("[SafeZone] Barrier layer chưa được tạo hoặc tên không khớp!");
            return;
        }

        int myBarrierMask = 1 << layerIndex;
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
        if (GameStarted && !_localStarted && _localTeam >= 0)
        {
            _localStarted = true;
            Debug.Log("[SafeZone] Game started!");
        }
    }

    public void OnPlayerLeftSafeZone(int teamID)
    {
        if (_localTeam != teamID) return;
        if (_localLeftSafeZone) return;

        _localLeftSafeZone = true;
        _isInSafeZone      = false;

        ShowMyBarrierOnCamera();

        Debug.Log($"[SafeZone] Player left safe zone permanently");
    }

    public void SetLocalPlayerInSafeZone(bool inZone)
    {
        if (_localLeftSafeZone) return;
        _isInSafeZone = inZone;
    }

    public bool CanFire()
    {
        if (!GameStarted) return false;
        if (_isInSafeZone) return false;
        return true;
    }

    public float GetCountdown() => Mathf.Max(0, _countdown);

    void SetLayerRecursive(GameObject go, int layer)
    {
        if (layer == -1)
        {
            Debug.LogError("[SafeZone] Layer chưa được tạo hoặc tên không khớp!");
            return;
        }

        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
