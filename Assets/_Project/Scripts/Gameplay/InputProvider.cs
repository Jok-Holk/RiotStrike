using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class InputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkInputData _input;
    private bool      _crouching;
    private FPSCamera _fpsCamera;

    public void SetCamera(FPSCamera cam) => _fpsCamera = cam;

    void Update()
    {
        _input.MoveDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        _input.LookDelta = Vector2.zero;
        _input.Yaw       = _fpsCamera != null ? _fpsCamera.Yaw   : 0f;
        _input.Pitch     = _fpsCamera != null ? _fpsCamera.Pitch : 0f; // thêm pitch

        if (Input.GetKeyDown(KeyCode.LeftControl)) _crouching = !_crouching;

        _input.Crouch        = _crouching;
        _input.Sprint        = Input.GetKey(KeyCode.LeftShift);
        _input.Fire          = Input.GetMouseButton(0);
        _input.Reload        = Input.GetKeyDown(KeyCode.R);
        _input.SwitchToRifle  = Input.GetKeyDown(KeyCode.Alpha1);
        _input.SwitchToPistol = Input.GetKeyDown(KeyCode.Alpha2);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        input.Set(_input);
        _input.SwitchToPistol = false;
        _input.SwitchToRifle  = false;
        _input.Reload         = false;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
