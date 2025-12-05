using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Maze/Algorithms/Binary Tree")]
public class BinaryTree : MazeGenerationAlgorithm
{
    public override IEnumerator Generate(
        int width,
        int height,
        CellState[,] cells,
        Action<int, int> onCellVisited,
        Action<int, int, CellWalls> onWallRemoved,
        float delayBetweenSteps)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                cells[x, y].Visited = true;
                onCellVisited?.Invoke(x, y);

                // List of possible directions in which we can remove walls
                List<CellWalls> possibleDirections = new List<CellWalls>();


                //This is the important part about the binary-tree algorithms.
                //It defines where the tree can grow to using 2D vector math

                // Southwest bias (feels like growing from bottom-right)
                if (y < height - 1) possibleDirections.Add(CellWalls.South);
                if (x < width - 1) possibleDirections.Add(CellWalls.East);

                // Northeast bias
                if (y > 0) possibleDirections.Add(CellWalls.North);
                if (x < width - 1) possibleDirections.Add(CellWalls.East);

                //If any possible directions are found pick a random one
                if (possibleDirections.Count > 0)
                {
                    CellWalls chosen = possibleDirections[UnityEngine.Random.Range(0, possibleDirections.Count)];

                    //Remove wall from current cell
                    cells[x, y].Walls &= ~chosen; //Perform a bitwie NOT operation to remove the chosen wall from the walls
                    onWallRemoved?.Invoke(x, y, chosen);

                    //Remove the neighbour's walls
                    if (chosen == CellWalls.North && y > 0)
                    {
                        cells[x, y - 1].Walls &= ~CellWalls.South;
                        onWallRemoved?.Invoke(x, y - 1, CellWalls.South);
                    }
                    else if (chosen == CellWalls.West && x > 0)
                    {
                        cells[x - 1, y].Walls &= ~CellWalls.East;
                        onWallRemoved?.Invoke(x - 1, y, CellWalls.East);
                    }
                }

                if(delayBetweenSteps <= 0) { continue; }
                yield return new WaitForSeconds(delayBetweenSteps);
            }
        }
    }
}