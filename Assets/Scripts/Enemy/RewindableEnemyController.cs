using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RewindableEnemyController : MonoBehaviour, ITimeRewindable
{
    private Rigidbody2D rb;
    private Health enemyHealth;
    private string entityId;

    [Header("Tilemap")]
    public Tilemap collisionTilemap;

    private Vector2 currentVelocity;

    [Header("Movement")]
    public float moveSpeed = 1f;
    public float wanderChangeInterval = 2f;
    private float wanderTimer;
    private Vector2 wanderDirection;

    [Header("Chase Behavior")]
    public float visionRange = 10f;
    public float chasePauseInterval = 2f;
    public float chasePauseDuration = 0.5f;
    private float chasePauseTimer;
    private bool isChasePaused;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 0.5f;
    private float shootTimer;

    [Header("Targeting")]
    private Transform currentTarget;
    private Transform lastDamagedBy;

    private static List<Transform> allPlayers = new();

    private EnemyDeathHandler deathHandler;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<Health>();
        entityId = System.Guid.NewGuid().ToString();

        GameObject tilemapObj = GameObject.FindGameObjectWithTag("Terrain");
        if (tilemapObj != null)
        {
            collisionTilemap = tilemapObj.GetComponent<Tilemap>();
        }

        UpdatePlayerList();

        if (TimeManager.Instance != null)
            TimeManager.Instance.RegisterEntity(this);

        wanderTimer = wanderChangeInterval;
        wanderDirection = GetRandomDirection();

        deathHandler = GetComponent<EnemyDeathHandler>();
        if (deathHandler == null)
        {
            deathHandler = gameObject.AddComponent<EnemyDeathHandler>();
        }
    }

    void Update()
    {
        UpdateTargeting();
        HandleShooting();
        
        // Update player list every few frames to catch new ghosts
        if (Time.frameCount % 30 == 0) // Every 0.5 seconds at 60fps
        {
            UpdatePlayerList();
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.UnregisterEntity(this);
    }

    // === Movement ===

    void HandleMovement()
    {
        Vector2 movement;

        if (currentTarget == null)
        {
            // Smooth wander behavior
            wanderTimer -= Time.fixedDeltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = wanderChangeInterval;
                wanderDirection = GetRandomDirection();
            }

            // Slowly blend to the new direction
            wanderDirection = Vector2.Lerp(wanderDirection, GetRandomDirection(), 0.05f).normalized;
            movement = wanderDirection;
        }
        else
        {
            // Chase behavior with smooth weaving
            if (isChasePaused)
            {
                chasePauseTimer -= Time.fixedDeltaTime;
                if (chasePauseTimer > 0f)
                    return;

                isChasePaused = false;
            }

            Vector2 toTarget = (currentTarget.position - transform.position).normalized;

            // Weaving
            float weaveAmount = 0.4f;  // Slightly reduced for smoother feel
            float weaveSpeed = 3f;     // Matches movement speed better
            Vector2 perp = new Vector2(-toTarget.y, toTarget.x);
            float sine = Mathf.Sin(Time.time * weaveSpeed);
            Vector2 weave = perp * sine * weaveAmount;

            Vector2 desiredDirection = (toTarget + weave).normalized;

            // Blend to the final direction for polish
            movement = Vector2.Lerp(rb.velocity.normalized, desiredDirection, 0.2f).normalized;

            // Setup next chase pause
            isChasePaused = true;
            chasePauseTimer = chasePauseDuration + Random.Range(0f, 0.15f); // Slight variation
        }

        // Attempt to move with smoother physics-aware check
        Vector2 targetPos = rb.position + movement * moveSpeed * Time.fixedDeltaTime;

        float checkRadius = 0.15f;  // Slightly larger to prevent edge clipping
        RaycastHit2D hit = Physics2D.CircleCast(rb.position, checkRadius, movement, moveSpeed * Time.fixedDeltaTime, LayerMask.GetMask("Terrain"));

        if (hit.collider == null)
        {
            rb.MovePosition(targetPos);
        }
        else
        {
            // Reflect or nudge in a new direction if blocked
            wanderDirection = Vector2.Reflect(movement, hit.normal);
        }
    }



    Vector2 GetRandomDirection()
    {
        return Random.insideUnitCircle.normalized;
    }

    // === Targeting ===

    void UpdateTargeting()
    {
        // Reprioritize only if current target is invalid or out of sight
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy || !CanSee(currentTarget))
        {
            Transform newTarget = null;
            float closestDist = visionRange;

            foreach (var p in allPlayers)
            {
                if (p == null || !p.gameObject.activeInHierarchy) continue;

                float dist = Vector2.Distance(transform.position, p.position);
                if (dist <= visionRange && CanSee(p))
                {
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        newTarget = p;
                    }
                }
            }

            currentTarget = newTarget;
        }

        // Overwrite if damaged
        if (lastDamagedBy != null && lastDamagedBy.gameObject.activeInHierarchy && CanSee(lastDamagedBy))
            currentTarget = lastDamagedBy;
    }

    bool CanSee(Transform target)
    {
        Vector2 start = transform.position;
        Vector2 end = target.position;
        Vector2 dir = (end - start).normalized;
        float dist = Vector2.Distance(start, end);

        LayerMask blockingLayer = LayerMask.GetMask("Terrain", "Default"); // Adjust as needed

        RaycastHit2D hit = Physics2D.Raycast(start, dir, dist, blockingLayer);
        if (hit.collider != null)
        {
            // Blocked if the first thing hit is not the target
            if (hit.collider.transform != target && 
                !hit.collider.CompareTag("Player") && 
                !hit.collider.CompareTag("Ghost")) // Add ghost tag check
                return false;
        }

        return true;
    }

    // === Shooting ===

    void HandleShooting()
    {
        if (currentTarget == null) return;

        Vector2 shootDir = currentTarget.position - firePoint.position;
        firePoint.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg);

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f && CanShootTarget(currentTarget))
        {
            Shoot(shootDir.normalized);
            shootTimer = shootCooldown;
        }
    }

    void Shoot(Vector2 dir)
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        RewindableEnemyBullet bulletScript = bullet.GetComponent<RewindableEnemyBullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(dir, 10f);
        }
    }

    bool CanShootTarget(Transform target)
    {
        Vector2 start = firePoint.position;
        Vector2 end = target.position;
        Vector2 dir = (end - start).normalized;
        float dist = Vector2.Distance(start, end);

        RaycastHit2D[] hits = Physics2D.RaycastAll(start, dir, dist);
        foreach (var hit in hits)
        {
            if (hit.collider != null)
            {
                GameObject go = hit.collider.gameObject;

                if (go == target.gameObject) return true;
                if (go.CompareTag("Enemy") && go != gameObject)
                {
                    return false; // Another enemy is blocking
                }

                if (collisionTilemap != null &&
                    collisionTilemap.GetTile(collisionTilemap.WorldToCell(hit.point)) != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // === Player Management ===

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
        // Clear and rebuild the list to include both players and ghosts
        allPlayers.Clear();
        
        // Find regular players
        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
            RegisterPlayer(go.transform);
        
        // Find ghost players - assuming they have tag "Ghost" or component "GhostPlayer"
        foreach (var go in GameObject.FindGameObjectsWithTag("Ghost"))
            RegisterPlayer(go.transform);
        
        // Alternative: Find by component if ghosts don't have specific tag
        GhostPlayer[] ghosts = FindObjectsOfType<GhostPlayer>();
        foreach (var ghost in ghosts)
        {
            if (ghost.gameObject.activeInHierarchy)
                RegisterPlayer(ghost.transform);
        }
    }

    // === Rewind Integration ===

    public string GetEntityId() => entityId;

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
        snapshot.currentTarget = currentTarget;
        snapshot.shootTimer = shootTimer;
        return snapshot;
    }

    public BulletSnapshot TakeBulletSnapshot() => null;

    public void RestoreFromSnapshot(EntitySnapshot snapshot)
    {
        if (deathHandler != null && deathHandler.IsDead() && snapshot.health > 0)
        {
            deathHandler.Revive();
        }

        transform.position = snapshot.position;
        transform.rotation = snapshot.rotation;
        rb.velocity = snapshot.velocity;
        currentTarget = snapshot.currentTarget;
        shootTimer = snapshot.shootTimer;

        if (enemyHealth != null)
            enemyHealth.CurrentHealth = snapshot.health;

        gameObject.SetActive(snapshot.isActive);
    }

    //public bool IsActive() => gameObject.activeInHierarchy;
    public bool IsActive() 
    { 
        // Consider enemy inactive if dead, even if GameObject is still active
        if (deathHandler != null && deathHandler.IsDead())
            return false;
            
        return gameObject.activeInHierarchy;
    }
}