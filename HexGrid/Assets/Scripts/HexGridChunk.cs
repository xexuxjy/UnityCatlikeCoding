using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{

    public Canvas GridCanvas;
    public HexMesh Terrain;
    public HexMesh Rivers;
    public HexMesh Roads;
    public HexMesh Water;
    public HexMesh WaterShore;
    public HexMesh Estuaries;
    public HexMesh Walls;

    public Texture2DArray SplatTextureArray;

    public HexFeatureManager FeatureManager;

    private List<IHexMeshChunkModule> m_chunkModules = new List<IHexMeshChunkModule>();

    HexCell[] m_cells;

    private bool m_dirty = false;

    private static Color Weights1 = new Color(1, 0, 0);
    private static Color Weights2 = new Color(0, 1, 0);
    private static Color Weights3 = new Color(0, 0, 1);


    private void Awake()
    {
        GridCanvas = GetComponentInChildren<Canvas>();
        m_cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
        //ShowUI(false);

        Type t  = typeof(HexGridChunk);
        FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach(FieldInfo field in fields)
        {
            if(field.GetValue(this) is IHexMeshChunkModule)
            {
                m_chunkModules.Add((IHexMeshChunkModule)field.GetValue(this));
            }
        }

    }

    public void Refresh()
    {
        m_dirty = true;

    }

    public void AddCell(int index, HexCell cell)
    {
        if (index >= 0 && index <= m_cells.Length - 1)
        {
            m_cells[index] = cell;
            cell.GridChunk = this;
            cell.transform.SetParent(transform, false);
            cell.UIRectTransform.SetParent(GridCanvas.transform, false);

        }
    }


    void LateUpdate()
    {
        if (m_dirty)
        {
            TriangulateCells();
            m_dirty = false;
        }
    }

    public void ShowUI(bool visible)
    {
        GridCanvas.gameObject.SetActive(visible);

    }



    public void TriangulateCells()
    {
        foreach (IHexMeshChunkModule chunkModule in m_chunkModules)
        {
            if (chunkModule != null)
            {
                chunkModule.Clear();
            }
        }
        foreach (HexCell cell in m_cells)
        {
            Triangulate(cell);
        }
        foreach (IHexMeshChunkModule chunkModule in m_chunkModules)
        {
            if (chunkModule != null)
            {
                chunkModule.Apply();
            }
        }
    }

    public void Triangulate(HexCell cell)
    {
        for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; ++dir)
        {
            Triangulate(dir, cell);
        }

        if (!(cell.HasRiver || cell.HasRoads || cell.IsUnderwater))
        {
            FeatureManager.AddFeature(cell,cell.Position);
        }

        if(!cell.IsUnderwater && cell.HasSpecialFeature)
        {
            FeatureManager.AddSpecialFeature(cell, cell.Position);
        }

    }


    public void Triangulate(HexDirection dir, HexCell cell)
    {
        Vector3 center = cell.Position;

        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(dir);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(dir);

        EdgeVertices edges = new EdgeVertices(v1, v2);


        if (cell.IsUnderwater)
        {
            TriangulateWater(dir, cell, center);
        }

        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(dir))
            {
                edges.v3.y = cell.StreamBedY;
                if (cell.HasRiverBeginningOrEnd)
                {
                    TriangulateWithRiverBeginningOrEnd(dir, cell, center, edges);
                }
                else
                {
                    TriangulateWithRiver(dir, cell, center, edges);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(dir, cell, center, edges);
            }
        }
        else
        {

            TriangulateWithoutRiver(dir, cell, center, edges);
            if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(dir))
            {
                // centred in triangle
                Vector3 position = (center + edges.v1 + edges.v5) *(1f / 3f);
                FeatureManager.AddFeature(cell,position);
            }
        }


        if (dir <= HexDirection.SE)
        {

            TriangulateConnection(dir, cell, edges);
        }
    }

    public void TriangulateWater(HexDirection dir, HexCell cell, Vector3 center)
    {
        center.y = cell.WaterSurfaceY;


        HexCell neighbour = cell.GetNeighbour(dir);

        if (neighbour != null && !neighbour.IsUnderwater)
        {
            TriangulateWaterShore(dir, cell, neighbour, center);
        }
        else
        {
            TriangulateOpenWater(dir, cell, neighbour, center);
        }
    }


    public void TriangulateOpenWater(HexDirection dir, HexCell cell, HexCell neighbour, Vector3 center)
    {

        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(dir);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(dir);

        Water.AddTriangle(center, c1, c2);
        if (dir <= HexDirection.SE && neighbour != null)
        {
            Vector3 bridge = HexMetrics.GetWaterBridge(dir);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            Water.AddQuad(c1, c2, e1, e2);

            if (dir <= HexDirection.E)
            {
                HexCell nextNeightbour = cell.GetNeighbour(dir.Next());
                if (nextNeightbour == null || !nextNeightbour.IsUnderwater)
                {
                    return;
                }
                Water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(dir.Next()));
            }


        }
    }
    public void TriangulateWaterShore(HexDirection dir, HexCell cell, HexCell neighbour, Vector3 center)
    {
        EdgeVertices edges1 = new EdgeVertices(center + HexMetrics.GetFirstWaterCorner(dir), center + HexMetrics.GetSecondWaterCorner(dir));
        Water.AddTriangle(center, edges1.v1, edges1.v2);
        Water.AddTriangle(center, edges1.v2, edges1.v3);
        Water.AddTriangle(center, edges1.v3, edges1.v4);
        Water.AddTriangle(center, edges1.v4, edges1.v5);

        Vector3 center2 = neighbour.Position;
        center2.y = center.y;


        Vector3 bridge = HexMetrics.GetWaterBridge(dir);
        EdgeVertices edges2 = new EdgeVertices(center2 + HexMetrics.GetSecondSolidCorner(dir.Opposite()), center2 + HexMetrics.GetFirstSolidCorner(dir.Opposite()));

        if (cell.HasRiverThroughEdge(dir))
        {
            TriangulateEstuary(edges1, edges2,cell.IncomingRiverDirection == dir);
        }
        else
        {

            WaterShore.AddQuad(edges1.v1, edges1.v2, edges2.v1, edges2.v2);
            WaterShore.AddQuad(edges1.v2, edges1.v3, edges2.v2, edges2.v3);
            WaterShore.AddQuad(edges1.v3, edges1.v4, edges2.v3, edges2.v4);
            WaterShore.AddQuad(edges1.v4, edges1.v5, edges2.v4, edges2.v5);

            WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
            WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
            WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
            WaterShore.AddQuadUV(0f, 0f, 0f, 1f);



            HexCell nextNeighbour = cell.GetNeighbour(dir.Next());
            if (nextNeighbour != null)
            {
                Vector3 v3 = nextNeighbour.Position + (nextNeighbour.IsUnderwater ? HexMetrics.GetFirstWaterCorner(dir.Previous()) : HexMetrics.GetFirstSolidCorner(dir.Previous()));
                v3.y = center.y;


                WaterShore.AddTriangle(edges1.v5, edges2.v5, v3);
                WaterShore.AddTriangleUV(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f));
            }
        }
    }

    public void TriangulateEstuary(EdgeVertices edges1,EdgeVertices edges2,bool incomingRiver)
    {
        WaterShore.AddTriangle(edges2.v1, edges1.v2, edges1.v1);
        WaterShore.AddTriangle(edges2.v5, edges1.v5, edges1.v4);
        WaterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        WaterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        Estuaries.AddQuad(edges2.v1, edges1.v2, edges2.v2, edges1.v3);
        Estuaries.AddTriangle(edges1.v3, edges2.v2, edges2.v4);
        Estuaries.AddQuad(edges1.v3, edges1.v4, edges2.v4, edges2.v5);

        Estuaries.AddQuadUV(new Vector2(0f, 1f), new Vector2(0f, 0f),new Vector2(0f, 1f), new Vector2(0f, 0f));
        Estuaries.AddTriangleUV(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        Estuaries.AddQuadUV(0f, 0f, 0f, 1f);

        if (incomingRiver)
        {
            Estuaries.AddQuadUV2(new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f), new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
            Estuaries.AddTriangleUV2(new Vector2(0.5f, 1.1f), new Vector2(1f, 0.8f), new Vector2(0f, 0.8f));
            Estuaries.AddQuadUV2(new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f), new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f));
        }
        else
        {
            Estuaries.AddQuadUV2(new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),new Vector2(0f, 0f), new Vector2(0.5f, -0.3f));
            Estuaries.AddTriangleUV2(new Vector2(0.5f, -0.3f),new Vector2(0f, 0f),new Vector2(1f, 0f));
            Estuaries.AddQuadUV2(new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),new Vector2(1f, 0f), new Vector2(1.5f, -0.2f));
        }
    }


    public void TriangulateWithoutRiver(HexDirection dir, HexCell cell, Vector3 center, EdgeVertices edges)
    {
        TriangulateEdgeFan(center, edges, Weights1, cell.CellIndex);
        if (cell.HasRoads)
        {
            Vector2 roadInterpolators = GetRoadInterpolators(dir, cell);

            TriangulateRoad(center, Vector3.Lerp(center, edges.v1, roadInterpolators.x), Vector3.Lerp(center, edges.v5, roadInterpolators.y), edges, cell.HasRoadThroughEdge(dir));
        }
    }

    public void TriangulateRoadEdge(Vector3 center, Vector3 midLeft, Vector3 midRight)
    {
        Roads.AddTriangle(center, midLeft, midRight);
        Roads.AddTriangleUV(new Vector2(1, 0), Vector2.zero, Vector2.zero);
    }

    public void TriangulateWithRiver(HexDirection dir, HexCell cell, Vector3 center, EdgeVertices edges)
    {
        Vector3 centerL;
        Vector3 centerR;

        float centerLinePinch = 2.0f / 3.0f;

        if (cell.HasRiverThroughEdge(dir.Opposite()))
        {
            centerL = center + HexMetrics.GetFirstSolidCorner(dir.Previous()) * HexMetrics.SubdivideFraction;
            centerR = center + HexMetrics.GetSecondSolidCorner(dir.Next()) * HexMetrics.SubdivideFraction;
            center = (centerL + centerR) / 2.0f;
        }
        else if (cell.HasRiverThroughEdge(dir.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, edges.v5, centerLinePinch);
        }
        else if (cell.HasRiverThroughEdge(dir.Previous()))
        {
            centerL = Vector3.Lerp(center, edges.v1, centerLinePinch);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(dir.Next2()))
        {
            centerL = center;
            centerR = center + HexMetrics.GetSolidEdgeMiddle(dir.Next()) * (0.5f * HexMetrics.InnerToOuter);
        }
        else
        {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(dir.Previous()) * (0.5f * HexMetrics.InnerToOuter);
            centerR = center;
        }

        EdgeVertices modifiedEdges = new EdgeVertices(Vector3.Lerp(centerL, edges.v1, 0.5f), Vector3.Lerp(centerR, edges.v5, 0.5f));
        modifiedEdges.v3.y = center.y = edges.v3.y;

        TriangulateEdgeStrip(modifiedEdges,  Weights1, cell.TerrainTypeIndex,edges, Weights1, cell.CellIndex);

        Terrain.AddTriangle(centerL, modifiedEdges.v1, modifiedEdges.v2);
        Terrain.AddQuad(centerL, center, modifiedEdges.v2, modifiedEdges.v3);
        Terrain.AddQuad(center, centerR, modifiedEdges.v3, modifiedEdges.v4);
        Terrain.AddTriangle(centerR, modifiedEdges.v4, modifiedEdges.v5);

        Vector3 indices = new Vector3(cell.CellIndex, cell.CellIndex, cell.CellIndex);

        Terrain.AddTriangleCellData(indices, Weights1);
        Terrain.AddQuadCellData(indices, Weights1);
        Terrain.AddQuadCellData(indices, Weights1);
        Terrain.AddTriangleCellData(indices, Weights1);

        if (!cell.IsUnderwater)
        {
            bool reverse = cell.IncomingRiverDirection == dir;

            TriangulateRiverQuad(centerL, centerR, modifiedEdges.v2, modifiedEdges.v4, cell.RiverSurfaceY, 0.4f, reverse);
            TriangulateRiverQuad(modifiedEdges.v2, modifiedEdges.v4, edges.v2, edges.v4, cell.RiverSurfaceY, 0.6f, reverse);
        }
    }


    public void TriangulateWithRiverBeginningOrEnd(HexDirection dir, HexCell cell, Vector3 center, EdgeVertices edges)
    {
        EdgeVertices modifiedEdges = new EdgeVertices(Vector3.Lerp(center, edges.v1, 0.5f), Vector3.Lerp(center, edges.v5, 0.5f));
        modifiedEdges.v3.y = edges.v3.y;

        Color color = Weights1; // cell.Color;


        TriangulateEdgeStrip(modifiedEdges, color, cell.TerrainTypeIndex,edges, color, cell.CellIndex);
        TriangulateEdgeFan(center, modifiedEdges, color, cell.CellIndex);


        if (!cell.IsUnderwater)
        {
            bool reverse = cell.HasIncomingRiver;
            TriangulateRiverQuad(modifiedEdges.v2, modifiedEdges.v4, edges.v2, edges.v4, cell.RiverSurfaceY, 0.6f, reverse);

            center.y = modifiedEdges.v2.y = modifiedEdges.v4.y = cell.RiverSurfaceY;

            Rivers.AddTriangle(center, modifiedEdges.v2, modifiedEdges.v4);
            if (reverse)
            {
                Rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
            }
            else
            {
                Rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
            }
        }

    }

    public void TriangulateAdjacentToRiver(HexDirection dir, HexCell cell, Vector3 center, EdgeVertices edges)
    {
        if (cell.HasRoads)
        {
            TriangulateRoadsAdjacentToRiver(dir, cell, center, edges);
        }


        if (cell.HasRiverThroughEdge(dir.Next()))
        {
            if (cell.HasRiverThroughEdge(dir.Previous()))
            {
                center += HexMetrics.GetSolidEdgeMiddle(dir) * (HexMetrics.InnerToOuter * 0.5f);
            }
            else if (cell.HasRiverThroughEdge(dir.Previous2()))
            {
                center += HexMetrics.GetFirstSolidCorner(dir) * 0.23f;
            }

        }
        else if (cell.HasRiverThroughEdge(dir.Previous()) && cell.HasRiverThroughEdge(dir.Next2()))
        {
            center += HexMetrics.GetSecondSolidCorner(dir) * 0.25f;
        }


        EdgeVertices modifiedEdges = new EdgeVertices(Vector3.Lerp(center, edges.v1, 0.5f), Vector3.Lerp(center, edges.v5, 0.5f));

        Color color = Weights1; // cell.Color;

        TriangulateEdgeStrip(modifiedEdges, color, cell.TerrainTypeIndex, edges, color, cell.CellIndex);
        TriangulateEdgeFan(center, modifiedEdges, color, cell.CellIndex);


        if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(dir))
        {
            // centred in triangle
            Vector3 position = (center + edges.v1 + edges.v5) * (1f / 3f);
            FeatureManager.AddFeature(cell,position);
        }
    }


    public void TriangulateConnection(HexDirection dir, HexCell cell, EdgeVertices edges)
    {
        HexCell neighbour = cell.GetNeighbour(dir);

        if (neighbour == null)
        {
            return;
        }

        Vector3 bridge = HexMetrics.GetBridge(dir);
        bridge.y = neighbour.Position.y - cell.Position.y;
        EdgeVertices edges2 = new EdgeVertices(edges.v1 + bridge, edges.v5 + bridge);

        bool hasRoad = cell.HasRoadThroughEdge(dir);
        bool hasRiver = cell.HasRiverThroughEdge(dir);


        if (hasRiver)
        {
            if (!cell.IsUnderwater)
            {
                if (!neighbour.IsUnderwater)
                {
                    edges2.v3.y = neighbour.StreamBedY;
                    bool reverse = cell.HasIncomingRiver && cell.IncomingRiverDirection == dir;
                    TriangulateRiverQuad(edges.v2, edges.v4, edges2.v2, edges2.v4, cell.RiverSurfaceY, neighbour.RiverSurfaceY, 0.8f, reverse);
                }
                else if(cell.Elevation > neighbour.WaterLevel)
                {
                    TriangulateWaterfallInWater(edges.v2, edges.v4, edges2.v2, edges2.v4, cell.RiverSurfaceY, neighbour.RiverSurfaceY, neighbour.WaterSurfaceY);
                }
            }
            else if (!neighbour.IsUnderwater && neighbour.Elevation > cell.WaterLevel)
            {
                TriangulateWaterfallInWater(edges2.v2, edges2.v4, edges.v2, edges.v4, neighbour.RiverSurfaceY, cell.RiverSurfaceY, cell.WaterSurfaceY);
            }

        }


        if (cell.GetEdgeType(dir) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(edges, cell, edges2, neighbour, hasRoad);
        }
        else
        {
            TriangulateEdgeStrip(edges, Weights1, cell.CellIndex,edges2, Weights2, neighbour.CellIndex, hasRoad);
        }


        FeatureManager.AddWall(edges, cell, edges2, neighbour,hasRoad,hasRiver);


        HexCell nextNeighbor = cell.GetNeighbour(dir.Next());
        if (dir <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 v5 = edges.v5 + HexMetrics.GetBridge(dir.Next());
            v5.y = nextNeighbor.Position.y;

            if (cell.Elevation <= neighbour.Elevation)
            {
                if (cell.Elevation < nextNeighbor.Elevation)
                {
                    TriangulateCorner(edges.v5, cell, edges2.v5, neighbour, v5, nextNeighbor);
                }
                else
                {
                    TriangulateCorner(v5, nextNeighbor, edges.v5, cell, edges2.v5, neighbour);
                }
            }
            else if (neighbour.Elevation < nextNeighbor.Elevation)
            {
                TriangulateCorner(edges2.v5, neighbour, v5, nextNeighbor, edges.v5, cell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, edges.v5, cell, edges2.v5, neighbour);
            }
        }


    }

    public void TriangulateEdgeTerraces(EdgeVertices beginEdges, HexCell beginCell, EdgeVertices endEdges, HexCell endCell, bool hasRoad)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(beginEdges, endEdges, 1);
        Color w2 = HexMetrics.TerraceLerp(Weights1, Weights2, 1);
        float index1 = beginCell.CellIndex;
        float index2 = endCell.CellIndex;

        TriangulateEdgeStrip(beginEdges, Weights1, index1,e2, w2, index2,hasRoad);

        for (int i = 2; i < HexMetrics.TerraceSteps; ++i)
        {
            EdgeVertices e1 = e2;
            Color w1 = w2;

            e2 = EdgeVertices.TerraceLerp(beginEdges, endEdges, i);
            w2 = HexMetrics.TerraceLerp(Weights1, Weights2, i);
            TriangulateEdgeStrip(e1, w1, index1,e2, w2, index2,hasRoad);
        }

        TriangulateEdgeStrip(e2, w2, index1,endEdges, Weights2, index2,hasRoad);


    }

    public void TriangulateCorner(Vector3 bottom, HexCell bottomCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
            {
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            else if (rightEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
            }
        }
        else
        {
            Terrain.AddTriangle(bottom, left, right);

            Vector3 indices;
            indices.x = bottomCell.CellIndex;
            indices.y = leftCell.CellIndex;
            indices.z = rightCell.CellIndex;
            Terrain.AddTriangleCellData(indices,Weights1,Weights2,Weights3);
        }

        FeatureManager.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);

    }

    public void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color w3 = HexMetrics.TerraceLerp(Weights1, Weights2, 1);
        Color w4 = HexMetrics.TerraceLerp(Weights1, Weights3, 1);

        Vector3 indices;
        indices.x = beginCell.CellIndex;
        indices.y = leftCell.CellIndex;
        indices.z = rightCell.CellIndex;

        Terrain.AddTriangle(begin, v3, v4);
        Terrain.AddTriangleCellData(indices,Weights1, w3, w4);
        

        for (int i = 2; i < HexMetrics.TerraceSteps; ++i)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color w1 = w3;
            Color w2 = w4;

            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            w3 = HexMetrics.TerraceLerp(Weights1, Weights2, i);
            w4 = HexMetrics.TerraceLerp(Weights1, Weights3, i); ;

            Terrain.AddQuad(v1, v2, v3, v4);
            Terrain.AddQuadCellData(indices,w1, w2, w3, w4);
        }


        Terrain.AddQuad(v3, v4, left, right);
        Terrain.AddQuadCellData(indices,w3, w4, Weights2, Weights3);
    }

    public void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = Mathf.Abs(1f / (rightCell.Elevation - beginCell.Elevation));
        Vector3 boundary = Vector3.Lerp(HexMetrics.PerturbVector(begin), HexMetrics.PerturbVector(right), b);
        Color boundaryWeights = Color.Lerp(Weights1, Weights2, b);
        Vector3 indices;
        indices.x = beginCell.CellIndex;
        indices.y = leftCell.CellIndex;
        indices.z = rightCell.CellIndex;

        TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryWeights,indices);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryWeights,indices);
        }
        else
        {
            Terrain.AddTriangleUnperturbed(HexMetrics.PerturbVector(left), HexMetrics.PerturbVector(right), boundary);
            Terrain.AddTriangleCellData(indices,Weights1, Weights2, boundaryWeights);
        }

    }

    public void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = Mathf.Abs(1f / (leftCell.Elevation - beginCell.Elevation));
        Vector3 boundary = Vector3.Lerp(HexMetrics.PerturbVector(begin), HexMetrics.PerturbVector(left), b);
        Color boundaryWeights = Color.Lerp(Weights1, Weights2, b);
        Vector3 indices;
        indices.x = beginCell.TerrainTypeIndex;
        indices.y = leftCell.TerrainTypeIndex;
        indices.z = rightCell.TerrainTypeIndex;

        TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryWeights,indices);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryWeights,indices);
        }
        else
        {
            Terrain.AddTriangleUnperturbed(HexMetrics.PerturbVector(left), HexMetrics.PerturbVector(right), boundary);
            Terrain.AddTriangleCellData(indices,Weights1, Weights2, boundaryWeights);
        }

    }



    public void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 boundary, Color boundaryWeights,Vector3 indices)
    {
        Vector3 v2 = HexMetrics.PerturbVector(HexMetrics.TerraceLerp(begin, left, 1));
        Color w2 = HexMetrics.TerraceLerp(Weights1, Weights2, 1);


        Terrain.AddTriangleUnperturbed(HexMetrics.PerturbVector(begin), v2, boundary);
        Terrain.AddTriangleCellData(indices,Weights1, w2, boundaryWeights);


        for (int i = 2; i < HexMetrics.TerraceSteps; ++i)
        {
            Vector3 v1 = v2;
            Color w1 = w2;

            v2 = HexMetrics.PerturbVector(HexMetrics.TerraceLerp(begin, left, i));
            w1 = HexMetrics.TerraceLerp(Weights1, Weights2, i);

            Terrain.AddTriangle(HexMetrics.PerturbVector(v1), v2, boundary);
            Terrain.AddTriangleCellData(indices,w1, w2, boundaryWeights);
        }


        Terrain.AddTriangleUnperturbed(v2, HexMetrics.PerturbVector(left), boundary);
        Terrain.AddTriangleCellData(indices,w2, Weights2, boundaryWeights);

    }


    public void TriangulateEdgeFan(Vector3 center, EdgeVertices edges, Color color,float index)
    {
        Terrain.AddTriangle(center, edges.v1, edges.v2);
        Terrain.AddTriangle(center, edges.v2, edges.v3);
        Terrain.AddTriangle(center, edges.v3, edges.v4);
        Terrain.AddTriangle(center, edges.v4, edges.v5);

        Vector3 indices = new Vector3(index, index, index);
        color = Weights1;
        Terrain.AddTriangleCellData(indices,color);
        Terrain.AddTriangleCellData(indices, color);
        Terrain.AddTriangleCellData(indices, color);
        Terrain.AddTriangleCellData(indices, color);

    }

    public void TriangulateEdgeStrip(EdgeVertices edges1, Color w1, float index1, EdgeVertices edges2, Color w2,float index2, bool hasRoad = false)
    {
        Terrain.AddQuad(edges1.v1, edges1.v2, edges2.v1, edges2.v2);
        Terrain.AddQuad(edges1.v2, edges1.v3, edges2.v2, edges2.v3);
        Terrain.AddQuad(edges1.v3, edges1.v4, edges2.v3, edges2.v4);
        Terrain.AddQuad(edges1.v4, edges1.v5, edges2.v4, edges2.v5);

        w1 = Weights1;
        w2 = Weights2;

        Vector3 indices;
        indices.x = indices.z = index1;
        indices.y = index2;

        Terrain.AddQuadCellData(indices,w1, w2);
        Terrain.AddQuadCellData(indices, w1, w2);
        Terrain.AddQuadCellData(indices, w1, w2);
        Terrain.AddQuadCellData(indices, w1, w2);

        if (hasRoad)
        {
            TriangulateRoadSegment(edges1.v2, edges1.v3, edges1.v4, edges2.v2, edges2.v3, edges2.v4);
        }
    }
    public void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool reverse)
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reverse);
    }

    public void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, bool reverse)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;

        Rivers.AddQuad(v1, v2, v3, v4);
        if (reverse)
        {
            Rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
        }
        else
        {
            Rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
        }

    }


    public void TriangulateRoadSegment(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6)
    {
        Roads.AddQuad(v1, v2, v4, v5);
        Roads.AddQuad(v2, v3, v5, v6);
        Roads.AddQuadUV(0, 1, 0, 0);
        Roads.AddQuadUV(1, 0, 0, 0);
    }

    public void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices edges, bool hasRoadThroughCellEdge)
    {
        if (hasRoadThroughCellEdge)
        {
            Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
            TriangulateRoadSegment(mL, mC, mR, edges.v2, edges.v3, edges.v4);
            Roads.AddTriangle(center, mL, mC);
            Roads.AddTriangle(center, mC, mR);
            Roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            Roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
        }
        else
        {
            TriangulateRoadEdge(center, mL, mR);
        }
    }

    public void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices edges)
    {
        Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
        TriangulateRoadSegment(mL, mC, mR, edges.v2, edges.v3, edges.v4);
        Roads.AddTriangle(center, mL, mC);
        Roads.AddTriangle(center, mC, mR);
        Roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
        Roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
    }


    public Vector2 GetRoadInterpolators(HexDirection dir, HexCell cell)
    {
        Vector2 interpolators;
        if (cell.HasRoadThroughEdge(dir))
        {
            interpolators.x = interpolators.y = 0.5f;
        }
        else
        {
            interpolators.x = cell.HasRoadThroughEdge(dir.Previous()) ? 0.5f : 0.25f;
            interpolators.y = cell.HasRoadThroughEdge(dir.Next()) ? 0.5f : 0.25f;
        }
        return interpolators;
    }

    public void TriangulateRoadsAdjacentToRiver(HexDirection dir, HexCell cell, Vector3 center, EdgeVertices edges)
    {
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(dir);
        bool previousHasRiver = cell.HasRiverThroughEdge(dir.Previous());
        bool nextHasRiver = cell.HasRiverThroughEdge(dir.Next());


        Vector2 roadInterpolators = GetRoadInterpolators(dir, cell);
        Vector3 roadCenter = center;

        if (cell.HasRiverBeginningOrEnd)
        {
            roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
        }
        else if (cell.IncomingRiverDirection == cell.OutgoingRiverDirection.Opposite())
        {
            Vector3 corner;

            if (previousHasRiver)
            {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(dir.Next()))
                {
                    return;
                }

                corner = HexMetrics.GetSecondSolidCorner(dir);
            }
            else
            {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(dir.Previous()))
                {
                    return;
                }
                corner = HexMetrics.GetFirstSolidCorner(dir);
            }

            roadCenter += corner * 0.5f;

            if (cell.IncomingRiverDirection == dir.Next() && (cell.HasRoadThroughEdge(dir.Next2()) ||cell.HasRoadThroughEdge(dir.Opposite())))
            {
                FeatureManager.AddBridge(roadCenter, center - corner * 0.5f);
            }

            center += corner * 0.25f;

        }
        else if (cell.IncomingRiverDirection == cell.OutgoingRiverDirection.Previous())
        {
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiverDirection) * 0.2f;
        }
        else if (cell.IncomingRiverDirection == cell.OutgoingRiverDirection.Next())
        {
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiverDirection) * 0.2f;
        }
        else if (previousHasRiver && nextHasRiver)
        {
            if (!hasRoadThroughEdge)
            {
                return;
            }

            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(dir) * HexMetrics.InnerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        else
        {
            HexDirection middle;
            if (previousHasRiver)
            {
                middle = dir.Next();
            }
            else if (nextHasRiver)
            {
                middle = dir.Previous();
            }
            else
            {
                middle = dir;
            }

            if (!cell.HasRoadThroughEdge(middle) && !cell.HasRoadThroughEdge(middle.Previous()) && !cell.HasRoadThroughEdge(middle.Next()))
            {
                return;
            }

            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(middle);
            roadCenter += offset * 0.25f;
            if (dir == middle && cell.HasRoadThroughEdge(dir.Opposite()))
            {
                FeatureManager.AddBridge(roadCenter, center - offset * (HexMetrics.InnerToOuter * 0.7f));
            }   
        }

        Vector3 mL = Vector3.Lerp(center, edges.v1, roadInterpolators.x);
        Vector3 mR = Vector3.Lerp(center, edges.v5, roadInterpolators.y);
        TriangulateRoad(roadCenter, mL, mR, edges, hasRoadThroughEdge);

        if (previousHasRiver)
        {
            TriangulateRoadEdge(roadCenter, center, mL);
        }

        if (nextHasRiver)
        {
            TriangulateRoadEdge(roadCenter, mR, center);
        }
    }

    public void TriangulateWaterfallInWater(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,float y1,float y2,float waterY)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;

        v1 = HexMetrics.PerturbVector(v1);
        v2 = HexMetrics.PerturbVector(v2);
        v3 = HexMetrics.PerturbVector(v3);
        v4 = HexMetrics.PerturbVector(v4);

        float t = (waterY - y2) / (y1 - y2);

        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);

        Rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        Rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
    }



}
