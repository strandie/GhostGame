using UnityEngine;

[System.Serializable]
public abstract class ShootingPatternData : ScriptableObject
{
    [Header("Base Pattern Settings")]
    public float baseCooldown = 1f;
    public bool requiresLineOfSight = true;
    
    public abstract ShootingPattern CreatePattern(Transform firePoint, EnemyConfiguration config);
}