using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//TODO: Globalize aliases (via Namespaces?)

using Real32 = System.Single;
using Units = System.Single;

//TODO: Implement(?) Coordinate System Library
using BlockCoord = UnityEngine.Vector2Int;
using WorldCoord = UnityEngine.Vector3;
using System;

//Immutable class formalizing access to mesh triangles
struct MeshTriangles
{
    private int[] RawTriangles { get; set; }
    private List<int[]> TriangleList { get; set; }

    public MeshTriangles(int[] triangles)
    {
        if (triangles.Length % 3 != 0)
        {
            throw new Exception("Invalid triangle indices!");

        }
        RawTriangles = triangles;

        TriangleList = new List<int[]>(triangles.Length + 20);
        for (int ind_triple = 0; ind_triple < triangles.Length / 3; ind_triple++)
        {
            int firstVertexIndex = ind_triple * 3;
            int[] currentTriangleIndices = new int[]
            {
                triangles[firstVertexIndex],
                triangles[firstVertexIndex + 1],
                triangles[firstVertexIndex + 2]
            };
            TriangleList.Add(currentTriangleIndices);
        }
    }
    public int[] GetTriangles(int[] inds)
    {
        List<int> resultList = new List<int>(inds.Length * 3 + 5);
        foreach (int ind in inds)
        {
            if (ind >= TriangleList.Count)
            {
                throw new Exception("Mesh triangle index is invalid");
            }
            resultList.AddRange(TriangleList[ind]);
        }
        return resultList.ToArray();
    }
    public int[] GetTrianglesExcept(int[] inds)
    {
        var allIndices = Enumerable.Range(0, TriangleList.Count);
        int[] exceptIndices = allIndices.Except(inds).ToArray();
        return GetTriangles(exceptIndices);
    }
}

//Immutable class representing axis information.
struct GridAxis
{
    public uint Index { get; private set; }
    public Real32 Length { get; private set; }
    public Vector3 UnitVector { get; private set; }

    public GridAxis(uint ind, Real32 len)
    {
        Index = ind;
        Length = len;
        UnitVector = ind == 0 ? Vector3.right : Vector3.up;
    }
}

//Immutable class representing Grid information.
class GridInfo
{
    public Units BorderSize { get; private set; }
    public Units BlockSize { get; private set; }
    public Units CombinedSize { get; private set; }
    public uint GridWidth { get; private set; }
    public uint GridHeight { get; private set; }
    public GameObject GridObject { get; private set; }

    public uint[] Dimensions { get; private set; }
    public Real32 Scaled_borderSize { get; private set; }
    public Real32 Scaled_blockSize { get; private set; }
    public Real32 Scaled_combinedSize { get; private set; }
    public Real32 ScalingCoefficient { get; private set; }
    public GridAxis WeakAxis { get; private set; }
    public GridAxis DominantAxis { get; private set; }
    public Vector3 MeshStart { get; private set; }
    public WorldCoord LowerLeftCorner { get; private set; }

    public GridInfo(GameObject gridObj, Units thickness, uint width, uint height)
    {
        GridObject = gridObj;
        BorderSize = thickness;
        BlockSize = 1.0f;
        CombinedSize = BlockSize + BorderSize;
        GridWidth = width;
        GridHeight = height;
        Dimensions = new uint[2] { width, height };

        uint domIndex = width >= height ? 0u : 1;
        uint weakIndex = (domIndex) ^ 1u;
        Real32 domLen = CalculateAxisLength(domIndex);
        Real32 weakLen = CalculateAxisLength(weakIndex);

        DominantAxis = new GridAxis(domIndex, domLen);
        WeakAxis = new GridAxis(weakIndex, weakLen);

        ScalingCoefficient = CalculateScalingCoefficient();
        Scaled_borderSize = BorderSize * ScalingCoefficient;
        Scaled_blockSize = BlockSize * ScalingCoefficient;
        Scaled_combinedSize = Scaled_blockSize + Scaled_borderSize;

        MeshStart = CalculateMeshStart();
        LowerLeftCorner = new WorldCoord(
            MeshStart.x * GetGridWidthInUnits(),
            MeshStart.z,
            MeshStart.y * GetGridHeightInUnits());

    }

    private Real32 CalculateAxisLength(uint axisIndex)
    {
        Debug.Assert(axisIndex < Dimensions.Length);

        uint axisSize = Dimensions[axisIndex];
        return axisSize * (BlockSize + BorderSize) + BorderSize;
    }

    private Real32 CalculateScalingCoefficient()
    {
        return 1.0f / DominantAxis.Length;
    }

    private Vector3 CalculateMeshStart()
    {
        Real32 weakCoord = -(WeakAxis.Length * ScalingCoefficient) / 2.0f;
        Real32 domCoord = -0.5f;
        return DominantAxis.UnitVector * domCoord + WeakAxis.UnitVector * weakCoord;
    }

