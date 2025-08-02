using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameStateSnapshot
{
    public float timestamp;
    public List<EntitySnapshot> entities = new List<EntitySnapshot>();
    public List<BulletSnapshot> bullets = new List<BulletSnapshot>();
    
    public GameStateSnapshot(float time)
    {
        timestamp = time;
    }
}

[System.Serializable]
public class EntitySnapshot
{
    public string entityId;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public bool isActive;
    public float health;
    
    // Player specific
    public bool flipX;
    public bool isRunning;
    public Vector3 gunRotation;
    
    // Enemy specific
    public Transform currentTarget;
    public float shootTimer;
    
    public EntitySnapshot(string id, Vector3 pos, Quaternion rot, Vector3 vel, bool active, float hp)
    {
        entityId = id;
        position = pos;
        rotation = rot;
        velocity = vel;
        isActive = active;
        health = hp;
    }
}

[System.Serializable]
public class BulletSnapshot
{
    public string bulletId;
    public Vector3 position;
    public Vector2 direction;
    public float speed;
    public float damage;
    public float remainingLifetime;
    public bool isPlayerBullet;
    public string ownerType; // "Player", "Ghost", "Enemy"
    
    public BulletSnapshot(string id, Vector3 pos, Vector2 dir, float spd, float dmg, float lifetime, bool isPlayer, string owner)
    {
        bulletId = id;
        position = pos;
        direction = dir;
        speed = spd;
        damage = dmg;
        remainingLifetime = lifetime;
        isPlayerBullet = isPlayer;
        ownerType = owner;
    }
}

[System.Serializable]
public class ShotRecord
{
    public Vector2 direction;
    public float timestamp;
    
    public ShotRecord(Vector2 dir, float time)
    {
        direction = dir;
        timestamp = time;
    }
}