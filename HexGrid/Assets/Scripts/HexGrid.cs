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

    public Text HexLabelPrefab;

    public Texture2D NoiseSource;

    HexCell[] m_cells;
    HexGridChunk[] m_gridChunks;

    HexCellPriorityQueue m_searchQueue;

    int m_searchFrontierPhase;


    void Awake()
    {
        HexMetrics.NoiseSource = NoiseSource;
        HexMetrics.InitialiseHashGrid(Seed);

        CreateMap(CellCountX,CellCountZ);

    }

    public void CreateMap(int x,int z)
    {
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

    public  HexCell GetCell(HexCoordinates coordinates)
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
        //hexCell.transform.SetParent(transform);
        hexCell.transform.localPosition = position;
        hexCell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        
        //hexCell.Color = DefaultColor;


        Text label = Instantiate(HexLabelPrefab);
        //label.rectTransform.SetParent(GridCanvas.transform,false);
        label.rectTransform.anchoredPosition = new Vector3(position.x, position.z);
        
        //label.text = hexCell.Coordinates.ToStringSeparateLines();

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

    //public void FindDistancesTo(HexCell cell)
    //{
    //    for (int i = 0; i < m_cells.Length; i++)
    //    {
    //        m_cells[i].Distance = cell.Coordinates.DistanceTo(m_cells[i].Coordinates);
    //    }
    //}

    public void FindPathTo(HexCell fromCell,HexCell toCell,int speed)
    {
        //StopAllCoroutines();
        //StartCoroutine(Search(fromCell,toCell,speed));
        Search(fromCell, toCell, speed);
    }


    void Search(HexCell fromCell,HexCell toCell,int speed)
    {
        m_searchFrontierPhase = 2;
        if (m_searchQueue == null)
        {
            m_searchQueue = new HexCellPriorityQueue();
        }
        else 
        {
            m_searchQueue.Clear();
        }

        for (int i = 0; i < m_cells.Length; i++)
        {
            //m_cells[i].Distance = int.MaxValue;
            m_cells[i].SetLabel(null);
            m_cells[i].DisableHighlight();
            m_cells[i].SearchPhase = 0;
        }

        fromCell.EnableHighlight(Color.blue);
        

        WaitForSeconds delay = new WaitForSeconds(1 / 60f);

        fromCell.Distance = 0;
        m_searchQueue.Enqueue(fromCell);
        while (m_searchQueue.Count > 0)
        {
            //yield return delay;
            HexCell current = m_searchQueue.Dequeue();
            current.SearchPhase += 1;

            if(current == toCell)
            {
                //current = current.PathFrom;
                while (current != fromCell)
                {
                    current.EnableHighlight(Color.white);
                    int turn = current.Distance / speed;
                    current.SetLabel(turn.ToString());
                    current = current.PathFrom;
                }
                toCell.EnableHighlight(Color.red);
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


                //if (neighbour.Distance == int.MaxValue)
                if(neighbour.SearchPhase < m_searchFrontierPhase)
                {
                    neighbour.SearchPhase = m_searchFrontierPhase;

                    neighbour.Distance = newDistance;
                    //neighbour.SetLabel(turn.ToString());
                    neighbour.PathFrom = current;
                    neighbour.SearchHeuristic = neighbour.Coordinates.DistanceTo(toCell.Coordinates);
                    m_searchQueue.Enqueue(neighbour);
                }
                
                if(newDistance < neighbour.Distance)
                {
                    neighbour.Distance = newDistance;
                    //neighbour.SetLabel(turn.ToString());
                    neighbour.PathFrom = current;
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

    public void Save(BinaryWriter writer)
    {
        for (int i = 0; i < m_cells.Length; i++)
        {
            m_cells[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader)
    {
        //StopAllCoroutines();
        for (int i = 0; i < m_cells.Length; i++)
        {
            m_cells[i].Load(reader);
        }

        for (int i = 0; i < m_gridChunks.Length; i++)
        {
            m_gridChunks[i].Refresh();
        }

    }

}
