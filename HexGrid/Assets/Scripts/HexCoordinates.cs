﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public struct HexCoordinates
{
    [SerializeField]
    private int m_x, m_z;

    public int X { get { return m_x; } set { m_x = value; } }
    public int Z { get { return m_z; } set { m_z = value; } }
    public int Y
    {
        get { return -X - Z; }
    }


    public HexCoordinates(int x, int z)
    {
        m_x = x;
        m_z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        //return new HexCoordinates(x, z);
        return new HexCoordinates(x - z / 2, z);
    }

    public static HexCoordinates FromPosition(Vector3 position)
    {
        float x = position.x / (HexMetrics.InnerRadius * 2f);
        float y = -x;

        float offset = position.z / (HexMetrics.OuterRadius * 3f);
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        if (iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinates(iX, iZ);
    }


    public override string ToString()
    {
        return String.Format("{0},{1},{2}", X, Y, Z);
    }

    public string ToStringSeparateLines()
    {
        return String.Format("{0}\n{1}\n{2}", X, Y, Z);
    }
}
