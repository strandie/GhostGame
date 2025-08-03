// EnemyConfiguration.cs - Scriptable Object for enemy stats
using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Config", menuName = "Enemy System/Enemy Configuration")]
public class EnemyConfiguration : ScriptableObject
{
    [Header("Basic Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 2f;
    public float visionRange = 10f;
    
    [Header("Movement Behavior")]
    public float wanderChangeInterval = 2f;
    public float chasePauseInterval = 2f;
    public float chasePauseDuration = 0.5f;
    
    [Header("Shooting Configuration")]
    public ShootingPatternData shootingPattern;
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float bulletDamage = 25f;
    
    [Header("AI Behavior")]
    public bool canWander = true;
    public bool canChase = true;
    public bool prioritizeDamager = true;
    public float loseTargetTime = 3f; // Time to lose target when out of sight
}