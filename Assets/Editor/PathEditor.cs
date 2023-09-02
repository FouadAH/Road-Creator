using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathCreator))]
public class PathEditor : Editor
{
    PathCreator creator;
    Path Path
    {
        get { return creator.path; }
    }

    const float segmentSelectDistanceThreshold = 1f;
    const float minDistanceToAnchorThreshold = 1f; 

    int currentSelectedSegmentIndex = -1; 
    Vector3 selecterSegmentPoint;
    int closestAnchorIndex = -1;
    int selectedAnchorIndex = -1;

    public override void OnInspectorGUI()
    {
        string[] options = new string[] { "3D - XYZ", "2D - XY", "2D - ZX" };
        creator.path.SelectedAxis = EditorGUILayout.Popup("Space", creator.path.SelectedAxis, options);

        string[] controlPointsOptions = new string[] { "Free", "Aligned", "Mirrored" };
        SwitchControlMode(EditorGUILayout.Popup("Control Point Mode", (int)creator.path.controlMode, controlPointsOptions));

        base.OnInspectorGUI();

        EditorGUI.BeginChangeCheck();

        if (GUILayout.Button("Create New Path"))
        {
            Undo.RecordObject(creator, "Create New");

            creator.CreatePath();
        }

        if (GUILayout.Button("Recalculate Spline Points"))
        {
            Undo.RecordObject(creator, "Calculate Spline Points");

            creator.path.CalculateSplinePointsData(creator.path.Spacing);
        }

        bool isClosedPath = GUILayout.Toggle(Path.IsClosed, "Toggle Closed");
        if (isClosedPath != Path.IsClosed)
        {
            Undo.RecordObject(creator, "Toggle closed");
            Path.IsClosed = isClosedPath;
        }

        bool autoSetControlPoints = GUILayout.Toggle(Path.AutoSetControlPoints, "Auto Set Control Points");
        if(autoSetControlPoints != Path.AutoSetControlPoints)
        {
            Undo.RecordObject(creator, "Toggle auto set control points");
            Path.AutoSetControlPoints = autoSetControlPoints;
        }

        if (creator.path.Spacing != creator.spacing)
        {
            creator.path.Spacing = creator.spacing; 
        }

        if (creator.path.GlobalNormalAngle != creator.globalNormalAngle)
        {
            creator.path.GlobalNormalAngle = creator.globalNormalAngle;
            creator.path.MaxMagnitudeDelta = creator.maxMagnitudeDelta;
            creator.path.MaxRadiansDelta = creator.maxRadiansDelta;
        }

        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        Input();
        Draw();
    }

    void Input()
    {
        Event guiEvent = Event.current;
        Vector3 origin = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;
        Vector3 direction = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).direction;
        Vector3 mousePosition = origin;
        Vector2 mousePosition2D = mousePosition;

        Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
        RaycastHit hitInfo;

        Undo.RecordObject(creator, "offset Points");
        
