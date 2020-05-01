using UnityEngine;

public class HexFeatureManager : MonoBehaviour, IHexMeshChunkModule
{

    public HexFeatureCollection[] UrbanFeatureCollections;
    public HexFeatureCollection[] FarmFeatureCollections;
    public HexFeatureCollection[] PlantFeatureCollections;

    public Transform[] SpecialFeatures;

    public HexMesh Walls;

    public Transform WallTower;
    public Transform Bridge;

    private Transform m_container;


    public void Clear()
    {
        if (m_container != null)
        {
            Destroy(m_container.gameObject);
        }
        m_container = new GameObject("FeaturesContainer").transform;
        m_container.SetParent(transform, false);


    }

    public void Apply()
    {

    }

    public Transform PickFeaturePrefab(HexFeatureCollection[] collections, int level, float hash, float choice)
    {
        float[] chances = HexMetrics.GetFeatureThresholds(level);
        for (int i = 0; i < chances.Length; ++i)
        {
            if (hash < chances[i])
            {
                return collections[i].Pick(choice);
            }
        }
        return null;
    }

    public void AddFeature(HexCell cell, Vector3 position)
    {
        if(cell.HasSpecialFeature)
        {
            return;
        }

        HexHash hexHash = HexMetrics.SampleHashGrid(position);
        if (hexHash.a >= 0.25f * cell.UrbanDensityLevel)
        {
            return;
        }


        Transform urbanFeature = PickFeaturePrefab(UrbanFeatureCollections, cell.UrbanDensityLevel - 1, hexHash.a, hexHash.choice);
        Transform farmFeature = PickFeaturePrefab(FarmFeatureCollections, cell.FarmDensityLevel - 1, hexHash.b, hexHash.choice);
        Transform plantFeature = PickFeaturePrefab(PlantFeatureCollections, cell.PlantDensityLevel - 1, hexHash.c, hexHash.choice);

        float defaultMax = 1000;
        Transform chosenTransform = null;
        float usedHash = Mathf.Min(urbanFeature != null ? hexHash.a : defaultMax, farmFeature != null ? hexHash.b : defaultMax, plantFeature != null ? hexHash.c : defaultMax);

        if (usedHash == hexHash.a)
        {
            chosenTransform = urbanFeature;
        }
        else if (usedHash == hexHash.b)
        {
            chosenTransform = farmFeature;
        }
        else if (usedHash == hexHash.c)
        {
            chosenTransform = plantFeature;
        }

        if (chosenTransform != null)
        {
            Transform instance = Instantiate(chosenTransform);
            position.y += instance.localScale.y * 0.5f;
            instance.localPosition = HexMetrics.PerturbVector(position);
            instance.localRotation = Quaternion.Euler(0f, 360f * hexHash.randomRotation, 0f);
            instance.SetParent(m_container, false);
        }
    }

    public void AddWall(EdgeVertices nearEdges, HexCell nearCell, EdgeVertices farEdges, HexCell farCell,bool hasRoad,bool hasRiver)
    {
        // simple guards.
        if(nearCell.IsUnderwater || farCell.IsUnderwater)
        {
            return;
        }

        if(nearCell.GetEdgeType(farCell) == HexEdgeType.Cliff)
        {
            return;
        }


        if(nearCell.Walled != farCell.Walled)
        {
            AddWallSegment(nearEdges.v1, farEdges.v1, nearEdges.v2, farEdges.v2);
            if (hasRoad || hasRiver)
            {
                AddWallCap(nearEdges.v2, farEdges.v2);
                AddWallCap(farEdges.v4, nearEdges.v4);
            }
            else
            {
                AddWallSegment(nearEdges.v2, farEdges.v2, nearEdges.v3, farEdges.v3);
                AddWallSegment(nearEdges.v3, farEdges.v3, nearEdges.v4, farEdges.v4);
            }
            AddWallSegment(nearEdges.v4, farEdges.v4, nearEdges.v5, farEdges.v5);
        }
    }

