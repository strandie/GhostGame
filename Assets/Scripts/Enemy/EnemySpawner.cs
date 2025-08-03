using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;

    [Header("Group Spawning")]
    public int totalGroups = 3;
    public int minPerGroup = 3;
    public int maxPerGroup = 6;
    public float groupRadius = 2f;

    private bool[,] walkable;
    private int mapWidth, mapHeight;
    private Vector2Int worldOffset;

    private List<GameObject> activeEnemies = new();

    public void Update()
    {
        Debug.Log(activeEnemies.Count);
    }

    public void SpawnEnemies(bool[,] walkableMap, int mapWidth, int mapHeight, Vector2Int offset)
    {
        this.walkable = walkableMap;
        this.mapWidth = mapWidth;
        this.mapHeight = mapHeight;
        this.worldOffset = offset;

        activeEnemies.Clear();

        int groupAttempts = 0;
        int groupsSpawned = 0;

        List<Vector3> spawnedPositions = new();

        while (groupsSpawned < totalGroups && groupAttempts < 1000)
        {
            groupAttempts++;

            int x = Random.Range(0, mapWidth);
            int y = Random.Range(0, mapHeight);

            if (!walkable[x, y])
                continue;

            Vector3 groupCenter = new Vector3(x + worldOffset.x + 0.5f, y + worldOffset.y + 0.5f, 0);
            int enemiesInGroup = Random.Range(minPerGroup, maxPerGroup + 1);

            int enemiesSpawned = 0;
            int subgroupAttempts = 0;

            while (enemiesSpawned < enemiesInGroup && subgroupAttempts < 50)
            {
                subgroupAttempts++;

                Vector2 offsetPos = Random.insideUnitCircle * groupRadius;
                Vector3 spawnPos = groupCenter + new Vector3(offsetPos.x, offsetPos.y, 0);

                int cellX = Mathf.FloorToInt(spawnPos.x - worldOffset.x);
                int cellY = Mathf.FloorToInt(spawnPos.y - worldOffset.y);

                if (cellX >= 0 && cellX < mapWidth && cellY >= 0 && cellY < mapHeight && walkable[cellX, cellY])
                {
                    Collider2D hit = Physics2D.OverlapCircle(spawnPos, 0.4f, LayerMask.GetMask("Terrain"));
                    if (hit != null) continue;

                    bool tooClose = false;
                    foreach (var pos in spawnedPositions)
                    {
                        if (Vector2.Distance(pos, spawnPos) < 0.9f)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                    {
                        GameObject enemy = SpawnEnemy(spawnPos);
                        activeEnemies.Add(enemy);
                        spawnedPositions.Add(spawnPos);
                        enemiesSpawned++;
                    }
                }
            }

            if (enemiesSpawned > 0)
                groupsSpawned++;
        }

        Debug.Log($"Spawned {groupsSpawned} groups of enemies.");
        StartCoroutine(CheckEnemiesRoutine());
    }

    GameObject SpawnEnemy(Vector3 position)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        Health health = enemy.GetComponent<Health>();
        HealthBarUI hbui = enemy.GetComponentInChildren<HealthBarUI>();

        if (hbui != null && health != null)
        {
            hbui.health = health;
            hbui.slider.value = health.CurrentHealth / health.MaxHealth;
        }

        return enemy;
    }

    IEnumerator CheckEnemiesRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            activeEnemies.RemoveAll(enemy =>
            {
                if (enemy == null) return true;

                var controller = enemy.GetComponent<RewindableEnemyController>();
                return controller == null || !controller.IsActive();  // <-- use IsActive
            });

            if (activeEnemies.Count == 0)
            {
                Debug.Log("All enemies eliminated. Regenerating stage.");
                StageGenerator stage = FindObjectOfType<StageGenerator>();
                if (stage != null)
                    stage.RegenerateStage();
                yield break;
            }
        }
    }
}