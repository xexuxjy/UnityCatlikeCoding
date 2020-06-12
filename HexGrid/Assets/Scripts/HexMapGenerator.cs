using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public bool UseFixedSeed;
    public int Seed;

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

        foreach (HexCell hexCell in HexGrid.ProcessHexCells())
        {
            hexCell.WaterLevel = WaterLevel;
        }

        CreateLand();
        SetTerrainType();
    }

    public void CreateLand()
    {
        int landBudget = Mathf.RoundToInt(HexGrid.CellCount * LandPercentage * 0.01f);
        while (landBudget > 0)
        {
            int chunkSize = Random.Range(ChunkSizeMin, ChunkSizeMax - 1);
            if (Random.value < SinkProbability)
            {
                landBudget = SinkTerrain(chunkSize, landBudget);
            }
            else
            { 
                landBudget = RaiseTerrain(chunkSize, landBudget);
            }
        }
    }


    public void SetTerrainType()
    {
        foreach (HexCell hexCell in HexGrid.ProcessHexCells())
        {
            if (!hexCell.IsUnderwater)
            {
                hexCell.TerrainTypeIndex = hexCell.Elevation - hexCell.WaterLevel;
            }
        }
    }

    public int RaiseTerrain(int chunkSize, int budget)
    {
        int searchPhase = 1;
        HexCell firstCell = HexGrid.GetRandomCell();
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

            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.SW; dir++)
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




    public int SinkTerrain(int chunkSize, int budget)
    {
        int searchPhase = 1;
        HexCell firstCell = HexGrid.GetRandomCell();
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

            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.SW; dir++)
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
}
