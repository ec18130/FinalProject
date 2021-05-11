using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Mesh2D : ScriptableObject
{
    [System.Serializable]
    public class Vertex
    {
        public Vector2 points;
        public Vector2 normal;
        public float U;
    }

    public Vertex[] vertices;
    public int[] lineIndices;

    public int VertexCount => vertices.Length;
    public int LineCount => lineIndices.Length;

    public float CalcUspan()
    {
        float distance = 0;
        for (int i = 0; i < LineCount; i += 2)
        {
            Vector2 a = vertices[lineIndices[i]].points;
            Vector2 b = vertices[lineIndices[i + 1]].points;
            distance += (a - b).magnitude;
        }
        return distance;
    }
}
