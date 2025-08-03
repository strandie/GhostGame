using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostPlayer : MonoBehaviour
{
    [Header("Ghost Settings")]
    public float playbackSpeed = 1f;
    public Color ghostColor = new Color(1f, 1f, 1f, 0.5f);
    
    [Header("Combat Settings")]
    public bool canTakeDamage = true;
    public bool canDealDamage = true;
    public float ghostHealth = 100f;
    public LayerMask enemyLayers = -1;
    
    [Header("References")]
    public SpriteRenderer ghostSpriteRenderer;
    public SpriteRenderer ghostGunSpriteRenderer;
    public Transform ghostGunTransform;
    public Transform ghostFirePoint;
    public GameObject ghostBulletPrefab;
    public Animator ghostAnimator;
    public DamageFlash damageFlash;
    public Collider2D ghostCollider;
    
    [Header("Auto Destroy")]
    public bool autoDestroy = true;
    public float destroyDelay = 1f;
    
    private List<PlayerAction> actionsToReplay;
    private int currentActionIndex = 0;
    private float replayStartTime;
    private bool isReplaying = false;
    private float currentHealth;
    
    // Track which shots have been executed
    private HashSet<float> executedShotTimestamps = new HashSet<float>();
    
    void Start()
    {
        currentHealth = ghostHealth;
        
        if (ghostSpriteRenderer != null)
        {
            ghostSpriteRenderer.color = ghostColor;
        }
        
        if (ghostGunSpriteRenderer != null)
        {
            ghostGunSpriteRenderer.color = ghostColor;
        }
        
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
            isReplaying = false;
            
            if (autoDestroy)
            {
                Destroy(gameObject, destroyDelay);
            }
            return;
        }

        PlayerAction currentAction = actionsToReplay[currentActionIndex];
        
        float originalActionTime = currentAction.timestamp - actionsToReplay[0].timestamp;
        float targetPlayTime = originalActionTime / playbackSpeed;
        float currentPlayTime = Time.time - replayStartTime;
        
        if (currentPlayTime >= targetPlayTime)
        {
            ExecuteAction(currentAction);
            currentActionIndex++;
        }
        
        // Execute any shots that should happen now from ANY action
        ExecuteShotsAtCurrentTime(currentPlayTime);
        
        // Interpolate position between actions for smooth movement
        if (currentActionIndex < actionsToReplay.Count)
        {
            PlayerAction nextAction = actionsToReplay[currentActionIndex];
            float nextActionTime = (nextAction.timestamp - actionsToReplay[0].timestamp) / playbackSpeed;
            
            if (nextActionTime > targetPlayTime)
            {
                float lerpFactor = (currentPlayTime - targetPlayTime) / (nextActionTime - targetPlayTime);
                lerpFactor = Mathf.Clamp01(lerpFactor);
                
                transform.position = Vector3.Lerp(currentAction.position, nextAction.position, lerpFactor);
            }
        }
    }
    
    private void ExecuteShotsAtCurrentTime(float currentPlayTime)
    {
        // Check all actions for shots that should happen now
        foreach (var action in actionsToReplay)
        {
            if (action.shots == null) continue;
            
            foreach (var shot in action.shots)
            {
                float shotTime = (shot.timestamp - actionsToReplay[0].timestamp) / playbackSpeed;
                
                // If it's time for this shot and we haven't executed it yet
                if (currentPlayTime >= shotTime && !executedShotTimestamps.Contains(shot.timestamp))
                {
                    GhostShoot(shot.direction);
                    executedShotTimestamps.Add(shot.timestamp);
                }
            }
        }
    }
    
    private void ExecuteAction(PlayerAction action)
    {
        transform.position = action.position;
        transform.rotation = action.rotation;
        
        if (ghostGunTransform != null)
        {
            ghostGunTransform.eulerAngles = action.gunRotation;
        }
        
        if (ghostSpriteRenderer != null)
        {
            ghostSpriteRenderer.flipX = action.flipX;
        }
        
        if (ghostGunSpriteRenderer != null)
        {
            ghostGunSpriteRenderer.flipY = action.flipX;
        }
        
        if (ghostAnimator != null)
        {
            ghostAnimator.SetBool("IsRunning", action.isRunning);
        }
        
        // Note: Shots are now handled separately in ExecuteShotsAtCurrentTime
    }
    
    // Replace the GhostShoot method in GhostPlayer with this:
    private void GhostShoot(Vector2 direction)
    {
        if (ghostBulletPrefab == null || ghostFirePoint == null)
            return;
            
        Vector2 spawnPos = (Vector2)ghostFirePoint.position + direction * 0.2f;
        GameObject bullet = Instantiate(ghostBulletPrefab, spawnPos, Quaternion.identity);
        
        RewindablePlayerBullet bulletScript = bullet.GetComponent<RewindablePlayerBullet>();
        if (bulletScript != null)
        {
            // Mark this bullet as coming from a ghost
            bulletScript.Initialize(direction, true); // true = fromGhost
            bullet.tag = "GhostBullet";
        }
        
        // Make ghost bullets slightly more transparent
        SpriteRenderer bulletRenderer = bullet.GetComponent<SpriteRenderer>();
        if (bulletRenderer != null)
        {
            Color bulletColor = bulletRenderer.color;
            bulletColor.a *= 0.8f;
            bulletRenderer.color = bulletColor;
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (!canTakeDamage) return;
        
        currentHealth -= damage;
        
        if (damageFlash != null)
        {
            damageFlash.Flash();
        }
        
        Debug.Log($"Ghost took {damage} damage! Health: {currentHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        isReplaying = false;
        Destroy(gameObject);
    }
    
    public bool CanDamage(GameObject target)
    {
        if (!canDealDamage) return false;
        
        int targetLayer = target.layer;
        return (enemyLayers.value & (1 << targetLayer)) > 0;
    }
    
    void OnEnable()
    {
        // Spawn effect could go here
    }
}