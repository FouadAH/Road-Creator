using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Path
{
    [SerializeField, HideInInspector]
    List<Vector3> points;

    [SerializeField, HideInInspector]
    bool isClosedPath;

    [SerializeField, HideInInspector]
    bool autoSetControlPoints;

    [SerializeField, HideInInspector]
    int selectedAxis;

    [SerializeField, HideInInspector]
    float spacing;

    [SerializeField, HideInInspector]
    public BezierControlPointMode controlMode;

    [SerializeField]
    public List<Spline> splines = new List<Spline>();

    [SerializeField]
    List<Vector3> evenlySpacedPoints = new List<Vector3>();

    [SerializeField]
    List<Vector3> evenlySpacedDirections = new List<Vector3>();

    [SerializeField]
    List<Vector3> evenlySpacedNormals = new List<Vector3>();

    [SerializeField]
    List<float> normalRotationAmount = new List<float>();

    public Path(Vector3 center)
    {
        points = new List<Vector3>
        {
            center + Vector3.left,
            center + (Vector3.left + Vector3.up) * 0.5f,
            center + Vector3.right,
            center + (Vector3.right + Vector3.down) * 0.5f
        };

        evenlySpacedPoints = new List<Vector3>();
        evenlySpacedDirections = new List<Vector3>();
        evenlySpacedNormals = new List<Vector3>();
        normalRotationAmount = new List<float>();

        CalculateSplinePointsData(spacing);
    }

    public Vector3 this[int index] { get { return points[index]; } }

    Vector3 offsetAmount;
    public Vector3 oldPosition;
    public Quaternion offsetRotation;
    public Vector3 offsetScale;

    public float Spacing
    {
        get { return spacing; }
        set { spacing = value; CalculateSplinePointsData(spacing); }
    }

    public int SelectedAxis
    {
        get { return selectedAxis; }
        set { selectedAxis = value; FixAxisCheck(); }
    }

    public Vector3 OffsetPosition
    {
        get { return offsetAmount; }
        set 
        { 
            //offsetAmount = value;
            //for (int i = 0; i < points.Count; i++)
            //{
            //    points[i] += offsetAmount;
            //}

            //CalculateSplinePointsData(spacing);
        }
    }

    public bool IsClosed
    {
        get{ return isClosedPath; }
        set
        {
            if (isClosedPath != value)
            {
                isClosedPath = value;
                ToggleClosedPath();
            }
        }
    }

    public bool AutoSetControlPoints
    {
        get { return autoSetControlPoints; }
        set
        {
            if(autoSetControlPoints != value)
            {
                autoSetControlPoints = value;
                if (autoSetControlPoints)
                {
                    AutoSetAllControlPoints();
                }
            }
        }
    }
    public int NumberOfSegments { get { return points.Count/3; } }

    public int NumberOfPoints { get { return points.Count; } }

    [SerializeField, HideInInspector]
    float globalNormalAngle;

    float maxRadiansDelta;
    float maxMagnitudeDelta;

    public float GlobalNormalAngle
    {
        get { return globalNormalAngle; }
        set { globalNormalAngle = value; RotateGlobalNormals(); }
    }
    public float MaxRadiansDelta
    {
        get { return maxRadiansDelta; }
        set { maxRadiansDelta = value; RotateGlobalNormals(); }
    }

    public float MaxMagnitudeDelta
    {
        get { return maxMagnitudeDelta; }
        set { maxMagnitudeDelta = value; RotateGlobalNormals(); }
    }

    void ToggleClosedPath()
    {
        if (isClosedPath)
        {
            points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
            points.Add(points[0] * 2 - points[1]);

            if (autoSetControlPoints)
            {
                AutoSetAnchorControlPoints(0);
                AutoSetAnchorControlPoints(points.Count - 3);
            }

            splines.Add(CalculateSplinePoints(NumberOfSegments - 1, spacing));
        }
        else
        {
            points.RemoveRange(points.Count - 2, 2);
            if (autoSetControlPoints)
            {
                AutoSetStartAndEndControls();
            }

            splines.RemoveAt(splines.Count - 1);
        }

        RotateGlobalNormals();
    }

    public void AddSegment(Vector3 anchorPosition)
    {
        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
        points.Add(points[points.Count - 1] + anchorPosition * 0.5f);
        points.Add(anchorPosition);

        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(points.Count -1);
        }

        Spline newSpline = CalculateSplinePoints(NumberOfSegments - 1, spacing);
        splines.Add(newSpline);
    }

    public void SplitSegment(Vector3 anchorPositon, int segmentIndex)
    {
        points.InsertRange(segmentIndex * 3 + 2, new Vector3[] { Vector3.zero, anchorPositon, Vector3.zero});

        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
        }
        else
        {
            AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
        }

        float rotationAmount = splines[segmentIndex].rotationAmount;
        splines.RemoveAt(segmentIndex);

        if (isClosedPath && segmentIndex == 0)
        {
            splines.Insert(segmentIndex, CalculateSplineNormals(segmentIndex, spacing, rotationAmount, 0, 1));
            splines.Insert(1, CalculateSplineNormals(1, spacing, rotationAmount, 1, 0));
        }
        else
        {
            splines.Insert(segmentIndex, CalculateSplineNormals(segmentIndex, spacing, rotationAmount, 0, 1));
            splines.Insert(segmentIndex + 1, CalculateSplineNormals(segmentIndex + 1, spacing, rotationAmount, 1, 0));
        }
        RotateGlobalNormals();
    }

    public void DeleteSegment(int anchorIndex)
    {
        if (NumberOfSegments > 2 || !isClosedPath && NumberOfSegments > 1)
        {
            if (anchorIndex == 0)
            {
                if (isClosedPath)
                {
                    points[points.Count - 1] = points[2];
                }
                points.RemoveRange(0, 3);
            }
            else if (anchorIndex == points.Count - 1 && !isClosedPath)
            {
                points.RemoveRange(anchorIndex - 2, 3);
            }
            else
            {
                points.RemoveRange(anchorIndex - 1, 3);
            }

            int segmentIndex = anchorIndex / 3;

            if (segmentIndex == 0)
            {
                if (isClosedPath)
                {
                    splines.RemoveAt(0);
                    splines.RemoveAt(NumberOfSegments - 1);

                    splines.Insert(NumberOfSegments - 1, CalculateSplinePoints(NumberOfSegments - 1, spacing));
                }
                else
                {
                    splines.RemoveAt(segmentIndex);
                }
            }
            else if (segmentIndex >= NumberOfSegments - 1)
            {
                if (isClosedPath)
                {
                    splines.RemoveAt(segmentIndex);
                    splines.RemoveAt(segmentIndex - 1);

                    splines.Insert(segmentIndex - 1, CalculateSplinePoints(segmentIndex - 1, spacing));
                }
                else
                {
                    splines.RemoveAt(segmentIndex - 1);
                }
            }
            else
            {
                splines.RemoveRange(segmentIndex - 1, 2);
                splines.Insert(segmentIndex - 1, CalculateSplinePoints(segmentIndex - 1, spacing));
            }
        }
    }

    public void RotateGlobalNormals()
    {
        for (int i = 0; i < splines.Count; i++)
        {
            for (int j = 0; j < splines[i].numberOfPoints; j++)
            {
                splines[i].normals[j] = Vector3.RotateTowards(splines[i].normals[j] , 
                    Quaternion.Euler(splines[i].normalRotations[j] + globalNormalAngle, 0, 0) * Vector3.up, maxRadiansDelta, maxMagnitudeDelta);
            }
        }
    }

    const float maxAngleChange = 0.5f;

    void RotateSegmentNormals(int segmentIndex, float angle, float from, float to)
    {
        splines[segmentIndex].rotationAmount = angle;

        for (int i = 0; i < splines[segmentIndex].numberOfPoints; i++)
        {
            int endPoint = (splines[segmentIndex].numberOfPoints);
            float completionPercent = i / (float)(endPoint);
            completionPercent = Remap(completionPercent, 0, 1, from, to);

            splines[segmentIndex].normalRotations[i] = angle * completionPercent;

            splines[segmentIndex].normals[i] = Vector3.RotateTowards(splines[segmentIndex].normals[i],
                Quaternion.Euler(splines[segmentIndex].normalRotations[i] + globalNormalAngle, 0, 0) * Vector3.up, maxRadiansDelta, maxMagnitudeDelta);
        }
    }

    public void RotateLocalNormals(int anchorIndex, float angle)
    {
        int segmentIndex = (anchorIndex >= NumberOfPoints - 1 && !IsClosed) ? (anchorIndex / 3) - 1 : (anchorIndex / 3);

        if (segmentIndex == 0)
        {
            if (isClosedPath)
            {
                RotateSegmentNormals(NumberOfSegments - 1, angle, 0, maxAngleChange);
                RotateSegmentNormals(0, angle, maxAngleChange, 0);
            }
            else
            {
                RotateSegmentNormals(segmentIndex, angle, maxAngleChange, 0);
            }
        }
        else if (segmentIndex > NumberOfSegments - 1)
        {
            if (isClosedPath)
            {
                RotateSegmentNormals(NumberOfSegments - 2, angle, 0, maxAngleChange);
                RotateSegmentNormals(NumberOfSegments - 1, angle, maxAngleChange, 0);
            }
            else
            {
                RotateSegmentNormals(segmentIndex, angle, 0, maxAngleChange);
            }
        }
        else
        {
            segmentIndex = LoopIndex(anchorIndex) / 3;

            RotateSegmentNormals(segmentIndex, angle, maxAngleChange, 0);
            RotateSegmentNormals(segmentIndex - 1, angle, 0, maxAngleChange);
        }
    }

    public void MovePoint(int updatedPointIndex, Vector3 newPosition)
    {
        Vector3 deltaMove = newPosition - points[updatedPointIndex];

        if (updatedPointIndex % 3 == 0 || !autoSetControlPoints)
        {
            points[updatedPointIndex] = newPosition;

            if (autoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(updatedPointIndex);
            }

            if (updatedPointIndex % 3 == 0)
            {
                if (updatedPointIndex + 1 < points.Count || isClosedPath)
                    points[LoopIndex(updatedPointIndex + 1)] += deltaMove;

                if (updatedPointIndex - 1 >= 0 || isClosedPath)
                    points[LoopIndex(updatedPointIndex - 1)] += deltaMove;
            }
            else
            {
                bool nextPointIsAnchor = (updatedPointIndex + 1) % 3 == 0;
                int correspondingControlIndex = (nextPointIsAnchor) ? updatedPointIndex + 2 : updatedPointIndex - 2;
                int anchorIndex = (nextPointIsAnchor) ? updatedPointIndex + 1 : updatedPointIndex - 1;

                if (correspondingControlIndex >= 0 && correspondingControlIndex < points.Count || isClosedPath)
                {
                    if (controlMode == BezierControlPointMode.Aligned)
                    {
                        AlignControlPoints(newPosition, anchorIndex, correspondingControlIndex);
                    }
                    else if (controlMode == BezierControlPointMode.Mirrored)
                    {
                        MirrorControlPoints(updatedPointIndex);
                    }
                }
            }
        }

        FixAxisCheck();

        int index;

        if(updatedPointIndex % 3 == 0 && isClosedPath)
        {
            index = updatedPointIndex;
        }
        else if(updatedPointIndex % 3 != 0)
        {
            bool nextPointIsAnchor = (updatedPointIndex + 1) % 3 == 0;
            index = (nextPointIsAnchor) ? updatedPointIndex + 1 : updatedPointIndex - 1;
        }
        else
        {
            index = updatedPointIndex;
        }

        int segmentIndex = index / 3;

        if (segmentIndex == 0)
        {
            if (isClosedPath)
            {
                float rotation1 = splines[0].rotationAmount;
                splines[0] = CalculateSplineNormals(0, spacing, rotation1, maxAngleChange, 0);

                float rotation2 = splines[NumberOfSegments - 1].rotationAmount;
                splines[NumberOfSegments - 1] = CalculateSplineNormals(NumberOfSegments - 1, spacing, rotation2, 0, maxAngleChange);
            }
            else
            {
                float rotation1 = splines[segmentIndex].rotationAmount;
                splines[segmentIndex] = CalculateSplineNormals(segmentIndex, spacing, rotation1, maxAngleChange, 0);
            }
        }
        else if (segmentIndex > NumberOfSegments - 1)
        {
            if (isClosedPath)
            {
                float rotation1 = splines[0].rotationAmount;
                splines[0] = CalculateSplineNormals(0, spacing, rotation1, maxAngleChange, 0);

                float rotation2 = splines[NumberOfSegments - 1].rotationAmount;
                splines[NumberOfSegments - 1] = CalculateSplineNormals(NumberOfSegments - 1, spacing, rotation2, 0, maxAngleChange);
            }
            else
            {
                float rotation2 = splines[segmentIndex - 1].rotationAmount;
                splines[segmentIndex - 1] = CalculateSplineNormals(segmentIndex - 1, spacing, rotation2, 0, maxAngleChange);
            }
        }
        else
        {
            float rotation1 = splines[segmentIndex].rotationAmount;
            float rotation2 = splines[segmentIndex - 1].rotationAmount;

            splines[segmentIndex] = CalculateSplineNormals(segmentIndex, spacing, rotation1, maxAngleChange, 0);
            splines[segmentIndex - 1] = CalculateSplineNormals(segmentIndex - 1, spacing, rotation2, 0, maxAngleChange);
        }
        RotateGlobalNormals();
    }

    void AlignControlPoints(Vector3 newPosition, int anchorPoint, int correspondingControlIndex)
    {
        float distance = (points[LoopIndex(anchorPoint)] - points[LoopIndex(correspondingControlIndex)]).magnitude;
        Vector3 direction = (points[LoopIndex(anchorPoint)] - newPosition).normalized;

        points[LoopIndex(correspondingControlIndex)] = points[LoopIndex(anchorPoint)] + direction * distance;
    }

    void MirrorControlPoints(int index)
    {
        int modeIndex = (index + 1) / 3;
        int middleIndex = modeIndex * 3;
        int fixedIndex, enforcedIndex;

        if (index <= middleIndex)
        {
            fixedIndex = middleIndex - 1;
            enforcedIndex = middleIndex + 1;
        }
        else
        {
            fixedIndex = middleIndex + 1;
            enforcedIndex = middleIndex - 1;
        }

        Vector3 middle = points[LoopIndex(middleIndex)];
        Vector3 enforcedTangent = middle - points[fixedIndex];
        points[LoopIndex(enforcedIndex)] = middle + enforcedTangent;
    }

    int CalculateDivisions(Vector3[] segmentPoints, float resolusion)
    {
        float controlNetLength = Vector3.Distance(segmentPoints[0], segmentPoints[1])
               + Vector3.Distance(segmentPoints[1], segmentPoints[2])
               + Vector3.Distance(segmentPoints[2], segmentPoints[3]);

        float estimatedCurveLength = Vector3.Distance(segmentPoints[0], segmentPoints[3]) + controlNetLength / 2;

        return Mathf.CeilToInt(estimatedCurveLength + resolusion * 10);
    }

    public void CalculateSplinePointsData(float spacing, float resolusion = 1)
    {
        if (splines == null)
        {
            splines = new List<Spline>();
        }

        if(evenlySpacedPoints == null)
        {
            evenlySpacedPoints = new List<Vector3>();
        }

        if (evenlySpacedDirections == null)
        {
            evenlySpacedDirections = new List<Vector3>();
        }

        if (evenlySpacedNormals == null)
        {
            evenlySpacedNormals = new List<Vector3>();
        }

        if (normalRotationAmount == null)
        {
            normalRotationAmount = new List<float>();
        }

        splines.Clear();
        
        evenlySpacedPoints.Clear();
        evenlySpacedDirections.Clear();
        evenlySpacedNormals.Clear();

        FixAxisCheck();

        Vector3 previousPoint = points[0];
        float distanceSinceLastPoint = 0;

        evenlySpacedPoints.Add(previousPoint);
        evenlySpacedDirections.Add(Vector3.up);
        evenlySpacedNormals.Add(Vector3.up);

        for (int segmentIndex = 0; segmentIndex < NumberOfSegments; segmentIndex++)
        {
            Vector3[] segmentPoints = GetPointsInSegment(segmentIndex);
            int divisions = CalculateDivisions(segmentPoints, resolusion);

            float t = 0;
            while (t <= 1)
            {
                t += 1f / divisions;
                Vector3 pointOnCurve = Bezier.EvaluateCubic(segmentPoints[0], segmentPoints[1], segmentPoints[2], segmentPoints[3], t);
                distanceSinceLastPoint += Vector3.Distance(previousPoint, pointOnCurve);

                while (distanceSinceLastPoint >= spacing)
                {
                    float overshootDistance = distanceSinceLastPoint - spacing;
                    Vector3 newEvenlySpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overshootDistance;

                    evenlySpacedPoints.Add(newEvenlySpacedPoint);
                    evenlySpacedDirections.Add(GetDirection(segmentPoints, t));
                    evenlySpacedNormals.Add(GetNormal(segmentPoints, t));

                    distanceSinceLastPoint = overshootDistance;
                    previousPoint = newEvenlySpacedPoint;
                }

                previousPoint = pointOnCurve;
            }

            Spline spline = new()
            {
                splineIndex = segmentIndex,
                numberOfPoints = evenlySpacedPoints.Count,
                points = evenlySpacedPoints.ToArray(),
                directions = evenlySpacedDirections.ToArray(),
                normals = evenlySpacedNormals.ToArray(),
                normalRotations = new float[evenlySpacedPoints.Count]
            };

            splines.Add(spline);

            evenlySpacedPoints.Clear();
            evenlySpacedDirections.Clear();
            evenlySpacedNormals.Clear();
            normalRotationAmount.Clear();
        }
    }

    void CalculateSplineEvenlySpacedPoints(int segmentIndex, float spacing, float resolusion = 1)
    {
        Vector3 previousPoint = points[0];
        float distanceSinceLastPoint = 0;

        if (segmentIndex != 0)
        {
            previousPoint = splines[segmentIndex - 1].points[splines[segmentIndex - 1].numberOfPoints - 1];
        }
        else if ((segmentIndex == 0 || segmentIndex == splines.Count - 1) && isClosedPath)
        {
            previousPoint = splines[splines.Count - 1].points[splines[splines.Count - 1].numberOfPoints - 1];
        }

        evenlySpacedPoints.Clear();
        evenlySpacedDirections.Clear();
        evenlySpacedNormals.Clear();
        normalRotationAmount.Clear();

        CalculateSegmentPoints(segmentIndex, spacing, distanceSinceLastPoint, previousPoint, resolusion);
    }

    void CalculateSegmentPoints(int segmentIndex, float spacing, float distanceSinceLastPoint, Vector3 previousPoint, float resolusion = 1)
    {
        Vector3[] segmentPoints = GetPointsInSegment(segmentIndex);
        int divisions = CalculateDivisions(segmentPoints, resolusion);
        float t = 0;

        while (t <= 1)
        {
            t += 1f / divisions;
            Vector3 pointOnCurve = Bezier.EvaluateCubic(segmentPoints[0], segmentPoints[1], segmentPoints[2], segmentPoints[3], t);
            distanceSinceLastPoint += Vector3.Distance(previousPoint, pointOnCurve);

            while (distanceSinceLastPoint >= spacing)
            {
                float overshootDistance = distanceSinceLastPoint - spacing;
                Vector3 newEvenlySpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overshootDistance;

                evenlySpacedPoints.Add(newEvenlySpacedPoint);
                evenlySpacedDirections.Add(GetDirection(segmentPoints, t));
                evenlySpacedNormals.Add(GetNormal(segmentPoints, t));
                distanceSinceLastPoint = overshootDistance;
                previousPoint = newEvenlySpacedPoint;
            }
            previousPoint = pointOnCurve;
        }
    }

    Spline CalculateSplinePoints(int segmentIndex, float spacing, float resolusion = 1)
    {
        CalculateSplineEvenlySpacedPoints(segmentIndex, spacing, resolusion);

        for (int i = 0; i < evenlySpacedPoints.Count; i++)
        {
            normalRotationAmount.Add(globalNormalAngle);
            evenlySpacedNormals[i] = Vector3.RotateTowards(evenlySpacedNormals[i],
                Quaternion.Euler(normalRotationAmount[i], 0, 0) * Vector3.up, maxRadiansDelta, maxMagnitudeDelta);
        }

        Spline spline = new()
        {
            splineIndex = segmentIndex,
            numberOfPoints = evenlySpacedPoints.Count,
            points = evenlySpacedPoints.ToArray(),
            directions = evenlySpacedDirections.ToArray(),
            normals = evenlySpacedNormals.ToArray(),
            rotationAmount = globalNormalAngle,
            normalRotations = normalRotationAmount.ToArray(),
        };

        return spline;
    }

    Spline CalculateSplineNormals(int segmentIndex, float spacing, float rotationAmount, float from, float to, float resolusion = 1)
    {
        CalculateSplineEvenlySpacedPoints(segmentIndex, spacing, resolusion);

        for (int i = 0; i < evenlySpacedPoints.Count; i++)
        {
            int endPoint = evenlySpacedPoints.Count;
            float completionPercent = i / (float)(endPoint);
            completionPercent = Remap(completionPercent, 0, 1, from, to);

            normalRotationAmount.Add(rotationAmount * completionPercent);
            evenlySpacedNormals[i] = Vector3.RotateTowards(evenlySpacedNormals[i],
                Quaternion.Euler(normalRotationAmount[i], 0, 0) * Vector3.up, maxRadiansDelta, maxMagnitudeDelta);
        }

        Spline spline = new()
        {
            splineIndex = segmentIndex,
            numberOfPoints = evenlySpacedPoints.Count,
            points = evenlySpacedPoints.ToArray(),
            directions = evenlySpacedDirections.ToArray(),
            normals = evenlySpacedNormals.ToArray(),
            rotationAmount = rotationAmount,
            normalRotations = normalRotationAmount.ToArray(),
        };

        return spline;
    }

    void AutoSetAllAffectedControlPoints(int updatedAnchorIndex)
    {
        FixAxisCheck();

        for (int i = updatedAnchorIndex - 3; i < updatedAnchorIndex + 3; i+=3)
        {
            if(i >=0 && i <points.Count || isClosedPath)
            {
                AutoSetAnchorControlPoints(LoopIndex(i));
            }
        }

        AutoSetStartAndEndControls();

        CalculateSplinePointsData(spacing);
    }

    void AutoSetAllControlPoints()
    {
        FixAxisCheck();

        for (int i = 0; i < points.Count; i+=3)
        {
            AutoSetAnchorControlPoints(i);
        }

        AutoSetStartAndEndControls();
        CalculateSplinePointsData(spacing);
    }

    void AutoSetAnchorControlPoints(int anchorIndex)
    {
        Vector3 anchorPositon = points[anchorIndex];
        Vector3 direction = Vector3.zero;
        float[] neighbourDistances = new float[2];

        if(anchorIndex - 3 >= 0 || isClosedPath)
        {
            Vector3 offset = points[LoopIndex(anchorIndex - 3)] - anchorPositon;
            direction += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }

        if (anchorIndex + 3 >= 0 || isClosedPath)
        {
            Vector3 offset = points[LoopIndex(anchorIndex + 3)] - anchorPositon;
            direction -= offset.normalized;
            neighbourDistances[1] = -offset.magnitude;
        }

        direction.Normalize();

        for (int i = 0; i < 2; i++) 
        {
            int controlIndex = anchorIndex + i * 2 - 1;
            if(controlIndex >=0 && controlIndex < points.Count || isClosedPath)
            {
                points[LoopIndex(controlIndex)] = anchorPositon + direction * neighbourDistances[i] * 0.5f;
            }
        }
    }

    void AutoSetStartAndEndControls()
    {
        if (!isClosedPath)
        {
            points[1] = (points[0] + points[2]) * 0.5f;
            points[points.Count - 2] = (points[points.Count -1] + points[ points.Count - 3]) * 0.5f;
        }
    }

    Vector3 GetVelocity(Vector3[] segmentPoints, float t)
    {
        return Bezier.GetFirstDerivativeCubic(segmentPoints[0], segmentPoints[1], segmentPoints[2], segmentPoints[3], t);
    }

    Vector3 GetDirection(Vector3[] segmentPoints, float t)
    {
        return GetVelocity(segmentPoints, t).normalized;
    }

    Vector3 GetNormal(Vector3[] segmentPoints, float t)
    {
        Vector3 normal = Bezier.getNormalAt(segmentPoints[0], segmentPoints[1], segmentPoints[2], segmentPoints[3], t);
        normal = Vector3.RotateTowards(normal, Quaternion.Euler(globalNormalAngle, 0, 0) * Vector3.up, maxRadiansDelta, maxMagnitudeDelta);
        return normal;
    }

    void FixAxisCheck()
    {
        if (selectedAxis == 1)
        {
            FixToXYAxis();
        }
        else if (selectedAxis == 2)
        {
            FixToZXAxis();
        }
    }

    void FixToXYAxis()
    {
        for (int i = 0; i < points.Count; i++)
        {
            points[i] = new Vector3(points[i].x, points[i].y, 0);
        }
    }

    void FixToZXAxis()
    {
        for (int i = 0; i < points.Count; i++)
        {
            points[i] = new Vector3(points[i].x, 0, points[i].z);
        }
    }

    public Vector3[] GetPointsInSegment(int i)
    {
        return new Vector3[] { points[i * 3], points[i * 3 + 1], points[i * 3 + 2], points[LoopIndex(i * 3 + 3)] };
    }

    public Vector3 GetPoint(int i)
    {
        return points[i];
    }

    public int SegmentIndexFromAnchor(int anchorIndex)
    {
        return LoopIndex(anchorIndex) / 3;
    }

    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    int LoopIndex(int i)
    {
        return (i + points.Count) % points.Count;
    }
}

[System.Serializable]
public class Spline
{
    public int splineIndex;
    public int numberOfPoints;

    public Vector3[] points;
    public Vector3[] directions;
    public Vector3[] normals;

    public float[] normalRotations;
    public float rotationAmount;
}

[System.Serializable]
public enum BezierControlPointMode
{
    Free,
    Aligned,
    Mirrored
}
