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

public class PlayerRecorder : MonoBehaviour
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
    
    private Queue<PlayerAction> recordedActions = new Queue<PlayerAction>();
    private float recordingInterval;
    private float lastRecordTime;
    
    // Track shooting
    private bool shotThisFrame = false;
    private Vector2 lastShootDirection;
    
    void Start()
    {
        recordingInterval = 1f / recordingsPerSecond;
        
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }
    
    void Update()
    {
        RecordPlayerActions();
        
        if (Input.GetKeyDown(spawnGhostKey))
        {
            SpawnGhost();
        }
        
        // Reset shoot tracking
        shotThisFrame = false;
    }
    
    private void RecordPlayerActions()
    {
        if (Time.time - lastRecordTime >= recordingInterval)
        {
            // Record current state
            PlayerAction action = new PlayerAction(
                Time.time,
                transform.position,
                transform.rotation,
                gunTransform.eulerAngles,
                playerSpriteRenderer.flipX,
                IsPlayerRunning(),
                shotThisFrame,
                lastShootDirection
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
        shotThisFrame = true;
        lastShootDirection = shootDirection;
    }
    
    private void SpawnGhost()
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
}