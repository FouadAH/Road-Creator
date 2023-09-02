using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Bezier
{
    public static Vector3 EvaluateQuadratic(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        Vector3 p0 = Vector3.Lerp(a, b, t);
        Vector3 p1 = Vector3.Lerp(b, c, t);
        return Vector3.Lerp(p0, p1, t);
    }

    public static Vector3 EvaluateCubic(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        Vector3 p0 = EvaluateQuadratic(a, b, c, t);
        Vector3 p1 = EvaluateQuadratic(b, c, d, t);
        return Vector3.Lerp(p0, p1, t);
    }

    public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
    }

    public static Vector3 GetFirstDerivativeCubic(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return -3f * Mathf.Pow((1f - t), 2) * p0 
            + 3f * Mathf.Pow((1f - t), 2) * p1
            - 6 * t * (1 - t) * p1
            - Mathf.Pow((3*t), 2) * p2
            + 6 * t * (1-t) * p2
            + Mathf.Pow((3 * t), 2) * p3;
    }

    public static Vector3 getNormalAt(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        Vector3 derivative = GetFirstDerivativeCubic(p0, p1, p2, p3, t);
        float q = Mathf.Sqrt(derivative.x * derivative.x + derivative.y * derivative.y);

        float x = -derivative.y / q;
        float y = derivative.x / q;

        return new Vector3(x, y, 0);
    }
}