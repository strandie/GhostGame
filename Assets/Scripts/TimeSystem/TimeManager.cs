using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameStateSnapshot
{
    public float timestamp;
    public List<EntitySnapshot> entities = new List<EntitySnapshot>();
    public List<BulletSnapshot> bullets = new List<BulletSnapshot>();
    
    public GameStateSnapshot(float time)
    {
        timestamp = time;
    }
}

[System.Serializable]
public class EntitySnapshot
{
    public string entityId;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public bool isActive;
    public float health;
    
    // Player specific
    public bool flipX;
    public bool isRunning;
    public Vector3 gunRotation;
    
    // Enemy specific
    public Transform currentTarget;
    public float shootTimer;
    
    public EntitySnapshot(string id, Vector3 pos, Quaternion rot, Vector3 vel, bool active, float hp)
    {
        entityId = id;
        position = pos;
        rotation = rot;
        velocity = vel;
        isActive = active;
        health = hp;
    }
}

[System.Serializable]
public class BulletSnapshot
{
    public string bulletId;
    public Vector3 position;
    public Vector2 direction;
    public float speed;
    public float damage;
    public float remainingLifetime;
    public bool isPlayerBullet;
    public string ownerType; // "Player", "Ghost", "Enemy"
    
    public BulletSnapshot(string id, Vector3 pos, Vector2 dir, float spd, float dmg, float lifetime, bool isPlayer, string owner)
    {
        bulletId = id;
        position = pos;
        direction = dir;
        speed = spd;
        damage = dmg;
        remainingLifetime = lifetime;
        isPlayerBullet = isPlayer;
        ownerType = owner;
    }
}

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
    
    private Queue<GameStateSnapshot> gameHistory = new Queue<GameStateSnapshot>();
    private float snapshotInterval;
    private float lastSnapshotTime;
    private bool isRewinding = false;
    
    // Track all entities that need rewinding
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
            
            // Record all entities
            foreach (var entity in rewindableEntities)
            {
                if (entity != null && entity.IsActive())
                {
                    snapshot.entities.Add(entity.TakeSnapshot());
                }
            }
            
            // Record all bullets
            foreach (var bullet in rewindableBullets)
            {
                if (bullet != null && bullet.IsActive())
                {
                    snapshot.bullets.Add(bullet.TakeBulletSnapshot());
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
            // Spawn ghost player
            SpawnGhostPlayer();
            
            // Rewind all entities to their past states
            RewindEntities(oldestSnapshot);
            
            // Recreate all bullets from that time
            RecreateBullets(oldestSnapshot);
        }
        
        isRewinding = false;
    }
    
    private void SpawnGhostPlayer()
    {
        if (ghostPlayerPrefab == null) return;
        
        // Create ghost with recorded actions (same as before)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerRecorder recorder = player.GetComponent<PlayerRecorder>();
            if (recorder != null)
            {
                recorder.SpawnGhostFromHistory();
            }
        }
    }
    
    private void RewindEntities(GameStateSnapshot snapshot)
    {
        foreach (var entitySnapshot in snapshot.entities)
        {
            // Find the corresponding entity
            ITimeRewindable entity = FindEntityById(entitySnapshot.entityId);
            if (entity != null)
            {
                entity.RestoreFromSnapshot(entitySnapshot);
            }
        }
    }
    
    private void RecreateBullets(GameStateSnapshot snapshot)
    {
        // First, destroy all current bullets
        DestroyAllCurrentBullets();
        
        // Then recreate bullets from snapshot
        foreach (var bulletSnapshot in snapshot.bullets)
        {
            RecreateBulletFromSnapshot(bulletSnapshot);
        }
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
    }
    
    private void RecreateBulletFromSnapshot(BulletSnapshot snapshot)
    {
        GameObject bulletPrefab = null;
        
        // Get the appropriate bullet prefab
        if (snapshot.isPlayerBullet)
        {
            // Get player bullet prefab (you'll need to assign this)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerController playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    bulletPrefab = playerController.bulletPrefab;
                }
            }
        }
        else
        {
            // Get enemy bullet prefab (you'll need to find a way to access this)
            // For now, we'll try to find an enemy and get its bullet prefab
            EnemyController enemy = FindObjectOfType<EnemyController>();
            if (enemy != null)
            {
                bulletPrefab = enemy.bulletPrefab;
            }
        }
        
        if (bulletPrefab != null)
        {
            GameObject recreatedBullet = Instantiate(bulletPrefab, snapshot.position, Quaternion.identity);
            
            // Configure the bullet
            if (snapshot.isPlayerBullet)
            {
                RewindablePlayerBullet playerBullet = recreatedBullet.GetComponent<RewindablePlayerBullet>();
                if (playerBullet != null)
                {
                    playerBullet.InitializeFromSnapshot(snapshot);
                }
            }
            else
            {
                RewindableEnemyBullet enemyBullet = recreatedBullet.GetComponent<RewindableEnemyBullet>();
                if (enemyBullet != null)
                {
                    enemyBullet.InitializeFromSnapshot(snapshot);
                }
            }
        }
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
