using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 10f;
    public float damage = 20f; // Make damage public for ghost system

    private Vector2 direction;

    public void Initialize(Vector2 shootDirection)
    {
        direction = shootDirection.normalized;

        // Rotate the bullet sprite to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // You can still apply damage or effects here
            Debug.Log("Hit player!");
            Destroy(gameObject); 
        }

        if (other.CompareTag("Enemy"))
        {
            Health enemyHealth = other.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                
                // Notify enemy who damaged them (for targeting)
                EnemyController enemyController = other.GetComponent<EnemyController>();
                if (enemyController != null)
                {
                    // Find the player who shot this bullet
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                        enemyController.SetLastDamagedBy(player.transform);
                }
            }
            Destroy(gameObject);
        }
        
        // Add ghost collision handling
        if (other.CompareTag("Ghost")) // You'll need to create this tag
        {
            GhostDamageHandler ghost = other.GetComponent<GhostDamageHandler>();
            if (ghost != null)
            {
                // The GhostDamageHandler will handle the damage
                // Bullet destruction is handled in GhostDamageHandler
                return;
            }
        }
    }
}