using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Maze/Algorithms/Recursive Backtracking")]
public class RecursiveBacktracking : MazeGenerationAlgorithm
{
    public override IEnumerator Generate(
        int width, int height,
        CellState[,] cells,
        Action<int, int> onCellVisited,
        Action<int, int, CellWalls> onWallRemoved,
        float delayBetweenSteps)
    {
        var stack = new Stack<Vector2Int>();
        var start = new Vector2Int(0, 0);

        cells[start.x, start.y].Visited = true;
        onCellVisited?.Invoke(start.x, start.y);
        stack.Push(start);

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var neighbors = GetUnvisitedNeighbors(current.x, current.y, cells, width, height);

            if (neighbors.Count > 0)
            {
                var next = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
                stack.Push(next);

                // Remove wall between current and next
                var direction = next - current;
                CellWalls wallToRemoveCurrent = DirectionToWall(direction);
                CellWalls wallToRemoveNext = DirectionToWall(-direction);

                cells[current.x, current.y].Walls &= ~wallToRemoveCurrent;
                cells[next.x, next.y].Walls &= ~wallToRemoveNext;

                onWallRemoved?.Invoke(current.x, current.y, wallToRemoveCurrent);
                onWallRemoved?.Invoke(next.x, next.y, wallToRemoveNext);

                cells[next.x, next.y].Visited = true;
                onCellVisited?.Invoke(next.x, next.y);

                if (delayBetweenSteps <= 0) { continue; }
                yield return new WaitForSeconds(delayBetweenSteps);
            }
            else
            {
                stack.Pop();
            }
        }
    }

    private List<Vector2Int> GetUnvisitedNeighbors(int x, int y, CellState[,] cells, int w, int h)
    {
        var neighbors = new List<Vector2Int>();
        var dirs = new Vector2Int[] {
            new Vector2Int(0, 1),  // North
            new Vector2Int(0, -1), // South
            new Vector2Int(1, 0),  // East
            new Vector2Int(-1, 0)  // West
        };

        foreach (var dir in dirs)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            if (nx >= 0 && nx < w && ny >= 0 && ny < h && !cells[nx, ny].Visited)
                neighbors.Add(new Vector2Int(nx, ny));
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