using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Cell //Class to store cell data in the game map.
{
    [HideInInspector] public Vector2 index; //Cell position in the map.
    [HideInInspector] public List<Door> currentDoors; //List of doors active in the cell

    bool emptyCell = false; //Value to check if the tile doesn't have any connections.

    #region Cell constructors
    public Cell(Vector2 newIndex) //Cell constructor to create an empty cell.
    {
        index = newIndex;
        emptyCell = true;
    }
    public Cell(Vector2 newIndex, TileScriptableObject t) //Cell constructor to create a cell with every door in the index.
    {
        index = newIndex;

        currentDoors = new List<Door>();

        foreach (Door d in t.currentDoors) //Pass door values.
        {
            Door newDoor = new Door();
            newDoor.cellOffset = d.cellOffset;
            newDoor.connected = d.connected;
            newDoor.position = d.position;

            currentDoors.Add(newDoor);
        }

        t.CleanDoors();
    }
    public Cell(Vector2 newIndex, Door doorPlaced)//Cell constructor to create a cell with a single door.
    {
        index = newIndex;

        currentDoors = new List<Door>();

        Door newDoor = new Door();
        newDoor.cellOffset = doorPlaced.cellOffset;
        newDoor.connected = doorPlaced.connected;
        newDoor.position = doorPlaced.position;

        currentDoors.Add(newDoor);
    }
    #endregion

    #region Get door values
    public int GetNumberOfDisconnectedDoors()
    {
        int numerOfDisconnectedDoors = 0;

        if (currentDoors != null)
        {
            foreach (Door d in currentDoors)
            {
                if (!d.connected)
                    numerOfDisconnectedDoors++;
            }
        }

        return numerOfDisconnectedDoors;
    }
    public Door GetDisconnectedDoor() //Returns a disconnected door in currentDoors list.
    {
        if (currentDoors != null)
        {
            foreach (Door d in currentDoors)
            {
                if (!d.connected)
                    return d;
            }
        }

        return null;
    }

    #endregion

    #region Door validations
    public bool HasDisconnectedDoor()
    {
        if (currentDoors != null)
        {
            foreach (Door d in currentDoors)
            {
                if (!d.connected)
                    return true;
            }

        }

        return false;
    }
    public Door HasDoorOnDirection(Directions dir)
    {
        if (currentDoors != null)
        {
            foreach (Door d in currentDoors)
            {
                if (d.position == dir)
                    return d;
            }
        }


        return null;
    }
    public bool IsEmptyCell()
    {
        return emptyCell;
    }
    #endregion

    #region Door management
    public void AddDoor(Door d) //Adds a door to the currentDoors list.
    {
        if (currentDoors == null)
            currentDoors = new List<Door>();

        currentDoors.Add(d);
    }
    #endregion
}