        Vector3 newPos = Handles.PositionHandle(creator.pivotTransfom.position, Quaternion.identity);
        creator.pivotTransfom.position = newPos;
        creator.path.OffsetPosition = newPos - creator.path.oldPosition;
        creator.path.oldPosition = newPos;
        creator.path.offsetRotation = creator.transform.rotation;
        creator.path.offsetScale = creator.transform.localScale;

        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity))
        {
            mousePosition = hitInfo.point;
        }

        if (creator.path.SelectedAxis == 3)
        {
            mousePosition = origin + direction * 50f;
        }

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
        {
            if (currentSelectedSegmentIndex != -1)
            {
                Undo.RecordObject(creator, "Split Segment");
                Path.SplitSegment(selecterSegmentPoint, currentSelectedSegmentIndex);
            }
            else if(!Path.IsClosed)
            {
                Undo.RecordObject(creator, "Add Segment");
                Path.AddSegment(mousePosition);
            }
        }

        if (Event.current.control)
        {
            int nearestControl = HandleUtility.nearestControl;
            bool isAnchor = (nearestControl - 1) % 3 == 0;

            if (Event.current.type == EventType.MouseDown && nearestControl > 0 && nearestControl <= creator.path.NumberOfPoints)
            {
                closestAnchorIndex = HandleUtility.nearestControl - 1;
            }

            if (isAnchor)
            {
                if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
                {
                    if (closestAnchorIndex != -1)
                    {
                        Undo.RecordObject(creator, "Delete Segment");
                        Path.DeleteSegment(closestAnchorIndex);
                    }
                }
            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1)
            {
                if (closestAnchorIndex != -1)
                {
                    Undo.RecordObject(creator, "Select Point");
                    selectedAnchorIndex = closestAnchorIndex;
                }
            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1 && HandleUtility.nearestControl == 0)
            {
                closestAnchorIndex = -1;
                selectedAnchorIndex = -1;
            }
        }
        else
        {
            closestAnchorIndex = -1;
        }

        if (guiEvent.type == EventType.MouseMove && Event.current.shift)
        {
            float minDistanceToSegement = segmentSelectDistanceThreshold;
            int newSelectedSegmentIndex = -1;

            for (int distanceAlongRay = 0; distanceAlongRay < creator.mouseDistanceSampleSize; distanceAlongRay++)
            {
                Vector3 pointOnMouseRay = ray.GetPoint(distanceAlongRay * creator.mouseDistanceMuliplier);

                for (int i = 0; i < Path.NumberOfSegments; i++)
                {
                    Vector3[] points = Path.GetPointsInSegment(i);

                    float distance = HandleUtility.DistancePointBezier(pointOnMouseRay, points[0], points[3], points[1], points[2]);
                    if (distance < minDistanceToSegement)
                    {
                        minDistanceToSegement = distance;
                        newSelectedSegmentIndex = i;
                        selecterSegmentPoint = pointOnMouseRay;
                    }
                }

                if (newSelectedSegmentIndex != -1)
                {
                    break;
                }
            }

            if(newSelectedSegmentIndex != currentSelectedSegmentIndex)
            {
                currentSelectedSegmentIndex = newSelectedSegmentIndex;
                HandleUtility.Repaint();
            }
        }
        HandleUtility.AddDefaultControl(0);
    }

    private void OnEnable()
    {
        creator = (PathCreator)target;

        if(creator.path == null)
        {
            creator.CreatePath();
        }
    }

    void Draw()
    {
        for (int i = 0; i < Path.NumberOfSegments; i++)
        {
            Vector3[] points = Path.GetPointsInSegment(i);

            if (creator.displayControlPoints)
            {
                Handles.color = Color.black;
                Handles.DrawLine(points[0], points[1]);
                Handles.DrawLine(points[2], points[3]);
            }

            Color segmentColor = (i == currentSelectedSegmentIndex && Event.current.shift) ? creator.selectedSegmentCol: creator.segmentCol;
            Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentColor, null, 2);
        }

        for (int i = 0; i < Path.NumberOfPoints; i++)
        { 
            if (i % 3 == 0 || creator.displayControlPoints)
            {
                Handles.color = (i % 3 == 0) ? creator.anchorCol : creator.controlCol;

                if (selectedAnchorIndex == i)
                {
                    Handles.color = creator.selectedAnchorCol;
                }

                float diameter = (i % 3 == 0) ? creator.anchorDiameter: creator.controlDiameter;
                var fmh_249_71_638284833412831475 = Quaternion.identity; Vector3 newPos = Handles.FreeMoveHandle(i+1, Path[i], diameter, Vector2.zero, Handles.SphereHandleCap);
               
                if (selectedAnchorIndex == i)
                {
                    if (Tools.current == Tool.Move)
                    {
                        newPos = Handles.PositionHandle(newPos, Quaternion.identity);
                    }
                    else if(Tools.current == Tool.Rotate && i % 3 == 0)
                    {                        
                        int splineIndex = (i >= creator.path.NumberOfPoints - 1 && !creator.path.IsClosed) ? (i / 3) - 1 : (i / 3);
                        Quaternion newRot = Quaternion.Euler(creator.path.splines[splineIndex].rotationAmount, 0, 0).normalized;

                        Vector3 fwd = SceneView.GetAllSceneCameras()[0].transform.forward;
                        Vector3 up = SceneView.GetAllSceneCameras()[0].transform.up;

                        newRot = Handles.Disc(newRot, newPos, fwd, 10, true, 0);
                        if (newRot != Quaternion.identity)
                        {
                            float angle = Vector3.Angle(Vector3.up, newRot * Vector3.up);
                            Handles.DrawSolidArc(newPos, fwd, up, angle, 8f);

                            Undo.RecordObject(creator, "Rotate Point");
                            creator.path.RotateLocalNormals(i, angle);
                        }
                    }
                }

                if (Path[i] != newPos)
                {
                    Undo.RecordObject(creator, "Move Point");
                    Path.MovePoint(i, newPos);
                }
            }
        } 

        if (creator.path.splines != null)
        {
            if ((creator.displayNormals || creator.displayDirection) && creator.path.splines.Count > 0)
            {
                for (int i = 0; i < creator.path.NumberOfSegments; i++)
                {
                    for (int j = 0; j < creator.path.splines[i].numberOfPoints; j++)
                    {
                        if (creator.displayDirection) Debug.DrawRay(creator.path.splines[i].points[j], creator.path.splines[i].directions[j] * 4f, Color.cyan);
                        if (creator.displayNormals) Debug.DrawRay(creator.path.splines[i].points[j], creator.path.splines[i].normals[j], Color.yellow);
                    }
                }
            }
        }
    }

    void SwitchControlMode(int controlModeIndex)
    {
        creator.path.controlMode = controlModeIndex switch
        {
            (int)BezierControlPointMode.Free => BezierControlPointMode.Free,
            (int)BezierControlPointMode.Aligned => BezierControlPointMode.Aligned,
            (int)BezierControlPointMode.Mirrored => BezierControlPointMode.Mirrored,
            _ => BezierControlPointMode.Mirrored,
        };
    }

}
