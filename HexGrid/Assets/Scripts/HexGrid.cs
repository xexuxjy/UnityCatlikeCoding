using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{




    public int Seed;

    //public Color DefaultColor;
    //public Color TouchedColor;

    public int CellCountX = 20;
    public int CellCountZ = 15;

    private int m_chunkCountX = 4;
    private int m_chunkCountZ = 3;



    public HexCell HexCellPrefab;
    public HexGridChunk HexGridChunkPrefab;
    public HexUnit HexUnitPrefab;

    public Text HexLabelPrefab;

    public Texture2D NoiseSource;

    HexCell[] m_cells;
    HexGridChunk[] m_gridChunks;

    HexCell m_currentPathFrom;
    HexCell m_currentPathTo;

    HexCellPriorityQueue m_searchQueue = new HexCellPriorityQueue();
    int m_searchFrontierPhase;


    List<HexUnit> m_unitsList = new List<HexUnit>();

    void Awake()
    {
        HexMetrics.NoiseSource = NoiseSource;
        HexMetrics.InitialiseHashGrid(Seed);
        HexUnit.UnitPrefab = HexUnitPrefab;

        CreateMap(CellCountX,CellCountZ);

    }

    public void CreateMap(int x,int z)
    {
        ClearPath();
        ClearUnits();

        if (x <= 0 || x % HexMetrics.ChunkSizeX != 0 ||z <= 0 || z % HexMetrics.ChunkSizeZ != 0)
        {
            Debug.LogError("Unsupported map size.");
            return;
        }

        if (m_gridChunks != null)
        {
            for (int i = 0; i < m_gridChunks.Length; i++)
            {
                Destroy(m_gridChunks[i].gameObject);
            }
        }

        CellCountX = x;
        CellCountZ = z;

        m_chunkCountX = CellCountX / HexMetrics.ChunkSizeX;
        m_chunkCountZ = CellCountZ / HexMetrics.ChunkSizeZ;

        ClearUnits();

        CreateChunks();
        CreateCells();
    }

    public int NumCellsX
    {
        get { return CellCountX; }
    }

    public int NumCellsZ
    {
        get { return CellCountZ; }
    }

    private void CreateChunks()
    {
        m_gridChunks = new HexGridChunk[m_chunkCountX * m_chunkCountZ];
        int count = 0;
        for(int z = 0;z< m_chunkCountZ; ++z)
        {
            for(int x=0;x< m_chunkCountX; ++x)
            {
                HexGridChunk chunk = Instantiate(HexGridChunkPrefab);
                chunk.transform.SetParent(transform);
                m_gridChunks[count++] = chunk;
            }
        }
    }

    private void CreateCells()
    {
        m_cells = new HexCell[CellCountZ * CellCountX];
        for (int z = 0, i = 0; z < CellCountZ; ++z)
        {
            for (int x = 0; x < CellCountX; ++x)
            {
                m_cells[i] = CreateCell(x, z, i++);
            }
        }

    }



    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        HexCell cell = GetCell(coordinates);
        return cell;
    }

    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return GetCell(hit.point);
        }
        return null;
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int cellIndex = coordinates.X + (coordinates.Z * CellCountX) + (coordinates.Z / 2);
        if (cellIndex >= 0 && cellIndex < m_cells.Length)
        {
            return m_cells[cellIndex];
        }
        return null;
    }

    private Random m_random = new Random();
    public HexCell GetRandomCell()
    {
        int index = Random.Range(0, m_cells.Length);
        if(index >= 0 && index < m_cells.Length)
        {
            return m_cells[index];
        }
        return null;
    }


    public HexCell CreateCell(int x,int z,int index)
    {
        Vector3 position = Vector3.zero;
        position.x = (x - (z/2) + (z * 0.5f )) *(HexMetrics.InnerRadius * 2);
        position.y = 0f;
        position.z = z * (HexMetrics.OuterRadius * 1.5f);


        HexCell hexCell = Instantiate(HexCellPrefab);
        hexCell.transform.localPosition = position;
        hexCell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        Text label = Instantiate(HexLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector3(position.x, position.z);

        hexCell.UIRectTransform = label.rectTransform;

        AddCellToChunk(x, z, hexCell);


        if(x > 0)
        {
            hexCell.SetNeighbour(HexDirection.W, m_cells[index - 1]);
        }

        if(z > 0)
        {
            if((z & 1) == 0)
            {
                hexCell.SetNeighbour(HexDirection.SE, m_cells[index - CellCountX]);

                if(x > 0)
                {
                    hexCell.SetNeighbour(HexDirection.SW, m_cells[index - CellCountX-1]);
                }

            }
            else
            {
                hexCell.SetNeighbour(HexDirection.SW, m_cells[index - CellCountX]);

                if (x < CellCountX-1)
                {
                    hexCell.SetNeighbour(HexDirection.SE, m_cells[index - CellCountX + 1]);
                }

            }
        }


        hexCell.Elevation = 0;

        return hexCell;
    }


    private void AddCellToChunk(int x, int z, HexCell hexCell)
    {
        int chunkX = x / HexMetrics.ChunkSizeX;
        int chunkZ = z / HexMetrics.ChunkSizeZ;

        int index = (chunkZ * m_chunkCountX) + chunkX;

        HexGridChunk chunk = m_gridChunks[index];

        int localX = x - chunkX * HexMetrics.ChunkSizeX;
        int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;

        chunk.AddCell((localZ * HexMetrics.ChunkSizeX) + localX, hexCell);

    }

    public void ShowUI(bool visible)
    {
        foreach(HexGridChunk chunk in m_gridChunks)
        {
            chunk.ShowUI(visible);
        }
    }

    public void FindPathTo(HexCell fromCell,HexCell toCell,int speed, List<HexCell> results)
    {
        //StopAllCoroutines();
        //StartCoroutine(Search(fromCell,toCell,speed));
        Search(fromCell, toCell, speed,results);
    }


    void Search(HexCell fromCell,HexCell toCell,int speed,List<HexCell> results)
    {
        m_currentPathFrom = fromCell;
        m_currentPathTo = toCell;

        m_searchFrontierPhase = 2;
        ClearPath();

        for (int i = 0; i < m_cells.Length; i++)
        {
            m_cells[i].SetLabel(null);
            m_cells[i].DisableHighlight();
            m_cells[i].SearchPhase = 0;
        }

        WaitForSeconds delay = new WaitForSeconds(1 / 60f);

        fromCell.Distance = 0;
        fromCell.SearchPhase = m_searchFrontierPhase;
        m_searchQueue.Enqueue(fromCell);
        while (m_searchQueue.Count > 0)
        {
            //yield return delay;s
            HexCell current = m_searchQueue.Dequeue();
            current.SearchPhase += 1;

            if(current == toCell)
            {
                BuildFinalPath(fromCell, toCell);
                if(results != null && m_finalPath.Count > 0)
                {
                    results.AddRange(m_finalPath);
                }
                break;
            }

            int currentTurn = current.Distance / speed;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbour = current.GetNeighbour(d);

                if (neighbour == null || neighbour.SearchPhase > m_searchFrontierPhase)
                {
                    continue;
                }

                HexEdgeType edgeType = current.GetEdgeType(neighbour);
                bool roadThroughEdge = current.HasRoadThroughEdge(d);



                if (neighbour.IsUnderwater)
                {
                    continue;
                }

                if (edgeType == HexEdgeType.Cliff)
                {
                    continue;
                }

                if (!roadThroughEdge && current.Walled != neighbour.Walled)
                {
                    continue;
                }


                int distance = current.Distance;
                int moveCost = 1;


                // fast travel via roads.
                if (roadThroughEdge)
                {
                    moveCost = 1;

                }
                else
                {
                    moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
                    moveCost += neighbour.UrbanDensityLevel;
                    moveCost += neighbour.FarmDensityLevel;
                    moveCost += neighbour.PlantDensityLevel;

                }

                int newDistance = distance + moveCost;

                int turn = newDistance / speed;
                if(turn > currentTurn)
                {
                    newDistance = (turn * speed) + moveCost;
                }


                if(neighbour.SearchPhase < m_searchFrontierPhase)
                {
                    neighbour.SearchPhase = m_searchFrontierPhase;
                    neighbour.Distance = newDistance;
                    SetPathCurrentNext(current,neighbour);
                    neighbour.SearchHeuristic = neighbour.Coordinates.DistanceTo(toCell.Coordinates);
                    m_searchQueue.Enqueue(neighbour);
                }
                else if(newDistance < neighbour.Distance)
                {
                    neighbour.Distance = newDistance;
                    SetPathCurrentNext(current, neighbour);
                    m_searchQueue.Change(neighbour);
                }

                
            }


        }


        //for (int i = 0; i < m_cells.Length; i++)
        //{
        //    yield return delay;
        //    m_cells[i].Distance = cell.Coordinates.DistanceTo(m_cells[i].Coordinates);
        //}
    }



    /// <summary>
    /// Generates the path from start to end.
    /// </summary>
    /// <returns>The path from start to end</returns>
    LinkedList<HexCell> m_finalPath= new LinkedList<HexCell>();
    private void BuildFinalPath(HexCell startPoint,HexCell endPoint)
    {
        HexCell curPrev = endPoint;
        m_finalPath.AddFirst(curPrev);
        while (m_finalPathDictionary.ContainsKey(curPrev))
        {
            curPrev = m_finalPathDictionary[curPrev];
            m_finalPath.AddFirst(curPrev);
        }

    }

    Dictionary<HexCell, HexCell> m_finalPathDictionary = new Dictionary<HexCell, HexCell>();
    public void SetPathCurrentNext(HexCell current,HexCell next)
    {
        m_finalPathDictionary[next] = current;
    }


    public void ClearPath()
    {
        m_searchQueue.Clear();
        foreach (HexCell hexCell in m_finalPath)
        { 
            hexCell.SetLabel(null);
            hexCell.DisableHighlight();
        }
        m_finalPath.Clear();
        m_finalPathDictionary.Clear();

    }

    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        m_unitsList.Add(unit);
        unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    public void RemoveUnit(HexUnit unit)
    {
        m_unitsList.Remove(unit);
        unit.Die();
    }


    void ClearUnits()
    {
        foreach(HexUnit unit in m_unitsList)
        {
            RemoveUnit(unit);
        }
        m_unitsList.Clear();
    }

    public void Save(BinaryWriter writer)
    {
        for (int i = 0; i < m_cells.Length; i++)
        {
            m_cells[i].Save(writer);
        }

        writer.Write(m_unitsList.Count);
        foreach (HexUnit unit in m_unitsList)
        {
            unit.Save(writer);
        }

    }

    public void Load(BinaryReader reader)
    {
        ClearPath();
        ClearUnits();

        for (int i = 0; i < m_cells.Length; i++)
        {
            m_cells[i].Load(reader);
        }

        for (int i = 0; i < m_gridChunks.Length; i++)
        {
            m_gridChunks[i].Refresh();
        }   


        int unitCount = reader.ReadInt32();
        for (int i = 0; i < unitCount; i++)
        {
            HexUnit.Load(reader, this);
        }


    }

}
