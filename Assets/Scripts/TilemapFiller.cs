using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapFiller : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase tile0; // 90% chance
    public TileBase tile1; // 5% chance
    public TileBase tile2;
    public TileBase tile3;

    public int width = 100;
    public int height = 100;

    void Start()
    {
        FillTilemap();
    }

    void FillTilemap()
    {
        Vector3Int center = Vector3Int.zero;

        for (int x = -width / 2; x < width / 2; x++)
        {
            for (int y = -height / 2; y < height / 2; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                float rand = Random.value;

                if (rand < 0.94f)
                    tilemap.SetTile(pos, tile0);
                else if (rand < 0.99f)
                    tilemap.SetTile(pos, tile1);
                else if (rand < 0.995f)
                    tilemap.SetTile(pos, tile2);
                else
                    tilemap.SetTile(pos, tile3);
            }
        }
    }
}
