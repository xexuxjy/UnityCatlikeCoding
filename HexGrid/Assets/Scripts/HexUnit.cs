
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HexUnit : MonoBehaviour
{
	HexCell m_location;
	HexCell m_currentTravelLocation;
	public static HexUnit UnitPrefab;

	public HexGrid HexGrid
	{ get; set; }

	public const int VisionRange = 3;

	public HexCell Location
	{
		get
		{
			return m_location;
		}
		set
		{
			if(m_location != null)
			{
				m_location.HexUnit = null;
				HexGrid.DecreaseVisibilty(m_location, VisionRange);
			}

			m_location = value;
			HexGrid.IncreaseVisibilty(m_location, VisionRange);
			transform.localPosition = m_location.Position;
			m_location.HexUnit = this;
		}
	}

	float m_orientation;
	public float Orientation
	{
		get
		{
			return m_orientation;
		}
		set
		{
			m_orientation = value;
			transform.localRotation = Quaternion.Euler(0f, value, 0f);
		}
	}

	public void ValidateLocation()
	{
		transform.localPosition = Location.Position;
	}

	public void Die()
	{
		Location.HexUnit = null;
		HexGrid.DecreaseVisibilty(Location, VisionRange);
		Destroy(gameObject);
	}

	public void Save(BinaryWriter writer)
	{
		Location.Coordinates.Save(writer);
		writer.Write(Orientation);
	}

	public static void Load(BinaryReader reader,HexGrid hexGrid)
	{
		HexCoordinates coordinates = HexCoordinates.Load(reader);
		float orientation = reader.ReadSingle();
		hexGrid.AddUnit(Instantiate(UnitPrefab), hexGrid.GetCell(coordinates), orientation);
	}

	public bool IsValidDestination(HexCell cell)
	{
		return !cell.IsUnderwater && !cell.HexUnit;
	}

	private List<HexCell> m_moveList = new List<HexCell>();
	public void CopyPath(List<HexCell> moveList)
	{
		m_moveList.Clear();
		m_moveList.AddRange(moveList);
	}

	public void Travel()
	{
		//Location = m_moveList[m_moveList.Count - 1];
		m_location.HexUnit = null;
		m_location = m_moveList[m_moveList.Count - 1];
		m_location.HexUnit = this;
		StopAllCoroutines();
		StartCoroutine(TravelPath());
		
	}


	public IEnumerator TravelPath()
	{
		if (m_moveList.Count > 0)
		{
			Vector3 a = m_moveList[0].Position;
			Vector3 b = a;
			Vector3 c = a;
			transform.localPosition = c;
			yield return LookAt(m_moveList[1].Position);
			HexGrid.DecreaseVisibilty(m_currentTravelLocation != null ? m_currentTravelLocation :  m_moveList[0], VisionRange);


			for (int i = 1; i < m_moveList.Count; ++i)
			{
				m_currentTravelLocation = m_moveList[i]; 
				a = c;
				b = m_moveList[i - 1].Position;
				c = (b + m_currentTravelLocation.Position) * 0.5f;
				HexGrid.IncreaseVisibilty(m_currentTravelLocation, VisionRange);
				yield return MoveStep(a, b,c);
				HexGrid.DecreaseVisibilty(m_currentTravelLocation, VisionRange);
			}
			m_currentTravelLocation = null;
			a = c;
			b = Location.Position;//m_moveList[m_moveList.Count - 1].Position;
			c = b;

			HexGrid.IncreaseVisibilty(Location, VisionRange);

			yield return MoveStep(a, b,c);
			transform.localPosition = Location.Position;
			Orientation = transform.localRotation.eulerAngles.y;
		}
	}

	public void OnEnable()
	{
		if(Location != null)
		{
			transform.localPosition = Location.Position;
			if(m_currentTravelLocation != null)
			{
				HexGrid.IncreaseVisibilty(m_location, VisionRange);
				HexGrid.DecreaseVisibilty(m_currentTravelLocation, VisionRange);
				m_currentTravelLocation = null;
			}
		}
	}

	public const float m_movementSpeed = 1.0f;

	private IEnumerator MoveStep(Vector3 a, Vector3 b,Vector3 c)
	{
		float step = (Time.deltaTime * m_movementSpeed);
		float count = 0;
		while (count <= 1f)
		{
			count += step;
			Vector3 position = Bezier.GetPoint(a, b, c,count);
			Vector3 orientation = Bezier.GetDerivative(a, b, c, count);
			// Keep upright
			orientation.y = 0f;

			transform.localPosition = position;
			transform.localRotation = Quaternion.LookRotation(orientation);


			yield return null;

		}
	}

	const float m_rotationSpeed = 180;
	public IEnumerator LookAt(Vector3 point)
	{
		point.y = transform.localPosition.y;

		Quaternion fromRotation = transform.localRotation;
		Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition);

		float angle = Quaternion.Angle(fromRotation, toRotation);
		float speed = m_rotationSpeed / angle;

		for(float t = (Time.deltaTime * speed) ;t < 1f;t+=(Time.deltaTime * speed))
		{
			transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
			yield return null;
		}

		transform.LookAt(point);
		Orientation = transform.localRotation.eulerAngles.y;
	}


}