    private BlockCoord InvertY(BlockCoord coord)
    {
        int blockY = (int) GridHeight - 1 - coord.y;
        return new BlockCoord(coord.x, blockY);
    }

    public Units GetGridWidthInUnits()
    {
        return CalculateAxisLength(0) * ScalingCoefficient;
    }

    public Units GetGridHeightInUnits()
    {
        return CalculateAxisLength(1) * ScalingCoefficient;
    }

    //TODO: LowerLeftCorner doesn't work! Use Mesh Corner for now
    //      The importance of lowerleftcorner is it is independant
    //      from mesh Size. I think... :/
    //TODO: UNSAFE, arbitrarily swaps x and y.
    //      not abstract!!!
    public WorldCoord GetCornerCoord()
    {
        return new WorldCoord(MeshStart.x, 0, MeshStart.y);
        //return LowerLeftCorner;
    }

    public BlockCoord GetBlockCoordFromWorldCoord(WorldCoord coord)
    {
        var start = GetCornerCoord();
        var dist = coord - start;

        var resultX = (int)Mathf.Floor((dist.x) / Scaled_combinedSize);
        resultX = (int)Mathf.Clamp(resultX, 0, GridWidth - 1);

        var resultY = (int)Mathf.Floor((dist.z) / Scaled_combinedSize);
        resultY = (int)Mathf.Clamp(resultY, 0, GridHeight - 1);

        return new BlockCoord(resultX, resultY);
    }

    public int[] CoordToTriangleIndices(BlockCoord coord)
    {
        //NOTE: Coord is non-nullable
        if (coord.x >= 0 && coord.x <= GridWidth &&
            coord.y >= 0 && coord.y <= GridHeight)
        {
            coord = InvertY(coord);
            var firstTriangleIndex = (int)(coord.y * GridWidth + coord.x) * 2;
            return new int[] { firstTriangleIndex, firstTriangleIndex + 1 };
        }

        return null;
    }

}

//Immutable class for managing grid's mesh
class MeshGrid
{
    private const int SubMeshCount = 3;
    private const int BorderMeshIndex = 0;
    private const int BlockMeshIndex = 1;
    private const int HighlightMeshIndex = 2;
    private Mesh MainMesh { get; set; }
    private MeshFilter ObjectMeshFilter { get; set; }
    private MeshTriangles MainBlockTriangles { get; set; }

    private List<Vector3> MeshVertices;
    private int[] BlockTriangles { get; set; }
    private int[] BorderTriangles { get; set; }
    private int[] HighlightTriangles { get; set; }
    private GridInfo GridProperties { get; set; }

    private MeshGrid(GridInfo info)
    {
        GridProperties = info;
        ObjectMeshFilter = GridProperties.GridObject.GetComponent("MeshFilter") as MeshFilter;
        MeshVertices = GetVertices();
        BlockTriangles = CalculateBlockTriangles();
        BorderTriangles = CalculateBorderTriangles();
        HighlightTriangles = new int[] { };
        MainBlockTriangles = new MeshTriangles(BlockTriangles);

        UpdateMesh();
    }

