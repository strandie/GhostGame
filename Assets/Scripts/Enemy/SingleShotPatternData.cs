using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Single Shot Pattern", menuName = "Enemy System/Shooting Patterns/Single Shot")]
public class SingleShotPatternData : ShootingPatternData
{
    [Header("Single Shot Settings")]
    public float shootCooldown = 0.8f;
    public float inaccuracy = 0f; // Degrees of random spread
    
    public override ShootingPattern CreatePattern(Transform firePoint, EnemyConfiguration config)
    {
        return new SingleShotPattern(firePoint, config, shootCooldown, inaccuracy);
    }
}