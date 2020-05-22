using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour
{
	public HexGrid HexGrid;

	HexCell m_currentCell;
	HexUnit m_selectedUnit;

	void Update()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			if (Input.GetMouseButtonDown(0))
			{
				DoSelection();
			}
			else if (m_selectedUnit != null)
			{
				DoPathfinding();
			}
		}
	}

	public void SetEditMode(bool toggle)
	{
		enabled = !toggle;
		HexGrid.ShowUI(!toggle);
	}


	bool UpdateCurrentCell()
	{
		HexCell cell = HexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
		if (cell != m_currentCell)
		{
			m_currentCell = cell;
			return true;
		}
		return false;
	}

	void DoSelection()
	{
		UpdateCurrentCell();
		if (m_currentCell != null)
		{
			m_selectedUnit = m_currentCell.HexUnit;
		}
	}

	void DoPathfinding()
	{
		if(UpdateCurrentCell())
		{
			if(m_currentCell != null)
			{
				int speed = 24;
				List<HexCell> results = new List<HexCell>();
				HexGrid.FindPathTo(m_selectedUnit.Location, m_currentCell, speed,results);
				DrawStartEndPath(results, speed);
			}
			else
			{
				HexGrid.ClearPath();
			}
		}
	}

	public void DrawStartEndPath(List<HexCell> path,int speed)
	{
		for(int i=0;i<path.Count;++i)
		{
			Color color = Color.white;
			if(i== 0)
			{
				color = Color.blue;
			}
			else if (i== path.Count-1)
			{
				color = Color.red;
			}
			path[i].EnableHighlight(color);
			int turn = path[i].Distance / speed;
			path[i].SetLabel(turn.ToString());


		}

	}
}
