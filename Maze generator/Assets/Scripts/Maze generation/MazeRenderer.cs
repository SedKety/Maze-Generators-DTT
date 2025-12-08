using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MazeRenderer : MonoBehaviour
{
    [SerializeField] private GameObject cam;
    [Header("Maze Settings")]
    [Tooltip("X scale of the maze")]
    [Range(1, 1000)][SerializeField] private int width;
    [Tooltip("Y scale of the maze")]
    [Range(1, 1000)][SerializeField] private int height;
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

    //GPU instancing
    private Mesh wallMesh;
    private Material wallMaterial;
    private Matrix4x4[] horizontalMatrices;
    private Matrix4x4[] verticalMatrices;
    private const int INSTANCES_PER_BATCH = 1023;
    //Unity has a limit on how many instance can be in one batch which is 1024,
    //Unity reserves one allowing us to take the remaining 1023

    private GameObject highlightCube;
    private GameObject bottomPlane;

    private void Awake()
    {
        // Create a shared mesh + material for all walls
        wallMesh = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(GameObject.Find("Cube")); // cleanup the temporary cube

        wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")); // or "Standard" if using Built-in RP
        wallMaterial.color = wallColour;
        wallMaterial.enableInstancing = true;
    }

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

    public void GenerateMaze()
    {
        StopAllCoroutines();
        ClearMaze();
        cells = new CellState[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new CellState(false, CellWalls.All);
            }
        }

        var highestVal = width >= height ? width : height;
        if (highestVal < 5) highestVal = 5;
        cam.transform.position = new Vector3(0, highestVal * cellSize, 0);

        GenerateBottomPlane();
        AllocateInstancingBuffers();
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

    void AllocateInstancingBuffers()
    {
        //Horizontal walls
        horizontalMatrices = new Matrix4x4[width * (height + 1)];

        //Vertical walls
        verticalMatrices = new Matrix4x4[(width + 1) * height];

        //Create an unmodified grid
        for (int i = 0; i < horizontalMatrices.Length; i++) horizontalMatrices[i] = Matrix4x4.identity;
        for (int i = 0; i < verticalMatrices.Length; i++) verticalMatrices[i] = Matrix4x4.identity;

        //Build all walls initially (full grid)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                int idx = x + y * width;
                horizontalMatrices[idx] = GetWallMatrix(x + 0.5f, y, cellSize, wallThickness);
            }
        }
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int idx = x + y * (width + 1);
                verticalMatrices[idx] = GetWallMatrix(x, y + 0.5f, wallThickness, cellSize);
            }
        }
    }

    Matrix4x4 GetWallMatrix(float gridX, float gridZ, float scaleX, float scaleZ)
    {
        Vector3 pos = new Vector3(
            (gridX - width * 0.5f) * cellSize,
            0f,
            (gridZ - height * 0.5f) * cellSize
        );
        Vector3 scale = new Vector3(scaleX, 0.5f, scaleZ);
        return Matrix4x4.TRS(pos, Quaternion.identity, scale);
    }

    void GenerateBottomPlane()
    {
        if (bottomPlane == null)
        {
            bottomPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomPlane.transform.parent = transform;
        }
        bottomPlane.transform.localScale = new Vector3(width * cellSize, 0.5f, height * cellSize);
        bottomPlane.transform.localPosition = new Vector3(0, -0.5f, 0);
        bottomPlane.GetComponent<MeshRenderer>().material.color = bottomPlaneColor;
    }

    void OnCellVisited(int x, int y)
    {
        MoveHighlight(x, y);
    }

    void OnWallRemoved(int x, int y, CellWalls removedWall)
    {
        //For clarification on as to why i dont just remove the walls:
        //Editing an array that big is way more expensive then just "empty-rendering"
        if ((removedWall & CellWalls.North) != 0)
        {
            int idx = x + (y + 1) * width;
            horizontalMatrices[idx] = Matrix4x4.zero; 
        }
        if ((removedWall & CellWalls.South) != 0)
        {
            int idx = x + y * width;
            horizontalMatrices[idx] = Matrix4x4.zero;
        }
        if ((removedWall & CellWalls.East) != 0)
        {
            int idx = (x + 1) + y * (width + 1);
            verticalMatrices[idx] = Matrix4x4.zero;
        }
        if ((removedWall & CellWalls.West) != 0)
        {
            int idx = x + y * (width + 1);
            verticalMatrices[idx] = Matrix4x4.zero;
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
        Vector3 pos = new Vector3(
            (x + 0.5f - width * 0.5f) * cellSize,
            0.1f,
            (y + 0.5f - height * 0.5f) * cellSize
        );
        highlightCube.transform.localPosition = pos;
        highlightCube.SetActive(true);
    }

    private void LateUpdate()
    {
        RenderInstancedWalls();

        if (algorithms != null && !IsGenerationRunning())
        {
            OpenEntranceAndExit();
        }
    }

    void RenderInstancedWalls()
    {
        if (wallMesh == null || wallMaterial == null) return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", wallColour); 

        //Horizontal walls
        for (int i = 0; i < horizontalMatrices.Length; i += INSTANCES_PER_BATCH)
        {
            int count = Mathf.Min(INSTANCES_PER_BATCH, horizontalMatrices.Length - i);
            Matrix4x4[] batch = new Matrix4x4[count];
            for (int j = 0; j < count; j++)
            {
                batch[j] = horizontalMatrices[i + j];
            }
            Graphics.DrawMeshInstanced(wallMesh, 0, wallMaterial, batch, count, block, UnityEngine.Rendering.ShadowCastingMode.On, true);
        }

        //Vertical walls
        for (int i = 0; i < verticalMatrices.Length; i += INSTANCES_PER_BATCH)
        {
            int count = Mathf.Min(INSTANCES_PER_BATCH, verticalMatrices.Length - i);
            Matrix4x4[] batch = new Matrix4x4[count];
            for (int j = 0; j < count; j++)
            {
                batch[j] = verticalMatrices[i + j];
            }
            Graphics.DrawMeshInstanced(wallMesh, 0, wallMaterial, batch, count, block, UnityEngine.Rendering.ShadowCastingMode.On, true);
        }
    }

    private bool IsGenerationRunning()
    {
        if (cells == null) return false;
        foreach (var cell in cells)
        {
            if (!cell.Visited) return true;
        }
        return false;
    }

    //Destroys the entrance and eit walls
    void OpenEntranceAndExit()
    {
        //Entrance cell: bottom left cell, bottom wall
        horizontalMatrices[0 + 0 * width] = Matrix4x4.zero;

        //Exit cell: top right, top wall
        horizontalMatrices[(width - 1) + height * width] = Matrix4x4.zero;

        if (highlightCube)
        {
            highlightCube.SetActive(false);
        }
    }

    void ClearMaze()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i).gameObject != bottomPlane && transform.GetChild(i).gameObject != highlightCube)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        if (wallMaterial) Destroy(wallMaterial);
    }
}