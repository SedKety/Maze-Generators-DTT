using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Maze/Algorithms/Growing Tree")]
public class GrowingTree : MazeGenerationAlgorithm
{
    public enum SelectionMode
    {
        Newest, 
        Random      
    }

    public SelectionMode mode = SelectionMode.Newest;

    public override IEnumerator Generate(
        int width, int height,
        CellState[,] cells,
        Action<int, int> onCellVisited,
        Action<int, int, CellWalls> onWallRemoved,
        float delayBetweenSteps)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y].Visited = false;
            }
        }

        List<Vector2Int> list = new List<Vector2Int>();

        Vector2Int startingCell = new Vector2Int(
            UnityEngine.Random.Range(0, width), 
            UnityEngine.Random.Range(0, height));

        cells[startingCell.x, startingCell.y].Visited = true;
        list.Add(startingCell);
        onCellVisited?.Invoke(startingCell.x, startingCell.y);

        while (list.Count > 0)
        {
            //Choose a cell according to the mode
            Vector2Int current;

            //For now there is no ui representing this choice
            current = mode == SelectionMode.Newest ? 
                list[list.Count - 1] : list[UnityEngine.Random.Range(0, list.Count)];
            //Using a ternary operator, "value == condition ? true : false

            var neighbors = GetUnvisitedNeighbors(current.x, current.y, cells, width, height);

            if (neighbors.Count > 0)
            {
                var next = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];

                Vector2Int dir = next - current;

                CellWalls wallA = DirectionToWall(dir);
                CellWalls wallB = DirectionToWall(-dir);

                cells[current.x, current.y].Walls &= ~wallA;
                cells[next.x, next.y].Walls &= ~wallB;

                onWallRemoved?.Invoke(current.x, current.y, wallA);
                onWallRemoved?.Invoke(next.x, next.y, wallB);

                cells[next.x, next.y].Visited = true;
                onCellVisited?.Invoke(next.x, next.y);

                list.Add(next);
            }
            else
            {
                list.Remove(current);
            }

            if (delayBetweenSteps > 0)
            {
                yield return new WaitForSeconds(delayBetweenSteps);
            }
        }
    }
    
    private List<Vector2Int> GetUnvisitedNeighbors(int x, int y, CellState[,] cells, int width, int height)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions =
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        foreach (var direction in directions)
        {
            int neighbourX = x + direction.x;
            int neighbourY = y + direction.y;
            

            if (neighbourX >= 0 && neighbourX < width && neighbourY >= 0 && neighbourY < height)
            {
                if (!cells[neighbourX, neighbourY].Visited)
                {
                    neighbors.Add(new Vector2Int(neighbourX, neighbourY));
                }
            }
        }

        return neighbors;
    }

    private CellWalls DirectionToWall(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return CellWalls.North;
        if (dir == Vector2Int.down) return CellWalls.South;
        if (dir == Vector2Int.right) return CellWalls.East;
        if (dir == Vector2Int.left) return CellWalls.West;
        return 0;
    }
}
