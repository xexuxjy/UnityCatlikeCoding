using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class NewMapMenu : MonoBehaviour
{
    public HexGrid HexGrid;
    public HexMapGenerator HexMapGenerator;


    public void Open()
    {
        HexMapCamera.Locked = true;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        HexMapCamera.Locked = false;
        gameObject.SetActive(false);
    }

    public void Small()
    {
        CreateMap(20, 15);
    }

    public void Medium()
    {
        CreateMap(40, 30);
    }

    public void Large()
    {
        CreateMap(80, 60);
    }

    private bool m_generateMaps;
    public void GenerateMap(bool status)
    {
        m_generateMaps = status;
    }

    public void CreateMap(int x,int z)
    {
        if(m_generateMaps)
        {
            HexMapGenerator.GenerateMap(x, z);
        }
        else
        {
            HexGrid.CreateMap(x, z);
        }
        
        HexMapCamera.ValidatePosition();
        Close();
    }

    public void Cancel()
    {
        Close();
    }

}
