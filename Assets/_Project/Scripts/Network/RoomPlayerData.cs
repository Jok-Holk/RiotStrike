using System.Collections.Generic;
using Fusion;
using UnityEngine;

public struct PlayerSlot : INetworkStruct
{
    public PlayerRef          PlayerRef;
    public NetworkString<_32> NickName;
    public int                Team;
    public NetworkBool        IsOccupied;
    public int                TeamJoinTick; // tick khi join team, dùng để sort
}

public class RoomPlayerData : NetworkBehaviour
{
    public static RoomPlayerData instance;

    [Networked, Capacity(8)]
    public NetworkArray<PlayerSlot> Slots => default;

    // Room config — sync từ host xuống tất cả client
    [Networked] public int WaitTime   { get; set; } = 30;
    [Networked] public int PistolTime { get; set; } = 60;
    [Networked] public int RifleTime  { get; set; } = 120;
    public int RoundTime => PistolTime + RifleTime;

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        instance = this;
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        FindFirstObjectByType<UIRoomManager>()?.RefreshPlayerList();
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(Slots))
                FindFirstObjectByType<UIRoomManager>()?.RefreshPlayerList();
        }
    }

    // ── Player management ──────────────────────────────────────────────────────

    public void ServerAddPlayer(PlayerRef player, string nickName)
    {
        if (!Object.HasStateAuthority) return;
        for (int i = 0; i < Slots.Length; i++)
        {
            var s = Slots.Get(i);
            if (s.IsOccupied && s.PlayerRef == player) return;
        }
        for (int i = 0; i < Slots.Length; i++)
        {
            if (!Slots.Get(i).IsOccupied)
            {
                Slots.Set(i, new PlayerSlot
                {
                    PlayerRef  = player,
                    NickName   = nickName,
                    Team       = 0,
                    IsOccupied = true
                });
                return;
            }
        }
    }

    public void ServerRemovePlayer(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        for (int i = 0; i < Slots.Length; i++)
        {
            var slot = Slots.Get(i);
            if (slot.IsOccupied && slot.PlayerRef == player)
            {
                Slots.Set(i, default);
                return;
            }
        }
    }

    [Rpc(RpcSources.Proxies | RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void RPC_RegisterPlayer(PlayerRef player, string nick)
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            var slot = Slots.Get(i);
            if (slot.IsOccupied && slot.PlayerRef == player)
            {
                slot.NickName = nick;
                Slots.Set(i, slot);
                return;
            }
        }
    }

    [Rpc(RpcSources.Proxies | RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestChangeTeam(PlayerRef player, int team)
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            var slot = Slots.Get(i);
            if (slot.IsOccupied && slot.PlayerRef == player)
            {
                slot.Team         = team;
                slot.TeamJoinTick = Runner.Tick; // ghi lại thời điểm đổi team
                Slots.Set(i, slot);
                return;
            }
        }
    }

    [Rpc(RpcSources.Proxies | RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void RPC_UpdateMyNickname(PlayerRef player, string nick)
    {
        RPC_RegisterPlayer(player, nick);
    }

    // ── Room config sync ───────────────────────────────────────────────────────

    // Chỉ host gọi — StateAuthority set trực tiếp, broadcast tự động qua Networked
    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void RPC_UpdateRoomConfig(int waitTime, int pistolTime, int rifleTime)
    {
        WaitTime   = waitTime;
        PistolTime = pistolTime;
        RifleTime  = rifleTime;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    public List<PlayerSlot> GetOccupied()
    {
        var result = new List<PlayerSlot>();
        for (int i = 0; i < Slots.Length; i++)
            if (Slots.Get(i).IsOccupied)
                result.Add(Slots.Get(i));
        // Sort theo TeamJoinTick — ai vào team trước thì ở trên
        result.Sort((a, b) => a.TeamJoinTick.CompareTo(b.TeamJoinTick));
        return result;
    }
}