    public void AddWall(Vector3 c1,HexCell cell1,Vector3 c2, HexCell cell2, Vector3 c3, HexCell cell3)
    {
        if (cell1.Walled)
        {
            if (cell2.Walled)
            {
                if (!cell3.Walled)
                {
                    AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
                }
            }
            else if (cell3.Walled)
            {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
            else
            {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
        }
        else if (cell2.Walled)
        {
            if (cell3.Walled)
            {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
            else
            {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
        }
        else if (cell3.Walled)
        {
            AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
        }
    }


    public void AddWallSegment(Vector3 pivot,HexCell pivotCell,Vector3 left,HexCell leftCell,Vector3 right,HexCell rightCell)
    {
        if(pivotCell.IsUnderwater)
        {
            return;
        }

        bool hasLeftWall = !leftCell.IsUnderwater && pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
        bool hasRightWall = !rightCell.IsUnderwater && pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

        if (hasLeftWall)
        {
            if (hasRightWall)
            {
                bool hasTower = false;
                if (leftCell.Elevation == rightCell.Elevation)
                {
                    HexHash hash = HexMetrics.SampleHashGrid((pivot + left + right) * (1f / 3f));
                    hasTower = hash.choice < HexMetrics.WallTowerThreshold;
                }
                AddWallSegment(pivot, left, pivot, right, hasTower);
            }
            else if (leftCell.Elevation < rightCell.Elevation)
            {
                AddWallWedge(pivot, left, right);
            }
            else
            {
                AddWallCap(pivot, left);
            }
        }
        else if (hasRightWall)
        {
            if (rightCell.Elevation < leftCell.Elevation)
            {
                AddWallWedge(right, pivot, left);
            }
            else
            {
                AddWallCap(right, pivot);
            }
        }



    }

    public void AddWallSegment(Vector3 nearLeft,Vector3 farLeft,Vector3 nearRight,Vector3 farRight,bool addTower = false)
    {
        nearLeft = HexMetrics.PerturbVector(nearLeft);
        farLeft = HexMetrics.PerturbVector(farLeft);
        nearRight = HexMetrics.PerturbVector(nearRight);
        farRight = HexMetrics.PerturbVector(farRight);


        Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
        Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

        Vector3 leftOffset = HexMetrics.WallThicknessOffset(nearLeft, farLeft);
        Vector3 rightOffset = HexMetrics.WallThicknessOffset(nearRight, farRight);


        float leftTop = left.y + HexMetrics.WallHeight;
        float rightTop = right.y + HexMetrics.WallHeight;

        Vector3 v1, v2, v3, v4;
        v1 = v3 = left - leftOffset;
        v2 = v4 = right - rightOffset;
        v3.y = leftTop;
        v4.y = rightTop;

        Walls.AddQuadUnperturbed(v1, v2, v3, v4);

        Vector3 t1 = v3, t2 = v4;


        v1 = v3 = left + leftOffset;
        v2 = v4 = right + rightOffset;

        v3.y = leftTop;
        v4.y = rightTop;

        Walls.AddQuadUnperturbed(v2, v1, v4, v3);
        
        // top
        Walls.AddQuadUnperturbed(t1, t2, v3, v4);

        if (addTower)
        {
            Transform towerInstance = Instantiate(WallTower);
            towerInstance.transform.localPosition = (left + right) * 0.5f;
            Vector3 rightDirection = right - left;
            rightDirection.y = 0;
            towerInstance.transform.right = rightDirection;
            towerInstance.SetParent(m_container, false);
        }
    }

    public void AddWallCap(Vector3 near,Vector3 far)
    {
        near = HexMetrics.PerturbVector(near);
        far = HexMetrics.PerturbVector(far);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = center.y + HexMetrics.WallHeight;
        Walls.AddQuadUnperturbed(v1, v2, v3, v4);

    }

    void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
    {
        near = HexMetrics.PerturbVector(near);
        far = HexMetrics.PerturbVector(far);
        point = HexMetrics.PerturbVector(point);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;
        Vector3 pointTop = point;
        point.y = center.y;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = pointTop.y = center.y + HexMetrics.WallHeight;

        //		walls.AddQuadUnperturbed(v1, v2, v3, v4);
        Walls.AddQuadUnperturbed(v1, point, v3, pointTop);
        Walls.AddQuadUnperturbed(point, v2, pointTop, v4);
        Walls.AddTriangleUnperturbed(pointTop, v3, v4);
    }


    public void AddBridge(Vector3 roadCenter1,Vector3 roadCenter2)
    {
        roadCenter1 = HexMetrics.PerturbVector(roadCenter1);
        roadCenter2 = HexMetrics.PerturbVector(roadCenter2);


        float distance = Vector3.Distance(roadCenter1, roadCenter2);

        float bridgeScale = distance / HexMetrics.BridgeDesignLength;

        Transform bridgeInstance = Instantiate(Bridge);
        bridgeInstance.transform.localPosition = (roadCenter1 + roadCenter2) * 0.5f;
        bridgeInstance.transform.forward = roadCenter2 - roadCenter1;
        bridgeInstance.localScale = new Vector3 (1f,1f, bridgeScale);
        bridgeInstance.SetParent(m_container, false);
    }

    public void AddSpecialFeature(HexCell cell, Vector3 position)
    {
        Transform instance = Instantiate(SpecialFeatures[cell.SpecialFeatureIndex - 1]);
        instance.localPosition = HexMetrics.PerturbVector(position);
        HexHash hash = HexMetrics.SampleHashGrid(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.randomRotation, 0f);
        instance.SetParent(m_container, false);
    }

}
