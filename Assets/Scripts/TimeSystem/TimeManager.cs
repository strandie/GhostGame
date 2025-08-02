using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;
    
    [Header("Time Settings")]
    [Range(3f, 10f)]
    public float rewindDuration = 5f;
    [Range(10, 60)]
    public int snapshotsPerSecond = 30;
    
    [Header("Rewind Settings")]
    public KeyCode rewindKey = KeyCode.Mouse1;
    public GameObject ghostPlayerPrefab;
    
    [Header("Bullet Prefabs - Assign These!")]
    public GameObject playerBulletPrefab;
    public GameObject enemyBulletPrefab;
    
    private Queue<GameStateSnapshot> gameHistory = new Queue<GameStateSnapshot>();
    private float snapshotInterval;
    private float lastSnapshotTime;
    private bool isRewinding = false;
    
    private List<ITimeRewindable> rewindableEntities = new List<ITimeRewindable>();
    private List<ITimeRewindable> rewindableBullets = new List<ITimeRewindable>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        snapshotInterval = 1f / snapshotsPerSecond;
        
        // Auto-assign bullet prefabs if not set
        if (playerBulletPrefab == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerController playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerBulletPrefab = playerController.bulletPrefab;
                }
            }
        }
        
        if (enemyBulletPrefab == null)
        {
            RewindableEnemyController enemy = FindObjectOfType<RewindableEnemyController>();
            if (enemy != null)
            {
                enemyBulletPrefab = enemy.bulletPrefab;
            }
        }
    }
    
    void Update()
    {
        if (!isRewinding)
        {
            RecordGameState();
            
            if (Input.GetKeyDown(rewindKey))
            {
                StartRewind();
            }
        }
    }
    
    private void RecordGameState()
    {
        if (Time.time - lastSnapshotTime >= snapshotInterval)
        {
            GameStateSnapshot snapshot = new GameStateSnapshot(Time.time);
            
            // Record all entities (including enemies)
            foreach (var entity in rewindableEntities)
            {
                if (entity != null && entity.IsActive())
                {
                    EntitySnapshot entitySnapshot = entity.TakeSnapshot();
                    if (entitySnapshot != null)
                    {
                        snapshot.entities.Add(entitySnapshot);
                    }
                }
            }
            
            // Record all bullets
            foreach (var bullet in rewindableBullets)
            {
                if (bullet != null && bullet.IsActive())
                {
                    BulletSnapshot bulletSnapshot = bullet.TakeBulletSnapshot();
                    if (bulletSnapshot != null)
                    {
                        snapshot.bullets.Add(bulletSnapshot);
                    }
                }
            }
            
            gameHistory.Enqueue(snapshot);
            lastSnapshotTime = Time.time;
            
            // Remove old snapshots
            while (gameHistory.Count > 0 && 
                   Time.time - gameHistory.Peek().timestamp > rewindDuration)
            {
                gameHistory.Dequeue();
            }
        }
    }
    
    private void StartRewind()
    {
        if (gameHistory.Count == 0) return;
        
        Debug.Log("Starting rewind...");
        isRewinding = true;
        
        // Get the oldest snapshot (rewindDuration seconds ago)
        GameStateSnapshot oldestSnapshot = null;
        foreach (var snapshot in gameHistory)
        {
            oldestSnapshot = snapshot;
            break; // Get the first (oldest) one
        }
        
        if (oldestSnapshot != null)
        {
            Debug.Log($"Rewinding to snapshot with {oldestSnapshot.entities.Count} entities and {oldestSnapshot.bullets.Count} bullets");
            
            // Spawn ghost player FIRST
            SpawnGhostPlayer();
            
            // Rewind all entities (including enemies) to their past states
            RewindEntities(oldestSnapshot);
            
            // Recreate all bullets from that time
            RecreateBullets(oldestSnapshot);
        }
        
        isRewinding = false;
    }
    
    private void SpawnGhostPlayer()
    {
        if (ghostPlayerPrefab == null) 
        {
            Debug.LogWarning("Ghost player prefab not assigned!");
            return;
        }
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerRecorder recorder = player.GetComponent<PlayerRecorder>();
            if (recorder != null)
            {
                recorder.SpawnGhostFromHistory();
                Debug.Log("Ghost player spawned");
            }
            else
            {
                Debug.LogError("Player doesn't have PlayerRecorder component!");
            }
        }
        else
        {
            Debug.LogError("No player found with 'Player' tag!");
        }
    }
    
    private void RewindEntities(GameStateSnapshot snapshot)
    {
        int rewindedEntities = 0;
        
        foreach (var entitySnapshot in snapshot.entities)
        {
            // Find the corresponding entity
            ITimeRewindable entity = FindEntityById(entitySnapshot.entityId);
            if (entity != null)
            {
                // Skip player - only rewind enemies
                if (entity.GetType() == typeof(PlayerController) || 
                    entity.GetType() == typeof(PlayerRecorder))
                {
                    continue; // Don't rewind the player
                }
                
                entity.RestoreFromSnapshot(entitySnapshot);
                rewindedEntities++;
                Debug.Log($"Rewound entity: {entity.GetType().Name}");
            }
            else
            {
                Debug.LogWarning($"Could not find entity with ID: {entitySnapshot.entityId}");
            }
        }
        
        Debug.Log($"Rewound {rewindedEntities} entities");
    }
    
    private void RecreateBullets(GameStateSnapshot snapshot)
    {
        // First, destroy all current bullets
        DestroyAllCurrentBullets();
        
        // Then recreate bullets from snapshot
        int recreatedBullets = 0;
        foreach (var bulletSnapshot in snapshot.bullets)
        {
            if (RecreateBulletFromSnapshot(bulletSnapshot))
            {
                recreatedBullets++;
            }
        }
        
        Debug.Log($"Recreated {recreatedBullets} bullets");
    }
    
    private void DestroyAllCurrentBullets()
    {
        // Destroy all player bullets
        GameObject[] playerBullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (var bullet in playerBullets)
        {
            Destroy(bullet);
        }
        
        // Destroy all enemy bullets
        GameObject[] enemyBullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        foreach (var bullet in enemyBullets)
        {
            Destroy(bullet);
        }
        
        // Also destroy ghost bullets
        GameObject[] ghostBullets = GameObject.FindGameObjectsWithTag("GhostBullet");
        foreach (var bullet in ghostBullets)
        {
            Destroy(bullet);
        }
    }
    
    private bool RecreateBulletFromSnapshot(BulletSnapshot snapshot)
    {
        GameObject bulletPrefab = null;
        
        // Get the appropriate bullet prefab
        if (snapshot.isPlayerBullet)
        {
            bulletPrefab = playerBulletPrefab;
        }
        else
        {
            bulletPrefab = enemyBulletPrefab;
        }
        
        if (bulletPrefab == null)
        {
            Debug.LogWarning($"No bullet prefab assigned for {(snapshot.isPlayerBullet ? "player" : "enemy")} bullets!");
            return false;
        }
        
        GameObject recreatedBullet = Instantiate(bulletPrefab, snapshot.position, Quaternion.identity);
        
        // Configure the bullet
        if (snapshot.isPlayerBullet)
        {
            RewindablePlayerBullet playerBullet = recreatedBullet.GetComponent<RewindablePlayerBullet>();
            if (playerBullet != null)
            {
                playerBullet.InitializeFromSnapshot(snapshot);
                return true;
            }
        }
        else
        {
            RewindableEnemyBullet enemyBullet = recreatedBullet.GetComponent<RewindableEnemyBullet>();
            if (enemyBullet != null)
            {
                enemyBullet.InitializeFromSnapshot(snapshot);
                return true;
            }
        }
        
        // If we get here, something went wrong
        Destroy(recreatedBullet);
        return false;
    }
    
    private ITimeRewindable FindEntityById(string id)
    {
        foreach (var entity in rewindableEntities)
        {
            if (entity != null && entity.GetEntityId() == id)
            {
                return entity;
            }
        }
        return null;
    }
    
    public void RegisterEntity(ITimeRewindable entity)
    {
        if (!rewindableEntities.Contains(entity))
        {
            rewindableEntities.Add(entity);
            Debug.Log($"Registered entity: {entity.GetType().Name}");
        }
    }
    
    public void UnregisterEntity(ITimeRewindable entity)
    {
        rewindableEntities.Remove(entity);
    }
    
    public void RegisterBullet(ITimeRewindable bullet)
    {
        if (!rewindableBullets.Contains(bullet))
        {
            rewindableBullets.Add(bullet);
        }
    }
    
    public void UnregisterBullet(ITimeRewindable bullet)
    {
        rewindableBullets.Remove(bullet);
    }
}