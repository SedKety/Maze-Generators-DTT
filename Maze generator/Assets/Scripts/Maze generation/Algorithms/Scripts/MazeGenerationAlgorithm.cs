using System;
using System.Collections;
using UnityEngine;

//The base class for all generators.
//Provides a ruleset all generators have to follow(such as returning an ienumerator, and holding information/settings on it)
public abstract class MazeGenerationAlgorithm : ScriptableObject
{
    //An description on how the maze is generated and where it's originally from.
    public string description;

    //Ways the client could use the maze in development(e.g: pinball, minigame, shooter, etc)
    public string useCaseExplanation;


    public abstract IEnumerator Generate(

        //The scale of the maze in 2 dimensions
        //Maze scale: (width, 0, height).
        int width, 
        int height, 

        CellState[,] cells, //Structs containing data on how the maze currently looks(based on the walls defined in CellState.c)
        Action<int, int> onCellVisited,  //Called when cell is first visited
        Action<int, int, CellWalls> onWallRemoved, //Called when a wall is carved
        float delayBetweenSteps //The timestep in between steps taken (where the step is taken is different per algorithms)
    );
}