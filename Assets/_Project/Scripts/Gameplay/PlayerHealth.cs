using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [Networked] public int Health { get; set; } = 100;

    public void TakeDamage(int damage)
    {
        if (!Object.HasStateAuthority) return;

        Health -= damage;
        if (Health <= 0)
        {
            // Handle death, respawn, etc.
            Debug.Log("Player died");
            // For now, just reset health
            Health = 100;
        }
    }
}