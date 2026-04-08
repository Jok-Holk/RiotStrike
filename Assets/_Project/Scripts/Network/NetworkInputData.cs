using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2     MoveDirection;
    public Vector2     LookDelta;
    public float       Yaw;
    public NetworkBool Crouch;
    public NetworkBool Fire;
    public NetworkBool Reload;
    public NetworkBool Sprint;
    public NetworkBool SwitchToRifle;  // phím 1
    public NetworkBool SwitchToPistol; // phím 2
}