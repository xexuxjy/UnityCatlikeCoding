using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HexMetrics 
{

    public const float OuterToInner = 0.866025404f;
    public const float InnerToOuter = 1f / OuterToInner;

    public const float OuterRadius = 10.0f;
    public const float InnerRadius = OuterRadius * OuterToInner;

    public const float SolidFactor = 0.75f;
    public const float BlendFactor = 1.0f - SolidFactor;

    public const float WaterFactor = 0.6f;
    public const float WaterBlendFactor = 1.0f - WaterFactor;


    public const float ElevationStep = 5.0f;


    public const int TerracesPerSlope = 2;
    public const int TerraceSteps = TerracesPerSlope * 2 + 1;

    public const float HorizontalTerraceStepSize = 1f / TerraceSteps;
    public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope +1);

    public static Texture2D NoiseSource;

    public const float CellPerturbationStrength = 0f;//5f;
    public const float ElevationPerturbationStrength = 1.5f;

    public const float NoiseScale = 0.003f;

    public const int ChunkSizeX = 5;
    public const int ChunkSizeZ = 5;

    public const int NumBridgeSubdivides = 4;
    public const float SubdivideFraction = 1.0f / NumBridgeSubdivides;

    public const float StreamBedElevationOffset = -1f;

    public const float WaterElevationOffset = -0.5f;

    public const int MaxRoadElevationDifference = 1;


    public static Vector3[] Corners = new Vector3[]
    {
        new Vector3(0,0,OuterRadius),
        new Vector3(InnerRadius,0,0.5f * OuterRadius),
        new Vector3(InnerRadius,0,-0.5f * OuterRadius),
        new Vector3(0,0,-OuterRadius),
        new Vector3(-InnerRadius,0,-0.5f * OuterRadius),
        new Vector3(-InnerRadius,0,0.5f * OuterRadius),
        new Vector3(0,0,OuterRadius)
    };

    public static Vector3 GetFirstCorner(HexDirection dir)
    {
        return Corners[(int)dir];
    }

    public static Vector3 GetSecondCorner(HexDirection dir)
    {
        return Corners[(int)dir + 1];
    }

    public static Vector3 GetFirstSolidCorner(HexDirection dir)
    {
        return Corners[(int)dir] * SolidFactor;
    }

    public static Vector3 GetSecondSolidCorner(HexDirection dir)
    {
        return Corners[(int)dir + 1] * SolidFactor;
    }


    public static Vector3 GetFirstWaterCorner(HexDirection dir)
    {
        return Corners[(int)dir] * WaterFactor;
    }

    public static Vector3 GetSecondWaterCorner(HexDirection dir)
    {
        return Corners[(int)dir + 1] * WaterFactor;
    }



    public static Vector3 GetBridge(HexDirection dir)
    {
        return (Corners[(int)dir] + Corners[(int)dir + 1]) * BlendFactor;
    }


    public static Vector3 GetWaterBridge(HexDirection dir)
    {
        return (Corners[(int)dir] + Corners[(int)dir + 1]) * WaterBlendFactor;
    }


    public static Vector3 TerraceLerp(Vector3 a, Vector3 b , int step)
    {
        float h = step * HorizontalTerraceStepSize;

        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;

        float v = ((step + 1) / 2) * VerticalTerraceStepSize;

        a.y += (b.y - a.y) * v;

        return a;
    }

    public static Color TerraceLerp(Color a,Color b,int step)
    {
        float h = step * HorizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1,int elevation2)
    {
        if (elevation1 == elevation2)
        {
            return HexEdgeType.Flat;
        }
        if(Mathf.Abs(elevation1-elevation2) == 1)
        {
            return HexEdgeType.Slope;
        }
        return HexEdgeType.Cliff;
    }


    public static Vector4 SampleNoise(Vector3 position)
    {
        return NoiseSource.GetPixelBilinear(position.x * NoiseScale, position.z* NoiseScale);
    }

    public static Vector3 GetSolidEdgeMiddle(HexDirection dir)
    {
        return(Corners[(int)dir] + Corners[(int)dir + 1]) * (0.5f * SolidFactor);
    }

    public static Vector3 PerturbVector(Vector3 v)
    {
        Vector4 sample = SampleNoise(v);
        v.x += (sample.x * 2f - 1) * CellPerturbationStrength;
        v.z += (sample.z * 2f - 1) * CellPerturbationStrength;
        return v;
    }


}

