using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCallbackHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private PlayerSpawner _spawner;
    private NetworkRunner _runner;
    private bool _sceneLoaded = false;

    void Awake()
    {
        _spawner = GetComponent<PlayerSpawner>();
    }

    void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner != null)
            _runner.AddCallbacks(this);
        // else
        //     Debug.LogError("SpawnCallbackHandler: No NetworkRunner found!");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;
        _sceneLoaded = true;

        Debug.Log("[SpawnCallbackHandler] Scene loaded, spawning all players...");

        // Scene load xong mới spawn — đảm bảo collider map đã có
        foreach (var player in runner.ActivePlayers)
        {
            if (_spawner != null)
                _spawner.SpawnPlayer(player);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        // Nếu scene chưa load xong thì bỏ qua
        // OnSceneLoadDone sẽ spawn tất cả
        if (!_sceneLoaded) return;

        Debug.Log("[SpawnCallbackHandler] Late join: " + player);
        _spawner?.SpawnPlayer(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        _spawner?.DespawnPlayer(player);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { _sceneLoaded = false; }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}