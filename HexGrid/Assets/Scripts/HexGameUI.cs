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
			else if(Input.GetMouseButtonDown(1))
			{
				DoMove();
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

	List<HexCell> m_pathFindingResults = new List<HexCell>();

	void DoPathfinding()
	{
		if(UpdateCurrentCell())
		{
			if(m_currentCell != null && m_selectedUnit.IsValidDestination(m_currentCell))
			{
				ClearPaths();
				int speed = 24;
				HexGrid.FindPathTo(m_selectedUnit.Location, m_currentCell, speed, m_pathFindingResults);
				DrawStartEndPath(m_pathFindingResults, speed);
				m_selectedUnit.CopyPath(m_pathFindingResults);
				//m_selectedUnit.Travel();
			}
			else
			{
				ClearPaths();
			}
		}
	}

	private void ClearPaths()
	{
		HexGrid.ClearPath();
		m_pathFindingResults.Clear();
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
			int turn = (path[i].Distance -1)/ speed;
			path[i].SetLabel(turn.ToString());


		}
	}

	void DoMove()
	{
		if(HexGrid.HasValidPath)
		{
			m_selectedUnit.Travel();
			ClearPaths();
			m_selectedUnit = null;
		}
	}

	//public void OnDrawGizmos()
	//{
	//	if(m_pathFindingResults.Count > 0)
	//	{
	//		Vector3 a = m_pathFindingResults[0].Position;
	//		Vector3 b = a;
	//		Vector3 c = a;
			
	//		for (int i=1;i<m_pathFindingResults.Count;++i)
	//		{
	//			a = c;
	//			b = m_pathFindingResults[i - 1].Position;
	//			c = (b + m_pathFindingResults[i].Position) * 0.5f;
	//			DrawStep(a, b,c);
	//		}

	//		a = c;
	//		b = m_pathFindingResults[m_pathFindingResults.Count - 1].Position;
	//		c = b;
	//		DrawStep(a, b,c);

	//	}
	//}

	//private void DrawStep(Vector3 a,Vector3 b,Vector3 c)
	//{
	//	float step = 0.1f;
	//	float count = 0;
	//	while (count <= 1f)
	//	{
	//		Vector3 position = Bezier.GetPoint(a, b, c, count);
	//		count += step;
	//		Gizmos.DrawSphere(position, 2f);
	//	}
	//}

}
