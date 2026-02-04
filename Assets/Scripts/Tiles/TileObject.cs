using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileObject : MonoBehaviour //Class to store scene relevant data of tiles spawned into the game map.
{
    [SerializeField] private SpriteRenderer tileSprite; 
    private TileType spawnedTileType;

    public void ChangeSprite(Sprite newSprite)
    {
        tileSprite.sprite = newSprite;
    }

    public void SetTileType(TileType newTileType)
    {
        spawnedTileType = newTileType;
    }

    public TileType GetTileType()
    {
        return spawnedTileType;
    }
}
