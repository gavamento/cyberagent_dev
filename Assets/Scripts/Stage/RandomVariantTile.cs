using UnityEngine;
using UnityEngine.Tilemaps;

// 置いた座標に応じて複数のスプライトから1枚を選ぶタイル。同じ座標なら常に同じ見た目になる
[CreateAssetMenu(menuName = "Tiles/Random Variant Tile")]
public class RandomVariantTile : Tile
{
    public Sprite[] sprites;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);
        if (sprites == null || sprites.Length == 0) return;

        uint h = (uint)(position.x * 73856093) ^ (uint)(position.y * 19349663);
        h ^= h >> 13;
        h *= 0x5bd1e995;
        h ^= h >> 15;
        tileData.sprite = sprites[h % (uint)sprites.Length];
    }
}
