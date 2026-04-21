using System;
using System.Collections;
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

    /// Dùng khi shooter (shared mode) cần gây sát thương lên player khác.
    /// StateAuthority của target mới được phép giảm HP — gọi qua RPC này.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage, PlayerRef killerRef)
    {
        Debug.Log($"[PlayerHealth] RPC_TakeDamage nhận: dmg={damage} killer={killerRef} isDead={_isDead} hp={Health} hasAuth={Object.HasStateAuthority}");
        TakeDamage(damage, killerRef);
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

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        if (!Object.HasStateAuthority) yield break;

        var spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner != null)
        {
            var np   = GetComponent<NetworkPlayer>();
            int team = np != null ? np.Team : 0;
            Vector3 spawnPos = spawner.GetPublicSpawnPoint(team);

            var fps = GetComponent<FPSController>();
            fps?.InitSpawnPosition(spawnPos);
        }

        Health  = maxHealth;
        _isDead = false;

        RPC_OnRespawned();
        RPC_NotifyHealthChanged();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_NotifyHealthChanged() => OnHealthChanged?.Invoke(this);

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnDied()
    {
        OnPlayerDied?.Invoke(this);
        GetComponent<FPSController>()?.TriggerDeath();
        Debug.Log($"[PlayerHealth] {gameObject.name} died");
    }

    // Thông báo respawn để client reset visual state
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnRespawned()
    {
        // Reset animator về idle khi sống lại
        var pac = GetComponent<PlayerAnimatorController>();
        if (pac != null)
        {
            // Reset trigger Death nếu còn pending
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.ResetTrigger("Death");
                animator.SetTrigger("Respawn"); // cần state Respawn trong animator, hoặc bỏ qua
            }
        }
        Debug.Log($"[PlayerHealth] {gameObject.name} respawned");
    }
}
