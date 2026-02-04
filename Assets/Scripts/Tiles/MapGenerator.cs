using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class handles map creation, it's rules and it's different variables like map size.
public class MapGenerator : MonoBehaviour
{

    [Header("Map size")]
    [SerializeField] private Vector2 startPosition;
    [Space]
    [SerializeField] private int minRooms; //if the algorithm cant place anymore tiles and the number of rooms created is lesser than this value, it restarts the algorithm
    [SerializeField] private int maxRooms; //When the rooms created value reaches this value, it starts closing all paths.
    [Space]
    [SerializeField] private Vector2 mapSize; //If any tile created exceds this value, it restarts the algorithm.
    [SerializeField] bool useMapSize; //Checks whether to limit de map created by the mapSize variable.

    [Space]
    [Header("Tile visual")]
    [SerializeField] private TileObject tilePrefab; //Prefab to instantiate rooms.
    [SerializeField] private float cellSize = 0.25f; //Size adjustment for the sprites.

    [Space]
    [SerializeField] private TileDatabase database; //Scriptable object to store all tiles.

    [Space]
    [Header("Map Seed")]
    [SerializeField] private int seed; //Seed for map generation.
    [SerializeField] bool useSeed; //Check to allow seed repetion.

    [Space]
    [Header("Debug")]
    [SerializeField] Vector2 indexToCheck; //Index for debugging tiles with errors or that have wrong connections.
    [SerializeField] int mapCreationTries; //Whenever a map fails, if the number of tries is equal or higher than this variable, the map creation stops.
    [SerializeField] bool useDebugs; //If this variable is true, all debugs checks will log.
    private int tries; //Value to check how many map creations have failed.

    //Map coroutine
    Coroutine mapGeneration; //The map creation coroutine is stored here.

    //Stop map creation check
    bool cantContinue = false;


    //Map generation data
    private Vector2 currentIndex; //Position of the current cell.
    private Cell currentCell; //The cell the algorithm will check when trying to place a new tile.
    private Door currentDoor; //The door the algorithm will check to place a fitting tile.

    //Map lists
    private List<TileObject> spawnedTiles = new(); //Current list of tiles spawned in the map.
    private List<Cell> spawnedCells = new List<Cell>(); //Current list of cells spawned in the map.

    //Tile exclusions
    //Different enum arrays to manage logic map creation
    //Each array is used to exclude said tiles in different parts of map creation.
    private TileType[] excludeEntrancesAndExits = { TileType.EntranceExit };
    private TileType[] onlyRooms = { TileType.EntranceExit, TileType.Path };
    private TileType[] onlyEntrancesAndExits = { TileType.Path, TileType.Room };
    private TileType[] onlyPaths = { TileType.EntranceExit, TileType.Room };


    private void Start()
    {
        spawnedTiles = new List<TileObject>();
        spawnedCells = new List<Cell>();
    }

