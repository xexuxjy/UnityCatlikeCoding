using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class NewMapMenu : MonoBehaviour
{
    public HexGrid HexGrid;


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

    public void CreateMap(int x,int z)
    {
        HexMapCamera.ValidatePosition();
        HexGrid.CreateMap(x, z);
        Close();
    }

    public void Cancel()
    {

    }

}
