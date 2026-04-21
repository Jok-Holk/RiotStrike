using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveDirection;
    public Vector2 LookDelta;
    public float Yaw;
    public float Pitch;
    public NetworkBool Crouch;
    public NetworkBool Fire;
    public NetworkBool Reload;
    public NetworkBool Sprint;
    public NetworkBool SwitchToRifle;
    public NetworkBool SwitchToPistol;
    public NetworkBool Aim;             // chuột phải = ADS
}