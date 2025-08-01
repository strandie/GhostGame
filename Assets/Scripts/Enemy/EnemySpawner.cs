using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // assign your enemy prefab here
    public int spawnCount = 2;
    public float spawnRadius = 5f;

    void Start()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Debug.Log("here");
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
            spawnPos.z = 0; // for 2D top-down games
            SpawnEnemy(spawnPos);
        }
    }

    void SpawnEnemy(Vector3 position)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);

        Health health = enemy.GetComponent<Health>();
        HealthBarUI hbui = enemy.GetComponentInChildren<HealthBarUI>();

        if (hbui != null && health != null)
        {
            hbui.health = health;
            hbui.slider.value = health.CurrentHealth / health.MaxHealth;
        }
    }
}

