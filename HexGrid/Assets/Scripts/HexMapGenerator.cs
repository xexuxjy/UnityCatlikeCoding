using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class HexMapGenerator : MonoBehaviour
{
    public HexGrid HexGrid;

    private HexCellPriorityQueue m_hexCellPriorityQueue;

    [Range(0f, 0.5f)]
    public float JitterProbability = 0.25f;

    [Range(20, 200)]
    public int ChunkSizeMin = 30;

    [Range(20, 200)]
    public int ChunkSizeMax = 100;

    [Range(5, 95)]
    public int LandPercentage = 50;

    [Range(1, 5)]
    public int WaterLevel = 3;

    [Range(0f, 1f)]
    public float HighRiseProbability = 0.25f;

    [Range(0f, 0.4f)]
    public float SinkProbability = 0.2f;

    [Range(-4, 0)]
    public int ElevationMinimum = -2;

    [Range(6, 10)]
    public int ElevationMaximum = 8;

    [Range(0, 10)]
    public int MapBorderX = 5;

    [Range(0, 10)]
    public int MapBorderZ = 5;

    [Range(0, 10)]
    public int RegionBorder = 5;

    [Range(1, 4)]
    public int RegionCount = 1;

    [Range(0, 100)]
    public int ErosionPercentage = 50;

    public bool UseFixedSeed;
    public int Seed;



    public HexCell GetRandomCell(MapRegion mapRegion)
    {
        return HexGrid.GetCell(Random.Range(mapRegion.xMin, mapRegion.xMax), Random.Range(mapRegion.zMin, mapRegion.zMax));
    }
    

    //MapRegion m_mapRegion;
    private List<MapRegion> m_mapRegions = new List<MapRegion>();


    public void GenerateMap(int x, int z)
    {
        Random.State originalRandomState = Random.state;
        if (!UseFixedSeed)
        {
            Seed = Random.Range(0, int.MaxValue);
            Seed ^= (int)System.DateTime.Now.Ticks;
            Seed ^= (int)Time.unscaledTime;
            Seed &= int.MaxValue;
        }
        Random.InitState(Seed);

        HexGrid.CreateMap(x, z);
        if (m_hexCellPriorityQueue == null)
        {
            m_hexCellPriorityQueue = new HexCellPriorityQueue();
        }
        HexGrid.ResetSearchPhase();

        foreach (HexCell hexCell in HexGrid.GetHexCells())
        {
            hexCell.WaterLevel = WaterLevel;
        }

        CreateRegions(x,z);

        CreateLand();
        ErodeLand();
        SetTerrainType();

        Random.state = originalRandomState;
    }


    public void CreateRegions(int x,int z)
    {
        m_mapRegions.Clear();
        MapRegion region = new MapRegion();

        switch(RegionCount)
        {
            default:
                region.xMin = MapBorderX;
                region.xMax = HexGrid.CellCountX - MapBorderX;
                region.zMin = MapBorderZ;
                region.zMax = HexGrid.CellCountZ - MapBorderZ;
                m_mapRegions.Add(region);
                break;
            case 2:
                if (Random.value < 0.5f)
                {
                    region.xMin = MapBorderX;
                    region.xMax = HexGrid.CellCountX / 2 - RegionBorder;
                    region.zMin = MapBorderZ;
                    region.zMax = HexGrid.CellCountZ - MapBorderZ;
                    m_mapRegions.Add(region);
                    region.xMin = HexGrid.CellCountX / 2 + RegionBorder;
                    region.xMax = HexGrid.CellCountX - MapBorderX;
                    m_mapRegions.Add(region);
                }
                else
                {
                    region.xMin = MapBorderX;
                    region.xMax = HexGrid.CellCountX - MapBorderX;
                    region.zMin = MapBorderZ;
                    region.zMax = HexGrid.CellCountZ / 2 - RegionBorder;
                    m_mapRegions.Add(region);
                    region.zMin = HexGrid.CellCountZ / 2 + RegionBorder;
                    region.zMax = HexGrid.CellCountZ - MapBorderZ;
                    m_mapRegions.Add(region);
                }
                break;
            case 3:
                region.xMin = MapBorderX;
                region.xMax = HexGrid.CellCountX / 3 - RegionBorder;
                region.zMin = MapBorderZ;
                region.zMax = HexGrid.CellCountZ - MapBorderZ;
                m_mapRegions.Add(region);
                region.xMin = HexGrid.CellCountX / 3 + RegionBorder;
                region.xMax = HexGrid.CellCountX * 2 / 3 - RegionBorder;
                m_mapRegions.Add(region);
                region.xMin = HexGrid.CellCountX * 2 / 3 + RegionBorder;
                region.xMax = HexGrid.CellCountX - MapBorderX;
                m_mapRegions.Add(region);
                break;
            case 4:
                region.xMin = MapBorderX;
                region.xMax = HexGrid.CellCountX / 2 - RegionBorder;
                region.zMin = MapBorderZ;
                region.zMax = HexGrid.CellCountZ / 2 - RegionBorder;
                m_mapRegions.Add(region);
                region.xMin = HexGrid.CellCountX / 2 + RegionBorder;
                region.xMax = HexGrid.CellCountX - MapBorderX;
                m_mapRegions.Add(region);
                region.zMin = HexGrid.CellCountZ / 2 + RegionBorder;
                region.zMax = HexGrid.CellCountZ - MapBorderZ;
                m_mapRegions.Add(region);
                region.xMin = MapBorderX;
                region.xMax = HexGrid.CellCountX / 2 - RegionBorder;
                m_mapRegions.Add(region);
                break;
        }


    }


    public void CreateLand()
    {
        int landBudget = Mathf.RoundToInt(HexGrid.CellCount * LandPercentage * 0.01f);
        //while (landBudget > 0)
        for(int guard = 0;guard < 10000;++guard)
        {
            bool sink = Random.value < SinkProbability;
            for (int i = 0; i < m_mapRegions.Count; ++i)
            {
                MapRegion mapRegion = m_mapRegions[i];
                int chunkSize = Random.Range(ChunkSizeMin, ChunkSizeMax - 1);
                if (sink)
                {
                    landBudget = SinkTerrain(chunkSize, landBudget, mapRegion);
                }
                else
                {
                    landBudget = RaiseTerrain(chunkSize, landBudget, mapRegion);
                    if (landBudget == 0)
                    {
                        return;
                    }
                }
            }
        }

        if (landBudget > 0)
        {
            Debug.LogWarning("Failed to use up " + landBudget + " land budget.");
        }
    }


    public void SetTerrainType()
    {
        foreach (HexCell hexCell in HexGrid.GetHexCells())
        {
            if (!hexCell.IsUnderwater)
            {
                hexCell.TerrainTypeIndex = hexCell.Elevation - hexCell.WaterLevel;
            }
        }
    }

    public int RaiseTerrain(int chunkSize, int budget,MapRegion mapRegion)
    {
        HexGrid.ResetSearchPhase();
        int searchPhase = 1;
        HexCell firstCell = GetRandomCell(mapRegion);
        firstCell.SearchPhase = searchPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;

        m_hexCellPriorityQueue.Clear();
        m_hexCellPriorityQueue.Enqueue(firstCell);

        HexCoordinates center = firstCell.Coordinates;

        int rise = Random.value < HighRiseProbability ? 2 : 1;

        int size = 0;
        while (size < chunkSize && m_hexCellPriorityQueue.Count > 0)
        {
            HexCell current = m_hexCellPriorityQueue.Dequeue();
            int originalElevation = current.Elevation;
            int newElevation = originalElevation + rise;
            if (newElevation > ElevationMaximum)
            {
                continue;
            }
            current.Elevation = newElevation;

            if (originalElevation < WaterLevel && newElevation >= WaterLevel && --budget == 0)
            {
                break;
            }

            size++;

            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
            {
                HexCell neighbour = current.GetNeighbour(dir);
                if (neighbour != null && neighbour.SearchPhase < searchPhase)
                {
                    neighbour.SearchPhase = searchPhase;
                    neighbour.Distance = neighbour.Coordinates.DistanceTo(center);
                    neighbour.SearchHeuristic = Random.value < JitterProbability ? 1 : 0;
                    m_hexCellPriorityQueue.Enqueue(neighbour);
                }
            }

        }
        return budget;
    }




    public int SinkTerrain(int chunkSize, int budget, MapRegion mapRegion)
    {
        HexGrid.ResetSearchPhase();
        int searchPhase = 1;
        HexCell firstCell = GetRandomCell(mapRegion);
        firstCell.SearchPhase = searchPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;

        m_hexCellPriorityQueue.Clear();
        m_hexCellPriorityQueue.Enqueue(firstCell);

        HexCoordinates center = firstCell.Coordinates;

        int sink = Random.value < HighRiseProbability ? 2 : 1;

        int size = 0;
        while (size < chunkSize && m_hexCellPriorityQueue.Count > 0)
        {
            HexCell current = m_hexCellPriorityQueue.Dequeue();
            int originalElevation = current.Elevation;
            int newElevation = originalElevation - sink;

            if (newElevation < ElevationMinimum)
            {
                continue;
            }

            current.Elevation = newElevation;

            if (originalElevation >= WaterLevel && newElevation < WaterLevel)
            {
                budget += 1;
            }

            size++;

            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
            {
                HexCell neighbour = current.GetNeighbour(dir);
                if (neighbour != null && neighbour.SearchPhase < searchPhase)
                {
                    neighbour.SearchPhase = searchPhase;
                    neighbour.Distance = neighbour.Coordinates.DistanceTo(center);
                    neighbour.SearchHeuristic = Random.value < JitterProbability ? 1 : 0;
                    m_hexCellPriorityQueue.Enqueue(neighbour);
                }
            }

        }
        return budget;
    }


    public void ErodeLand()
    {
        List<HexCell> erodibleCells = ListPool<HexCell>.Get();
        foreach (HexCell hexCell in HexGrid.GetHexCells())
        {
            if (IsErodible(hexCell))
            {
                erodibleCells.Add(hexCell);
            }
        }

        int targetErodibleCount = (int) (erodibleCells.Count * (100 - ErosionPercentage) * 0.01f);

        while (erodibleCells.Count > targetErodibleCount)
        {
            int index = Random.Range(0, erodibleCells.Count);
            HexCell erodibleCell = erodibleCells[index];
            HexCell targetCell = GetErosionTarget(erodibleCell);
            erodibleCell.Elevation -= 1;
            targetCell.Elevation += 1;

            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
            {
                HexCell neighbour = targetCell.GetNeighbour(dir);
                if (neighbour != null && neighbour != erodibleCell && !IsErodible(neighbour) && neighbour.Elevation == targetCell.Elevation + 1)
                {
                    erodibleCells.Remove(neighbour);
                }
            }


            if (!IsErodible(erodibleCell))
            {
                erodibleCells.Remove(erodibleCell);
            }
            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
            {
                HexCell neighbour = erodibleCell.GetNeighbour(dir);
                if (neighbour != null && IsErodible(neighbour) && !erodibleCells.Contains(neighbour))
                {
                    erodibleCells.Add(neighbour);
                }
            }
        }

        ListPool<HexCell>.Return(erodibleCells);

    }

    public bool IsErodible(HexCell hexCell)
    {
        int erodibleElevation = hexCell.Elevation - 2;
        for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
        {
            HexCell neighbor = hexCell.GetNeighbour(dir);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                return true;
            }
        }
        return false;
    }

    public HexCell GetErosionTarget(HexCell cell)
    {
        List<HexCell> candidates = ListPool<HexCell>.Get();
        int erodibleElevation = cell.Elevation - 2;
        for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
        {
            HexCell neighbor = cell.GetNeighbour(dir);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                candidates.Add(neighbor);
            }
        }
        HexCell target = candidates[Random.Range(0, candidates.Count)];
        ListPool<HexCell>.Return(candidates);
        return target;
    }

}


public struct MapRegion
{
    public int xMin;
    public int xMax;

    public int zMin;
    public int zMax;
}

