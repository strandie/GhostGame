using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 10f;
    private float damage;
    private Vector2 direction;

    public void Initialize(Vector2 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
    }

    // Add this getter method for the ghost system
    public float GetDamage()
    {
        return damage;
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth player = other.GetComponent<PlayerHealth>();
            if (player != null)
                player.TakeDamage(damage);

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