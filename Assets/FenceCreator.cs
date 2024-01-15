using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FenceCreator : MonoBehaviour
{
    public bool allowTilt = true;

    [Range(1f, 10f)]
    public float spacing = 1;

    //[Range (1,8)]
    int numOfTopSections = 4;

    public float roadWidth = 1;
    public float roadHeight = 1;
    public float fenceHeight = 1;

    [Range(0, 0.5f)]
    public float fencePercentageWidth = 0.25f;
    public bool autoUpdate;
    public float tiling = 1;

    const int numberOfVerts = 11;
    const int numberOfTriangles = 26;

    const int numberOfVertsInSegment = 11;
    int[] tris;
    //int numberOfVerts = 7;
    //int numberOfTriangles = 14;

    //const int numberOfVertsInSegment = 7;

    public void UpdateRoad()
    {
        Path path = GetComponent<PathCreator>().path;
        if (path.splines == null)
        {
            return;
        }

        int numberOfPoints = 0;
        for (int i = 0; i < path.NumberOfSegments; i++)
        {
            for (int j = 0; j < path.splines[i].numberOfPoints; j++)
            {
                numberOfPoints++;
            }
        }

        Vector3[] points = new Vector3[numberOfPoints];
        Vector3[] normals = new Vector3[numberOfPoints];
        int indexOffset = 0;

        for (int i = 0; i < path.NumberOfSegments; i++)
        {
            for (int j = 0; j < path.splines[i].numberOfPoints; j++)
            {
                points[j + indexOffset] = path.splines[i].points[j];
                normals[j + indexOffset] = path.splines[i].normals[j];
            }

            indexOffset += path.splines[i].numberOfPoints;
        }

        GetComponent<MeshFilter>().sharedMesh = CreateRoadMesh(points, normals, path.IsClosed);

        int textureRepeat = Mathf.RoundToInt(tiling * points.Length * spacing * .05f);
        GetComponent<MeshRenderer>().sharedMaterial.mainTextureScale = new Vector2(1, textureRepeat);
    }

    public void GenerateCollider()
    {
        GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;
    }

    Mesh CreateRoadMesh(Vector3[] points, Vector3[] normals, bool isClosed)
    {
        Vector3[] verts = new Vector3[points.Length * numberOfVerts];
        Vector2[] uvs = new Vector2[verts.Length];

        int numTris = numberOfTriangles * (points.Length - 1) + ((isClosed) ? numberOfVerts * 2 : 0);
        tris = new int[numTris * 3];
        int vertIndex = 0;
        int triIndex = 0;

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 forward = Vector3.zero;
            if (i < points.Length - 1 || isClosed)
            {
                forward += points[(i + 1) % points.Length] - points[i];
            }
            if (i > 0 || isClosed)
            {
                forward += points[i] - points[(i - 1 + points.Length) % points.Length];
            }

            forward.Normalize();
            Vector3 up = (allowTilt) ? normals[i] : Vector3.up;
            //up = normals[i];
            Vector3 left = Vector3.Cross(forward, up);

            left.Normalize();
            Vector3 right = -left;
            Vector3 down = -up;
            verts[vertIndex] = points[i] + up * roadHeight * .5f + left * roadWidth * 0.5f; // TOP LEFT
            verts[vertIndex + 1] = points[i] + up * roadHeight * .5f + left * roadWidth * fencePercentageWidth; // TOP MIDDLE LEFT

            verts[vertIndex + 2] = points[i] + up * roadHeight * .5f; // MIDDLE

            verts[vertIndex + 3] = points[i] + up * roadHeight * .5f + right * roadWidth * fencePercentageWidth; // MIDDLE RIGHT
            verts[vertIndex + 4] = points[i] + up * roadHeight * .5f + right * roadWidth * 0.5f; // TOP RIGHT


            verts[vertIndex + 5] = points[i] + down * roadHeight * .5f + right * roadWidth * 0.5f; // BOTTOM RIGHT
            verts[vertIndex + 6] = points[i] + down * roadHeight * .5f + left * roadWidth * 0.5f; // BOTTOM LEFT

            verts[vertIndex + 7] = points[i] + up * fenceHeight * .5f + up * roadHeight * .5f + left * roadWidth * 0.5f; // BOTTOM LEFT
            verts[vertIndex + 8] = points[i] + up * fenceHeight * .5f + up * roadHeight * .5f + left * roadWidth * fencePercentageWidth; // BOTTOM LEFT

            verts[vertIndex + 9] = points[i] + up * fenceHeight * .5f + up * roadHeight * .5f + right * roadWidth * fencePercentageWidth; // BOTTOM LEFT
            verts[vertIndex + 10] = points[i] + up * fenceHeight * .5f + up * roadHeight * .5f + right * roadWidth * 0.5f; // BOTTOM LEFT


            float completionPercent = i / (float)(points.Length - 1);
            float v = 1 - Mathf.Abs(2 * completionPercent - 1);

            for (int j = 0; j < numberOfVerts; j++)
            {
                float percent = j / (float)(numberOfVerts + 1);
                float u = 1 - Mathf.Abs(2 * percent - 1);
                uvs[vertIndex + j] = new Vector2(u, v);
            }

            if (i < points.Length - 1 || isClosed)
            {
                //TOP SIDE
                int indexOffset = 0;

                for (int j = 0; j < numOfTopSections; j++)
                {
                    if (j == 0 || j == numOfTopSections - 1)
                    {
                        indexOffset += 5;
                        continue;
                    }

                    int index = triIndex + indexOffset + j;

                    tris[index] = vertIndex + j;
                    tris[index + 1] = (vertIndex + j + 11) % verts.Length;
                    tris[index + 2] = vertIndex + j + 1;

                    tris[index + 3] = vertIndex + j + 1;
                    tris[index + 4] = (vertIndex + j + 11) % verts.Length;
                    tris[index + 5] = (vertIndex + j + 12) % verts.Length;

                    //GenerateFace(
                    //    index,
                    //    vertIndex + j,
                    //    (vertIndex + j + 11) % verts.Length,
                    //    vertIndex + j + 1,
                    //    (vertIndex + j + 12) % verts.Length
                    //    );

                    indexOffset += 5;
                }

                //RIGHT SIDE
                GenerateFace(
                    triIndex + 24,
                    vertIndex + 4, 
                    (vertIndex + 16) % verts.Length, 
                    vertIndex + 5, 
                    (vertIndex + 15) % verts.Length);

                //LEFT SIDE
                GenerateFace(
                    triIndex + 30,
                    vertIndex,
                    (vertIndex + 17) % verts.Length,
                    (vertIndex + 11) % verts.Length,
                    (vertIndex + 6));

                //BOTTOM SIDE
                GenerateFace(
                   triIndex + 36,
                   vertIndex + 6,
                   (vertIndex + 16) % verts.Length,
                   (vertIndex + 5),
                   (vertIndex + 17) % verts.Length);


                //LEFT FENCE - OUT SIDE
                GenerateFace(
                  triIndex + 42,
                  vertIndex,
                  (vertIndex + numberOfVertsInSegment + 7) % verts.Length,
                  (vertIndex + 7),
                  (vertIndex + numberOfVertsInSegment)
                  );

                //LEFT FENCE - TOP SIDE
                GenerateFace(
                   triIndex + 48,
                   vertIndex + 7,
                   (vertIndex + numberOfVertsInSegment + 8) % verts.Length,
                   (vertIndex + 8),
                   (vertIndex + numberOfVertsInSegment + 7) % verts.Length
                   );

                //LEFT FENCE - IN SIDE
                GenerateFace(
                   triIndex + 54,
                   vertIndex + 8,
                   (vertIndex + numberOfVertsInSegment + 1) % verts.Length,
                   (vertIndex + 1),
                   (vertIndex + numberOfVertsInSegment + 8) % verts.Length);


                //RIGHT FENCE - OUT SIDE
                tris[triIndex + 60] = vertIndex + 4;
                tris[triIndex + 61] = (vertIndex + numberOfVertsInSegment + 10) % verts.Length;
                tris[triIndex + 62] = (vertIndex + numberOfVertsInSegment + 4) % verts.Length;

                tris[triIndex + 63] = vertIndex +4;
                tris[triIndex + 64] = (vertIndex + 10);
                tris[triIndex + 65] = (vertIndex + numberOfVertsInSegment + 10) % verts.Length;

                //RIGHT FENCE - TOP SIDE
                tris[triIndex + 66] = vertIndex + 10;
                tris[triIndex + 67] = (vertIndex + numberOfVertsInSegment + 9) % verts.Length;
                tris[triIndex + 68] = (vertIndex + numberOfVertsInSegment + 10) % verts.Length;

                tris[triIndex + 69] = vertIndex + 10;
                tris[triIndex + 70] = (vertIndex  + 9);
                tris[triIndex + 71] = (vertIndex + numberOfVertsInSegment + 9) % verts.Length;

                //RIGHT FENCE - IN SIDE
                tris[triIndex + 72] = vertIndex + 9;
                tris[triIndex + 73] = (vertIndex + 3);
                tris[triIndex + 74] = (vertIndex + numberOfVertsInSegment + 3) % verts.Length;

                tris[triIndex + 75] = vertIndex + 9;
                tris[triIndex + 76] = (vertIndex + numberOfVertsInSegment + 9) % verts.Length;
                tris[triIndex + 77] = (vertIndex + numberOfVertsInSegment + 3) % verts.Length;

            }
            //Debug.Log("index: " +triIndex);
            vertIndex += 11;
            triIndex += 78;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;

        return mesh;
    }

    //   v4------v2
    //   |        |
    //   |        |
    //   |        |
    //   |        |
    //   v1------v3

    //triangle 1: v1 -> v2 -> v3
    //triangle 2: v1 -> v4 -> v2

    void GenerateFace(int triIndex, int v1, int v2, int v3, int v4)
    {
        //int index 
        tris[triIndex] = v1;
        tris[triIndex + 1] = v2;
        tris[triIndex + 2] = v3;

        tris[triIndex + 3] = v1;
        tris[triIndex + 4] = v4;
        tris[triIndex + 5] = v2;
    }
}
