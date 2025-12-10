using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Settings", menuName = "Maze/Settings")]
public class Settings : ScriptableObject
{
    public int mazeIndex;

    [Tooltip("X scale of the maze")]
    [Range(1, 1000)] public int width;
    [Tooltip("Y scale of the maze")]
    [Range(1, 1000)] public int height;

    [Tooltip("Time between generation steps (what counts as a \"step\" depends on the algorithms)")]
    public float generationDelay = 0.015f;

    [Tooltip("The maze generation algorithms available for the renderer.")]
    public MazeGenerationAlgorithm[] algorithms;

    public delegate void GenerationFunc(
    MazeGenerationAlgorithm algorithm,
    int width,
    int height,
    float density
    );
    public GenerationFunc OnGenerationEvent;


    public void CallGenerationFunc()
    {
        if (OnGenerationEvent != null)
        {
            OnGenerationEvent.Invoke(algorithms[mazeIndex], width, height, generationDelay);
        }
    }
}
