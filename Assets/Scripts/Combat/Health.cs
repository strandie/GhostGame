using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public UnityEvent<float> onHealthChanged;
    public UnityEvent onDeath;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    void Awake()
    {
        currentHealth = maxHealth;
        onHealthChanged.Invoke(currentHealth / maxHealth); // Initialize UI
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        onHealthChanged.Invoke(currentHealth / maxHealth);

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
        Destroy(gameObject); 
    }
}
