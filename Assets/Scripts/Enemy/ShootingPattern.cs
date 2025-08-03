using UnityEngine;

public abstract class ShootingPattern
{
    protected Transform firePoint;
    protected EnemyConfiguration config;
    protected float lastShotTime;
    protected bool isActive = true;
    
    public ShootingPattern(Transform firePoint, EnemyConfiguration config)
    {
        this.firePoint = firePoint;
        this.config = config;
    }
    
    public abstract bool CanShoot(Transform target);
    public abstract void Shoot(Transform target, MonoBehaviour coroutineRunner);
    
    protected void CreateBullet(Vector2 direction, Vector2 position)
    {
        if (config.bulletPrefab == null) return;
        
        GameObject bullet = Object.Instantiate(config.bulletPrefab, position, Quaternion.identity);
        RewindableEnemyBullet bulletScript = bullet.GetComponent<RewindableEnemyBullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(direction, config.bulletDamage);
        }
    }
    
    protected bool HasLineOfSight(Transform target)
    {
        if (!config.shootingPattern.requiresLineOfSight) return true;
        
        Vector2 start = firePoint.position;
        Vector2 end = target.position;
        Vector2 dir = (end - start).normalized;
        float dist = Vector2.Distance(start, end);
        
        LayerMask blockingLayer = LayerMask.GetMask("Terrain", "Default");
        RaycastHit2D hit = Physics2D.Raycast(start, dir, dist, blockingLayer);
        
        if (hit.collider != null)
        {
            return hit.collider.transform == target || 
                   hit.collider.CompareTag("Player") || 
                   hit.collider.CompareTag("Ghost");
        }
        
        return true;
    }
}