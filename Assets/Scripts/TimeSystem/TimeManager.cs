
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    
    [Header("Cooldown Settings")]
    public float rewindCooldown = 10f;
    private float lastRewindTime = -10f; // Allow immediate first use
    
    [Header("Bullet Prefabs - Assign These!")]
    public GameObject playerBulletPrefab;
    public GameObject enemyBulletPrefab;
    
    [Header("Rewind Effects")]
    [Tooltip("Clock UI Image to animate")]
    public Image clockImage;
    [Tooltip("Wave filter GameObject with SpriteRenderer")]
    public SpriteRenderer waveFilter;
    [Tooltip("Audio source for rewind sound effect")]
    public AudioSource rewindAudioSource;
    [Tooltip("Sound clip to play during rewind")]
    public AudioClip rewindSoundClip;
    [Tooltip("Duration of the rewind effect in seconds")]
    public float rewindEffectDuration = 2f;
    [Tooltip("Wave effect properties")]
    public float waveSpeed = 5f;
    public float waveIntensity = 0.3f;
    public Color waveColor = new Color(0.2f, 0.6f, 1f, 0.4f); // Blue wave
    [Tooltip("Clock animation properties")]
    public float clockSpinSpeed = -3000f; // degrees per animation
    public float clockMaxScale = 1.5f;
    public float clockMinScale = 0.3f;
    
    private Queue<GameStateSnapshot> gameHistory = new Queue<GameStateSnapshot>();
    private float snapshotInterval;
    private float lastSnapshotTime;
    private bool isRewinding = false;
    
    private List<ITimeRewindable> rewindableEntities = new List<ITimeRewindable>();
    private List<ITimeRewindable> rewindableBullets = new List<ITimeRewindable>();
    
    // Effect state tracking
    private bool isPlayingRewindEffect = false;
    
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
        
        // Initialize effects
        InitializeRewindEffects();

        // Ensure clock starts invisible
        if (clockImage != null)
        {
            Color transparent = clockImage.color;
            transparent.a = 0f;
            clockImage.color = transparent;
        }
        
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
        if (!isRewinding && !isPlayingRewindEffect)
        {
            RecordGameState();
            
            if (Input.GetKeyDown(rewindKey))
            {
                TryStartRewind();
            }
        }
        
        // Update UI cooldown
        UpdateRewindUI();
    }
    
    private void InitializeRewindEffects()
    {
        // Auto-find components if not assigned
        if (clockImage == null)
        {
            // Try to find clock image in UI Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Transform clockTransform = canvas.transform.Find("ClockAnimation");
                if (clockTransform != null)
                {
                    clockImage = clockTransform.GetComponent<Image>();
                }
            }
        }
        
        if (waveFilter == null)
        {
            // Try to find wave filter GameObject
            GameObject waveObj = GameObject.Find("WaveFilter");
            if (waveObj != null)
            {
                waveFilter = waveObj.GetComponent<SpriteRenderer>();
            }
        }
        
        if (rewindAudioSource == null)
        {
            // Try to find audio source on this object or create one
            rewindAudioSource = GetComponent<AudioSource>();
            if (rewindAudioSource == null)
            {
                rewindAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Ensure wave filter starts invisible
        if (waveFilter != null)
        {
            Color transparent = waveColor;
            transparent.a = 0f;
            waveFilter.color = transparent;
        }
    }
    
    public void TryStartRewind()
    {
        float timeSinceLastRewind = Time.time - lastRewindTime;
        
        if (timeSinceLastRewind >= rewindCooldown)
        {
            // Successful rewind - play effects!
            StartCoroutine(PlayRewindEffectsAndRewind());
            lastRewindTime = Time.time;
        }
        else
        {
            float remainingCooldown = rewindCooldown - timeSinceLastRewind;
            Debug.Log($"Rewind on cooldown! {remainingCooldown:F1} seconds remaining");
        }
    }
    
    private IEnumerator PlayRewindEffectsAndRewind()
    {
        if (gameHistory.Count == 0) yield break;
        
        isPlayingRewindEffect = true;
        
        // Start all effects simultaneously
        StartCoroutine(PlayClockAnimation());
        StartCoroutine(PlayWaveEffect());
        PlayRewindSound();
        
        // Wait a brief moment for effect buildup
        yield return new WaitForSeconds(0.2f);
        
        // Perform the actual rewind
        StartRewind();
        
        // Wait for effects to finish
        yield return new WaitForSeconds(rewindEffectDuration - 0.2f);
        
        isPlayingRewindEffect = false;
    }
    
    private IEnumerator PlayClockAnimation()
    {
        if (clockImage == null)
        {
            Debug.LogWarning("Clock Image not assigned!");
            yield break;
        }
        
        RectTransform clockRect = clockImage.rectTransform;
        Vector3 originalScale = clockRect.localScale;
        float originalRotation = clockRect.eulerAngles.z;
        Color originalColor = clockImage.color;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < rewindEffectDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / rewindEffectDuration;
            
            // Rotation: spin continuously
            float currentRotation = originalRotation + (clockSpinSpeed * progress);
            clockRect.rotation = Quaternion.Euler(0, 0, currentRotation);
            
            // Scale: start small, grow big, shrink back
            float scaleProgress;
            if (progress < 0.5f)
            {
                // First half: grow from min to max
                scaleProgress = Mathf.Lerp(clockMinScale, clockMaxScale, progress * 2f);
            }
            else
            {
                // Second half: shrink from max to min
                scaleProgress = Mathf.Lerp(clockMaxScale, clockMinScale, (progress - 0.5f) * 2f);
            }
            clockRect.localScale = Vector3.one * scaleProgress;
            
            // Alpha: fade in quickly, stay visible, fade out
            float alpha;
            if (progress < 0.2f)
            {
                // Fade in
                alpha = progress / 0.2f;
            }
            else if (progress > 0.8f)
            {
                // Fade out
                alpha = 1f - ((progress - 0.8f) / 0.2f);
            }
            else
            {
                // Full visibility
                alpha = 1f;
            }
            
            Color currentColor = originalColor;
            currentColor.a = alpha;
            clockImage.color = currentColor;
            
            yield return null;
        }
        
        // Reset to original state
        clockRect.localScale = originalScale;
        clockRect.rotation = Quaternion.Euler(0, 0, originalRotation);
        Color transparent = originalColor;
        transparent.a = 0f;
        clockImage.color = transparent;
        
        Debug.Log("Clock animation completed");
    }
    
    private IEnumerator PlayWaveEffect()
    {
        if (waveFilter == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < rewindEffectDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Create wave effect using sine wave
            float wavePhase = elapsedTime * waveSpeed;
            float waveValue = Mathf.Sin(wavePhase) * 0.5f + 0.5f; // Normalize to 0-1
            
            // Create intensity envelope (fade in and out)
            float intensityEnvelope;
            if (elapsedTime < rewindEffectDuration * 0.3f)
            {
                // Fade in
                intensityEnvelope = elapsedTime / (rewindEffectDuration * 0.3f);
            }
            else if (elapsedTime > rewindEffectDuration * 0.7f)
            {
                // Fade out
                float fadeOutStart = rewindEffectDuration * 0.7f;
                float fadeOutDuration = rewindEffectDuration * 0.3f;
                intensityEnvelope = 1f - ((elapsedTime - fadeOutStart) / fadeOutDuration);
            }
            else
            {
                // Full intensity
                intensityEnvelope = 1f;
            }
            
            // Apply wave and intensity to alpha
            float finalAlpha = waveValue * waveIntensity * intensityEnvelope;
            Color currentColor = waveColor;
            currentColor.a = finalAlpha;
            waveFilter.color = currentColor;
            
            yield return null;
        }
        
        // Ensure it's fully transparent at the end
        Color transparent = waveColor;
        transparent.a = 0f;
        waveFilter.color = transparent;
    }
    
    private void PlayRewindSound()
    {
        if (rewindAudioSource != null && rewindSoundClip != null)
        {
            rewindAudioSource.clip = rewindSoundClip;
            rewindAudioSource.Play();
            Debug.Log("Rewind sound played");
        }
        else
        {
            Debug.LogWarning("Rewind audio source or clip not assigned!");
        }
    }
    
    private void UpdateRewindUI()
    {
        if (RewindUI.Instance != null)
        {
            float timeSinceLastRewind = Time.time - lastRewindTime;
            float remainingCooldown = Mathf.Max(0, rewindCooldown - timeSinceLastRewind);
            bool isOnCooldown = remainingCooldown > 0;
            
            RewindUI.Instance.UpdateCooldown(remainingCooldown, rewindCooldown, isOnCooldown);
        }
    }
    
    private void RecordGameState()
    {
        if (Time.time - lastSnapshotTime >= snapshotInterval)
        {
            GameStateSnapshot snapshot = new GameStateSnapshot(Time.time);
            
            // Record all entities (including enemies) - with null checking
            for (int i = rewindableEntities.Count - 1; i >= 0; i--)
            {
                var entity = rewindableEntities[i];
                
                // Remove null references
                if (entity == null)
                {
                    rewindableEntities.RemoveAt(i);
                    continue;
                }
                
                // Check if entity is still valid and active
                try
                {
                    if (entity.IsActive())
                    {
                        EntitySnapshot entitySnapshot = entity.TakeSnapshot();
                        if (entitySnapshot != null)
                        {
                            snapshot.entities.Add(entitySnapshot);
                        }
                    }
                }
                catch (MissingReferenceException)
                {
                    // Entity was destroyed, remove it from the list
                    rewindableEntities.RemoveAt(i);
                    continue;
                }
            }
            
            // Record all bullets - with null checking
            for (int i = rewindableBullets.Count - 1; i >= 0; i--)
            {
                var bullet = rewindableBullets[i];
                
                // Remove null references
                if (bullet == null)
                {
                    rewindableBullets.RemoveAt(i);
                    continue;
                }
                
                try
                {
                    if (bullet.IsActive())
                    {
                        BulletSnapshot bulletSnapshot = bullet.TakeBulletSnapshot();
                        if (bulletSnapshot != null)
                        {
                            snapshot.bullets.Add(bulletSnapshot);
                        }
                    }
                }
                catch (MissingReferenceException)
                {
                    // Bullet was destroyed, remove it from the list
                    rewindableBullets.RemoveAt(i);
                    continue;
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
                // Entity might be dead - try to revive it
                ReviveDeadEntity(entitySnapshot);
            }
        }
        
        Debug.Log($"Rewound {rewindedEntities} entities");
    }
    
    private void ReviveDeadEntity(EntitySnapshot snapshot)
    {
        // Look for dead enemies to revive
        if (snapshot.entityId.Contains("Enemy") || snapshot.health > 0)
        {
            // Try to find a dead enemy controller in the scene
            RewindableEnemyController[] allEnemies = FindObjectsOfType<RewindableEnemyController>(true); // Include inactive
            
            foreach (var enemy in allEnemies)
            {
                if (enemy.GetEntityId() == snapshot.entityId)
                {
                    // Found the dead enemy - revive it
                    enemy.gameObject.SetActive(true);
                    enemy.RestoreFromSnapshot(snapshot);
                    Debug.Log($"Revived dead enemy: {enemy.name}");
                    return;
                }
            }
            
            // If we can't find the enemy, it might have been destroyed
            // In that case, we'd need to recreate it from a prefab
            // This would require storing enemy prefab references
            Debug.LogWarning($"Could not revive entity with ID: {snapshot.entityId}");
        }
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
        // Remove from list when entity is destroyed
        rewindableEntities.Remove(entity);
        Debug.Log($"Entity unregistered: {entity?.GetType().Name}");
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