    #region Map Creation
    public void SetUpMap() //This is called everytime the map is initiated or rebooted.
    {
        if (maxRooms <= 0) //Max room validation.
        {
            Debug.Log("Max room size invalid");
            return;
        }

        if (minRooms >= maxRooms)
            minRooms = maxRooms - 1;

        if(mapSize.x <= 0)
        {
            if(useDebugs)
                Debug.Log("Map size x invalid");

            mapSize = new Vector2(2, mapSize.y);
        }

        if(mapSize.y <= 0)
        {
            if(useDebugs)
                Debug.Log("Map size y invalid");

            mapSize = new Vector2(mapSize.x, 2);
        }
        
        if (mapGeneration != null) //If the map creation is in progress we stop it.
            StopCoroutine(mapGeneration);

        if (GetNumberOfDisconnectedDoors() == 0) //If the previous map has been completed, we reset the tries.
            ResetMapTries();

        database.CleanTileDoors();

        SetSeedValue(); 

        for (int i = 0; i < spawnedTiles.Count; i++) //Clean spawned tiles
        {
            Destroy(spawnedTiles[i].gameObject);
        }

        //Clean lists.
        spawnedCells.Clear();
        spawnedTiles.Clear();

        //Set the start position.
        currentIndex = startPosition;

        currentCell = null;
        VisitCell(currentIndex);

        //Reset failure check
        cantContinue = false;

        //Begin map generation.
        mapGeneration = StartCoroutine(MapGeneration());
    }
    IEnumerator MapGeneration() //This coroutine spawns the tiles in a while loop and resets itself if the map created isn't valid.
    {
        while (GetNumberOfDisconnectedDoors() > 0 && !cantContinue)
        {
            Vector2 index = currentIndex; //Checking current cell position.

            if(useDebugs)
                Debug.Log("Currently trying to place door for " + currentCell.index + " in " + currentDoor.position); //Cell information debug.

            switch (currentDoor.position) //Checking door direction for the tile in said direction.
            {
                case Directions.Up:
                    index += Vector2.up;
                    break;
                case Directions.Down:
                    index += Vector2.down;
                    break;
                case Directions.Right:
                    index += Vector2.right;
                    break;
                case Directions.Left:
                    index += Vector2.left;
                    break;
                default:
                    break;
            }

            if (useMapSize) 
            {
                if (!IsIndexValid(index))//If the tile we're trying to check is outside the limits, the map generation has failed.
                {
                    if(tries >= mapCreationTries) //If the algorithm has already tried generation the amount of times specified, it stops map generation. 
                    {
                        if (useDebugs)
                            Debug.Log("Map creation failed");

                        yield break;
                    }
                    else
                    {
                        if(useDebugs)
                            Debug.Log("Cell outside map limits");

                        tries++; 
                        SetUpMap();
                        yield break;
                    }
                    
                }
            }

            VisitCell(index);

            yield return new WaitForSeconds(0.1f);

        }

        if (spawnedTiles.Count < minRooms && GetNumberOfDisconnectedDoors() > 0 && !DoesMapContainExitAndEntrance()) //If the map generation has stopped and the requirements have not been met, the map generation has failed.
        {
            if (tries >= mapCreationTries) //If the algorithm has already tried generation the amount of times specified, it stops map generation. 
            {
                if (useDebugs)
                    Debug.Log("Map creation failed");
            }
            else
            {
                if (useDebugs)
                    Debug.Log("Cell outside map limits");

                tries++;
                SetUpMap();
            }
        }
    }
    private void VisitCell(Vector2 index) //Method to check out the cell where we want to spawn a new tile.
    {

        if (useDebugs)
            if (index == indexToCheck) //This debug is used for invalid tiles that can get spawned so that we can debug it properly
                Debug.Log("Checking troublesome cell");
        

        if (spawnedTiles.Count == 0) //If this is the first tile spawned, we always want to create and entrance/exit.
        {
            TileScriptableObject entrance = database.GetEntrance();
            SpawnTile(index, entrance, entrance.GetRandomDoor());
        }

        else
        {
            if (spawnedTiles.Count >= maxRooms) //If the room limit has been surpased, we close out the map by only generatin rooms.
            {
                if (GetNumberOfDisconnectedDoors() == 1 && !DoesMapContainExitAndEntrance()) //If the map has only one door left to close and the exit has not been placed yet, we spawn the exit here.
                    TrySpawnTile(index, onlyEntrancesAndExits, true);
                else
                {
                    if (!TrySpawnTile(index, onlyRooms, true)) //If there isn't a room available to fit the necessary unconnected doors, we switch to paths.
                    {
                        if (!TrySpawnTile(index, onlyPaths, true)) //If using randomness we dont get a valid path, get try to get all available paths.
                        {
                            TrySpawnTile(index, onlyPaths, false);
                            cantContinue = true;
                            return;
                        }
                    }
                }
            }
            else
            {
                if (GetNumberOfDisconnectedDoors() > 0 && GetNumberOfDisconnectedDoors() <= 2) //For the first few tiles we try to create a map that has a few paths to connect.
                    TrySpawnTile(index, onlyPaths, true);
                else
                    TrySpawnTile(index, excludeEntrancesAndExits, true); //Then we only exclude entrance/exits so that we can place only two of them later.
            }


        }
    }
    private void SpawnTile(Vector2 index, TileScriptableObject tileToSpawn, Door doorChosen) //Method to instantiate the tile chosen in the validation process.
    {
        Vector2 tilePos = new Vector2((index.x + doorChosen.cellOffset.x) * cellSize, (index.y + doorChosen.cellOffset.y) * cellSize); //Get the map position.

        //Spawning tile object and setting its values
        TileObject newTile = Instantiate(tilePrefab, tilePos, Quaternion.identity);
        newTile.SetTileType(tileToSpawn.GetTileType());
        newTile.name = tileToSpawn.tileName;
        newTile.ChangeSprite(tileToSpawn.GetSprite());
        spawnedTiles.Add(newTile);


        if (tileToSpawn.GetSize() == new Vector2(1, 1)) //All doors are in the same tile so there's no point in checking for other tiles.
        {
            //Creating cell.
            Cell newCell = new Cell(index, tileToSpawn);
            spawnedCells.Add(newCell);

            if (currentCell == null) //Setting current cell.
            {
                currentCell = newCell;
                GetCellWithDisconnectedDoor();
                currentDoor = currentCell.GetDisconnectedDoor();

            }

        }

        else
        {
            //Spawning cell
            Cell newCell = new Cell(index, doorChosen);
            spawnedCells.Add(newCell);

            //If the tile if larger than 1x1, we spawn the other cells in the tile and set the doors.
            List<Vector2> offsetTiles = tileToSpawn.GetOffsetTiles(doorChosen);

            if (tileToSpawn.currentDoors.Count > 1) //If the cell has more than one door, we need set the tile in the correct space.
            {
                //We dont need to check for the door we have already placed.
                doorChosen.assigned = true;

                //Setting the index correctly depending on which door was spawned.
                int doorChosenIndex = tileToSpawn.currentDoors.IndexOf(doorChosen);

                foreach (Vector2 e in offsetTiles)
                {
                    foreach (Door d in tileToSpawn.currentDoors)
                    {
                        if (!d.assigned) //If the door has already been placed we skip it.
                        {
                            if (e + index == d.GetOffsetPosition(doorChosenIndex) + index) //If the position is corresponding with the index. We place the door.
                            {
                                
                                if (spawnedCells.Contains(GetCellOnIndex(e + index))) //If the current cell already exits and its part of the same tile (if a big tile has more than one door) we add the door to the cell.
                                {
                                    Cell existingCell = GetCellOnIndex(e + index);

                                    if (existingCell.HasDoorOnDirection(d.position) == null) //Making sure we're not adding more than one door in the same direction
                                    {
                                        //Placing the door.
                                        d.assigned = true;
                                        existingCell.AddDoor(d);
                                    }
                                    else
                                    {
                                        if(useDebugs)
                                            Debug.Log("Trying to add a door to a cell with a door in the same direction on " + tileToSpawn.tileName);
                                    }
                                }
                                else
                                {
                                    //If there's not another door in this position, we create a new cell for the door.
                                    Cell c = new Cell(e + index, d);
                                    d.assigned = true;
                                    spawnedCells.Add(c);
                                }

                            }
                        }
                    }

                    if (!spawnedCells.Contains(GetCellOnIndex(index + e))) //If the cell hasn't already been placed (in the previous if) we spawn an empty cell.
                    {
                        Cell c = new Cell(e + index);
                        spawnedCells.Add(c);
                    }
                }
            }
            else
            {
                foreach (Vector2 e in offsetTiles) //If the tile is bigger than 1x1, we spawn all the other empty cells.
                {
                    if (!spawnedCells.Contains(GetCellOnIndex(index + e)))
                    {
                        Cell c = new Cell(index + e);
                        spawnedCells.Add(c);
                    }

                }
            }

            //Setting current cell
            if (currentCell == null)
                currentCell = newCell;

        }

        if (tileToSpawn.GetTileType() == TileType.EntranceExit) //If the tile is an entrance/exit, we also set the cell reserved for the exit.
        {
            Cell entranceOrExitDoorCell = new Cell(tileToSpawn.mainDoorLocation + index);
            spawnedCells.Add(entranceOrExitDoorCell);
        }

        //Clean tile to not store wrong data.
        tileToSpawn.CleanDoors();

        if (!currentCell.HasDisconnectedDoor()) //If the current cell has already been connected, we find a new cell with disconnected doors.
        {
            //Setting current door.
            GetCellWithDisconnectedDoor();
            currentIndex = currentCell.index;
            currentDoor = currentCell.GetDisconnectedDoor();
        }
        else
        {
            //If not, we find a new disconnected door in the same cell.
            currentDoor = currentCell.GetDisconnectedDoor();
        }

    }
    #endregion

