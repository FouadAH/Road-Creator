using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathCreator : MonoBehaviour
{
    [HideInInspector]
    public Path path;
    public Transform pivotTransfom;

    public Color anchorCol = Color.red;
    public Color controlCol = Color.white;
    public Color segmentCol = Color.green;
    public Color selectedSegmentCol = Color.yellow;
    public Color selectedAnchorCol = Color.yellow;

    public float anchorDiameter = .1f;
    public float controlDiameter = .075f;
    public bool displayControlPoints = true;

    public float displayResolusion = 2f;
    public bool displayNormals = false;
    public bool displayDirection = false;

    public float mouseDistanceMuliplier = 1f;
    public int mouseDistanceSampleSize = 50;

    [Range(0.5f, 10f)]
    public float spacing = 1f;

    public float globalNormalAngle = 0;
    public float maxRadiansDelta;
    public float maxMagnitudeDelta;

    public void CreatePath()
    {
        path = new Path(transform.position);
    }

    void Reset()
    {
        CreatePath();
    }
}
