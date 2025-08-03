using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleShotPattern : ShootingPattern
{
    private float shootCooldown;
    private float inaccuracy;
    
    public SingleShotPattern(Transform firePoint, EnemyConfiguration config, float cooldown, float inaccuracy) 
        : base(firePoint, config)
    {
        this.shootCooldown = cooldown;
        this.inaccuracy = inaccuracy;
    }
    
    public override bool CanShoot(Transform target)
    {
        return Time.time - lastShotTime >= shootCooldown && HasLineOfSight(target);
    }
    
    public override void Shoot(Transform target, MonoBehaviour coroutineRunner)
    {
        Vector2 direction = (target.position - firePoint.position).normalized;
        
        // Apply inaccuracy
        if (inaccuracy > 0)
        {
            float randomAngle = Random.Range(-inaccuracy / 2f, inaccuracy / 2f);
            direction = Quaternion.Euler(0, 0, randomAngle) * direction;
        }
        
        CreateBullet(direction, firePoint.position);
        lastShotTime = Time.time;
    }
}