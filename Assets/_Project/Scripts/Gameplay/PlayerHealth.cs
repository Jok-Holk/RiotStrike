using System;
using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [Networked] public int Health { get; set; }

    [Header("Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float respawnDelay = 3f;

    // UI / HUD có thể subscribe event này
    public static event Action<PlayerHealth> OnHealthChanged;
    public static event Action<PlayerHealth> OnPlayerDied;

    private bool _isDead = false;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            Health = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (!Object.HasStateAuthority) return;
        if (_isDead) return;

        Health = Mathf.Max(0, Health - damage);
        RPC_NotifyHealthChanged();

        if (Health <= 0)
            StartDeath();
    }

    void StartDeath()
    {
        _isDead = true;
        RPC_OnDied();

        // Respawn sau delay
        StartCoroutine(RespawnAfterDelay());
    }

    System.Collections.IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (!Object.HasStateAuthority) yield break;

        Health = maxHealth;
        _isDead = false;

        // Teleport về spawn point
        var spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner != null)
        {
            var np = GetComponent<NetworkPlayer>();
            int team = np != null ? np.Team : 0;
            Vector3 spawnPos = spawner.GetPublicSpawnPoint(team);
            transform.position = spawnPos;

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
        Debug.Log($"Player {gameObject.name} died");
    }
}