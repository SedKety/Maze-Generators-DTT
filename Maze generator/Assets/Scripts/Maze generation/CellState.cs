using System;


//The bordering walls the assigned cell has
[Flags]
public enum CellWalls : byte
{
    North = 1 << 0,
    South = 1 << 1,
    East = 1 << 2,
    West = 1 << 3,
    All = North | South | East | West
}

//Acts as an storage container to document the data per cell
public struct CellState
{
    public bool Visited;
    public CellWalls Walls; 

    public CellState(bool visited = false, CellWalls walls = CellWalls.All)
    {
        Visited = visited;
        Walls = walls;
    }
}