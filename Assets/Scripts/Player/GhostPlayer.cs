using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostPlayer : MonoBehaviour
{
    [Header("Ghost Settings")]
    public float playbackSpeed = 1f; // Speed multiplier for replay
    public Color ghostColor = new Color(1f, 1f, 1f, 0.5f); // Semi-transparent
    
    [Header("Combat Settings")]
    public bool canTakeDamage = true;
    public bool canDealDamage = true;
    public float ghostHealth = 100f; // Same as player or different
    public LayerMask enemyLayers = -1; // What layers this ghost can damage
    
    [Header("References")]
    public SpriteRenderer ghostSpriteRenderer;
    public SpriteRenderer ghostGunSpriteRenderer;
    public Transform ghostGunTransform;
    public Transform ghostFirePoint;
    public GameObject ghostBulletPrefab; // Use same bullet prefab as player
    public Animator ghostAnimator;
    public DamageFlash damageFlash; // For damage feedback
    public Collider2D ghostCollider; // For taking damage
    
    [Header("Auto Destroy")]
    public bool autoDestroy = true;
    public float destroyDelay = 1f; // Extra time after replay finishes
    
    private List<PlayerAction> actionsToReplay;
    private int currentActionIndex = 0;
    private float replayStartTime;
    private bool isReplaying = false;
    private float currentHealth;
    
    void Start()
    {
        currentHealth = ghostHealth;
        
        // Make ghost semi-transparent but keep it visible
        if (ghostSpriteRenderer != null)
        {
            ghostSpriteRenderer.color = ghostColor;
        }
        
        if (ghostGunSpriteRenderer != null)
        {
            ghostGunSpriteRenderer.color = ghostColor;
        }
        
        // Ensure collider is enabled for damage
        if (ghostCollider == null)
            ghostCollider = GetComponent<Collider2D>();
            
        if (damageFlash == null)
            damageFlash = GetComponent<DamageFlash>();
    }
    
    public void Initialize(List<PlayerAction> actions)
    {
        actionsToReplay = new List<PlayerAction>(actions);
        
        if (actionsToReplay.Count > 0)
        {
            replayStartTime = Time.time;
            isReplaying = true;
            
            // Set initial position to first recorded action
            PlayerAction firstAction = actionsToReplay[0];
            transform.position = firstAction.position;
            transform.rotation = firstAction.rotation;
            
            if (ghostGunTransform != null)
                ghostGunTransform.eulerAngles = firstAction.gunRotation;
                
            if (ghostSpriteRenderer != null)
                ghostSpriteRenderer.flipX = firstAction.flipX;
        }
    }
    
    void Update()
    {
        if (!isReplaying || actionsToReplay == null || actionsToReplay.Count == 0)
            return;
            
        ReplayActions();
    }
    
    private void ReplayActions()
    {
        if (currentActionIndex >= actionsToReplay.Count)
        {
            // Replay finished
            isReplaying = false;
            
            if (autoDestroy)
            {
                Destroy(gameObject, destroyDelay);
            }
            return;
        }

        PlayerAction currentAction = actionsToReplay[currentActionIndex];
        
        // Calculate the time this action should be played based on the original timestamp
        float originalActionTime = currentAction.timestamp - actionsToReplay[0].timestamp;
        float targetPlayTime = originalActionTime / playbackSpeed;
        float currentPlayTime = Time.time - replayStartTime;
        
        // Check if it's time to execute this action
        if (currentPlayTime >= targetPlayTime)
        {
            ExecuteAction(currentAction);
            currentActionIndex++;
        }
        
        // Interpolate position between actions for smooth movement
        if (currentActionIndex < actionsToReplay.Count)
        {
            PlayerAction nextAction = actionsToReplay[currentActionIndex];
            float nextActionTime = (nextAction.timestamp - actionsToReplay[0].timestamp) / playbackSpeed;
            
            if (nextActionTime > targetPlayTime)
            {
                float lerpFactor = (currentPlayTime - targetPlayTime) / (nextActionTime - targetPlayTime);
                lerpFactor = Mathf.Clamp01(lerpFactor);
                
                // Smooth position interpolation
                transform.position = Vector3.Lerp(currentAction.position, nextAction.position, lerpFactor);
            }
        }
    }
    
    private void ExecuteAction(PlayerAction action)
    {
        // Set position and rotation
        transform.position = action.position;
        transform.rotation = action.rotation;
        
        // Set gun rotation
        if (ghostGunTransform != null)
        {
            ghostGunTransform.eulerAngles = action.gunRotation;
        }
        
        // Set sprite flip
        if (ghostSpriteRenderer != null)
        {
            ghostSpriteRenderer.flipX = action.flipX;
        }
        
        // Set gun sprite flip
        if (ghostGunSpriteRenderer != null)
        {
            ghostGunSpriteRenderer.flipY = action.flipX;
        }
        
        // Handle animation
        if (ghostAnimator != null)
        {
            ghostAnimator.SetBool("IsRunning", action.isRunning);
        }
        
        // Handle shooting
        if (action.shouldShoot)
        {
            GhostShoot(action.shootDirection);
        }
    }
    
    private void GhostShoot(Vector2 direction)
    {
        if (ghostBulletPrefab == null || ghostFirePoint == null)
            return;
            
        Vector2 spawnPos = (Vector2)ghostFirePoint.position + direction * 0.2f;
        GameObject bullet = Instantiate(ghostBulletPrefab, spawnPos, Quaternion.identity);
        
        // Initialize bullet with same damage as player
        PlayerBullet bulletScript = bullet.GetComponent<PlayerBullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(direction);
            
            // Optional: Tag ghost bullets differently if needed
            bullet.tag = "GhostBullet"; // You can create this tag
        }
        
        // Optional: Make ghost bullets slightly more transparent
        SpriteRenderer bulletRenderer = bullet.GetComponent<SpriteRenderer>();
        if (bulletRenderer != null)
        {
            Color bulletColor = bulletRenderer.color;
            bulletColor.a *= 0.8f; // Make bullets slightly more transparent
            bulletRenderer.color = bulletColor;
        }
    }
    
    // Method to take damage - call this from your damage system
    public void TakeDamage(float damage)
    {
        if (!canTakeDamage) return;
        
        currentHealth -= damage;
        
        // Flash effect when taking damage
        if (damageFlash != null)
        {
            damageFlash.Flash();
        }
        
        Debug.Log($"Ghost took {damage} damage! Health: {currentHealth}");
        
        // Check if ghost should die
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        // Stop replaying
        isReplaying = false;
        
        // Optional: Death effect here (particles, sound, etc.)
        
        // Destroy the ghost
        Destroy(gameObject);
    }
    
    // Method to check if ghost can damage a target
    public bool CanDamage(GameObject target)
    {
        if (!canDealDamage) return false;
        
        // Check if target is on a layer this ghost can damage
        int targetLayer = target.layer;
        return (enemyLayers.value & (1 << targetLayer)) > 0;
    }
    
    // Optional: Visual effect when ghost spawns
    void OnEnable()
    {
        // You could add a spawn effect here
    }
}