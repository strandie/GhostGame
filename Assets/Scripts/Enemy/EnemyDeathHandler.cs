using UnityEngine;

public class EnemyDeathHandler : MonoBehaviour
{
    [Header("Death Settings")]
    public bool useDeactivation = true; // Set to true for rewind system
    public float destroyDelay = 5f; // Time before actual destruction (if not using deactivation)
    
    private Health enemyHealth;
    private RewindableEnemyController enemyController;
    private bool isDead = false;
    private bool originalDestroyBehavior = false;
    
    void Start()
    {
        enemyHealth = GetComponent<Health>();
        enemyController = GetComponent<RewindableEnemyController>();
        
        if (enemyHealth != null)
        {
            // Subscribe to UnityEvent death event
            enemyHealth.onDeath.AddListener(HandleDeath);
        }
    }
    
    void Update()
    {
        // Check if enemy should die (backup check)
        if (!isDead && enemyHealth != null && enemyHealth.CurrentHealth <= 0)
        {
            HandleDeath();
        }
    }
    
    private void HandleDeath()
    {
        if (isDead) return; // Already handled
        
        isDead = true;
        
        Debug.Log($"Enemy {name} died");
        
        if (useDeactivation)
        {
            // Deactivate for rewind system
            DeactivateEnemy();
        }
        else
        {
            // Let the original Health script handle destruction
            // (This method won't be called if Health.Die() destroys first)
        }
    }
    
    private void DeactivateEnemy()
    {
        // Disable components but keep GameObject active for rewind
        var colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        
        // Disable renderers
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        // Disable movement and behavior
        if (enemyController != null)
        {
            enemyController.enabled = false;
        }
        
        // Stop rigidbody movement
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }
        
        // Keep the GameObject active but make it non-functional
        // This allows TimeManager to still access it for rewind
        
        Debug.Log($"Enemy {name} deactivated for rewind system");
    }
    
    public void Revive()
    {
        if (!isDead) return;
        
        isDead = false;
        
        Debug.Log($"Enemy {name} revived");
        
        // Re-enable all components
        var colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = true;
        }
        
        // Re-enable renderers
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = true;
        }
        
        // Re-enable controller
        if (enemyController != null)
        {
            enemyController.enabled = true;
        }
        
        // Re-enable rigidbody
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
        }
        
        gameObject.SetActive(true);
        
        // Trigger health changed event to update UI
        if (enemyHealth != null)
        {
            enemyHealth.onHealthChanged.Invoke(enemyHealth.CurrentHealth / enemyHealth.MaxHealth);
        }
    }
    
    public bool IsDead()
    {
        return isDead;
    }
    
    void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.onDeath.RemoveListener(HandleDeath);
        }
    }
}