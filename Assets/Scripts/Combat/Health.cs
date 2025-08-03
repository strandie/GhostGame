using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Death Behavior")]
    public bool allowDestruction = true; // Set to false for enemies that need rewind

    [Header("Death Effects")]
    public ParticleSystem bloodParticlesPrefab; // Make sure this is a 2D-looking prefab


    public float CurrentHealth 
    {
        get => currentHealth;
        set => currentHealth = value;
    }

    public UnityEvent<float> onHealthChanged;
    public UnityEvent onDeath;

    public float MaxHealth => maxHealth;

    void Awake()
    {
        currentHealth = maxHealth;
        onHealthChanged.Invoke(currentHealth / maxHealth); // Initialize UI
        
        // Auto-detect if this is an enemy that should use rewind system
        if (GetComponent<RewindableEnemyController>() != null)
        {
            allowDestruction = false;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        onHealthChanged.Invoke(currentHealth / maxHealth);

        Debug.Log($"{gameObject.name} took {amount} damage from");

        DamageFlash flash = GetComponent<DamageFlash>();
        if (flash != null)
            flash.Flash();

        if (currentHealth <= 0f)
        {
            Die();
        }

    }

    private void Die()
    {
        onDeath.Invoke();

        if (bloodParticlesPrefab != null)
        {
            ParticleSystem blood = Instantiate(bloodParticlesPrefab, transform.position, Quaternion.identity);
            Destroy(blood.gameObject, blood.main.duration + blood.main.startLifetime.constantMax);
        }

        if (CompareTag("Player"))
        {
            // Load the Game Over scene (make sure it’s added to Build Settings)
            SceneManager.LoadScene("GameOverScene");
            return;
        }
        
        // Only destroy if allowed (players, non-rewindable objects)
        if (allowDestruction)
        {
            Destroy(gameObject);
        }
        // If allowDestruction is false, the EnemyDeathHandler will handle the death
    }
}