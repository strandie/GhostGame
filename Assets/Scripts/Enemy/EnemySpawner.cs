using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int spawnCount = 2;

    private bool[,] walkable;
    private int mapWidth, mapHeight;
    private Vector2Int worldOffset;

    public void SpawnEnemies(bool[,] walkableMap, int mapWidth, int mapHeight, Vector2Int offset)
    {
        this.walkable = walkableMap;
        this.mapWidth = mapWidth;
        this.mapHeight = mapHeight;
        this.worldOffset = offset;

        int attempts = 0;
        int spawned = 0;

        while (spawned < spawnCount && attempts < 1000)
        {
            attempts++;
            int x = Random.Range(0, mapWidth);
            int y = Random.Range(0, mapHeight);

            if (walkable[x, y])
            {
                Vector3 spawnPos = new Vector3(x + worldOffset.x + 0.5f, y + worldOffset.y + 0.5f, 0);
                SpawnEnemy(spawnPos);
                spawned++;
            }
        }

        Debug.Log($"Spawned {spawned} enemies.");
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