using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewindablePlayerBullet : MonoBehaviour, ITimeRewindable
{
    public float speed = 10f;
    public float lifeTime = 10f;
    public float damage = 20f;

    private Vector2 direction;
    private string bulletId;
    private float remainingLifetime;
    private float startTime;

    public void Initialize(Vector2 shootDirection)
    {
        direction = shootDirection.normalized;
        bulletId = System.Guid.NewGuid().ToString();
        remainingLifetime = lifeTime;
        startTime = Time.time;

        // Rotate the bullet sprite to face direction
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
        
        // Rotate the bullet sprite to face direction
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
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
                RewindableEnemyController enemyController = other.GetComponent<RewindableEnemyController>();
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
            true, // isPlayerBullet
            "Player"
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
