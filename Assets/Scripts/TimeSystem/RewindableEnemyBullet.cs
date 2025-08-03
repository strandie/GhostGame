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
    
    // Enhanced features
    private bool canChangeDirection = false;
    private HomingBullet homingComponent;

    public void Initialize(Vector2 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        bulletId = System.Guid.NewGuid().ToString();
        remainingLifetime = lifeTime;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
        
        // Check for homing component
        homingComponent = GetComponent<HomingBullet>();
        canChangeDirection = homingComponent != null;
        
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
        
        Destroy(gameObject, remainingLifetime);
        
        homingComponent = GetComponent<HomingBullet>();
        canChangeDirection = homingComponent != null;
        
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.RegisterBullet(this);
        }
    }

    public float GetDamage() => damage;
    
    // Allow external direction changes (for homing)
    public void SetDirection(Vector2 newDirection)
    {
        if (canChangeDirection)
        {
            direction = newDirection.normalized;
        }
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

        if (other.CompareTag("Ghost"))
        {
            GhostDamageHandler ghost = other.GetComponent<GhostDamageHandler>();
            if (ghost != null)
            {
                return;
            }
        }
        
        if (other.CompareTag("Terrain"))
        {
            Destroy(gameObject);
        }
    }
    
    // ITimeRewindable implementation
    public string GetEntityId() => bulletId;
    public EntitySnapshot TakeSnapshot() => null;
    
    public BulletSnapshot TakeBulletSnapshot()
    {
        return new BulletSnapshot(
            bulletId,
            transform.position,
            direction,
            speed,
            damage,
            remainingLifetime,
            false,
            "Enemy"
        );
    }
    
    public void RestoreFromSnapshot(EntitySnapshot snapshot) { }
    public bool IsActive() => gameObject.activeInHierarchy && remainingLifetime > 0;
}