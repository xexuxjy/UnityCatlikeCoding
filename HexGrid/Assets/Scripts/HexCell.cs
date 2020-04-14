using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HexCell : MonoBehaviour
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
        if(!neighbour || Elevation < neighbour.Elevation)
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

        neighbour.RemoveIncomingRiver();
        neighbour.m_hasIncomingRiver = true;
        neighbour.m_incomingRiverDirection = dir.Opposite();

        SetRoad((int)dir, false);

    }


    private void RefreshSelfOnly()
    {
        GridChunk.Refresh();
    }




    private Color m_color;
    public Color Color
    {
        get { return m_color; }
        set
        {
            if (m_color == value)
            {
                return;
            }
            m_color = value;
            Refresh();
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
                m_waterLevel = value;
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

            m_elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.ElevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2 - 1) * HexMetrics.ElevationPerturbationStrength;
            transform.localPosition = position;

            Vector3 uiPosition = UIRectTransform.localPosition;
            uiPosition.z = -position.y;
            UIRectTransform.localPosition = uiPosition;


            if (HasOutgoingRiver && Elevation < GetNeighbour(OutgoingRiverDirection).Elevation)
            {
                RemoveOutgoingRiver();
            }

            if (HasIncomingRiver && Elevation > GetNeighbour(IncomingRiverDirection).Elevation)
            {
                RemoveIncomingRiver();
            }

            foreach(HexDirection dir in Enum.GetValues(typeof(HexDirection)))
            {
                if((HasRoadThroughEdge(dir)) && GetEleveationDifference(dir) > HexMetrics.MaxRoadElevationDifference)
                {
                    SetRoad((int)dir, false);
                }

            }



            Refresh();
        }
    }

    public void Refresh()
    {
        if (GridChunk != null)
        {
            GridChunk.Refresh();
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

}