    #region Map Validations
    private bool CanPlaceTile(Door d, TileScriptableObject t, Vector2 index) //Method to check if tile chosen is valid in the current space
    {

        List<Vector2> offsetTiles = t.GetOffsetTiles(d); 

        foreach (Vector2 e in offsetTiles)
        {
            if (useMapSize) //If the current index exceeds map limits, its not a valid tile.
                if (!IsIndexValid(index + e))
                    return false;

            if (spawnedCells.Contains(GetCellOnIndex(index + e))) //If the cells that the tile ocuppies in the map space have already been filled, its not a valid tile.
                return false;
        }

        if (t.GetTileType() == TileType.EntranceExit) //We check for the empty cell in case of the tile being entrance/exit.
        {
            if (spawnedCells.Contains(GetCellOnIndex(index + t.mainDoorLocation)))
                return false;
        }

        //If tile has more than one door, we check if the other doors are going to be connected to an empty tile.
        //If it's going to be connected a compatible door, we store it and we connect them.
        List<Door> possibleExtraDoorConnections = new List<Door>();

        if (t.currentDoors.Count > 1)
        {
            if (t.GetSize() != new Vector2(1, 1)) //If tile is bigger than 1x1, it's doors area probably spread out, so we check if any of its doors are going to be connected to an empty cell.
            {
                int doorChosenIndex = t.currentDoors.IndexOf(d);

                foreach (Door doorCheck in t.currentDoors)
                {
                    //The current door is already valid, so we dont need to check it.
                    if (doorCheck == d)
                        continue;

                    Vector2 doorPosition = index + doorCheck.GetOffsetPosition(doorChosenIndex);

                    switch (doorCheck.position) //We check in the door direction if the cell with door is valid.
                    {
                        case Directions.Up:
                            if (ExisitingDoorCheck(doorCheck.position, doorPosition + Vector2.up, doorCheck, possibleExtraDoorConnections))
                                break;
                            return false;

                        case Directions.Down:
                            if (ExisitingDoorCheck(doorCheck.position, doorPosition + Vector2.down, doorCheck, possibleExtraDoorConnections))
                                break;
                            return false;
                        case Directions.Right:
                            if (ExisitingDoorCheck(doorCheck.position, doorPosition + Vector2.right, doorCheck, possibleExtraDoorConnections))
                                break;
                            return false;
                        case Directions.Left:
                            if (ExisitingDoorCheck(doorCheck.position, doorPosition + Vector2.left, doorCheck, possibleExtraDoorConnections))
                                break;
                            return false;
                        default:
                            break;
                    }
                }
            }
            else
            {
                foreach (Door smallDoor in t.currentDoors) //We check if any of the doors in the cell is connected to an empty cell.
                {
                    if (smallDoor == d)
                        continue;

                    switch (smallDoor.position)
                    {
                        case Directions.Up:
                            if (ExisitingDoorCheck(smallDoor.position, index + Vector2.up, smallDoor, possibleExtraDoorConnections))
                                break;
                            return false;
                        case Directions.Down:
                            if (ExisitingDoorCheck(smallDoor.position, index + Vector2.down, smallDoor, possibleExtraDoorConnections))
                                break;
                            return false;
                        case Directions.Right:
                            if (ExisitingDoorCheck(smallDoor.position, index + Vector2.right, smallDoor, possibleExtraDoorConnections))
                                break;
                            return false;
                        case Directions.Left:
                            if (ExisitingDoorCheck(smallDoor.position, index + Vector2.left, smallDoor, possibleExtraDoorConnections))
                                break;
                            return false;
                        default:
                            break;
                    }

                }
            }

        }

        //Now we want to check if any of the already spawned cells have conflict with the new cells.
        foreach (Cell c in spawnedCells)
        {
            if (!c.IsEmptyCell()) //Skip if the cell doesnt have any doors.
            {
                if (c.HasDisconnectedDoor()) //Skip if the cell doesnt have any disconnected cells.
                {
                    foreach (Door testDoors in c.currentDoors)
                    {
                        if (testDoors == currentDoor) //Skip if its the door we're trying to connect.
                            continue;
                        if (possibleExtraDoorConnections.Contains(testDoors)) //Skip if it's an already accounted for door.
                            continue;
                        if (testDoors.connected) //Skip if door is already connected.
                            continue;

                        switch (testDoors.position) //We look in the door position if it contains an empty tile in the tile we want to place.
                        {
                            case Directions.Up:
                                foreach (Vector2 p in offsetTiles)
                                {
                                    if (c.index + Vector2.up == p + index)
                                        return false;
                                }
                                break;
                            case Directions.Down:
                                foreach (Vector2 p in offsetTiles)
                                {
                                    if (c.index + Vector2.down == p + index)
                                        return false;
                                }
                                break;
                            case Directions.Right:
                                foreach (Vector2 p in offsetTiles)
                                {
                                    if (c.index + Vector2.right == p + index)
                                        return false;
                                }
                                break;
                            case Directions.Left:
                                foreach (Vector2 p in offsetTiles)
                                {
                                    if (c.index + Vector2.left == p + index)
                                        return false;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        //If there are any extra connections, we connect the doors.
        foreach (Door extraDoor in possibleExtraDoorConnections)
        {
            extraDoor.connected = true;
        }

        return true;

    }
    private bool IsIndexValid(Vector2 index) //Method to check if index within map limits.
    {
        return index.x <= mapSize.x && index.y <= mapSize.y && index.x > -mapSize.x && index.y > -mapSize.y;
    }
    private bool TrySpawnTile(Vector2 index, TileType[] exclusion, bool useRarity) //method to pick a random tile and check if it can be spawned.
    {
        Directions dir = database.GetOppositeDirection(currentDoor.position); //Direction we want to place the new cell in.

        foreach (TileScriptableObject tileToSpawn in database.GetAllTilesWithDoorInDirection(dir, exclusion)) //Get a random tile with a valid door.
        {
            if (!tileToSpawn.tileActive) //Skip if tile is deactivated.
                continue;

            if (useRarity) //If the rarity is used, we do a rarity check.
            {
                float rarityCheck = Random.Range(0, 100);

                if (tileToSpawn.tileRarity < rarityCheck)
                    continue;
            }

            Door doorToPlace = tileToSpawn.GetRandomDoorOnDirection(dir); //If the tile has multiple doors in that same direction we pick a random one.

            if (CanPlaceTile(doorToPlace, tileToSpawn, index)) //If we can place the tile in that position, we connect the doors and spawn it.
            {
                currentDoor.connected = true;
                doorToPlace.connected = true;

                SpawnTile(index, tileToSpawn, doorToPlace);

                return true; //We break the loop if we manage to spawn it.
            }
        }

        if(useDebugs)
            Debug.Log("Couldn't place any tiles in cell " + currentCell.index);

        return false; //If the algorithm couldnt place any tile in that position with the restrictions and rarity given, it returns false.

    }

    private bool DoesMapContainExitAndEntrance() //Method to check if the map has two entrances and exits.
    {
        int value = 0;
        foreach (TileObject t in spawnedTiles)
        {
            if (t.GetTileType() == TileType.EntranceExit)
                value++;
        }

        return value >= 2;
    }

    private bool ExisitingDoorCheck(Directions doorPosition, Vector2 direction, Door currentDoor, List<Door> extraDoors) //Check if the cell given has a door in the given direction, and if it can connect it to the given door.
    {
        Cell c = GetCellOnIndex(direction);

        if (c != null) //Skip if the cell doesnt exist
        {
            if (c.IsEmptyCell()) //If the cell is empty, we cant connect the door given to it, so it returns false.
                return false;

            Door extraDoor = c.HasDoorOnDirection(database.GetOppositeDirection(doorPosition));

            if (extraDoor == null) //If the cell doesnt have a door in that direction, it returns false.
                return false;

            //Add the valid doors to the extra connections.
            extraDoors.Add(currentDoor);
            extraDoors.Add(extraDoor);

            return true;
        }

        //If the cell doesnt exits, the given door can still be connected, so we return true.
        return true;
    }
    #endregion

    #region Map Values
    private int GetNumberOfDisconnectedDoors()
    {
        int value = 0;
        foreach (Cell c in spawnedCells)
        {
            value += c.GetNumberOfDisconnectedDoors();
        }

        return value;
    }
    private Cell GetCellOnIndex(Vector2 index)
    {
        foreach (Cell c in spawnedCells)
        {
            if (c.index == index)
                return c;
        }

        return null;
    }
    private void GetCellWithDisconnectedDoor()
    {
        foreach (Cell c in spawnedCells)
        {
            if (c.IsEmptyCell())
                continue;
            if (c.HasDisconnectedDoor())
            {
                currentCell = c;
                return;
            }

        }

        if(useDebugs)
            Debug.Log("No disconnected cells were found");
    }
    #endregion 

    #region Debug and seeding
    public void ResetMapTries()
    {
        tries = 0;
    }

    private void SetSeedValue() 
    {
        if (useSeed) //If the seed is used, we set the value to random.
        {
            Random.InitState(seed);
        }
        else //If not, we set a random seed for this map.
        {
            seed = (int)System.DateTime.Now.Ticks;
            Random.InitState(seed);
        }
    }

    #endregion
}
