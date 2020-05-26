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

            Shader.SetGlobalTexture("_HexCellData", m_cellTexture);

        }

        Shader.SetGlobalVector("_HexCellData_TexelSize", new Vector4(1f / x, 1f / z, x, z));
        enabled = true;
    }

    public void RefreshTerrain(HexCell hexCell)
    {
        Color32 data = m_cellTextureData[hexCell.CellIndex];
        data.a = (byte)hexCell.TerrainTypeIndex;
        m_cellTextureData[hexCell.CellIndex] = data;
        enabled = true;
    }

    public void RefreshVisibility(HexCell hexCell)
    {
        Color32 data = m_cellTextureData[hexCell.CellIndex];
        data.r = (byte)(hexCell.IsVisible ? 255 : 0);
        m_cellTextureData[hexCell.CellIndex] = data;
        enabled = true;
    }


    public void LateUpdate()
    {
        m_cellTexture.SetPixels32(m_cellTextureData);
        m_cellTexture.Apply();
        enabled = false;
    }

}
