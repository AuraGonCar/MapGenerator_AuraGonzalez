using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Scriptable object to store tile data an various functions relating to tile functions (Setting rarity, activating a deactivating).
[CreateAssetMenu(fileName = "Tile Database", menuName = "ScriptableObjects/Tile DataBase ScriptableObject", order = 1)]
public class TileDatabase : ScriptableObject
{
    [HideInInspector] public List<TileScriptableObject> tileCollections = new List<TileScriptableObject>(); //List storing all tile scriptable objects
    [SerializeField] private Vector2 sizeToDeactivate;
    #region Get data from tile database
    public TileScriptableObject GetEntrance() //Returns a random tile of the type entrance/exit
    {
        List<TileScriptableObject> tiles = tileCollections.OrderBy(i => Random.value).ToList(); //Randomly order the list.
        foreach (TileScriptableObject t in tiles)
        {
            if (t.GetTileType() == TileType.EntranceExit && t.tileActive)
                return t;
        }

        Debug.Log("No entrance was found"); //If there are no tiles active of the tile entrance, this returns null.
        return null;
    }
    public List<TileScriptableObject> GetAllTilesWithDoorInDirection(Directions doorDirection, TileType[] typesToExclude) //Returns all tiles with a door in a set direction and within the exclusion parameters
    {
        List<TileScriptableObject> tiles = new List<TileScriptableObject>();

        List<TileScriptableObject> shuffledTiles = tileCollections.OrderBy(i => Random.value).ToList(); //Randomly order the list.

        foreach (TileScriptableObject t in shuffledTiles)
        {
            if (t.HasDoorOnDirection(doorDirection) != null)
            {
                bool invalid = false;

                if (typesToExclude != null)
                {
                    for (int i = 0; i < typesToExclude.Length; i++)
                    {
                        if (t.GetTileType() == typesToExclude[i]) //If the tile is of the excluded type, we mark it so that we dont add it to the final list.
                        {
                            invalid = true;
                            break;
                        }

                    }
                }


                if (!invalid)
                    tiles.Add(t);
            }
        }

        return tiles;
    }
    public Directions GetOppositeDirection(Directions dir) 
    {
        switch (dir)
        {
            case Directions.Up:
                return Directions.Down;
            case Directions.Down:
                return Directions.Up;
            case Directions.Right:
                return Directions.Left;
            case Directions.Left:
                return Directions.Right;
            default:
                return Directions.Up;
        }
    }
    #endregion

    #region Tile database set up
    private void OnEnable()
    {
        tileCollections.Clear();
        LoadTiles();
        CleanTileDoors();
    }
    private void OnDisable()
    {
        CleanTileDoors();
    }
    public void LoadTiles() //Take the tiles from the resources folder into the database list.
    {
        TileScriptableObject[] tileLoad = Resources.LoadAll<TileScriptableObject>("Tiles");

        for (int i = 0; i < tileLoad.Length; i++)
        {
            if (tileCollections.Contains(tileLoad[i]))
                continue;
            tileCollections.Add(tileLoad[i]);
        }
    }
    #endregion

    #region Tile configuration
    public void CleanTileDoors() //Set the tiles to its default values.
    {
        foreach (TileScriptableObject t in tileCollections)
        {
            t.CleanDoors();
        }
    }
    #endregion

    //Functions to set debug values like deactivating tiles and setting rarities.
    #region Debug functions
    public void ActivateAllTiles()
    {
        if (tileCollections.Count == 0)
            LoadTiles();

        foreach (TileScriptableObject t in tileCollections)
        {
            t.tileActive = true;
        }

        Debug.Log("All tiles activated");
    }

    public void DeactivateAllTiles()
    {
        if (tileCollections.Count == 0)
            LoadTiles();
        foreach (TileScriptableObject t in tileCollections)
        {
            t.tileActive = false;
        }

        Debug.Log("All tiles deactivated");
    }

    public void RandomRarities()
    {
        if (tileCollections.Count == 0)
            LoadTiles();

        foreach (TileScriptableObject t in tileCollections)
        {
            t.tileRarity = Random.Range(0, 100);
        }

        Debug.Log("All tiles set to random rarity");
    }

    public void SetAllTilesRaritiesToMax()
    {
        if (tileCollections.Count == 0)
            LoadTiles();

        foreach (TileScriptableObject t in tileCollections)
        {
            t.tileRarity = 100;
        }

        Debug.Log("All tiles set to max rarity");
    }

    public void DeactivateSpecificSizedTiles()
    {
        if (tileCollections.Count == 0)
            LoadTiles();

        bool doesSizeExist = false;
        foreach (TileScriptableObject t in tileCollections)
        {
            if (t.GetSize() == sizeToDeactivate)
            {
                t.tileActive = false;

                if(!doesSizeExist)
                    doesSizeExist = true;
            }
        }

        if (doesSizeExist)
            Debug.Log("Deactivated tiles with size " + sizeToDeactivate);
        else
            Debug.Log("Tiles with size " + sizeToDeactivate+ " not found");
    }
    #endregion 

}





