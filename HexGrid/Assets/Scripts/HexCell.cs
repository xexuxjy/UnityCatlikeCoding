using C5;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class HexCell : MonoBehaviour , IComparable<HexCell>
{
    public HexCoordinates Coordinates;
    public RectTransform UIRectTransform;
    public HexGridChunk GridChunk;


    private bool m_hasIncomingRiver;
    private bool m_hasOutgoingRiver;

    private HexDirection m_incomingRiverDirection;
    private HexDirection m_outgoingRiverDirection;


    [SerializeField]
    bool[] m_roads = new bool[6] ;

    [SerializeField]
    HexCell[] m_neighbours = new HexCell[6];


    private bool m_explored;
    public bool IsExplored
    {
        get { return m_explored && IsExplorable; }
        private set { m_explored = value; }
    }

    public bool IsExplorable
    { get; set; }

    public bool HasIncomingRiver
    {
        get { return m_hasIncomingRiver; }
    }

    public bool HasOutgoingRiver
    {
        get { return m_hasOutgoingRiver; }
    }

    public HexDirection IncomingRiverDirection
    {
        get { return m_incomingRiverDirection; }
    }

    public HexDirection OutgoingRiverDirection
    {
        get { return m_outgoingRiverDirection; }
    }


    public bool HasRiver
    {
        get { return HasIncomingRiver || HasOutgoingRiver; }
    }

    public bool HasRiverBeginningOrEnd
    {
        get { return HasIncomingRiver != HasOutgoingRiver; }
    }


    public int ViewElevation
    {
        get { return m_elevation >= m_waterLevel ? m_elevation : m_waterLevel; }
    }

    public bool HasRiverThroughEdge(HexDirection dir)
    {
        return (HasIncomingRiver && IncomingRiverDirection == dir) || (HasOutgoingRiver && OutgoingRiverDirection == dir);
    }

    public void RemoveOutgoingRiver()
    {
        if(HasOutgoingRiver)
        {
            m_hasOutgoingRiver = false;
            RefreshSelfOnly();

            HexCell neighbour = GetNeighbour(OutgoingRiverDirection);
            if(neighbour)
            {
                neighbour.m_hasIncomingRiver = false;
                neighbour.RefreshSelfOnly();
            }   
        }
    }

    public void RemoveIncomingRiver()
    {
        if (HasIncomingRiver)
        {
            m_hasIncomingRiver = false;
            RefreshSelfOnly();

            HexCell neighbour = GetNeighbour(IncomingRiverDirection);
            if (neighbour)
            {
                neighbour.m_hasOutgoingRiver = false;
                neighbour.RefreshSelfOnly();
            }
        }
    }


    public void RemoveRiver()
    {
        RemoveIncomingRiver();
        RemoveOutgoingRiver();
    }


    public void SetOutgoingRiver(HexDirection dir)
    {
        if(HasOutgoingRiver && OutgoingRiverDirection == dir)
        {
            return;
        }
        HexCell neighbour = GetNeighbour(dir);
        if(!IsValidRiverDestination(neighbour))
        {
            return;
        }

        RemoveOutgoingRiver();
        if(HasIncomingRiver && IncomingRiverDirection == dir)
        {
            // replace incoming with outgoing.
            RemoveIncomingRiver();
        }

        m_hasOutgoingRiver = true;
        m_outgoingRiverDirection = dir;
        m_specialFeatureIndex = 0;


        neighbour.RemoveIncomingRiver();
        neighbour.m_hasIncomingRiver = true;
        neighbour.m_incomingRiverDirection = dir.Opposite();
        neighbour.m_specialFeatureIndex = 0;

        SetRoad((int)dir, false);

    }


    private void RefreshSelfOnly()
    {
        GridChunk.Refresh();
        if(HexUnit != null)
        {
            HexUnit.ValidateLocation();
        }
    }


    private int m_terrainTypeIndex;
    public int TerrainTypeIndex
    {
        get { return m_terrainTypeIndex; }
        set
        {
            if (m_terrainTypeIndex == value)
            {
                return;
            }
            m_terrainTypeIndex = value;
            //Refresh();
            HexCellDataShader.RefreshTerrain(this);
        }
    }


    private int m_waterLevel;

    public int WaterLevel
    {
        get { return m_waterLevel; }
        set
        {
            if (m_waterLevel != value)
            {
                int originalViewElevation = ViewElevation;
                m_waterLevel = value;

                if (originalViewElevation != ViewElevation)
                {
                    HexCellDataShader.ViewElevationChanged();
                }


                ValidateRivers();
                Refresh();
            }
        }
    }

    public bool IsUnderwater
    {
        get { return WaterLevel > Elevation; }
    }


    private int m_elevation= int.MinValue;
    public int Elevation
    {
        get { return m_elevation; }
        set
        {
            if(m_elevation == value)
            {
                return;
            }

            int originalViewElevation = ViewElevation;

            m_elevation = value;

            if(originalViewElevation != ViewElevation)
            {
                HexCellDataShader.ViewElevationChanged();
            }

            RefreshPosition();

            ValidateRivers();

            foreach (HexDirection dir in Enum.GetValues(typeof(HexDirection)))
            {
                if ((HasRoadThroughEdge(dir)) && GetEleveationDifference(dir) > HexMetrics.MaxRoadElevationDifference)
                {
                    SetRoad((int)dir, false);
                }

            }

        }
    }

    public void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = m_elevation * HexMetrics.ElevationStep;
        position.y += (HexMetrics.SampleNoise(position).y * 2 - 1) * HexMetrics.ElevationPerturbationStrength;
        transform.localPosition = position;

        Vector3 uiPosition = UIRectTransform.localPosition;
        uiPosition.z = -position.y;
        UIRectTransform.localPosition = uiPosition;




        Refresh();

    }


    public void Refresh()
    {
        if (GridChunk != null)
        {
            GridChunk.Refresh();
            if (HexUnit != null)
            {
                HexUnit.ValidateLocation();
            }

        }
    }




    public HexCell GetNeighbour(HexDirection dir)
    {
        return m_neighbours[(int)dir];
    }

    public void SetNeighbour(HexDirection dir, HexCell neighbour)
    {
        m_neighbours[(int)dir] = neighbour;
        neighbour.m_neighbours[(int)dir.Opposite()] = this;

    }

    public HexEdgeType GetEdgeType(HexDirection dir)
    {
        return HexMetrics.GetEdgeType(Elevation, m_neighbours[(int)dir].Elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(Elevation, otherCell.Elevation);
    }


    public Vector3 Position
    {
        get {return transform.localPosition; }
    }

    public float StreamBedY
    {
        get
        {
            return (Elevation + HexMetrics.StreamBedElevationOffset) *HexMetrics.ElevationStep;
        }
    }

    public float RiverSurfaceY
    {
        get
        {
            return (Elevation + HexMetrics.WaterElevationOffset) *HexMetrics.ElevationStep;
        }
    }

    public float WaterSurfaceY
    {
        get
        {
            return (WaterLevel + HexMetrics.WaterElevationOffset) *HexMetrics.ElevationStep;
        }
    }


    public bool HasRoadThroughEdge(HexDirection dir)
    {
        return m_roads[(int)dir];
    }

    public bool HasRoads
    {
        get
        {
            for (int i = 0; i < m_roads.Length; ++i)
            {
                if (m_roads[i])
                {
                    return true;
                }
            }
            return false;
        }
    }

    public void RemoveRoads()
    {
        for (int i = 0; i < m_roads.Length; ++i)
        {
            if (m_roads[i])
            {
                m_roads[i] = false;
                HexDirection opposite = ((HexDirection)i).Opposite();
                m_neighbours[i].m_roads[(int)opposite] = false;
                m_neighbours[i].RefreshSelfOnly();
                RefreshSelfOnly();
            }
        }

    }


    public void AddRoad(HexDirection dir)
    {
        if(HasRoadThroughEdge(dir))
        {
            return;
        }

        if(HasRiverThroughEdge(dir))
        {
            return;
        }
        if(GetEleveationDifference(dir) > HexMetrics.MaxRoadElevationDifference)
        {
            return;
        }

        if(HasSpecialFeature || GetNeighbour(dir).HasSpecialFeature)
        {
            return;
        }

        if(!HasRoadThroughEdge(dir) && !HasRiverThroughEdge(dir) && GetEleveationDifference(dir) <= HexMetrics.MaxRoadElevationDifference)
        {
            SetRoad((int)dir, true);
        }
    }

    private void SetRoad(int index,bool value)
    {
        m_roads[index] = value;
        HexDirection opposite = ((HexDirection)index).Opposite();
        //m_neighbours[index].SetRoad((int)opposite, value);
        m_neighbours[index].m_roads[(int)opposite] =  value;
        m_neighbours[index].RefreshSelfOnly();
        RefreshSelfOnly();

    }

    public int GetEleveationDifference(HexDirection dir)
    {
        return Math.Abs(Elevation - m_neighbours[(int)dir].Elevation);
    }


    public HexDirection RiverBeginOrEndDirection
    {
        get
        {
            return HasIncomingRiver ? IncomingRiverDirection : OutgoingRiverDirection;
        }
    }

    bool IsValidRiverDestination(HexCell neighbour)
    {
        return neighbour && (Elevation >= neighbour.Elevation || WaterLevel == neighbour.Elevation);
    }

    private void ValidateRivers()
    {
        if(HasOutgoingRiver && !IsValidRiverDestination(GetNeighbour(OutgoingRiverDirection)))
        {
            RemoveOutgoingRiver();
        }
        if(HasIncomingRiver && !GetNeighbour(IncomingRiverDirection).IsValidRiverDestination(this))
        {
            RemoveIncomingRiver();
        }

    }


    private int m_urbanDensityLevel;
    public int UrbanDensityLevel
    {
        get { return m_urbanDensityLevel; }
        set
        {
            if (m_urbanDensityLevel != value)
            {
                m_urbanDensityLevel = value;
                RefreshSelfOnly();
            }

        }
    }


    private int m_farmDensityLevel;
    public int FarmDensityLevel
    {
        get { return m_farmDensityLevel; }
        set
        {
            if (m_farmDensityLevel != value)
            {
                m_farmDensityLevel = value;
                RefreshSelfOnly();
            }

        }
    }

    private int m_plantDensityLevel;
    public int PlantDensityLevel
    {
        get { return m_plantDensityLevel; }
        set
        {
            if (m_plantDensityLevel != value)
            {
                m_plantDensityLevel = value;
                RefreshSelfOnly();
            }

        }
    }

    private bool m_walled;
    public bool Walled
    {
        get { return m_walled; }
        set 
        { 
            if(m_walled != value)
            {
                m_walled = value;
                Refresh();
            }
        }
    }

    private int m_specialFeatureIndex = 0;
    public int SpecialFeatureIndex
    {
        get
        {
            return m_specialFeatureIndex;
        }
        set
        {
            if (m_specialFeatureIndex != value && !HasRiver)
            {
                m_specialFeatureIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }


    public bool HasSpecialFeature
    {
        get { return m_specialFeatureIndex > 0; }
    }


    private int m_distance = int.MaxValue;
    public int Distance
    {
        get
        {
            return m_distance;
        }
        set
        {
            m_distance = value;
            //UpdateDistanceLabel();
        }
    }

    public void SetLabel(String text)
    {
        Text label = UIRectTransform.GetComponent<Text>();
        label.text = text;

    }


    void UpdateDistanceLabel()
    {
        Text label = UIRectTransform.GetComponent<Text>();
        label.text = m_distance == int.MaxValue ? "" : m_distance.ToString();
    }


    public void Load(BinaryReader binReader)
    {
        m_terrainTypeIndex = binReader.ReadByte();
        m_elevation = binReader.ReadByte();
        IsExplored = binReader.ReadBoolean();
        m_waterLevel = binReader.ReadByte();
        m_urbanDensityLevel = binReader.ReadByte();
        m_farmDensityLevel = binReader.ReadByte();
        m_plantDensityLevel = binReader.ReadByte();
        m_specialFeatureIndex = binReader.ReadByte();
        m_walled = binReader.ReadBoolean();
        m_hasIncomingRiver = binReader.ReadBoolean();
        m_incomingRiverDirection = (HexDirection)binReader.ReadByte();
        m_hasOutgoingRiver = binReader.ReadBoolean();
        m_outgoingRiverDirection = (HexDirection)binReader.ReadByte();
        for (int i = 0; i < m_roads.Length; ++i)
        {
            m_roads[i] = binReader.ReadBoolean();
        }

        HexCellDataShader.RefreshTerrain(this);
        RefreshPosition();

    }

    public void Save(BinaryWriter binWriter)
    {
        binWriter.Write((byte)m_terrainTypeIndex);
        binWriter.Write((byte)m_elevation);
        binWriter.Write(IsExplored);
        binWriter.Write((byte)m_waterLevel);
        binWriter.Write((byte)m_urbanDensityLevel);
        binWriter.Write((byte)m_farmDensityLevel);
        binWriter.Write((byte)m_plantDensityLevel);
        binWriter.Write((byte)m_specialFeatureIndex);
        binWriter.Write(m_walled);
        binWriter.Write(m_hasIncomingRiver);
        binWriter.Write((byte)m_incomingRiverDirection);
        binWriter.Write(m_hasOutgoingRiver);
        binWriter.Write((byte)m_outgoingRiverDirection);
        for(int i=0;i<m_roads.Length;++i)
        {
            binWriter.Write(m_roads[i]);
        }



    }

    public void DisableHighlight()
    {
        Image highlight = UIRectTransform.GetChild(0).GetComponent<Image>();
        highlight.gameObject.SetActive(false);
    }

    public void EnableHighlight()
    {
        Image highlight = UIRectTransform.GetChild(0).GetComponent<Image>();
        highlight.gameObject.SetActive(true);
    }

    public void EnableHighlight(Color color)
    {
        Image highlight = UIRectTransform.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.gameObject.SetActive(true);
    }

    public int CompareTo(HexCell other)
    {
        if(other != null)
        {
            return SearchPriority.CompareTo(other.SearchPriority);
        }
        return 0;
    }
    
    public int SearchHeuristic { get; set; }

    public int SearchPriority
    {
        get
        {
            return Distance + SearchHeuristic;
        }
    }

    public HexUnit HexUnit
    { get; set; }

    public int SearchPhase { get; set; }

    public override string ToString()
    {
        return Coordinates.ToString();
    }

    public HexCellDataShader HexCellDataShader
    {
        get; set;
    }

    public int CellIndex
    { get; set; }


    private int m_visibilityCount;
    public bool IsVisible
    {
        get { return m_visibilityCount > 0 && IsExplorable; }
    }

    public void IncreaseVisibility()
    {
        m_visibilityCount++;
        if(m_visibilityCount == 1)
        {
            IsExplored = true;
            HexCellDataShader.RefreshVisibility(this);
        }
    }
    public void DecreaseVisibility()
    {
        m_visibilityCount--;
        if (m_visibilityCount == 0)
        {
            HexCellDataShader.RefreshVisibility(this);
        }

    }

    public void ResetVisability()
    {
        if(m_visibilityCount > 0)
        {
            m_visibilityCount = 0;
            HexCellDataShader.RefreshVisibility(this);
        }
    }

}
