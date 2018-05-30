using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WorldCoord = UnityEngine.Vector3;

[ExecuteInEditMode]
public class Hover : MonoBehaviour
{

    public Camera MainCamera;
    GridManager gridManager;
    // Use this for initialization
    void Start()
    {
        gridManager = GridManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (gridManager == null)
        {
            return;
        }
        var hoverRay = MainCamera.ScreenPointToRay(Input.mousePosition);
        WorldCoord intersectPoint;
        if (gridManager.RaycastToGrid(hoverRay, out intersectPoint))
        {
            gridManager.HighlightBlockAtWorldCoord(intersectPoint);
        }
    }
}
