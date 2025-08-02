using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerAction
{
    public float timestamp;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 gunRotation;
    public bool flipX;
    public bool isRunning;
    public bool shouldShoot;
    public Vector2 shootDirection;
    
    public PlayerAction(float time, Vector3 pos, Quaternion rot, Vector3 gunRot, bool flip, bool running, bool shoot, Vector2 shootDir)
    {
        timestamp = time;
        position = pos;
        rotation = rot;
        gunRotation = gunRot;
        flipX = flip;
        isRunning = running;
        shouldShoot = shoot;
        shootDirection = shootDir;
    }
}

public class PlayerRecorder : MonoBehaviour, ITimeRewindable
{
    [Header("Recording Settings")]
    [Range(3f, 10f)]
    public float recordingDuration = 5f; // How many seconds to remember
    [Range(10, 60)]
    public int recordingsPerSecond = 30; // Recording frequency
    
    [Header("Ghost Settings")]
    public GameObject ghostPrefab; // The ghost player prefab
    public KeyCode spawnGhostKey = KeyCode.Mouse1; // Right click
    
    [Header("References")]
    public PlayerController playerController;
    public Transform gunTransform;
    public SpriteRenderer playerSpriteRenderer;
    public Health playerHealth;
    
    private Queue<PlayerAction> recordedActions = new Queue<PlayerAction>();
    private float recordingInterval;
    private float lastRecordTime;
    
    // Track shooting with frame-perfect accuracy
    private List<ShotRecord> shotsThisFrame = new List<ShotRecord>();
    
    [System.Serializable]
    public class ShotRecord
    {
        public Vector2 direction;
        public float timestamp;
        
        public ShotRecord(Vector2 dir, float time)
        {
            direction = dir;
            timestamp = time;
        }
    }
    
    private string entityId;
    
    void Start()
    {
        recordingInterval = 1f / recordingsPerSecond;
        entityId = System.Guid.NewGuid().ToString();
        
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
            
        if (playerHealth == null)
            playerHealth = GetComponent<Health>();
            
        // Register with TimeManager
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.RegisterEntity(this);
        }
    }
    
    void Update()
    {
        RecordPlayerActions();
        
        // Clear shots from last frame
        shotsThisFrame.Clear();
    }
    
    void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.UnregisterEntity(this);
        }
    }
    
    private void RecordPlayerActions()
    {
        if (Time.time - lastRecordTime >= recordingInterval)
        {
            // Record current state with all shots from this recording interval
            PlayerAction action = new PlayerAction(
                Time.time,
                transform.position,
                transform.rotation,
                gunTransform.eulerAngles,
                playerSpriteRenderer.flipX,
                IsPlayerRunning(),
                shotsThisFrame.Count > 0, // True if any shots this frame
                shotsThisFrame.Count > 0 ? shotsThisFrame[0].direction : Vector2.zero
            );
            
            recordedActions.Enqueue(action);
            lastRecordTime = Time.time;
            
            // Remove old actions beyond our recording duration
            while (recordedActions.Count > 0 && 
                   Time.time - recordedActions.Peek().timestamp > recordingDuration)
            {
                recordedActions.Dequeue();
            }
        }
    }
    
    private bool IsPlayerRunning()
    {
        // Get movement input to determine if running
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        return (horizontal * horizontal + vertical * vertical) > 0.01f;
    }
    
    public void OnPlayerShoot(Vector2 shootDirection)
    {
        shotsThisFrame.Add(new ShotRecord(shootDirection, Time.time));
    }
    
    public void SpawnGhostFromHistory()
    {
        if (ghostPrefab == null)
        {
            Debug.LogWarning("Ghost prefab not assigned!");
            return;
        }
        
        if (recordedActions.Count == 0)
        {
            Debug.Log("No recorded actions to replay!");
            return;
        }
        
        // Create a copy of the recorded actions for the ghost
        List<PlayerAction> actionsToReplay = new List<PlayerAction>(recordedActions);
        
        GameObject ghost = Instantiate(ghostPrefab, transform.position, transform.rotation);
        GhostPlayer ghostScript = ghost.GetComponent<GhostPlayer>();
        
        if (ghostScript != null)
        {
            ghostScript.Initialize(actionsToReplay);
        }
        else
        {
            Debug.LogError("Ghost prefab needs a GhostPlayer component!");
        }
    }
    
    // Call this method from your PlayerController's Shoot method
    public void NotifyShoot(Vector2 direction)
    {
        OnPlayerShoot(direction);
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
            GetComponent<Rigidbody2D>() != null ? GetComponent<Rigidbody2D>().velocity : Vector3.zero,
            gameObject.activeInHierarchy,
            playerHealth != null ? playerHealth.GetComponent<Health>().CurrentHealth : 100f
        );
        
        // Add player-specific data
        snapshot.flipX = playerSpriteRenderer.flipX;
        snapshot.isRunning = IsPlayerRunning();
        snapshot.gunRotation = gunTransform.eulerAngles;
        
        return snapshot;
    }
    
    public BulletSnapshot TakeBulletSnapshot()
    {
        // Players don't implement this - only bullets do
        return null;
    }
    
    public void RestoreFromSnapshot(EntitySnapshot snapshot)
    {
        // Players don't get rewound - only enemies do
        // The player stays in current time while ghost replays past actions
    }
    
    public bool IsActive()
    {
        return gameObject.activeInHierarchy;
    }
}