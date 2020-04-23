using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public Color[] Colors;
    public HexGrid HexGrid;

    private int m_activeElevation;
    private int m_activeWaterLevel;
    private int m_activeUrbanDensityLevel;
    private int m_activeFarmDensityLevel;
    private int m_activePlantDensityLevel;

    private Color m_activeColor;

    bool m_applyColor;
    bool m_applyElevation = true;
    bool m_applyWaterLevel = true;
    bool m_applyUrbanDensity = true;
    bool m_applyFarmDensity = true;
    bool m_applyPlantDensity = true;

    int m_brushSize = 0;

    OptionalToggle m_riverMode;
    OptionalToggle m_roadMode;
    OptionalToggle m_waterMode;

    bool m_isDrag;
    HexDirection m_dragDirection;
    HexCell m_previousHexCell;


    private void Awake()
    {
        SelectColor(0);
    }

    public void Update()
    {
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleInput();
        }
        else
        {
            m_previousHexCell = null;
        }
    }

    public void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            HexCell hexCell = HexGrid.GetCell(hit.point);
            if(m_previousHexCell && m_previousHexCell != hexCell)
            {
                ValidateDrag(hexCell);
            }
            else
            {
                m_isDrag = false;
            }


            EditCells(hexCell);
            m_previousHexCell = hexCell;
        }
        else
        {
            m_previousHexCell = null;
        }
    }

    private void ValidateDrag(HexCell hexCell)
    {
        for (m_dragDirection = HexDirection.NE; m_dragDirection <= HexDirection.NW ;m_dragDirection++)
        {
            if(m_previousHexCell.GetNeighbour(m_dragDirection) == hexCell)
            {
                m_isDrag = true;
                return;
            }
        }
        m_isDrag = false;
    }


    public void EditCells(HexCell center)
    {
        if (center != null)
        {
            int centerX = center.Coordinates.X;
            int centerZ = center.Coordinates.Z;

            for (int r = 0, z = centerZ - m_brushSize; z <= centerZ; z++, r++)
            {
                for (int x = centerX - r; x <= centerX + m_brushSize; x++)
                {
                    EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }
            for (int r = 0, z = centerZ + m_brushSize; z > centerZ; z--, r++)
            {
                for (int x = centerX - m_brushSize; x <= centerX + r; x++)
                {
                    EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }

        }
    }

    public void EditCell(HexCell cell)
    {
        if (cell != null)
        {
            if (m_applyColor)
            {
                cell.Color = m_activeColor;
            }
            if (m_applyElevation)
            {
                cell.Elevation = m_activeElevation;
            }
            if(m_applyWaterLevel)
            {
                cell.WaterLevel = m_activeWaterLevel;
            }
            if(m_applyUrbanDensity)
            {
                cell.UrbanDensityLevel = m_activeUrbanDensityLevel;
            }
            


            if(m_riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }

            if (m_roadMode == OptionalToggle.No)
            {
                cell.RemoveRoads();
            }


            if (m_isDrag)
            {
                HexCell otherCell = cell.GetNeighbour(m_dragDirection.Opposite());
                if(otherCell)
                {
                    if (m_riverMode == OptionalToggle.Yes)
                    {
                        otherCell.SetOutgoingRiver(m_dragDirection);
                    }
                    else if(m_roadMode == OptionalToggle.Yes)
                    {
                        otherCell.AddRoad(m_dragDirection);
                    }
                }
            }

        }
    }

    public void SelectColor(int index)
    {
        if(index == -1)
        {
            m_applyColor = false;
        }

        if (index >= 0 && index < Colors.Length)
        {
            m_applyColor = true;
            m_activeColor = Colors[index];
        }
    }

    public void SetElevation(float value)
    {
        m_activeElevation = (int)value;
    }

    public void SetApplyElevation(bool toggle)
    {
        m_applyElevation = toggle;
    }

    public void SetWaterLevel(float value)
    {
        m_activeWaterLevel = (int)value;
    }

    public void SetApplyWaterLevel(bool toggle)
    {
        m_applyWaterLevel = toggle;
    }

    public void SetUrbanDensityLevel(float value)
    {
        m_activeUrbanDensityLevel= (int)value;
    }

    public void SetApplyUrbanDensityLevel(bool toggle)
    {
        m_applyUrbanDensity = toggle;
    }

    public void SetFarmDensityLevel(float value)
    {
        m_activeFarmDensityLevel = (int)value;
    }

    public void SetApplyFarmDensityLevel(bool toggle)
    {
        m_applyFarmDensity = toggle;
    }

    public void SetPlantDensityLevel(float value)
    {
        m_activePlantDensityLevel = (int)value;
    }

    public void SetApplyPlantDensityLevel(bool toggle)
    {
        m_applyPlantDensity = toggle;
    }


    public void SetBrushSize(float value)
    {
        m_brushSize = (int)value;
    }

    public void ShowUI(bool value)
    {
        HexGrid.ShowUI(value);
    }


    public void SetRiverMode(int value)
    {
        m_riverMode = (OptionalToggle)value;
    }

    public void SetRoadMode(int value)
    {
        m_roadMode = (OptionalToggle)value;
    }

    public void SetWaterMode(int value)
    {
        m_waterMode = (OptionalToggle)value;
    }

    public void ShowTerrainMesh(bool value)
    {
        HexGridChunk[] gridChunks = GameObject.FindObjectsOfType<HexGridChunk>();
        if(gridChunks != null)
        {
            foreach(HexGridChunk gridChunk in gridChunks)
            {
                gridChunk.Terrain.gameObject.SetActive(value);
            }
        }
    }

    public void ShowRiverMesh(bool value)
    {
        HexGridChunk[] gridChunks = GameObject.FindObjectsOfType<HexGridChunk>();
        if (gridChunks != null)
        {
            foreach (HexGridChunk gridChunk in gridChunks)
            {
                gridChunk.Rivers.gameObject.SetActive(value);
            }
        }
    }

    public void ShowRoadsMesh(bool value)
    {
        HexGridChunk[] gridChunks = GameObject.FindObjectsOfType<HexGridChunk>();
        if (gridChunks != null)
        {
            foreach (HexGridChunk gridChunk in gridChunks)
            {
                gridChunk.Roads.gameObject.SetActive(value);
            }
        }
    }

    public void ShowWaterMesh(bool value)
    {
        HexGridChunk[] gridChunks = GameObject.FindObjectsOfType<HexGridChunk>();
        if (gridChunks != null)
        {
            foreach (HexGridChunk gridChunk in gridChunks)
            {
                gridChunk.Water.gameObject.SetActive(value);
            }
        }
    }

    public void ShowWaterShore(bool value)
    {
        HexGridChunk[] gridChunks = GameObject.FindObjectsOfType<HexGridChunk>();
        if (gridChunks != null)
        {
            foreach (HexGridChunk gridChunk in gridChunks)
            {
                gridChunk.WaterShore.gameObject.SetActive(value);
            }
        }
    }

    public void ShowWaterEstuaries(bool value)
    {
        HexGridChunk[] gridChunks = GameObject.FindObjectsOfType<HexGridChunk>();
        if (gridChunks != null)
        {
            foreach (HexGridChunk gridChunk in gridChunks)
            {
                gridChunk.Estuaries.gameObject.SetActive(value);
            }
        }
    }


}
