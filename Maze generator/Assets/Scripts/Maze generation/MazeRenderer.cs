using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MazeRenderer : MonoBehaviour
{
    [SerializeField] private GameObject cam;
 
    [Header("Maze Settings")]
    [Tooltip("X scale of the maze")]
    [Range(5, 250)][SerializeField] private int width;
    [Tooltip("Y scale of the maze")]
    [Range(5, 250)][SerializeField] private int height;

    [Tooltip("The scale of each individual cell. cell.localScale * cellSize")]
    [SerializeField] private float cellSize = 1;

    [Tooltip("Time between generation steps (what counts as a \"step\" depends on the algorithms)")]
    [SerializeField] private float generationDelay = 0.015f;

    [Header("Visuals")]
    [SerializeField] private Color wallColour = Color.black;
    [SerializeField] private Color highlightColor = Color.blue;
    [SerializeField] private Color bottomPlaneColor = Color.white;
    [SerializeField] private float wallThickness = 0.15f;

    [Header("Algorithm")]
    [Tooltip("The maze generation algorithms available for this renderer. The first algorithms in the list is used to generate the maze.")]
    [SerializeField] private MazeGenerationAlgorithm[] algorithms;


    private CellState[,] cells;
    private GameObject[,] horizontalWalls;
    private GameObject[,] verticalWalls;
    private GameObject highlightCube;
    private GameObject bottomPlane;

    private void Start()
    {
        GenerateMaze();
    }


    //Temporary devtool to help with debugging.
    //If you(the reviewer sees this, my bad. sloppy work on my end.)
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateMaze();
        }
    }


    //Reset all the data from the last generated maze (if there was one)
    //Creates all the necessary data for the next maze (eg: walls, bottom plane, highlight cube, cells)

    public void GenerateMaze()
    {
        StopAllCoroutines();
        ClearMaze();

        cells = new CellState[width, height];
        horizontalWalls = new GameObject[width, height + 1];
        verticalWalls = new GameObject[width + 1, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new CellState(false, CellWalls.All);
            }
        }

        var highestVal = width >= height ? height : width;
        cam.transform.position = new Vector3(0, highestVal * cellSize, 0);

        GenerateBottomPlane();
        BuildAllWalls();
        CreateHighlightCube();

        StartCoroutine(algorithms[0].Generate(
            width,
            height,
            cells,
            OnCellVisited,
            OnWallRemoved,
            generationDelay
        ));
    }


    //Creates an border for the maze
    void BuildAllWalls()
    {
        // Horizontal walls (above each row)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                horizontalWalls[x, y] = CreateWall(x + 0.5f, y, cellSize, wallThickness);
            }
        }

        // Vertical walls (left of each column)
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                verticalWalls[x, y] = CreateWall(x, y + 0.5f, wallThickness, cellSize);
            }
        }
    }

    void GenerateBottomPlane()
    {
        if (bottomPlane == null)
        {
            bottomPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        bottomPlane.transform.localScale = new Vector3(width * cellSize, 0.5f, height * cellSize);
        bottomPlane.transform.localPosition = new Vector3(0, -0.5f, 0);
        bottomPlane.GetComponent<MeshRenderer>().material.color = bottomPlaneColor;
    }

    GameObject CreateWall(float gridX, float gridZ, float scaleX, float scaleZ)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.SetParent(transform);
        wall.transform.localPosition = new Vector3(
            (gridX - width * 0.5f) * cellSize,
            0f,
            (gridZ - height * 0.5f) * cellSize
        );
        wall.transform.localScale = new Vector3(scaleX, 0.5f, scaleZ);
        wall.GetComponent<MeshRenderer>().material.color = wallColour;
        return wall;
    }

    void OnCellVisited(int x, int y)
    {
        MoveHighlight(x, y);
    }

    //Destroys the gameobject at the given poition(
    void OnWallRemoved(int x, int y, CellWalls removedWall)
    {
        // Remove the visual wall(s) that were just carved
        if ((removedWall & CellWalls.North) != 0 && horizontalWalls[x, y + 1] != null)
        {
            Destroy(horizontalWalls[x, y + 1]);
        }

        if ((removedWall & CellWalls.South) != 0 && horizontalWalls[x, y] != null)
        {
            Destroy(horizontalWalls[x, y]);
        }

        if ((removedWall & CellWalls.East) != 0 && verticalWalls[x + 1, y] != null)
        {
            Destroy(verticalWalls[x + 1, y]);
        }

        if ((removedWall & CellWalls.West) != 0 && verticalWalls[x, y] != null)
        {
            Destroy(verticalWalls[x, y]);
        }
    }

    void CreateHighlightCube()
    {
        highlightCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        highlightCube.transform.SetParent(transform);
        highlightCube.transform.localScale = Vector3.one * cellSize * (1 - wallThickness);
        var rend = highlightCube.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Unlit/Color"));
        rend.material.color = highlightColor;
        highlightCube.SetActive(false);
    }


    void MoveHighlight(int x, int y)
    {
        //The middle of the current cell
        Vector3 pos = new Vector3(
            (x + 0.5f - width * 0.5f) * cellSize,
            0.1f,
            (y + 0.5f - height * 0.5f) * cellSize
        );

        highlightCube.transform.localPosition = pos;
        highlightCube.SetActive(true);
    }

    // Called after generation finishes (simple delay)
    private void LateUpdate()
    {
        if (algorithms != null && !IsGenerationRunning())
        {
            OpenEntranceAndExit();
        }
    }

    //Expensive. Rewrite if there's leftover time
    private bool IsGenerationRunning()
    {
        if (cells == null) return false;

        //Loops over each cell to see if there are still unchecked cells. If so the generation is still running
        foreach (var cell in cells)
        {
            if (!cell.Visited) return true;
        }
        return false;
    }

    void OpenEntranceAndExit()
    {
        //Entrance: top-left north wall
        if (horizontalWalls[0, 0] != null)
        {
            Destroy(horizontalWalls[0, 0]);
        }

        //Exit: bottom-right south wall
        if (horizontalWalls[width - 1, height] != null)
        {
            Destroy(horizontalWalls[width - 1, height]);
        }

        //Hide highlight when done
        if (highlightCube) highlightCube.SetActive(false);
    }


    void ClearMaze()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}