using System;
using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [Networked] public int Health { get; set; }

    [Header("Settings")]
    [SerializeField] private int   maxHealth    = 100;
    [SerializeField] private float respawnDelay = 3f;

    public static event Action<PlayerHealth> OnHealthChanged;
    public static event Action<PlayerHealth> OnPlayerDied;

    private bool _isDead = false;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            Health = maxHealth;
    }

    public void TakeDamage(int damage, PlayerRef killerRef = default)
    {
        if (!Object.HasStateAuthority) return;
        if (_isDead) return;

        Health = Mathf.Max(0, Health - damage);
        RPC_NotifyHealthChanged();

        if (Health <= 0)
            StartDeath(killerRef);
    }

    void StartDeath(PlayerRef killerRef)
    {
        _isDead = true;

        // Tìm team của killer để register kill
        if (GameManager.instance != null && killerRef != default)
        {
            int killerTeam = GetKillerTeam(killerRef);
            GameManager.instance.RegisterKill(killerTeam);
        }

        RPC_OnDied();
        StartCoroutine(RespawnAfterDelay());
    }

    int GetKillerTeam(PlayerRef killerRef)
    {
        if (RoomPlayerData.instance == null) return 0;
        foreach (var slot in RoomPlayerData.instance.GetOccupied())
            if (slot.PlayerRef == killerRef)
                return slot.Team;
        return 0;
    }

    System.Collections.IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        if (!Object.HasStateAuthority) yield break;

        Health  = maxHealth;
        _isDead = false;

        var spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner != null)
        {
            var np   = GetComponent<NetworkPlayer>();
            int team = np != null ? np.Team : 0;
            Vector3 spawnPos = spawner.GetPublicSpawnPoint(team);

            var fps = GetComponent<FPSController>();
            fps?.InitSpawnPosition(spawnPos);
        }

        RPC_NotifyHealthChanged();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_NotifyHealthChanged() => OnHealthChanged?.Invoke(this);

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnDied()
    {
        OnPlayerDied?.Invoke(this);
        Debug.Log($"[PlayerHealth] {gameObject.name} died");
    }
}