using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugSetup : MonoBehaviour
{
    public HexMapEditor HexMapEditor;

    public int TerrainIterations = 50;
    public int UrbanFeatureIterations = 50;
    public int NumRivers = 4;
    public int NumRoads = 5;

    public int NumWater = 5;

    public int NumWalls = 40;

    public int NumSpecials = 100;

    public bool Rivers;
    public bool Roads;
    public bool Water;

    bool m_haveBuilt = false;
    private void Update()
    {
        HexMapEditor mapEditor = HexMapEditor;
        if (mapEditor != null && !m_haveBuilt)
        {
            for (int i = 0; i < TerrainIterations; ++i)
            {

                HexCell hexCell = mapEditor.HexGrid.GetRandomCell();

                int brushSize = Random.Range(1, 4);
                int elevation = Random.Range(1, 3);
                int terrainType = Random.Range(0, 5);

                mapEditor.SetBrushSize(brushSize);
                mapEditor.SetElevation(elevation);
                mapEditor.SetTerrainType(terrainType);
                //mapEditor.SE(color);
                mapEditor.EditCells(hexCell);
            }

            if (Rivers)
            {
                BuildRiverOrRoad(mapEditor, NumRivers, 8, 15, false);
            }

            if (Roads)
            {
                BuildRiverOrRoad(mapEditor, NumRoads, 8, 15, true);
            }

            if(Water)
            {
                for (int i = 0; i < NumWater; ++i)
                {
                    HexCell hexCell = mapEditor.HexGrid.GetRandomCell();

                    int brushSize = Random.Range(1, 4);
                    int waterLevel = Random.Range(0, 2);

                    mapEditor.SetBrushSize(brushSize);
                    mapEditor.SetWaterLevel(waterLevel);
                    mapEditor.SetApplyElevation(false);
                    mapEditor.SetApplyWaterLevel(true);
                    mapEditor.EditCells(hexCell);
                }
            }
            mapEditor.SetBrushSize(1);
            mapEditor.SetApplyElevation(false);
            mapEditor.SetApplyWaterLevel(false);
            mapEditor.SetRiverMode((int)OptionalToggle.Ignore);
            mapEditor.SetRoadMode((int)OptionalToggle.Ignore);

            for (int i=0;i< UrbanFeatureIterations;++i)
            {
                HexCell hexCell = mapEditor.HexGrid.GetRandomCell();
                hexCell.UrbanDensityLevel = Random.Range(1, 4);
                hexCell.FarmDensityLevel = Random.Range(1, 4);
                hexCell.PlantDensityLevel = Random.Range(1, 4);
            }

            for(int i=0;i<NumWalls;++i)
            {
                HexCell hexCell = mapEditor.HexGrid.GetRandomCell();
                hexCell.Walled = true;
            }

            for (int i = 0; i < NumSpecials; ++i)
            {
                HexCell hexCell = mapEditor.HexGrid.GetRandomCell();
                hexCell.SpecialFeatureIndex = Random.Range(1, 4);
            }


            m_haveBuilt = true;

        }
    }

    private void BuildRiverOrRoad(HexMapEditor mapEditor, int numIterations, int minLength, int maxLength, bool isRoad)
    {
        for (int i = 0; i < numIterations; ++i)
        {

            int startX = Random.Range(0, mapEditor.HexGrid.NumCellsX);
            int startZ = Random.Range(0, mapEditor.HexGrid.NumCellsZ);

            int length = Random.Range(minLength, maxLength);
            HexCell currentCell = mapEditor.HexGrid.GetCell(new HexCoordinates(startX, startZ));
            if (currentCell != null)
            {
                HexCell lastCell = null;

                for (int r = 2; r < length; ++r)
                {

                    HexDirection dir = (HexDirection)0;
                    HexCell neighbourCell = null;
                    for (int j = 0; j < 5; ++j)
                    {
                        dir = (HexDirection)Random.Range(0, 6);
                        //dir = HexDirection.E;
                        neighbourCell = currentCell.GetNeighbour(dir);
                        if (neighbourCell != null && neighbourCell != lastCell)
                        {
                            break;
                        }
                    }

                    if (neighbourCell != null)
                    {
                        if (isRoad)
                        {
                            currentCell.AddRoad(dir);
                        }
                        else
                        {
                            currentCell.SetOutgoingRiver(dir);
                        }

                        lastCell = currentCell;
                        currentCell = neighbourCell;
                    }
                }
            }
        }

    }
}

