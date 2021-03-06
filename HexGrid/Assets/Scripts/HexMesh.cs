﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour, IHexMeshChunkModule
{
    public bool UseCollider;
    public bool UseUVs;
    public bool UseUV2s;
    public bool UseCellData;

    Mesh m_hexMesh;
    MeshCollider m_meshCollider;

    [NonSerialized] List<Vector3> m_vertices;
    [NonSerialized] List<int> m_indices;
    [NonSerialized] List<Color> m_cellWeights;
    [NonSerialized] List<Vector2> m_uvs;
    [NonSerialized] List<Vector2> m_uv2s;
    [NonSerialized] List<Vector3> m_cellIndices;

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = m_hexMesh = new Mesh();
        m_hexMesh.name = "Hex Mesh";
        if (UseCollider)
        {
            m_meshCollider = gameObject.AddComponent<MeshCollider>();
        }
    }

    public void Clear()
    {
        m_hexMesh.Clear();
        m_vertices = ListPool<Vector3>.Get();
        if (UseCellData)
        {
            m_cellWeights = ListPool<Color>.Get();
            m_cellIndices = ListPool<Vector3>.Get();
        }
        m_indices = ListPool<int>.Get();

        if(UseUVs)
        {
            m_uvs = ListPool<Vector2>.Get();
        }
        if (UseUV2s)
        {
            m_uv2s = ListPool<Vector2>.Get();
        }

    }

    public void Apply()
    {
        m_hexMesh.SetVertices(m_vertices);
        ListPool<Vector3>.Return(m_vertices);

        if (m_cellWeights != null)
        {
            m_hexMesh.SetColors(m_cellWeights);
            ListPool<Color>.Return(m_cellWeights);
            m_hexMesh.SetUVs(2, m_cellIndices);
            ListPool<Vector3>.Return (m_cellIndices);
        }

        m_hexMesh.SetTriangles(m_indices, 0);
        ListPool<int>.Return(m_indices);
        m_hexMesh.RecalculateNormals();

        if(m_uvs != null)
        {
            m_hexMesh.SetUVs(0,m_uvs);
            ListPool<Vector2>.Return(m_uvs);
        }

        if (m_uv2s != null)
        {
            m_hexMesh.SetUVs(1, m_uv2s);
            ListPool<Vector2>.Return(m_uv2s);
        }


        if (m_meshCollider)
        {
            m_meshCollider.sharedMesh = m_hexMesh;
        }
    }

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = m_vertices.Count;
        m_vertices.Add(HexMetrics.PerturbVector(v1));
        m_vertices.Add(HexMetrics.PerturbVector(v2));
        m_vertices.Add(HexMetrics.PerturbVector(v3));

        m_indices.Add(vertexIndex);
        m_indices.Add(vertexIndex + 1);
        m_indices.Add(vertexIndex + 2);

    }

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = m_vertices.Count;
        m_vertices.Add(v1);
        m_vertices.Add(v2);
        m_vertices.Add(v3);

        m_indices.Add(vertexIndex);
        m_indices.Add(vertexIndex + 1);
        m_indices.Add(vertexIndex + 2);
    }

    public void AddTriangleCellData(Vector3 indices, Color color)
    {
        AddTriangleCellData(indices, color, color, color);

    }

    public void AddTriangleCellData(Vector3 indices,Color weights1,Color weights2,Color weights3)
    {
        if(m_cellIndices != null)
        {
            m_cellIndices.Add(indices);
            m_cellIndices.Add(indices);
            m_cellIndices.Add(indices);
        }
        
        if (m_cellWeights != null)
        {
            m_cellWeights.Add(weights1);
            m_cellWeights.Add(weights2);
            m_cellWeights.Add(weights3);
        }
    }



    public void AddTriangleUV(Vector2 v1, Vector2 v2, Vector2 v3)
    {
        if (m_uvs != null)
        {
            m_uvs.Add(v1);
            m_uvs.Add(v2);
            m_uvs.Add(v3);
        }
    }

    public void AddQuadUV(Vector2 v1, Vector2 v2, Vector2 v3,Vector2 v4)
    {
        if (m_uvs != null)
        {
            m_uvs.Add(v1);
            m_uvs.Add(v2);
            m_uvs.Add(v3);
            m_uvs.Add(v4);
        }
    }

    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        m_uvs.Add(new Vector2(uMin, vMin));
        m_uvs.Add(new Vector2(uMax, vMin));
        m_uvs.Add(new Vector2(uMin, vMax));
        m_uvs.Add(new Vector2(uMax, vMax));
    }


    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = m_vertices.Count;
        m_vertices.Add(HexMetrics.PerturbVector(v1));
        m_vertices.Add(HexMetrics.PerturbVector(v2));
        m_vertices.Add(HexMetrics.PerturbVector(v3));
        m_vertices.Add(HexMetrics.PerturbVector(v4));

        m_indices.Add(vertexIndex);
        m_indices.Add(vertexIndex + 2);
        m_indices.Add(vertexIndex + 1);
        m_indices.Add(vertexIndex + 1);
        m_indices.Add(vertexIndex + 2);
        m_indices.Add(vertexIndex + 3);

    }

    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = m_vertices.Count;
        m_vertices.Add(v1);
        m_vertices.Add(v2);
        m_vertices.Add(v3);
        m_vertices.Add(v4);

        m_indices.Add(vertexIndex);
        m_indices.Add(vertexIndex + 2);
        m_indices.Add(vertexIndex + 1);
        m_indices.Add(vertexIndex + 1);
        m_indices.Add(vertexIndex + 2);
        m_indices.Add(vertexIndex + 3);

    }


    public void AddQuadCellData(Vector3 indices,Color c1)
    {
        AddQuadCellData(indices, c1, c1, c1, c1);
    }

    public void AddQuadCellData(Vector3 indices, Color c1, Color c2)
    {
        AddQuadCellData(indices, c1, c1, c2, c2);
    }

    public void AddQuadCellData(Vector3 indices,Color c1, Color c2, Color c3, Color c4)
    {
        if (m_cellIndices != null)
        {
            m_cellIndices.Add(indices);
            m_cellIndices.Add(indices);
            m_cellIndices.Add(indices);
            m_cellIndices.Add(indices);
        }

        if (m_cellWeights != null)
        {
            m_cellWeights.Add(c1);
            m_cellWeights.Add(c2);
            m_cellWeights.Add(c3);
            m_cellWeights.Add(c4);
        }
    }

    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        m_uv2s.Add(uv1);
        m_uv2s.Add(uv2);
        m_uv2s.Add(uv3);
    }

    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        m_uv2s.Add(uv1);
        m_uv2s.Add(uv2);
        m_uv2s.Add(uv3);
        m_uv2s.Add(uv4);
    }

    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
    {
        m_uv2s.Add(new Vector2(uMin, vMin));
        m_uv2s.Add(new Vector2(uMax, vMin));
        m_uv2s.Add(new Vector2(uMin, vMax));
        m_uv2s.Add(new Vector2(uMax, vMax));
    }
}