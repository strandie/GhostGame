using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class StageGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;    // Decorative
    public Tilemap blockTilemap;     // Collision

    [Header("Ground Tiles")]
    public TileBase tile0; // 94% chance
    public TileBase tile1; // 5% chance
    public TileBase tile2; // 0.5% chance
    public TileBase tile3; // 0.5% chance

    [Header("Block Tile")]
    public TileBase blockTile;

    [Header("Roof Tilemap")]
    public Tilemap roofTilemap;      // Visual-only roof tiles
    public TileBase roofTile;

    [Header("Map Size")]
    public int width = 100;
    public int height = 100;
    public int padding = 10;

    [Header("Block Generation")]
    [Range(0, 1)] public float blockFillChance = 0.06f;

    [Header("Auto-Regeneration")]
    [Range(0f, 1f)] public float minWalkableRatio = 0.1f; // Regenerate if < 10% walkable

    private bool[,] walkable;
    private int mapWidth, mapHeight;

    void Start()
    {
        GenerateStage();

        // Move player to spawn position
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerSpawnWorldPos;
        }

        // Call enemy spawner after terrain is finalized
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            Vector2Int offset = new Vector2Int(-mapWidth / 2, -mapHeight / 2);
            spawner.SpawnEnemies(walkable, mapWidth, mapHeight, offset);
        }
    }

    void GenerateStage()
    {
        mapWidth = width + padding * 2;
        mapHeight = height + padding * 2;
        walkable = new bool[mapWidth, mapHeight];

        Vector2Int center = new Vector2Int(padding + 2, padding + 2); // center of 3x3 reserved area

        FillGroundTiles();
        GenerateBlockClusters();
        ReservePlayerSpawnArea(); // 👈 add here
        EnsureConnectivity(center);
        RemoveDiagonalConnections(); // Added step to fix diagonals
        Remove1x1Holes();
        PaintBlockTiles();
        PaintRoofTiles();
        CheckAndRegenerateTerrain(); // fix if stage full
    }

    void FillGroundTiles()
    {
        groundTilemap.ClearAllTiles();

        int xOffset = -mapWidth / 2;
        int yOffset = -mapHeight / 2;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int pos = new Vector3Int(x + xOffset, y + yOffset, 0);
                float rand = Random.value;

                if (rand < 0.94f)
                    groundTilemap.SetTile(pos, tile0);
                else if (rand < 0.99f)
                    groundTilemap.SetTile(pos, tile1);
                else if (rand < 0.995f)
                    groundTilemap.SetTile(pos, tile2);
                else
                    groundTilemap.SetTile(pos, tile3);
            }
        }
    }

    void GenerateBlockClusters()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                walkable[x, y] = true;
            }
        }

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (x < padding || x >= mapWidth - padding || y < padding || y >= mapHeight - padding)
                {
                    walkable[x, y] = false;
                }
            }
        }

        for (int x = padding; x < mapWidth - padding; x++)
        {
            for (int y = padding; y < mapHeight - padding; y++)
            {
                if (Random.value < blockFillChance)
                {
                    int clusterSize = Random.Range(2, 4);
                    for (int dx = -clusterSize; dx <= clusterSize; dx++)
                    {
                        for (int dy = -clusterSize; dy <= clusterSize; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && ny >= 0 && nx < mapWidth && ny < mapHeight)
                            {
                                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                                if (dist < clusterSize && Random.value > dist / clusterSize)
                                    walkable[nx, ny] = false;
                            }
                        }
                    }
                }
            }
        }
    }

    void EnsureConnectivity(Vector2Int start)
    {
        bool[,] visited = new bool[mapWidth, mapHeight];
        Queue<Vector2Int> queue = new();

        if (walkable[start.x, start.y])
        {
            queue.Enqueue(start);
            visited[start.x, start.y] = true;
        }

        while (queue.Count > 0)
        {
            Vector2Int pos = queue.Dequeue();
            foreach (var dir in directions)
            {
                int nx = pos.x + dir.x;
                int ny = pos.y + dir.y;
                if (nx >= 0 && ny >= 0 && nx < mapWidth && ny < mapHeight)
                {
                    if (walkable[nx, ny] && !visited[nx, ny])
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }
        }

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (walkable[x, y] && !visited[x, y])
                {
                    walkable[x, y] = false;
                }
            }
        }
    }

    // New function to fix diagonal connections
    void RemoveDiagonalConnections()
    {
        for (int x = 0; x < mapWidth - 1; x++)
        {
            for (int y = 0; y < mapHeight - 1; y++)
            {
                // Pattern 1:
                // Block    Walkable
                // Walkable Block
                if (!walkable[x, y + 1] && walkable[x + 1, y + 1] &&
                     walkable[x, y] && !walkable[x + 1, y])
                {
                    if (Random.value < 0.5f)
                        walkable[x + 1, y + 1] = false;
                    else
                        walkable[x, y] = false;
                }
                // Pattern 2:
                // Walkable Block
                // Block    Walkable
                else if (walkable[x, y + 1] && !walkable[x + 1, y + 1] &&
                        !walkable[x, y] && walkable[x + 1, y])
                {
                    if (Random.value < 0.5f)
                        walkable[x, y + 1] = false;
                    else
                        walkable[x + 1, y] = false;
                }
            }
        }
    }

    void PaintBlockTiles()
    {
        blockTilemap.ClearAllTiles();
        int xOffset = -mapWidth / 2;
        int yOffset = -mapHeight / 2;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (!walkable[x, y])
                {
                    Vector3Int pos = new Vector3Int(x + xOffset, y + yOffset, 0);
                    blockTilemap.SetTile(pos, blockTile);
                }
            }
        }
        Debug.Log("Block tilemap populated.");
    }

    static readonly Vector2Int[] directions = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    void PaintRoofTiles()
    {
        roofTilemap.ClearAllTiles();

        int xOffset = -mapWidth / 2;
        int yOffset = -mapHeight / 2;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (!walkable[x, y])
                {
                    Vector3Int blockPos = new Vector3Int(x + xOffset, y + yOffset, 0);
                    Vector3 roofWorldPos = roofTilemap.CellToWorld(blockPos);
                    Vector3Int roofCell = roofTilemap.WorldToCell(roofWorldPos);
                    roofTilemap.SetTile(roofCell, roofTile);
                }
            }
        }

        Debug.Log("Roof tiles painted.");
    }

    void CheckAndRegenerateTerrain()
    {
        int total = mapWidth * mapHeight;
        int walkableCount = 0;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (walkable[x, y]) walkableCount++;
            }
        }

        float walkableRatio = walkableCount / (float)total;

        if (walkableRatio < minWalkableRatio)
        {
            Debug.LogWarning("Too little walkable space! Regenerating terrain...");
            GenerateStage(); // Recursive call
        }
    }

    void Remove1x1Holes()
    {
        for (int x = 1; x < mapWidth - 1; x++)
        {
            for (int y = 1; y < mapHeight - 1; y++)
            {
                if (walkable[x, y])
                {
                    // Check 4-neighbor blocks (up, down, left, right)
                    if (!walkable[x + 1, y] &&
                        !walkable[x - 1, y] &&
                        !walkable[x, y + 1] &&
                        !walkable[x, y - 1])
                    {
                        walkable[x, y] = false;
                    }
                }
            }
        }
    }

    public Vector3 playerSpawnWorldPos; // Add this at the top of the class

    void ReservePlayerSpawnArea()
    {
        int spawnX = padding + 1;
        int spawnY = padding + 1;

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                walkable[spawnX + x, spawnY + y] = true;
            }
        }

        // Save the center position in world space
        int centerX = spawnX + 1;
        int centerY = spawnY + 1;
        playerSpawnWorldPos = new Vector3(
            centerX + -mapWidth / 2 + 0.5f,
            centerY + -mapHeight / 2 + 0.5f,
            0f
        );
    }
    
    public void RegenerateStage()
    {
        groundTilemap.ClearAllTiles();
        blockTilemap.ClearAllTiles();
        roofTilemap.ClearAllTiles();
        GenerateStage();

        // Respawn player at safe zone
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerSpawnWorldPos;
        }

        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            Vector2Int offset = new Vector2Int(-mapWidth / 2, -mapHeight / 2);
            spawner.SpawnEnemies(walkable, mapWidth, mapHeight, offset);
        }
    }
}