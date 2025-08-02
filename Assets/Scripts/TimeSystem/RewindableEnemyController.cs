using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewindableEnemyController : MonoBehaviour, ITimeRewindable
{
    private Rigidbody2D rb;
    private Health enemyHealth;
    private string entityId;

    [Header("Movement")]
    public float moveSpeed = 1f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 0.5f;
    private float shootTimer;

    [Header("Targeting")]
    private Transform currentTarget;
    private Transform lastDamagedBy;

    private static List<Transform> allPlayers = new();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<Health>();
        entityId = System.Guid.NewGuid().ToString();
        
        UpdatePlayerList();
        
        // Register with TimeManager
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.RegisterEntity(this);
        }
    }

    void Update()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            currentTarget = GetTarget();

        AimAndShoot(currentTarget);
    }

    void FixedUpdate()
    {
        if (currentTarget == null)
            return;

        MoveToward(currentTarget);
    }
    
    void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.UnregisterEntity(this);
        }
    }

    // Self Explanatory
    void MoveToward(Transform target)
    {
        Vector2 dir = (target.position - transform.position).normalized;
        Vector2 nextPos = rb.position + dir * moveSpeed * Time.deltaTime;
        rb.MovePosition(nextPos);
    }

    // Self Explanatory
    void AimAndShoot(Transform target)
    {
        if (target == null) return;
        
        Vector2 shootDir = target.position - firePoint.position;
        firePoint.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg);

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            Shoot(shootDir.normalized);
            shootTimer = shootCooldown;
        }
    }

    // Self Explanatory
    void Shoot(Vector2 dir)
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        RewindableEnemyBullet bulletScript = bullet.GetComponent<RewindableEnemyBullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(dir, 10f);
        }
    }

    // Choose target based on who damage most recently otherwise does closest
    Transform GetTarget()
    {
        if (lastDamagedBy != null && lastDamagedBy.gameObject.activeInHierarchy)
            return lastDamagedBy;

        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (var p in allPlayers)
        {
            if (p == null || !p.gameObject.activeInHierarchy) continue;

            float dist = Vector2.Distance(transform.position, p.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = p;
            }
        }

        return closest;
    }

    // Simple helpers
    public void SetLastDamagedBy(Transform player)
    {
        lastDamagedBy = player;
    }

    public static void RegisterPlayer(Transform player)
    {
        if (!allPlayers.Contains(player))
            allPlayers.Add(player);
    }

    public static void UnregisterPlayer(Transform player)
    {
        allPlayers.Remove(player);
    }

    void UpdatePlayerList()
    {
        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            RegisterPlayer(go.transform);
        }
    }
    
    // ITimeRewindable implementation
    public string GetEntityId()
    {
        return entityId;
    }
    
    public EntitySnapshot TakeSnapshot()
    {
        EntitySnapshot snapshot = new EntitySnapshot(
            entityId,
            transform.position,
            transform.rotation,
            rb.velocity,
            gameObject.activeInHierarchy,
            enemyHealth != null ? enemyHealth.CurrentHealth : 100f
        );
        
        // Add enemy-specific data
        snapshot.currentTarget = currentTarget;
        snapshot.shootTimer = shootTimer;
        
        return snapshot;
    }
    
    public BulletSnapshot TakeBulletSnapshot()
    {
        // Enemies don't implement this - only bullets do
        return null;
    }
    
    public void RestoreFromSnapshot(EntitySnapshot snapshot)
    {
        // Restore position and state
        transform.position = snapshot.position;
        transform.rotation = snapshot.rotation;
        rb.velocity = snapshot.velocity;
        
        // Restore enemy-specific data
        currentTarget = snapshot.currentTarget;
        shootTimer = snapshot.shootTimer;
        
        // Restore health
        if (enemyHealth != null)
        {
            enemyHealth.CurrentHealth = snapshot.health;
        }
        
        // Set active state
        gameObject.SetActive(snapshot.isActive);
    }
    
    public bool IsActive()
    {
        return gameObject.activeInHierarchy;
    }
}
