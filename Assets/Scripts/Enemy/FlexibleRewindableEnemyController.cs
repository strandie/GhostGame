using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FlexibleRewindableEnemyController : MonoBehaviour, ITimeRewindable
{
    [Header("Enemy Configuration")]
    public EnemyConfiguration enemyConfig;
    
    [Header("Runtime Overrides (Optional)")]
    [SerializeField] private bool useRuntimeOverrides = false;
    [SerializeField] private float overrideHealth = 100f;
    [SerializeField] private float overrideMoveSpeed = 2f;
    [SerializeField] private float overrideVisionRange = 10f;

    // Core components
    private Rigidbody2D rb;
    private Health enemyHealth;
    private string entityId;
    private EnemyDeathHandler deathHandler;

    [Header("Tilemap")]
    public Tilemap collisionTilemap;

    [Header("References")]
    public Transform firePoint;

    // Movement state
    private float wanderTimer;
    private Vector2 wanderDirection;
    private float chasePauseTimer;
    private bool isChasePaused;

    // Targeting state
    private Transform currentTarget;
    private Transform lastDamagedBy;
    private float loseTargetTimer;

    // Shooting system
    private ShootingPattern shootingPattern;

    // Player tracking
    private static List<Transform> allPlayers = new();

    void Start()
    {
        InitializeComponents();
        InitializeConfiguration();
        InitializeAI();
        RegisterWithSystems();
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<Health>();
        entityId = System.Guid.NewGuid().ToString();

        GameObject tilemapObj = GameObject.FindGameObjectWithTag("Terrain");
        if (tilemapObj != null)
        {
            collisionTilemap = tilemapObj.GetComponent<Tilemap>();
        }

        deathHandler = GetComponent<EnemyDeathHandler>();
        if (deathHandler == null)
        {
            deathHandler = gameObject.AddComponent<EnemyDeathHandler>();
        }
    }

    void InitializeConfiguration()
    {
        if (enemyConfig == null)
        {
            Debug.LogError($"Enemy Configuration not assigned to {gameObject.name}! Creating default config.");
            return;
        }

        // Apply configuration to health component
        if (enemyHealth != null)
        {
            enemyHealth.maxHealth = useRuntimeOverrides ? overrideHealth : enemyConfig.maxHealth;
            enemyHealth.CurrentHealth = enemyHealth.maxHealth;
            Debug.Log($"{name} Initialized with health: {enemyHealth.CurrentHealth}");

        }

        // Initialize shooting pattern
        if (enemyConfig.shootingPattern != null && firePoint != null)
        {
            shootingPattern = enemyConfig.shootingPattern.CreatePattern(firePoint, enemyConfig);
        }
    }

    void InitializeAI()
    {
        wanderTimer = GetWanderChangeInterval();
        wanderDirection = GetRandomDirection();
        UpdatePlayerList();
    }

    void RegisterWithSystems()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.RegisterEntity(this);
    }

    void Update()
    {
        UpdateTargeting();
        HandleShooting();
        
        // Update player list periodically
        if (Time.frameCount % 30 == 0)
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

    // === Configuration Getters ===
    
    private float GetMoveSpeed() => useRuntimeOverrides ? overrideMoveSpeed : enemyConfig.moveSpeed;
    private float GetVisionRange() => useRuntimeOverrides ? overrideVisionRange : enemyConfig.visionRange;
    private float GetWanderChangeInterval() => enemyConfig.wanderChangeInterval;
    private float GetChasePauseInterval() => enemyConfig.chasePauseInterval;
    private float GetChasePauseDuration() => enemyConfig.chasePauseDuration;
    private float GetLoseTargetTime() => enemyConfig.loseTargetTime;

    // === Movement System ===

    void HandleMovement()
    {
        if (!enemyConfig.canWander && !enemyConfig.canChase) return;

        Vector2 movement = Vector2.zero;

        if (currentTarget == null && enemyConfig.canWander)
        {
            movement = HandleWanderMovement();
        }
        else if (currentTarget != null && enemyConfig.canChase)
        {
            movement = HandleChaseMovement();
        }

        if (movement != Vector2.zero)
        {
            MoveWithPhysics(movement);
        }
    }

    Vector2 HandleWanderMovement()
    {
        wanderTimer -= Time.fixedDeltaTime;
        if (wanderTimer <= 0f)
        {
            wanderTimer = GetWanderChangeInterval();
            wanderDirection = GetRandomDirection();
        }
        return wanderDirection;
    }

    Vector2 HandleChaseMovement()
    {
        if (isChasePaused)
        {
            chasePauseTimer -= Time.fixedDeltaTime;
            if (chasePauseTimer > 0f)
                return Vector2.zero;
            isChasePaused = false;
        }

        Vector2 movement = (currentTarget.position - transform.position).normalized;

        // Set up next pause
        if (GetChasePauseInterval() > 0)
        {
            isChasePaused = true;
            chasePauseTimer = GetChasePauseDuration() + Random.Range(0f, 0.2f);
        }

        return movement;
    }

    void MoveWithPhysics(Vector2 movement)
    {
        float moveSpeed = GetMoveSpeed();
        Vector2 targetPos = rb.position + movement * moveSpeed * Time.fixedDeltaTime;

        // Use Rigidbody2D.Cast to detect obstacles
        RaycastHit2D[] hits = new RaycastHit2D[1];
        int count = rb.Cast(movement, hits, moveSpeed * Time.fixedDeltaTime);

        if (count == 0)
        {
            rb.MovePosition(targetPos);
        }
    }

    Vector2 GetRandomDirection()
    {
        return Random.insideUnitCircle.normalized;
    }

    // === Targeting System ===

    void UpdateTargeting()
    {
        // Handle lose target timer
        if (currentTarget != null && !CanSee(currentTarget))
        {
            loseTargetTimer += Time.deltaTime;
            if (loseTargetTimer >= GetLoseTargetTime())
            {
                currentTarget = null;
                loseTargetTimer = 0f;
            }
        }
        else
        {
            loseTargetTimer = 0f;
        }

        // Find new target if current is invalid
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = FindBestTarget();
        }

        // Prioritize damager if enabled
        if (enemyConfig.prioritizeDamager && lastDamagedBy != null && 
            lastDamagedBy.gameObject.activeInHierarchy && CanSee(lastDamagedBy))
        {
            currentTarget = lastDamagedBy;
        }
    }

    Transform FindBestTarget()
    {
        Transform bestTarget = null;
        float closestDist = GetVisionRange();

        foreach (var player in allPlayers)
        {
            if (player == null || !player.gameObject.activeInHierarchy) continue;

            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= GetVisionRange() && CanSee(player))
            {
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestTarget = player;
                }
            }
        }

        return bestTarget;
    }

    bool CanSee(Transform target)
    {
        Vector2 start = transform.position;
        Vector2 end = target.position;
        Vector2 dir = (end - start).normalized;
        float dist = Vector2.Distance(start, end);

        LayerMask blockingLayer = LayerMask.GetMask("Terrain", "Default");

        RaycastHit2D hit = Physics2D.Raycast(start, dir, dist, blockingLayer);
        if (hit.collider != null)
        {
            if (hit.collider.transform != target && 
                !hit.collider.CompareTag("Player") && 
                !hit.collider.CompareTag("Ghost"))
                return false;
        }

        return true;
    }

    // === Shooting System ===

    void HandleShooting()
    {
        if (shootingPattern == null || currentTarget == null || firePoint == null) return;

        // Orient fire point towards target
        Vector2 shootDir = currentTarget.position - firePoint.position;
        firePoint.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg);

        // Let the shooting pattern handle the actual shooting logic
        if (shootingPattern.CanShoot(currentTarget))
        {
            shootingPattern.Shoot(currentTarget, this);
        }
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
        allPlayers.Clear();
        
        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
            RegisterPlayer(go.transform);
        
        foreach (var go in GameObject.FindGameObjectsWithTag("Ghost"))
            RegisterPlayer(go.transform);
        
        GhostPlayer[] ghosts = FindObjectsOfType<GhostPlayer>();
        foreach (var ghost in ghosts)
        {
            if (ghost.gameObject.activeInHierarchy)
                RegisterPlayer(ghost.transform);
        }
    }

    // === Runtime Configuration Modification ===

    public void SetEnemyConfiguration(EnemyConfiguration newConfig)
    {
        enemyConfig = newConfig;
        InitializeConfiguration();
    }

    public void ModifyStats(float? health = null, float? speed = null, float? vision = null)
    {
        useRuntimeOverrides = true;
        if (health.HasValue) overrideHealth = health.Value;
        if (speed.HasValue) overrideMoveSpeed = speed.Value;
        if (vision.HasValue) overrideVisionRange = vision.Value;
        
        if (health.HasValue && enemyHealth != null)
        {
            enemyHealth.maxHealth = health.Value;
            enemyHealth.CurrentHealth = health.Value;
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
        
        // Store additional state in existing fields (reusing currentTarget and shootTimer for compatibility)
        snapshot.currentTarget = currentTarget;
        snapshot.shootTimer = wanderTimer; // Reuse shootTimer for wanderTimer temporarily
        
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
        
        // Restore state from existing fields
        currentTarget = snapshot.currentTarget;
        wanderTimer = snapshot.shootTimer; // Restore from reused field
        
        // Reset other state variables to reasonable defaults
        wanderDirection = GetRandomDirection();
        chasePauseTimer = 0f;
        isChasePaused = false;
        loseTargetTimer = 0f;

        if (enemyHealth != null)
            enemyHealth.CurrentHealth = snapshot.health;

        gameObject.SetActive(snapshot.isActive);
    }

    public bool IsActive() 
    { 
        if (deathHandler != null && deathHandler.IsDead())
            return false;
            
        return gameObject.activeInHierarchy;
    }

    // === Debug/Editor Support ===
    
    void OnDrawGizmosSelected()
    {
        if (enemyConfig == null) return;
        
        // Draw vision range using DrawWireSphere (Unity's correct method)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GetVisionRange());
        
        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
        
        // Draw wander direction
        if (currentTarget == null && enemyConfig.canWander)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, wanderDirection * 2f);
        }
    }
}