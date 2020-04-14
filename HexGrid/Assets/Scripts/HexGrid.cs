using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{

    public const int ChunkCountX = 4;
    public const int ChunkCountZ = 3;


    public Color DefaultColor;
    public Color TouchedColor;

    private int m_cellCountX = 6;
    private int m_cellCountZ = 6;


    public HexCell HexCellPrefab;
    public HexGridChunk HexGridChunkPrefab;

    public Text HexLabelPrefab;

    public Texture2D NoiseSource;

    HexCell[] m_cells;
    HexGridChunk[] m_gridChunks;

    void Awake()
    {
        HexMetrics.NoiseSource = NoiseSource;
        m_cellCountX = ChunkCountX * HexMetrics.ChunkSizeX;
        m_cellCountZ = ChunkCountZ * HexMetrics.ChunkSizeZ;

        CreateChunks();
        CreateCells();


    }

    private void CreateChunks()
    {
        m_gridChunks = new HexGridChunk[ChunkCountX * ChunkCountZ];
        int count = 0;
        for(int z = 0;z<ChunkCountZ;++z)
        {
            for(int x=0;x<ChunkCountX;++x)
            {
                HexGridChunk chunk = Instantiate(HexGridChunkPrefab);
                chunk.transform.SetParent(transform);
                m_gridChunks[count++] = chunk;
            }
        }
    }

    private void CreateCells()
    {
        m_cells = new HexCell[m_cellCountZ * m_cellCountX];
        for (int z = 0, i = 0; z < m_cellCountZ; ++z)
        {
            for (int x = 0; x < m_cellCountX; ++x)
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
        int cellIndex = coordinates.X + (coordinates.Z * m_cellCountX) + (coordinates.Z / 2);
        if (cellIndex >= 0 && cellIndex < m_cells.Length)
        {
            return m_cells[cellIndex];
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
        hexCell.Color = DefaultColor;


        Text label = Instantiate(HexLabelPrefab);
        //label.rectTransform.SetParent(GridCanvas.transform,false);
        label.rectTransform.anchoredPosition = new Vector3(position.x, position.z);
        label.text = hexCell.Coordinates.ToStringSeparateLines();

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
                hexCell.SetNeighbour(HexDirection.SE, m_cells[index - m_cellCountX]);

                if(x > 0)
                {
                    hexCell.SetNeighbour(HexDirection.SW, m_cells[index - m_cellCountX-1]);
                }

            }
            else
            {
                hexCell.SetNeighbour(HexDirection.SW, m_cells[index - m_cellCountX]);

                if (x < m_cellCountX-1)
                {
                    hexCell.SetNeighbour(HexDirection.SE, m_cells[index - m_cellCountX + 1]);
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


        int index = (chunkZ * ChunkCountX) + chunkX;
        if(index >= m_gridChunks.Length)
        {
            int ibreak = 0;
        }

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

}
