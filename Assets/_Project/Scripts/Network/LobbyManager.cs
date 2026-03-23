using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static LobbyManager instance;

    private NetworkRunner _runner;
    private InputProvider _inputProvider;
    private List<SessionInfo> _sessionList = new List<SessionInfo>();

    public event Action<List<SessionInfo>> OnRoomListUpdated;
    public event Action<string> OnJoinFailed;
    public event Action OnConnected;

    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void Connect(string nickName)
    {
        if (_runner != null) return;

        PlayerPrefs.SetString("NickName", nickName);

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = false;
        _runner.AddCallbacks(this);

        var result = await _runner.JoinSessionLobby(SessionLobby.Shared);
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
        if (_runner == null) return;

        _runner.ProvideInput = true;
        _inputProvider = gameObject.AddComponent<InputProvider>();
        _runner.AddCallbacks(_inputProvider);

        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = roomName,
            PlayerCount = maxPlayers,
            Scene = SceneRef.FromIndex(2),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            CustomPhotonAppSettings = new Fusion.Photon.Realtime.FusionAppSettings
            {
                FixedRegion = "asia"
            }
        });

        if (!result.Ok)
        {
            Debug.LogError("Create room failed: " + result.ShutdownReason);
            OnJoinFailed?.Invoke(result.ShutdownReason.ToString());
        }
    }

    public async void JoinRoom(string roomName, int maxPlayers = 8)
    {
        if (_runner == null) return;

        _runner.ProvideInput = true;
        _inputProvider = gameObject.AddComponent<InputProvider>();
        _runner.AddCallbacks(_inputProvider);

        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = roomName,
            PlayerCount = maxPlayers,
            Scene = SceneRef.FromIndex(2),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            CustomPhotonAppSettings = new Fusion.Photon.Realtime.FusionAppSettings
            {
                FixedRegion = "asia"
            }
        });

        if (!result.Ok)
        {
            Debug.LogError("Join room failed: " + result.ShutdownReason);
            OnJoinFailed?.Invoke(result.ShutdownReason.ToString());
        }
    }

    public List<SessionInfo> GetRoomList() => _sessionList;

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _sessionList = sessionList;
        OnRoomListUpdated?.Invoke(_sessionList);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer) Debug.Log("Player joined: " + player);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("Shutdown: " + shutdownReason);
        SceneManager.LoadScene("MainMenu");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log("Disconnected: " + reason);
        SceneManager.LoadScene("MainMenu");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        => OnJoinFailed?.Invoke(reason.ToString());

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        _inputProvider?.OnInput(runner, input);
        Debug.Log($"[OnInput] MoveDir={_inputProvider?._input.MoveDirection}");
    }
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