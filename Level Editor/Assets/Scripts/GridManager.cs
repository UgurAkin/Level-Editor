using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Real32 = System.Single;
using Units = System.Single;
using BlockCoord = UnityEngine.Vector2Int;
using WorldCoord = UnityEngine.Vector3;

//TODO: Singleton


//[ExecuteInEditMode]
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Range(0.0f, 1.0f)]
    public Units BorderThickness = 0.1f;
    public uint GridWidth = 5;
    public uint GridHeight = 5;
    [SerializeField]
    private GameObject GridGameObject;
    private MeshGrid Grid;

    private GridManager() : base()
    { }

    //TODO: add mutability to meshes (complicated)
    //TODO: Bad DesignPattern, fix pattern.
    private void Awake()
    {
        GridGameObject = this.gameObject;
        Grid = MeshGrid.Make(GridGameObject, BorderThickness, GridWidth, GridHeight);
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        Grid = MeshGrid.Make(GridGameObject, BorderThickness, GridWidth, GridHeight);
    }

    public bool RaycastToGrid(Ray ray, out Vector3 intersect)
    {
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red);
        Plane gridPlane = Grid.GetGridPlane();
        DebugDrawPlane(gridPlane.normal, Grid.GetCorner());

        Real32 entryDistance;
        bool intersected = gridPlane.Raycast(ray, out entryDistance);
        if (intersected)
        {
            intersect = ray.GetPoint(entryDistance);
            Debug.DrawRay(intersect, gridPlane.normal,Color.blue);
            if (Grid.IsOnGrid(intersect))
            {
                return true;
            }
        }
        intersect = new Vector3();
        return false;
    }

    public void HighlightBlockAtWorldCoord(WorldCoord coord)
    {
        Grid.Highlight(coord);
    }

    private void DebugDrawPlane(Vector3 normal, WorldCoord position)
    {

        var v3 = new Vector3();

        if (normal.normalized != Vector3.forward)
            v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
        else
            v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude; ;

        var corner0 = position + v3;
        var corner2 = position - v3;
        var q = Quaternion.AngleAxis(90f, normal);
        v3 = q * v3;
        var corner1 = position + v3;
        var corner3 = position - v3;

        Debug.DrawLine(corner0, corner2, Color.green);
        Debug.DrawLine(corner1, corner3, Color.green);
        Debug.DrawLine(corner0, corner1, Color.green);
        Debug.DrawLine(corner1, corner2, Color.green);
        Debug.DrawLine(corner2, corner3, Color.green);
        Debug.DrawLine(corner3, corner0, Color.green);
        Debug.DrawRay(position, normal, Color.red);

    }
}


