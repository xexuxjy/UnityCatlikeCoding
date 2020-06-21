using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCellDataShader : MonoBehaviour
{
    Texture2D m_cellTexture;
    Color32[] m_cellTextureData;

    public bool ImmediateMode
    { get; set; }

    List<HexCell> m_transitioningCells = new List<HexCell>();
    List<HexCell> m_transitioningCellsRemove = new List<HexCell>();

    const float TransitionSpeed = 255;

    public void Intialize(int x, int z)
    {
        m_transitioningCells.Clear();

        if (m_cellTexture == null || m_cellTextureData.Length != x * z)
        {
            m_cellTextureData = new Color32[x * z];
        }
        else
        {
            for (int i = 0; i < m_cellTextureData.Length; ++i)
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
            //m_cellTexture.wrapMode = TextureWrapMode.Clamp;
            m_cellTexture.wrapModeU = TextureWrapMode.Repeat;
            m_cellTexture.wrapModeV = TextureWrapMode.Clamp;

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
        if (ImmediateMode)
        {
            Color32 data = m_cellTextureData[hexCell.CellIndex];
            data.r = (byte)(hexCell.IsVisible ? 255 : 0);
            data.g = (byte)(hexCell.IsExplored ? 255 : 0);

            m_cellTextureData[hexCell.CellIndex] = data;
        }
        else if(m_cellTextureData[hexCell.CellIndex].b != 255)
        {
            m_cellTextureData[hexCell.CellIndex].b = 255;
            m_transitioningCells.Add(hexCell);
        }
        enabled = true;
    }

    public HexGrid HexGrid
    {
        get;set;
    }


    public void LateUpdate()
    {
        if(m_needsVisibilityReset)
        {
            m_needsVisibilityReset = false;
            HexGrid.ResetVisability();
        }

        int delta = (int)(Time.deltaTime * TransitionSpeed);
        if (delta == 0)
        {
            delta = 1;
        }
        m_cellTexture.SetPixels32(m_cellTextureData);
        m_cellTexture.Apply();
        enabled = m_transitioningCells.Count > 0;
        m_transitioningCellsRemove.Clear();

        foreach (HexCell hexCell in m_transitioningCells)
        {
            if (!UpdateCellData(hexCell, delta))
            {
                m_transitioningCellsRemove.Add(hexCell);
            }
        }
        foreach (HexCell removeCell in m_transitioningCellsRemove)
        {
            m_transitioningCells.Remove(removeCell);
        }

    }

    public bool UpdateCellData(HexCell hexCell, int delta)
    {
        int index = hexCell.CellIndex;
        Color32 data = m_cellTextureData[index];
        bool stillUpdating = false;

        if (hexCell.IsExplored && data.g < 255)
        {
            stillUpdating = true;
            int t = data.g + delta;
            data.g = (byte)Mathf.Min(t, 255);
        }

        if (hexCell.IsVisible)
        {
            if (data.r < 255)
            {
                stillUpdating = true;
                int t = data.r + delta;
                data.r = (byte)Mathf.Min(t, 255);
            }
        }
        else if (data.r > 0)
        {
            stillUpdating = true;
            int t = data.r - delta;
            data.r = (byte)Mathf.Max(t, 0);
        }

        if(!stillUpdating)
        {
            data.b = 0;
        }

        m_cellTextureData[index] = data;
        return stillUpdating;
    }

    public void SetMapData(HexCell hexCell, float data)
    {
        m_cellTextureData[hexCell.CellIndex].b = data < 0f ? (byte)0 : (data < 1f ? (byte)(data * 254f) : (byte)254);
        enabled = true;
    }

    private bool m_needsVisibilityReset;
    public void ViewElevationChanged()
    {
        m_needsVisibilityReset = true;
        enabled = true;
    }

}
