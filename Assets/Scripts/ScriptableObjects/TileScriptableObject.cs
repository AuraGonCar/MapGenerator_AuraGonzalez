using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//This file stores all the classes relevant to tile data, such as the tile itself and doors.
[CreateAssetMenu(fileName = "Tile", menuName = "ScriptableObjects/Tile Scriptable Object", order = 1)]
public class TileScriptableObject : ScriptableObject //Scriptable object class to store tile data, it's doors and it's different values.
{
    public string tileName;
    public bool tileActive = true; //If the tile is not active, the database won't pick it up when collecting tiles.
    public Vector2 size; //Tile size in the game map
    public Sprite tileSprite;
    public TileType type;
    public List<Door> currentDoors; 
    public Vector2 mainDoorLocation;//If the tile type is entrance/exit, this defines the space "outside" of the map, so that it doesn't clash with other tiles.

    [Range(0, 100f)]public float tileRarity = 100f; //Value to check for rarity when picking tiles

    #region Get tile data
    public TileType GetTileType()
    {
        return type;
    }
    public Sprite GetSprite()
    {
        return tileSprite;
    }
    public Door GetRandomDoorOnDirection(Directions dir) //Returns door in specified direction.
    {
        if (currentDoors.Count == 1) //If tile only has one door, it already has been called in that direction so we dont need to check if the door has that direction.
            return currentDoors[0];

        List<Door> shuffledDoors = currentDoors.OrderBy(i => Random.value).ToList();//Randomly ordering list.

        foreach (Door d in shuffledDoors) //If not, we check if the tile has other doors in that same direction.
        {
            if (d.position == dir)
                return d;
        }

        return null;
    }
    public Door GetRandomDoor()
    {
        if (currentDoors.Count == 1) //If tile only has one door, it already has been called in that direction so we dont need to check if the door has that direction.
            return currentDoors[0];

        return currentDoors[Random.Range(0, currentDoors.Count)];
    }
    public Vector2 GetSize()
    {
        return size;
    }
    public List<Vector2> GetSizeTiles(Vector2 sizeValue) //Gets position in map based on the assumption that tile is place on the top left corner of the grid (0,0).
    {
        int xValue = (int)sizeValue.x;
        int yValue = (int)sizeValue.y;

        List<Vector2> size = new List<Vector2>();

        for (int y = -(yValue - 1); y <= 0; y++) 
        {
            for (int x = 0; x <= xValue - 1; x++)
            {
                size.Add(new Vector2(x, y));
            }
        }

        return size;
    }
    public List<Vector2> GetOffsetTiles(Door d) //Returns the size tiles adjusted by the current door.
    {
        List<Vector2> normalPosition = GetSizeTiles(this.size);

        List<Vector2> offsetPosition = new List<Vector2>();

        if (d.cellOffset == Vector2.zero)
            return normalPosition;

        foreach (Vector2 n in normalPosition)
        {
            offsetPosition.Add(n + d.cellOffset);
        }

        return offsetPosition;
    }
    #endregion

    #region Tile validations
    public Door HasDoorOnDirection(Directions dir) 
    {
        foreach (Door d in currentDoors)
        {
            if (d.position == dir)
                return d;
        }

        return null;
    }
    #endregion

    #region Tile management
    public void CleanDoors()
    {
        foreach (Door d in currentDoors)
        {
            d.connected = false;
            d.assigned = false;
        }
    }

    #endregion
}


//Door class to handle connections between tiles and its position on the game map.
[System.Serializable]
public class Door
{
    public bool connected;
    public Directions position;

    public Vector2 cellOffset; //Offset to adjust the position in the game map. This value sets the door on the "origin" of the tile we're trying to place.

    public List<DoorOffsetPosition> offsetPositions; //List of positions in the game map in case another door other than this one is placed.

    [HideInInspector] public bool assigned; //Value to check if door has been checked in different processes during cell spawning.

    public Vector2 GetOffsetPosition(int index) //Returns the offset position when another door is placed (depending on the door placed).
    {
        foreach (DoorOffsetPosition d in offsetPositions)
        {
            if (d.inCaseOfIndex == index)
                return d.offsetPosition;
        }

        Debug.Log("An offset position wasnt found for this case");
        return Vector2.zero;
    }
}


//Class to handle door cell positions when other doors are placed.
[System.Serializable]
public class DoorOffsetPosition
{
    public int inCaseOfIndex; //Index to check door array in the tile.
    public Vector2 offsetPosition;
}

