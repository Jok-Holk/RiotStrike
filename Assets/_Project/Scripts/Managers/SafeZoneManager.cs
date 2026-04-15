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
    private bool _hasSafeZones      = false;

    const string LAYER_BARRIER_A = "BarrierA";
    const string LAYER_BARRIER_B = "BarrierB";
    const string LAYER_TEAM_A    = "TeamA";
    const string LAYER_TEAM_B    = "TeamB";

    public override void Spawned()
    {
        instance = this;

        _barriersA = GameObject.FindGameObjectsWithTag("BarrierA");
        _barriersB = GameObject.FindGameObjectsWithTag("BarrierB");

        _hasSafeZones = (_barriersA.Length > 0 || _barriersB.Length > 0);

        if (_hasSafeZones)
            SetBarrierLayers();
        else
        {
            _isInSafeZone = false;
            Debug.Log("[SafeZoneManager] Không tìm thấy barrier — bỏ qua safe zone.");
        }

        if (Object.HasStateAuthority)
        {
            if (_hasSafeZones)
            {
                int waitTime = RoomPlayerData.instance != null ? RoomPlayerData.instance.WaitTime : 10;
                _countdown  = waitTime;
                GameStarted = false;
            }
            else
            {
                _countdown  = 0f;
                GameStarted = true;
            }
        }

        if (_hasSafeZones)
            StartCoroutine(InitLocalPlayer());
    }

    void SetBarrierLayers()
    {
        int layerA = LayerMask.NameToLayer(LAYER_BARRIER_A);
        int layerB = LayerMask.NameToLayer(LAYER_BARRIER_B);

        if (layerA == -1 || layerB == -1)
        {
            Debug.LogWarning("[SafeZoneManager] Barrier layer chưa tạo — bỏ qua safe zone.");
            _hasSafeZones = false;
            _isInSafeZone = false;
            return;
        }

        foreach (var b in _barriersA) if (b != null) b.layer = layerA;
        foreach (var b in _barriersB) if (b != null) b.layer = layerB;
    }

    IEnumerator InitLocalPlayer()
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        while (runner == null || RoomPlayerData.instance == null)
        {
            runner = FindFirstObjectByType<NetworkRunner>();
            yield return null;
        }

        foreach (var slot in RoomPlayerData.instance.GetOccupied())
            if (slot.PlayerRef == runner.LocalPlayer) { _localTeam = slot.Team; break; }

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
        if (_hasSafeZones && GameStarted && !_localStarted && _localTeam >= 0)
        {
            _localStarted = true;
            Debug.Log("[SafeZoneManager] Game started!");
        }
    }

    public void OnPlayerLeftSafeZone(int teamID)
    {
        if (_localTeam != teamID || _localLeftSafeZone) return;
        _localLeftSafeZone = true;
        _isInSafeZone      = false;
        ShowMyBarrierOnCamera();
    }

    public void SetLocalPlayerInSafeZone(bool inZone)
    {
        if (_localLeftSafeZone) return;
        _isInSafeZone = inZone;
    }

    public bool CanFire()
    {
        if (!GameStarted) return false;
        if (!_hasSafeZones) return true;
        return !_isInSafeZone;
    }

    public float GetCountdown() => Mathf.Max(0, _countdown);

    void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
