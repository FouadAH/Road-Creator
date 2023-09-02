using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathCreator))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RoadCreator : MonoBehaviour
{
    public bool allowTilt = true;

    [Range(1f, 10f)]
    public float spacing = 1;

    //[Range (1,8)]
    int numOfTopSections = 4;

    public float roadWidth = 1;
    public float roadHeight = 1;

    public bool autoUpdate;
    public float tiling = 1;

    int numberOfVerts = 7;
    int numberOfTriangles = 14;

    public void UpdateRoad()
    {
        Path path = GetComponent<PathCreator>().path;
        if(path.splines == null)
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

        int numTris = numberOfTriangles * (points.Length - 1) + ((isClosed) ? numberOfVerts*2 : 0);
        int[] tris = new int[numTris * 3];

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
            verts[vertIndex + 1] = points[i] + up * roadHeight * .5f + left * roadWidth * 0.25f; // TOP MIDDLE LEFT

            verts[vertIndex + 2] = points[i] + up * roadHeight * .5f; // MIDDLE
            
            verts[vertIndex + 3] = points[i] + up * roadHeight * .5f + right * roadWidth * 0.25f; // MIDDLE RIGHT
            verts[vertIndex + 4] = points[i] + up * roadHeight * .5f + right * roadWidth * 0.5f; // TOP RIGHT


            verts[vertIndex + 5] = points[i] + down * roadHeight * .5f + right * roadWidth * 0.5f; // BOTTOM RIGHT
            verts[vertIndex + 6] = points[i] + down * roadHeight * .5f + left * roadWidth * 0.5f; // BOTTOM LEFT

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
                    int index = triIndex + indexOffset + j;

                    tris[index] = vertIndex + j;
                    tris[index + 1] = (vertIndex + j + 7) % verts.Length;
                    tris[index + 2] = vertIndex + j + 1;

                    tris[index + 3] = vertIndex + j + 1;
                    tris[index + 4] = (vertIndex + j + 7) % verts.Length;
                    tris[index + 5] = (vertIndex + j + 8) % verts.Length;

                    indexOffset += 5;
                }

                //RIGHT SIDE
                tris[triIndex + 24] = vertIndex + 4;
                tris[triIndex + 25] = (vertIndex + 12) % verts.Length;
                tris[triIndex + 26] = vertIndex + 5;

                tris[triIndex + 27] = vertIndex + 4;
                tris[triIndex + 28] = (vertIndex + 11) % verts.Length;
                tris[triIndex + 29] = (vertIndex + 12) % verts.Length;

                //LEFT SIDE
                tris[triIndex + 30] = vertIndex;
                tris[triIndex + 31] = (vertIndex + 13) % verts.Length;
                tris[triIndex + 32] = (vertIndex + 7) % verts.Length;

                tris[triIndex + 33] = vertIndex;
                tris[triIndex + 34] = (vertIndex + 6);
                tris[triIndex + 35] = (vertIndex + 13) % verts.Length;

                //BOTTOM SIDE
                tris[triIndex + 36] = vertIndex + 6;
                tris[triIndex + 37] = (vertIndex + 5);
                tris[triIndex + 38] = (vertIndex + 13) % verts.Length;

                tris[triIndex + 39] = vertIndex + 5;
                tris[triIndex + 40] = (vertIndex + 12) % verts.Length;
                tris[triIndex + 41] = (vertIndex + 13) % verts.Length;
            }

            vertIndex += 7;
            triIndex += 42;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;

        return mesh;
    }
}