    private List<Vector3> GetVertices()
    {
        uint amtVertexRows = 2 * GridProperties.GridHeight + 2;
        uint amtVertexColumns = 2 * GridProperties.GridWidth + 2;

        List<Vector3> result = new List<Vector3>((int)(amtVertexRows * amtVertexColumns));
        Vector3 borderOffset_x = new Vector3(GridProperties.Scaled_borderSize, 0);
        Vector3 borderOffset_y = new Vector3(0, GridProperties.Scaled_borderSize);
        Vector3 blockOffset_x = new Vector3(GridProperties.Scaled_blockSize, 0);
        Vector3 blockOffset_y = new Vector3(0, GridProperties.Scaled_blockSize);


        Vector3 start = GridProperties.MeshStart;
        Vector3 rowStart = new Vector3();
        for (uint row = 0; row < amtVertexRows; row++)
        {
            //NOTE(ugur): Offset the row start
            if (row == 0)
            {
                rowStart = start;
            }
            else if (row % 2 == 0)
            {
                rowStart += blockOffset_y;
            }
            else
            {
                rowStart += borderOffset_y;
            }
            result.Add(rowStart);
            Vector3 rowNext = rowStart;
            for (int column = 1; column < amtVertexColumns; column++)
            {
                //NOTE(ugur): Offset the next vertex
                if (column % 2 == 0)
                {
                    rowNext += blockOffset_x;
                }
                else
                {
                    rowNext += borderOffset_x;
                }

                result.Add(rowNext);
            }
        }

        return result;
    }
    private int[] CalculateBorderTriangles()
    {
        List<int> result = new List<int>();

        int amtTriangleRows = (int)GridProperties.GridHeight * 2 + 1;
        int amtVerticesInRow = (int)GridProperties.GridWidth * 2 + 2;
        for (int row = 0; row < amtTriangleRows; row++)
        {
            int[] firstRowVertices = Enumerable.Range(row * amtVerticesInRow, amtVerticesInRow).ToArray();
            int[] secondRowVertices = Enumerable.Range((row + 1) * amtVerticesInRow, amtVerticesInRow).ToArray();

            int columnIncrement = row % 2 + 1;
            for (int column = 0; column < amtVerticesInRow - 1; column += columnIncrement)
            {
                result.AddRange(firstRowVertices.Skip(column).Take(2));
                result.AddRange(secondRowVertices.Skip(column).Take(1));
                result.AddRange(secondRowVertices.Skip(column).Take(2).Reverse());
                result.AddRange(firstRowVertices.Skip(column + 1).Take(1));
            }
        }

        return result.ToArray();
    }
    private int[] CalculateBlockTriangles()
    {
        List<int> result = new List<int>();

        int amtTriangleRows = (int)GridProperties.GridHeight * 2 + 1;
        int amtVerticesInRow = (int)GridProperties.GridWidth * 2 + 2;
        for (int row = 1; row < amtTriangleRows; row += 2)
        {
            int[] firstRowVertices = Enumerable.Range(row * amtVerticesInRow, amtVerticesInRow).ToArray();
            int[] secondRowVertices = Enumerable.Range((row + 1) * amtVerticesInRow, amtVerticesInRow).ToArray();

            for (int column = 1; column < amtVerticesInRow - 1; column += 2)
            {
                result.AddRange(firstRowVertices.Skip(column).Take(2));
                result.AddRange(secondRowVertices.Skip(column).Take(1));
                result.AddRange(secondRowVertices.Skip(column).Take(2).Reverse());
                result.AddRange(firstRowVertices.Skip(column + 1).Take(1));
            }
        }

        return result.ToArray();
    }
    private Mesh MakeGridMesh()
    {
        Mesh result = new Mesh();
        result.SetVertices(MeshVertices);
        result.subMeshCount = SubMeshCount;
        result.SetTriangles(BorderTriangles, BorderMeshIndex);
        result.SetTriangles(BlockTriangles, BlockMeshIndex);
        result.SetTriangles(HighlightTriangles, HighlightMeshIndex);

        result.RecalculateBounds();
        result.RecalculateNormals();
        result.RecalculateTangents();

        return result;
    }
    private void UpdateMesh()
    {
        MainMesh = MakeGridMesh();
        ObjectMeshFilter.mesh = MainMesh;
    }
 

    //NOTE: We check for x and y but in future we might wanna check 
    //      projections of x and y onto grid.x and grid.y
    //TODO: Abstractify to solve this issue

    private void Highlight(BlockCoord coord)
    {
        Highlight(new BlockCoord[] { coord });
    }
    private void Highlight(BlockCoord[] coords)
    {
        List<int> triangleIndices = new List<int>(coords.Length * 2 + 5);
        foreach (var coord in coords)
        {
            int[] blockTriangleIndices = GridProperties.CoordToTriangleIndices(coord);
            if (blockTriangleIndices != null)
            {
                triangleIndices.AddRange(blockTriangleIndices);
            }
            else
            {
                Debug.Log("Highlight index is out of bounds!");
            }
        }
        var triangleIndexArray = triangleIndices.ToArray();

        HighlightTriangles = MainBlockTriangles.GetTriangles(triangleIndexArray);
        BlockTriangles = MainBlockTriangles.GetTrianglesExcept(triangleIndexArray);
        UpdateMesh();
    }

    public bool IsOnGrid(WorldCoord coord)
    {
        WorldCoord start = GridProperties.GetCornerCoord();
        var width = GridProperties.GetGridWidthInUnits();
        var height = GridProperties.GetGridHeightInUnits();
        if (coord.x >= start.x && coord.x <= start.x + width &&
            coord.z >= start.z && coord.z <= start.z + height &&
            coord.y == GridProperties.GridObject.transform.position.y)
        {
            return true;
        }
        return false;
    }
    public void Highlight(WorldCoord coord)
    {
        Highlight(GridProperties.GetBlockCoordFromWorldCoord(coord));
    }
    public Plane GetGridPlane()
    {
        return new Plane(GridProperties.GridObject.transform.forward, GridProperties.GridObject.transform.position);
    }
    public WorldCoord GetCorner()
    {
        return GridProperties.GetCornerCoord();
    }
    public static MeshGrid Make(GameObject obj, Units borderThickness, uint width, uint height)
    {
        GridInfo info = new GridInfo(obj, borderThickness, width, height);
        return new MeshGrid(info);
    }
}