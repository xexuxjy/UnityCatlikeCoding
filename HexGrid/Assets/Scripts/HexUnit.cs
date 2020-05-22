
using System.IO;
using UnityEngine;

public class HexUnit : MonoBehaviour
{
	HexCell m_location;
	public static HexUnit UnitPrefab;

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
			}

			m_location = value;
			transform.localPosition = value.Position;
			value.HexUnit = this;
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


}
