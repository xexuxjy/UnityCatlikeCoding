using UnityEngine;

public class SaveLoadItem : MonoBehaviour
{
    public SaveLoadMenu Menu;
    public TMPro.TextMeshProUGUI Label;


    private string m_mapName;
    public string MapName
    {
        get { return m_mapName; }
        set
        {
            m_mapName = value;
            Label.text = m_mapName;
        }
    }

    public void Select()
    {
        Menu.SelectItem(MapName);
    }

}
