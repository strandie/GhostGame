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
    public List<ShotRecord> shots; // Changed to support multiple shots
    
    public PlayerAction(float time, Vector3 pos, Quaternion rot, Vector3 gunRot, bool flip, bool running, List<ShotRecord> shotList)
    {
        timestamp = time;
        position = pos;
        rotation = rot;
        gunRotation = gunRot;
        flipX = flip;
        isRunning = running;
        shots = new List<ShotRecord>(shotList); // Copy the list
    }
}

public class PlayerRecorder : MonoBehaviour, ITimeRewindable
{
    [Header("Recording Settings")]
    [Range(3f, 10f)]
    public float recordingDuration = 5f;
    [Range(10, 60)]
    public int recordingsPerSecond = 30;
    
    [Header("Ghost Settings")]
    public GameObject ghostPrefab;
    public KeyCode spawnGhostKey = KeyCode.Mouse1;
    
    [Header("References")]
    public PlayerController playerController;
    public Transform gunTransform;
    public SpriteRenderer playerSpriteRenderer;
    public Health playerHealth;
    
    private Queue<PlayerAction> recordedActions = new Queue<PlayerAction>();
    private float recordingInterval;
    private float lastRecordTime;
    
    // Track all shots since last recording
    private List<ShotRecord> shotsSinceLastRecord = new List<ShotRecord>();
    
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
            // Record current state with ALL shots since last recording
            PlayerAction action = new PlayerAction(
                Time.time,
                transform.position,
                transform.rotation,
                gunTransform.eulerAngles,
                playerSpriteRenderer.flipX,
                IsPlayerRunning(),
                shotsSinceLastRecord // Pass all shots
            );
            
            recordedActions.Enqueue(action);
            lastRecordTime = Time.time;
            
            // Clear shots for next interval
            shotsSinceLastRecord.Clear();
            
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
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        return (horizontal * horizontal + vertical * vertical) > 0.01f;
    }
    
    public void OnPlayerShoot(Vector2 shootDirection)
    {
        shotsSinceLastRecord.Add(new ShotRecord(shootDirection, Time.time));
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
        
        snapshot.flipX = playerSpriteRenderer.flipX;
        snapshot.isRunning = IsPlayerRunning();
        snapshot.gunRotation = gunTransform.eulerAngles;
        
        return snapshot;
    }
    
    public BulletSnapshot TakeBulletSnapshot()
    {
        return null;
    }
    
    public void RestoreFromSnapshot(EntitySnapshot snapshot)
    {
        // Players don't get rewound - only enemies do
    }
    
    public bool IsActive()
    {
        return gameObject.activeInHierarchy;
    }
}