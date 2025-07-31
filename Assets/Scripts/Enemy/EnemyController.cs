using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 1f;
    private float shootTimer;
    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (!player) return;

        // Move toward player
        Vector2 dir = (player.position - transform.position).normalized;
        transform.position += (Vector3)dir * moveSpeed * Time.deltaTime;

        // Face and shoot
        Vector2 shootDir = player.position - firePoint.position;
        firePoint.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg);

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            Shoot(shootDir.normalized);
            shootTimer = shootCooldown;
        }
    }

    void Shoot(Vector2 dir)
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<EnemyBullet>().Initialize(dir, 10f); // 10 dmg
    }
}
