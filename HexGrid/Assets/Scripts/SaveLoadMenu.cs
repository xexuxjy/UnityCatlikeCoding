using System;
using System.IO;
using UnityEngine;

public class SaveLoadMenu : MonoBehaviour
{
    public HexGrid HexGrid;

    public TMPro.TextMeshProUGUI HeaderLabel;
    public TMPro.TextMeshProUGUI ActionButtonLabel;
    public TMPro.TMP_InputField InputField;


    public RectTransform ListContainer;
    public SaveLoadItem SaveLaodItemPrefab;

    private bool m_saveMode;

    public void Open(bool saveMode)
    {
        m_saveMode = saveMode;

        if (m_saveMode)
        {
            HeaderLabel.text = "Save Map";
            ActionButtonLabel.text = "Save";
        }
        else
        {
            HeaderLabel.text = "Load Map";
            ActionButtonLabel.text = "Load";
        }

        FillList();

        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }


    public string GetSelectedPath()
    {
        string mapName = InputField.text;
        if (mapName.Length == 0)
        {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    void FillList()
    {
        for (int i = 0; i < ListContainer.childCount; i++)
        {
            Destroy(ListContainer.GetChild(i).gameObject);
        }

        string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);
        for (int i = 0; i < paths.Length; i++)
        {
            SaveLoadItem item = Instantiate(SaveLaodItemPrefab);
            item.Menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
            item.transform.SetParent(ListContainer, false);
        }

    }


    public void Load(string path)
    {
        if (path != null)
        {
            if (!File.Exists(path))
            {
                Debug.LogError("File does not exist " + path);
                return;
            }

            using (BinaryReader binReader = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                binReader.ReadInt32();
                int x = binReader.ReadInt32();
                int z = binReader.ReadInt32();
                HexGrid.CreateMap(x, z, HexMetrics.Wrap);
                HexGrid.Load(binReader);
                HexMapCamera.ValidatePosition();
            }
        }
    }

    public void Save(string path)
    {
        if (path != null)
        {
            using (BinaryWriter binWriter = new BinaryWriter(File.OpenWrite(path)))
            {
                binWriter.Write(0);
                binWriter.Write(HexGrid.CellCountX);
                binWriter.Write(HexGrid.CellCountZ);
                HexGrid.Save(binWriter);
            }
        }
    }

    public void Action()
    {
        string path = GetSelectedPath();
        if (path == null)
        {
            return;
        }
        if (m_saveMode)
        {
            Save(path);
        }
        else
        {
            Load(path);
        }
        Close();
    }


    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    public void SelectItem(string itemName)
    {
        InputField.text = itemName;
    }

    public void Delete()
    {
        string path = GetSelectedPath();
        if (path == null)
        {
            return;
        }
        if (File.Exists(path))
        {
            File.Delete(path);
            InputField.text = "";
            FillList();
        }
    }

}
