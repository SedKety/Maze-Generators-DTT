using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MazeRenderer : MonoBehaviour
{
    [Header("Maze Settings")]

    [Tooltip("The scale of each individual cell. cell.localScale * cellSize")]
    [SerializeField] private float cellSize = 1;

    [Header("Visuals")]
    [SerializeField] private Color wallColour = Color.black;
    [SerializeField] private Color highlightColor = Color.blue;
    [SerializeField] private Color bottomPlaneColor = Color.white;
    [SerializeField] private float wallThickness = 0.15f;
    [SerializeField] private Material floorMat;
 

    [Header("Settings")]
    [SerializeField] private Settings settings;
    private MazeGenerationAlgorithm algorithm;
    private float _generationDelay = 0.015f;
    private int _width;
    private int _height;

    private CellState[,] cells;

    [Header("GPU instancing")]
    private Mesh wallMesh;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material highlightMaterial;

    private Matrix4x4[] horizontalMatrices = new Matrix4x4[0];
    private Matrix4x4[] verticalMatrices = new Matrix4x4[0];

    //Unity has a limit on how many instance can be in one batch which is 1024,
    //Unity reserves one allowing us to take the remaining 1023
    private const int INSTANCES_PER_BATCH = 1023;

    private GameObject highlightCube;
    private GameObject bottomPlane;

    private void Awake()
    {
        //Create a shared mesh + material for all walls
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallMesh = wall.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(wall); //cleanup the temporary cube

        wallMaterial.color = wallColour;
        wallMaterial.enableInstancing = true;
    }

    private void Start()
    {
        settings.OnGenerationEvent += GenerateMaze;
    }

    public void GenerateMaze(MazeGenerationAlgorithm MGA, int width, int height, float generationDelay)
    {
        algorithm = MGA;
        _width = width;
        _height = height;
        _generationDelay = generationDelay;

        StopAllCoroutines();
        ClearMaze();

        if (highlightCube != null)
        {
            highlightCube.SetActive(false);
        }

        cells = new CellState[_width, _height];

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                cells[x, y] = new CellState(false, CellWalls.All);
            }
        }

        GenerateBottomPlane();
        AllocateInstancingBuffers();
        CreateHighlightCube();

        StartCoroutine(algorithm.Generate(
            _width,
            _height,
            cells,
            OnCellVisited,
            OnWallRemoved,
            _generationDelay
        ));
    }

    void AllocateInstancingBuffers()
    {
        //Horizontal walls
        horizontalMatrices = new Matrix4x4[_width * (_height + 1)];

        //Vertical walls
        verticalMatrices = new Matrix4x4[(_width + 1) * _height];

        //Create an unmodified grid
        for (int i = 0; i < horizontalMatrices.Length; i++) horizontalMatrices[i] = Matrix4x4.identity;
        for (int i = 0; i < verticalMatrices.Length; i++) verticalMatrices[i] = Matrix4x4.identity;

        //Build all walls initially (full grid)
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y <= _height; y++)
            {
                int idx = x + y * _width;
                horizontalMatrices[idx] = GetWallMatrix(x + 0.5f, y, cellSize, wallThickness);
            }
        }
        for (int x = 0; x <= _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int idx = x + y * (_width + 1);
                verticalMatrices[idx] = GetWallMatrix(x, y + 0.5f, wallThickness, cellSize);
            }
        }
    }

    Matrix4x4 GetWallMatrix(float gridX, float gridZ, float scaleX, float scaleZ)
    {
        Vector3 pos = new Vector3(
            (gridX - _width * 0.5f) * cellSize,
            0f,
            (gridZ - _height * 0.5f) * cellSize
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
        bottomPlane.transform.localScale = new Vector3(_width * cellSize, 0.5f, _height * cellSize);
        bottomPlane.transform.localPosition = new Vector3(0, -0.5f, 0);
        var renderer = bottomPlane.GetComponent<Renderer>();
        renderer.material = floorMat;
        renderer.material.color = bottomPlaneColor;
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
            int idx = x + (y + 1) * _width;
            horizontalMatrices[idx] = Matrix4x4.zero; 
        }
        if ((removedWall & CellWalls.South) != 0)
        {
            int idx = x + y * _width;
            horizontalMatrices[idx] = Matrix4x4.zero;
        }
        if ((removedWall & CellWalls.East) != 0)
        {
            int idx = (x + 1) + y * (_width + 1);
            verticalMatrices[idx] = Matrix4x4.zero;
        }
        if ((removedWall & CellWalls.West) != 0)
        {
            int idx = x + y * (_width + 1);
            verticalMatrices[idx] = Matrix4x4.zero;
        }
    }

    void CreateHighlightCube()
    {
        highlightCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        highlightCube.transform.SetParent(transform);
        highlightCube.transform.localScale = Vector3.one * cellSize * (1 - wallThickness);
        var rend = highlightCube.GetComponent<Renderer>();
        rend.material = highlightMaterial;
        rend.material.color = highlightColor;
        highlightCube.SetActive(false);
    }

    void MoveHighlight(int x, int y)
    {
        Vector3 pos = new Vector3(
            (x + 0.5f - _width * 0.5f) * cellSize,
            0.1f,
            (y + 0.5f - _height * 0.5f) * cellSize
        );
        highlightCube.transform.localPosition = pos;
        highlightCube.SetActive(true);
    }

    private void LateUpdate()
    {
        RenderInstancedWalls();

        if (algorithm != null && !IsGenerationRunning())
        {
            OpenEntranceAndExit();
        }
    }

    void RenderInstancedWalls()
    {
        if (wallMesh == null || wallMaterial == null) return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", wallColour);

        //Prevent calling before generation
        if (horizontalMatrices.Length <= 0) return;
        if (verticalMatrices.Length <= 0) return;


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
        horizontalMatrices[0 + 0 * _width] = Matrix4x4.zero;

        //Exit cell: top right, top wall
        horizontalMatrices[(_width - 1) + _height * _width] = Matrix4x4.zero;

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