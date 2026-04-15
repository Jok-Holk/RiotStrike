using Fusion;
using UnityEngine;

/// <summary>
/// Gắn vào Canvas_HUD trong PlayerPrefab.
/// Tự ẩn canvas nếu đây không phải local player.
/// Giải quyết: HUD bị chồng lên nhau, scoreboard bị nhân nhiều lần.
/// </summary>
public class PlayerHUD : NetworkBehaviour
{
    public override void Spawned()
    {
        // Chỉ local player (HasInputAuthority) mới thấy HUD của mình
        gameObject.SetActive(Object.HasInputAuthority);
    }
}
