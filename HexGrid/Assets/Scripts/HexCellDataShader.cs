using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCellDataShader : MonoBehaviour
{
    Texture2D m_cellTexture;
    Color32[] m_cellTextureData;

    public void Intialize(int x,int z)
    {
        if(m_cellTexture == null || m_cellTextureData.Length != x*z)
        {
            m_cellTextureData = new Color32[x * z];
        }
        else
        {
            for(int i=0;i<m_cellTextureData.Length;++i)
            {
                m_cellTextureData[i] = new Color(0, 0, 0, 0);
            }
        }

        if (m_cellTexture != null)
        {
            m_cellTexture.Resize(x, z);
        }
        else
        {
            m_cellTexture = new Texture2D(x, z, TextureFormat.RGBA32, false, true);
            m_cellTexture.filterMode = FilterMode.Point;
            m_cellTexture.wrapMode = TextureWrapMode.Clamp;
        }
        //gameObject.SetActive(true);
        enabled = true;
    }

    public void RefreshTerrain(HexCell hexCell)
    {
        m_cellTextureData[hexCell.CellIndex].a = (byte)hexCell.TerrainTypeIndex;
        enabled = false;

    }

    public void LateUpdate()
    {
        m_cellTexture.SetPixels32(m_cellTextureData);
        m_cellTexture.Apply();
        enabled = false;
    }

}
