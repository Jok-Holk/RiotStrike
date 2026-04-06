using UnityEngine;

// Gắn vào SafeZone_Visual (thảm) — cần IsTrigger = true trên MeshCollider
public class SafeZoneDetector : MonoBehaviour
{
    [Header("Team của safe zone này")]
    [SerializeField] private int teamID; // 0 = TeamA, 1 = TeamB

    void OnTriggerExit(Collider other)
    {
        var fp = other.GetComponentInParent<FPSController>();
        if (fp == null || !fp.Object.HasInputAuthority) return;

        // Chỉ xử lý player cùng team với safe zone này
        var np = other.GetComponentInParent<NetworkPlayer>();
        if (np == null || np.Team != teamID) return;

        Debug.Log($"[SafeZone] Team {teamID} player left safe zone");

        // Báo SafeZoneManager player đã ra
        SafeZoneManager.instance?.OnPlayerLeftSafeZone(teamID);
    }

    void OnTriggerStay(Collider other)
    {
        var fp = other.GetComponentInParent<FPSController>();
        if (fp == null || !fp.Object.HasInputAuthority) return;

        var np = other.GetComponentInParent<NetworkPlayer>();
        if (np == null || np.Team != teamID) return;

        // Báo đang trong safe zone để lock fire
        SafeZoneManager.instance?.SetLocalPlayerInSafeZone(true);
    }

    void OnTriggerEnter(Collider other)
    {
        var fp = other.GetComponentInParent<FPSController>();
        if (fp == null || !fp.Object.HasInputAuthority) return;

        var np = other.GetComponentInParent<NetworkPlayer>();
        if (np == null || np.Team != teamID) return;

        SafeZoneManager.instance?.SetLocalPlayerInSafeZone(true);
    }
}