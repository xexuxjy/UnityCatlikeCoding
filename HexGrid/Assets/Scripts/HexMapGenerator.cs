using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using UnityEditor;
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

    [Range(0, 1f)]
    public float Evaporation = 0.5f;

    [Range(0, 1f)]
    public float PrecipitationFactor = 0.25f;

    [Range(0, 1f)]
    public float EvaporationFactor = 0.5f;

    [Range(0, 1f)]
    public float RunoffFactor = 0.25f;

    [Range(0, 1f)]
    public float SeepageFactor = 0.125f;

    [Range(0, 1f)]
    public float StartingMoisture = 0.1f;


    public HexDirection WindDirection = HexDirection.NW;

    [Range(0, 10f)]
    public float WindStrength = 4f;

    [Range(0, 20)]
    public int RiverPercentage = 10;

    [Range(0f, 1f)]
    public float ExtraLakeProability = 0.25f;

    [Range(0f, 1f)]
    public float LowTemperature = 0f;

    [Range(0f, 1f)]
    public float HighTemperature = 1f;

    [Range(0f, 1f)]
    public float TemperatureJitter = 0.1f;

    public HemisphereMode HemisphereMode;

    public bool UseFixedSeed;
    public int Seed;

    private int m_numLandCells;


    public static float[] TemperatureBands = { 0.1f, 0.3f, 0.6f };
    public static float[] MoistureBands = { 0.12f, 0.28f, 0.85f };

    public static Biome[] Biomes = {new Biome(0,0),new Biome(4,0),new Biome(4,0),new Biome(4,0),
                                    new Biome(0,0),new Biome(2,0),new Biome(2,1),new Biome(2,2),
                                    new Biome(0,0),new Biome(1,0),new Biome(1,1),new Biome(1,2),
                                    new Biome(0,0),new Biome(1,1),new Biome(1,2),new Biome(1,3) };

    public HexCell GetRandomCell(MapRegion mapRegion)
    {
        return HexGrid.GetCell(Random.Range(mapRegion.xMin, mapRegion.xMax), Random.Range(mapRegion.zMin, mapRegion.zMax));
    }


    //MapRegion m_mapRegion;
    private List<MapRegion> m_mapRegions = new List<MapRegion>();

    private List<ClimateData> m_climateData = new List<ClimateData>();
    private List<ClimateData> m_nextClimateData = new List<ClimateData>();

    public void GenerateMap(int x, int z,bool wrap)
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

        HexGrid.CreateMap(x, z, wrap);
        if (m_hexCellPriorityQueue == null)
        {
            m_hexCellPriorityQueue = new HexCellPriorityQueue();
        }
        HexGrid.ResetSearchPhase();

        foreach (HexCell hexCell in HexGrid.GetHexCells())
        {
            hexCell.WaterLevel = WaterLevel;
        }

        CreateRegions(x, z);

        CreateLand();
        ErodeLand();

        CreateClimate();
        CreateRivers();
        SetTerrainType();

        Random.state = originalRandomState;
    }


    public void CreateRegions(int x, int z)
    {
        m_mapRegions.Clear();
        MapRegion region = new MapRegion();

        int borderX = HexGrid.Wrap ? RegionBorder : MapBorderX;

        switch (RegionCount)
        {
            default:
                if (HexGrid.Wrap)
                {
                    borderX = 0;
                }
                region.xMin = borderX;
                region.xMax = HexGrid.CellCountX - borderX;
                region.zMin = MapBorderZ;
                region.zMax = HexGrid.CellCountZ - MapBorderZ;
                m_mapRegions.Add(region);
                break;
            case 2:
                if (Random.value < 0.5f)
                {
                    region.xMin = borderX;
                    region.xMax = HexGrid.CellCountX / 2 - RegionBorder;
                    region.zMin = MapBorderZ;
                    region.zMax = HexGrid.CellCountZ - MapBorderZ;
                    m_mapRegions.Add(region);
                    region.xMin = HexGrid.CellCountX / 2 + RegionBorder;
                    region.xMax = HexGrid.CellCountX - borderX;
                    m_mapRegions.Add(region);
                }
                else
                {
                    if (HexGrid.Wrap)
                    {
                        borderX = 0;
                    }

                    region.xMin = borderX;
                    region.xMax = HexGrid.CellCountX - borderX;
                    region.zMin = MapBorderZ;
                    region.zMax = HexGrid.CellCountZ / 2 - RegionBorder;
                    m_mapRegions.Add(region);
                    region.zMin = HexGrid.CellCountZ / 2 + RegionBorder;
                    region.zMax = HexGrid.CellCountZ - MapBorderZ;
                    m_mapRegions.Add(region);
                }
                break;
            case 3:
                region.xMin = borderX;
                region.xMax = HexGrid.CellCountX / 3 - RegionBorder;
                region.zMin = MapBorderZ;
                region.zMax = HexGrid.CellCountZ - MapBorderZ;
                m_mapRegions.Add(region);
                region.xMin = HexGrid.CellCountX / 3 + RegionBorder;
                region.xMax = HexGrid.CellCountX * 2 / 3 - RegionBorder;
                m_mapRegions.Add(region);
                region.xMin = HexGrid.CellCountX * 2 / 3 + RegionBorder;
                region.xMax = HexGrid.CellCountX - borderX;
                m_mapRegions.Add(region);
                break;
            case 4:
                region.xMin = borderX;
                region.xMax = HexGrid.CellCountX / 2 - RegionBorder;
                region.zMin = MapBorderZ;
                region.zMax = HexGrid.CellCountZ / 2 - RegionBorder;
                m_mapRegions.Add(region);
                region.xMin = HexGrid.CellCountX / 2 + RegionBorder;
                region.xMax = HexGrid.CellCountX - borderX;
                m_mapRegions.Add(region);
                region.zMin = HexGrid.CellCountZ / 2 + RegionBorder;
                region.zMax = HexGrid.CellCountZ - MapBorderZ;
                m_mapRegions.Add(region);
                region.xMin = borderX;
                region.xMax = HexGrid.CellCountX / 2 - RegionBorder;
                m_mapRegions.Add(region);
                break;
        }


    }


    public void CreateLand()
    {
        int landBudget = Mathf.RoundToInt(HexGrid.CellCount * LandPercentage * 0.01f);
        m_numLandCells = landBudget;

        //while (landBudget > 0)
        for (int guard = 0; guard < 10000; ++guard)
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
            m_numLandCells -= landBudget;
        }
    }


    int m_temperatureJitterChannel;
    public void SetTerrainType()
    {
        m_temperatureJitterChannel = Random.Range(0, 4);
        int rockDesertElevation = ElevationMinimum - (ElevationMaximum - WaterLevel) / 2;


        foreach (HexCell hexCell in HexGrid.GetHexCells())
        {
            float moisture = m_climateData[hexCell.CellIndex].moisture;
            float temperature = DeterminTemperature(hexCell);

            int terrainTYpe = 2;
            int plantTYpe = 0;
            if (!hexCell.IsUnderwater)
            {
                int t = 0;
                for (; t < TemperatureBands.Length; ++t)
                {
                    if (temperature < TemperatureBands[t])
                    {
                        break;
                    }
                }
                int m = 0;
                for (; m < MoistureBands.Length; ++m)
                {
                    if (moisture < MoistureBands[m])
                    {
                        break;
                    }
                }
                Biome cellBiome = Biomes[(t * 4) + m];
                if (cellBiome.terrain == 0)
                {
                    if (hexCell.Elevation >= rockDesertElevation)
                    {
                        cellBiome.terrain = 3;
                    }
                }
                else if (hexCell.Elevation == ElevationMaximum)
                {
                    cellBiome.terrain = 4;
                }

                if (cellBiome.terrain == 4)
                {
                    cellBiome.plant = 0;
                }
                else if (cellBiome.plant < 3 && hexCell.HasRiver)
                {
                    cellBiome.plant++;
                }

                terrainTYpe = cellBiome.terrain;
                plantTYpe = cellBiome.plant;
            }
            else
            {
                if (hexCell.Elevation == WaterLevel - 1)
                {
                    int cliffs = 0;
                    int slopes = 0;

                    for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
                    {
                        HexCell neighbour = hexCell.GetNeighbour(dir);
                        if (neighbour == null)
                        {
                            continue;
                        }
                        int delta = neighbour.Elevation - hexCell.WaterLevel;
                        if (delta == 0)
                        {
                            slopes++;
                        }
                        else if (delta > 0)
                        {
                            cliffs++;
                        }
                    }
                    if (cliffs + slopes > 3)
                    {
                        terrainTYpe = 1;
                    }
                    else if (cliffs > 0)
                    {
                        terrainTYpe = 3;
                    }
                    else if (slopes > 0)
                    {
                        terrainTYpe = 0;
                    }
                    else
                    {
                        terrainTYpe = 1;
                    }
                }
                else if (hexCell.Elevation >= WaterLevel)
                {
                    terrainTYpe = 1;
                }
                else if (hexCell.Elevation < 0)
                {
                    terrainTYpe = 3;
                }
                else
                {
                    terrainTYpe = 2;
                }

                if (terrainTYpe == 1 && temperature < TemperatureBands[0])
                {
                    terrainTYpe = 2;
                }

            }
            hexCell.SetMapData(temperature);

            hexCell.TerrainTypeIndex = terrainTYpe;
            hexCell.PlantDensityLevel = plantTYpe;
        }
    }

    public int RaiseTerrain(int chunkSize, int budget, MapRegion mapRegion)
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

        int targetErodibleCount = (int)(erodibleCells.Count * (100 - ErosionPercentage) * 0.01f);

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

    private void CreateClimate()
    {
        m_climateData.Clear();
        m_nextClimateData.Clear();

        ClimateData initialClimateData = new ClimateData();
        ClimateData clearClimateData = new ClimateData();
        initialClimateData.moisture = StartingMoisture;
        foreach (HexCell hexCell in HexGrid.GetHexCells())
        {
            m_climateData.Add(initialClimateData);
            m_nextClimateData.Add(clearClimateData);
        }

        for (int i = 0; i < 40; ++i)
        {
            foreach (HexCell hexCell in HexGrid.GetHexCells())
            {
                EvolveClimate(hexCell);
            }
            List<ClimateData> swap = m_climateData;
            m_climateData = m_nextClimateData;
            m_nextClimateData = swap;

        }

    }

    public void EvolveClimate(HexCell hexCell)
    {
        ClimateData climateData = m_climateData[hexCell.CellIndex];
        if (hexCell.IsUnderwater)
        {
            climateData.moisture = 1f;
            climateData.clouds += Evaporation;
        }
        else
        {
            float evaporation = climateData.moisture * EvaporationFactor;
            climateData.moisture -= evaporation;
            climateData.clouds += evaporation;
        }

        float precipitation = climateData.clouds * PrecipitationFactor;
        climateData.clouds -= precipitation;
        climateData.moisture += precipitation;

        float cloudMaximum = 1f - hexCell.ViewElevation / (ElevationMaximum + 1f);

        if (climateData.clouds > cloudMaximum)
        {
            climateData.moisture += climateData.clouds - cloudMaximum;
            climateData.clouds = cloudMaximum;
        }

        float perCellFactor = (1f / 6f);

        HexDirection mainDispersalDirection = WindDirection.Opposite();

        float cloudDispersion = climateData.clouds * (1f / (5f + WindStrength));
        float runoff = climateData.moisture * RunoffFactor * perCellFactor;
        float seepage = climateData.moisture * SeepageFactor * perCellFactor;

        for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
        {
            HexCell neighbor = hexCell.GetNeighbour(dir);
            if (neighbor == null)
            {
                continue;
            }

            ClimateData neighbourClimateData = m_nextClimateData[neighbor.CellIndex];
            if (dir == mainDispersalDirection)
            {
                neighbourClimateData.clouds += cloudDispersion * WindStrength;
            }
            else
            {
                neighbourClimateData.clouds += cloudDispersion;
            }

            int elevationDelta = neighbor.ViewElevation - hexCell.ViewElevation;
            if (elevationDelta < 0)
            {
                climateData.moisture -= runoff;
                neighbourClimateData.moisture += runoff;
            }
            else if (elevationDelta == 0)
            {
                climateData.moisture -= seepage;
                neighbourClimateData.moisture += seepage;

            }

            m_nextClimateData[neighbor.CellIndex] = neighbourClimateData;
        }

        ClimateData nextCellClimateData = m_nextClimateData[hexCell.CellIndex];
        nextCellClimateData.moisture += climateData.moisture;
        nextCellClimateData.moisture = Mathf.Min(1f, nextCellClimateData.moisture);

        m_nextClimateData[hexCell.CellIndex] = nextCellClimateData;
        m_climateData[hexCell.CellIndex] = new ClimateData();

    }

    List<HexDirection> m_flowDirections = new List<HexDirection>();

    public void CreateRivers()
    {
        List<HexCell> riverOrigins = ListPool<HexCell>.Get();
        foreach (HexCell hexCell in HexGrid.GetHexCells())
        {
            if (hexCell.IsUnderwater)
            {
                continue;
            }

            ClimateData climateData = m_climateData[hexCell.CellIndex];
            float data = climateData.moisture * (float)(hexCell.Elevation - WaterLevel) / (ElevationMaximum - WaterLevel);
            if (data > 0.75f)
            {
                riverOrigins.Add(hexCell);
                riverOrigins.Add(hexCell);
            }
            if (data > 0.5f)
            {
                riverOrigins.Add(hexCell);
            }
            if (data > 0.25f)
            {
                riverOrigins.Add(hexCell);
            }
        }

        int riverBudget = Mathf.RoundToInt(m_numLandCells * RiverPercentage * 0.01f);
        while (riverBudget > 0 && riverOrigins.Count > 0)
        {
            int index = Random.Range(0, riverOrigins.Count);
            HexCell origin = riverOrigins[index];
            riverOrigins.RemoveAt(index);
            if (!origin.HasRiver)
            {
                bool isValidOrigin = true;
                for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
                {
                    HexCell neighbour = origin.GetNeighbour(dir);
                    if (neighbour != null && (neighbour.HasRiver || neighbour.IsUnderwater))
                    {
                        isValidOrigin = false;
                        break;
                    }
                }

                if (isValidOrigin)
                {
                    riverBudget -= CreateRiver(origin);
                }
            }
        }

        if (riverBudget > 0)
        {
            Debug.LogWarning("Failed to use up river budget");
        }


        ListPool<HexCell>.Return(riverOrigins);


    }

    public int CreateRiver(HexCell riverOrigin)
    {
        int length = 0;
        HexCell hexCell = riverOrigin;
        HexDirection direction = HexDirection.NE;
        int minNeighbourElevation = int.MaxValue;

        while (!hexCell.IsUnderwater)
        {
            m_flowDirections.Clear();

            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
            {
                HexCell neighbour = hexCell.GetNeighbour(dir);
                if (neighbour == null)
                {
                    continue;
                }
                if (neighbour.Elevation < minNeighbourElevation)
                {
                    minNeighbourElevation = neighbour.Elevation;
                }

                if (neighbour == riverOrigin || neighbour.HasIncomingRiver)
                {
                    continue;
                }

                int delta = neighbour.Elevation - hexCell.Elevation;
                if (delta > 0)
                {
                    continue;
                }

                if (neighbour.HasOutgoingRiver)
                {
                    hexCell.SetOutgoingRiver(dir);
                    return length;
                }

                if (length == 1 || (dir != direction.Next2() && dir != direction.Previous2()))
                {
                    m_flowDirections.Add(dir);
                }


                // weight dowhills
                if (delta < 0)
                {
                    m_flowDirections.Add(dir);
                    m_flowDirections.Add(dir);
                    m_flowDirections.Add(dir);
                }

                 m_flowDirections.Add(dir);
            }

            if (m_flowDirections.Count == 0)
            {
                if (length == 1)
                {
                    return 0;
                }

                if (minNeighbourElevation >= hexCell.Elevation)
                {
                    hexCell.WaterLevel = minNeighbourElevation;
                    if (minNeighbourElevation == hexCell.Elevation)
                    {
                        hexCell.Elevation = minNeighbourElevation - 1;
                    }
                }
                break;
            }


            direction = m_flowDirections[Random.Range(0, m_flowDirections.Count)];
            hexCell.SetOutgoingRiver(direction);
            length += 1;

            if (minNeighbourElevation >= hexCell.Elevation && Random.value < ExtraLakeProability)
            {
                hexCell.WaterLevel = hexCell.Elevation;
                hexCell.Elevation--;
            }

            hexCell = hexCell.GetNeighbour(direction);
        }

        return length;
    }

    public float DeterminTemperature(HexCell hexCell)
    {
        float latitude = (float)hexCell.Coordinates.Z / HexGrid.CellCountZ;
        if (HemisphereMode == HemisphereMode.Both)
        {
            latitude *= 2f;
            if (latitude > 1f)
            {
                latitude = 2f - latitude;
            }
        }
        else if (HemisphereMode == HemisphereMode.North)
        {
            latitude = 1f - latitude;
        }
        float temperature = Mathf.LerpUnclamped(LowTemperature, HighTemperature, latitude);

        float jitter = HexMetrics.SampleNoise(hexCell.Position * 0.1f)[m_temperatureJitterChannel];
        temperature *= 1f - (hexCell.ViewElevation - WaterLevel) / (ElevationMaximum - (WaterLevel + 1f));

        temperature += ( jitter * 2f - 1f);

        return temperature;
    }


}

public enum HemisphereMode
{
    Both, North, South
}


public struct MapRegion
{
    public int xMin;
    public int xMax;

    public int zMin;
    public int zMax;
}



public struct ClimateData
{
    public float clouds;
    public float moisture;
}


public struct Biome
{
    public int terrain;
    public int plant;

    public Biome(int _terrain,int _plant)
    {
        terrain = _terrain;
        plant = _plant;
    }
}
