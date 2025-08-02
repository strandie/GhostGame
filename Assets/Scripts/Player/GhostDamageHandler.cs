using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostDamageHandler : MonoBehaviour
{
    private GhostPlayer ghostPlayer;
    
    void Start()
    {
        ghostPlayer = GetComponent<GhostPlayer>();
        if (ghostPlayer == null)
        {
            Debug.LogError("GhostDamageHandler requires GhostPlayer component!");
        }
        
        // Register this ghost as a player target for enemies
        EnemyController.RegisterPlayer(transform);
    }
    
    void OnDestroy()
    {
        // Unregister when ghost is destroyed
        EnemyController.UnregisterPlayer(transform);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Handle enemy bullet damage
        if (other.CompareTag("EnemyBullet"))
        {
            EnemyBullet enemyBullet = other.GetComponent<EnemyBullet>();
            
            if (enemyBullet != null && ghostPlayer != null)
            {
                ghostPlayer.TakeDamage(enemyBullet.GetDamage());
                Destroy(other.gameObject);
            }
        }
        
        // Handle player bullet damage (if you want player to damage their own ghost)
        if (other.CompareTag("Bullet"))
        {
            PlayerBullet playerBullet = other.GetComponent<PlayerBullet>();
            
            if (playerBullet != null && ghostPlayer != null)
            {
                ghostPlayer.TakeDamage(20f); // Same damage as player bullets do to enemies
                Destroy(other.gameObject);
            }
        }
    }
}