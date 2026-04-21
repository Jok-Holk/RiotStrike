using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static LobbyManager instance;

    [SerializeField] private NetworkObject roomPlayerDataPrefab;

    private NetworkRunner _runner;
    private InputProvider _inputProvider;
    private List<SessionInfo> _sessionList = new();
    private string _localNickName = "";

    // Lấy AppSettings từ PhotonAppSettings.asset, chỉ override Region
    private static Fusion.Photon.Realtime.FusionAppSettings GetAppSettings()
    {
        var settings = Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings.GetCopy();
        settings.FixedRegion = "asia";
        return settings;
    }

    public event Action<List<SessionInfo>> OnRoomListUpdated;
    public event Action<string> OnJoinFailed;
    public event Action OnConnected;
    public event Action OnRoomJoined;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void Connect(string nickName)
    {
        if (_runner != null) return;
        _localNickName = nickName;

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = false;
        _runner.AddCallbacks(this);

        // FIX: Thêm AppSettings vào JoinSessionLobby để cùng region với StartGame
        var result = await _runner.JoinSessionLobby(SessionLobby.Shared, customAppSettings: GetAppSettings());

        if (result.Ok)
        {
            Debug.Log("Connected to lobby as: " + nickName);
            OnConnected?.Invoke();
        }
        else
        {
            Debug.LogError("Failed to connect: " + result.ShutdownReason);
            OnJoinFailed?.Invoke(result.ShutdownReason.ToString());
            Destroy(_runner);
            _runner = null;
        }
    }

    public async void CreateRoom(string roomName, int maxPlayers = 8)
    {
        if (_runner == null) { OnJoinFailed?.Invoke("Chưa kết nối."); return; }

        _runner.ProvideInput = true;
        if (_inputProvider == null)
        {
            _inputProvider = gameObject.AddComponent<InputProvider>();
            _runner.AddCallbacks(_inputProvider);
        }

        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = roomName,
            PlayerCount = maxPlayers,
            Scene = SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath("Assets/_Project/Scenes/Lobby.unity")),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            CustomPhotonAppSettings = GetAppSettings()
        });

        if (result.Ok)
            OnRoomJoined?.Invoke();
        else
        {
            Debug.LogError("Create room failed: " + result.ShutdownReason);
            OnJoinFailed?.Invoke(result.ShutdownReason.ToString());
        }
    }

    public async void JoinRoom(string roomName, int maxPlayers = 8)
    {
        if (_runner == null) { OnJoinFailed?.Invoke("Chưa kết nối."); return; }

        _runner.ProvideInput = true;
        if (_inputProvider == null)
        {
            _inputProvider = gameObject.AddComponent<InputProvider>();
            _runner.AddCallbacks(_inputProvider);
        }

        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = roomName,
            PlayerCount = maxPlayers,
            Scene = SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath("Assets/_Project/Scenes/Lobby.unity")),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            CustomPhotonAppSettings = GetAppSettings()
        });

        if (result.Ok)
            OnRoomJoined?.Invoke();
        else
        {
            Debug.LogError("Join room failed: " + result.ShutdownReason);
            OnJoinFailed?.Invoke(result.ShutdownReason.ToString());
        }
    }

    public void StartGame()
    {
        if (_runner == null || !_runner.IsServer) return;
        _runner.LoadScene(SceneRef.FromIndex(
            SceneUtility.GetBuildIndexByScenePath("Assets/_Project/Scenes/Game.unity")));
    }

    public List<SessionInfo> GetRoomList() => _sessionList;
    public string GetLocalNickName() => _localNickName;

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        if (RoomPlayerData.instance == null)
            runner.Spawn(roomPlayerDataPrefab, Vector3.zero, Quaternion.identity);

        string nick = player == runner.LocalPlayer
            ? (_localNickName.Length > 0 ? _localNickName : "Player")
            : "...";

        StartCoroutine(WaitAndAddPlayer(player, nick));
    }

    IEnumerator WaitAndAddPlayer(PlayerRef player, string nick)
    {
        while (RoomPlayerData.instance == null) yield return null;

        var occupied = RoomPlayerData.instance.GetOccupied();
        foreach (var slot in occupied)
        {
            if (slot.PlayerRef == player)
            {
                Debug.Log($"Player {player} already in slot, skip add.");
                yield break;
            }
        }

        RoomPlayerData.instance.ServerAddPlayer(player, nick);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        if (RoomPlayerData.instance == null)
        {
            Debug.LogWarning("OnPlayerLeft: RoomPlayerData.instance is null, skip.");
            return;
        }
        RoomPlayerData.instance.ServerRemovePlayer(player);
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _sessionList = sessionList;
        OnRoomListUpdated?.Invoke(_sessionList);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("Shutdown: " + shutdownReason);
        if (_runner != null) Destroy(_runner);
        if (_inputProvider != null) Destroy(_inputProvider);
        _runner = null;
        _inputProvider = null;
        SceneManager.LoadScene("Lobby");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log("Disconnected: " + reason);
        if (_runner != null) Destroy(_runner);
        if (_inputProvider != null) Destroy(_inputProvider);
        _runner = null;
        _inputProvider = null;
        SceneManager.LoadScene("Lobby");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        => OnJoinFailed?.Invoke(reason.ToString());

    public void OnConnectedToServer(NetworkRunner runner) { }
    // InputProvider đã được đăng ký trực tiếp với runner qua AddCallbacks(_inputProvider)
    // → Fusion gọi InputProvider.OnInput() trực tiếp — KHÔNG delegate lại ở đây
    // Nếu delegate, OnInput() bị gọi 2 lần: lần 1 set Reload=true rồi reset false,
    // lần 2 ghi đè Reload=false → input one-shot (R, 1, 2) bị mất.
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}