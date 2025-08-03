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
    private bool isGhostBullet = false; // Track if this is from a ghost

    public void Initialize(Vector2 shootDirection, bool fromGhost = false)
    {
        direction = shootDirection.normalized;
        bulletId = System.Guid.NewGuid().ToString();
        remainingLifetime = lifeTime;
        startTime = Time.time;
        isGhostBullet = fromGhost;

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
    
    // Overload for backward compatibility
    public void Initialize(Vector2 shootDirection)
    {
        Initialize(shootDirection, false);
    }
    
    public void InitializeFromSnapshot(BulletSnapshot snapshot)
    {
        direction = snapshot.direction;
        bulletId = snapshot.bulletId;
        damage = snapshot.damage;
        speed = snapshot.speed;
        remainingLifetime = snapshot.remainingLifetime;
        // Assume snapshots from ghosts are ghost bullets (could be improved with better snapshot data)
        isGhostBullet = gameObject.CompareTag("GhostBullet");
        
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
        // Handle Player collision
        if (other.CompareTag("Player"))
        {
            // Only ghost bullets can damage the player
            if (isGhostBullet || gameObject.CompareTag("GhostBullet"))
            {
                Health playerHealth = other.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                    Debug.Log($"Ghost bullet hit player for {damage} damage!");
                }
                
                // Try PlayerHealth component as backup
                PlayerHealth pH = other.GetComponent<PlayerHealth>();
                if (pH != null && pH.GetComponent<Health>() != null)
                {
                    pH.GetComponent<Health>().TakeDamage(damage);
                    Debug.Log($"Ghost bullet hit player for {damage} damage!");
                }
            }
            else
            {
                Debug.Log("Player bullet hit player - no damage");
            }
            
            Destroy(gameObject);
            return;
        }

        // Handle Enemy collision
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
                    // Find the appropriate shooter
                    if (isGhostBullet || gameObject.CompareTag("GhostBullet"))
                    {
                        // Find the ghost that shot this
                        GhostPlayer ghost = FindObjectOfType<GhostPlayer>();
                        if (ghost != null)
                            enemyController.SetLastDamagedBy(ghost.transform);
                    }
                    else
                    {
                        // Find the player who shot this bullet
                        GameObject player = GameObject.FindGameObjectWithTag("Player");
                        if (player != null)
                            enemyController.SetLastDamagedBy(player.transform);
                    }
                }
            }
            Destroy(gameObject);
            return;
        }
        
        // Handle Terrain collision
        if (other.CompareTag("Terrain"))
        {
            Destroy(gameObject);
            return;
        }
        
        // Handle Ghost collision
        if (other.CompareTag("Ghost"))
        {
            // Only non-ghost bullets can damage ghosts (prevent friendly fire)
            if (!isGhostBullet && !gameObject.CompareTag("GhostBullet"))
            {
                GhostPlayer ghost = other.GetComponent<GhostPlayer>();
                if (ghost != null)
                {
                    ghost.TakeDamage(damage);
                    Debug.Log($"Player bullet hit ghost for {damage} damage!");
                }
            }
            else
            {
                Debug.Log("Ghost bullet hit ghost - no friendly fire");
            }
            
            Destroy(gameObject);
            return;
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
            isGhostBullet ? "Ghost" : "Player"
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
    
    // Public method to mark bullet as from ghost
    public void SetAsGhostBullet(bool isGhost = true)
    {
        isGhostBullet = isGhost;
    }
}