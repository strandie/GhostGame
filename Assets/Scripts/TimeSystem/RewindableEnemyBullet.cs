using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewindableEnemyBullet : MonoBehaviour, ITimeRewindable
{
    public float speed = 10f;
    public float lifeTime = 10f;
    private float damage;
    private Vector2 direction;
    private string bulletId;
    private float remainingLifetime;

    public void Initialize(Vector2 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        bulletId = System.Guid.NewGuid().ToString();
        remainingLifetime = lifeTime;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
        
        // Register with TimeManager
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.RegisterBullet(this);
        }
    }
    
    public void InitializeFromSnapshot(BulletSnapshot snapshot)
    {
        direction = snapshot.direction;
        bulletId = snapshot.bulletId;
        damage = snapshot.damage;
        speed = snapshot.speed;
        remainingLifetime = snapshot.remainingLifetime;
        
        transform.position = snapshot.position;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Destroy after remaining lifetime
        Destroy(gameObject, remainingLifetime);
        
        // Register with TimeManager
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.RegisterBullet(this);
        }
    }

    // Add this getter method for the ghost system
    public float GetDamage()
    {
        return damage;
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
        remainingLifetime -= Time.deltaTime;
    }
    
    void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.UnregisterBullet(this);
        }
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
        if (other.CompareTag("Ghost"))
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
    
    // ITimeRewindable implementation
    public string GetEntityId()
    {
        return bulletId;
    }
    
    public EntitySnapshot TakeSnapshot()
    {
        // Bullets don't use EntitySnapshot - they use BulletSnapshot
        return null;
    }
    
    public BulletSnapshot TakeBulletSnapshot()
    {
        return new BulletSnapshot(
            bulletId,
            transform.position,
            direction,
            speed,
            damage,
            remainingLifetime,
            false, // isPlayerBullet
            "Enemy"
        );
    }
    
    public void RestoreFromSnapshot(EntitySnapshot snapshot)
    {
        // Bullets don't restore from EntitySnapshot
    }
    
    public bool IsActive()
    {
        return gameObject.activeInHierarchy && remainingLifetime > 0;
    }
}