using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{

    private Rigidbody2D rb;

    [Header("Movement")]
    public float moveSpeed = 1f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 0.5f;
    private float shootTimer;

    [Header("Targeting")]
    private Transform currentTarget;
    private Transform lastDamagedBy;

    private static List<Transform> allPlayers = new();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        UpdatePlayerList();
    }

    void Update()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            currentTarget = GetTarget();

        AimAndShoot(currentTarget);
    }

    void FixedUpdate()
    {
        if (currentTarget == null)
            return;

        MoveToward(currentTarget);
    }

    // Self Explanatory
    void MoveToward(Transform target)
    {
        Vector2 dir = (target.position - transform.position).normalized;
        Vector2 nextPos = rb.position + dir * moveSpeed * Time.deltaTime;
        rb.MovePosition(nextPos);
    }

    // Self Explanatory
    void AimAndShoot(Transform target)
    {
        Vector2 shootDir = target.position - firePoint.position;
        firePoint.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg);

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            Shoot(shootDir.normalized);
            shootTimer = shootCooldown;
        }
    }

    // Self Explanatory
    void Shoot(Vector2 dir)
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<EnemyBullet>().Initialize(dir, 10f); // I'll make this a var later
    }

    // Choose target based on who damage most recently otherwise does closest
    Transform GetTarget()
    {
        if (lastDamagedBy != null && lastDamagedBy.gameObject.activeInHierarchy)
            return lastDamagedBy;

        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (var p in allPlayers)
        {
            if (p == null || !p.gameObject.activeInHierarchy) continue;

            float dist = Vector2.Distance(transform.position, p.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = p;
            }
        }

        return closest;
    }


    // Simple helpers
    public void SetLastDamagedBy(Transform player)
    {
        lastDamagedBy = player;
    }

    public static void RegisterPlayer(Transform player)
    {
        if (!allPlayers.Contains(player))
            allPlayers.Add(player);
    }

    public static void UnregisterPlayer(Transform player)
    {
        allPlayers.Remove(player);
    }

    void UpdatePlayerList()
    {
        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            RegisterPlayer(go.transform);
        }
    }
}
