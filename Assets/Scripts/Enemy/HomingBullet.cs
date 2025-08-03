using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingBullet : MonoBehaviour
{
    private Transform target;
    private float homingStrength = 2f;
    private float homingDuration = 3f;
    private float homingTimer;
    private bool isHoming = true;
    
    private Rigidbody2D rb;
    private Vector2 currentDirection;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }
    }
    
    public void SetHomingProperties(Transform homingTarget, float strength, float duration)
    {
        target = homingTarget;
        homingStrength = strength;
        homingDuration = duration;
        homingTimer = duration;
        isHoming = true;
        
        // Get initial direction from bullet component
        RewindableEnemyBullet bulletScript = GetComponent<RewindableEnemyBullet>();
        if (bulletScript != null)
        {
            // We'll need to modify RewindableEnemyBullet to expose direction
            currentDirection = transform.right; // Fallback
        }
    }
    
    void Update()
    {
        if (!isHoming || target == null) return;
        
        homingTimer -= Time.deltaTime;
        if (homingTimer <= 0f)
        {
            isHoming = false;
            return;
        }
        
        // Calculate homing force
        Vector2 directionToTarget = (target.position - transform.position).normalized;
        currentDirection = Vector2.Lerp(currentDirection, directionToTarget, homingStrength * Time.deltaTime);
        currentDirection.Normalize();
        
        // Update rotation to match direction
        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Apply the new direction to the bullet's movement
        // This requires coordination with the bullet's movement system
        RewindableEnemyBullet bulletScript = GetComponent<RewindableEnemyBullet>();
        if (bulletScript != null)
        {
            bulletScript.SetDirection(currentDirection);
        }
    }
}