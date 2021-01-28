using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTile : MonoBehaviour
{
	[SerializeField]
	Transform arrow = default;

	GameTile north;
	GameTile south;
	GameTile east;
	GameTile west;

	public Direction PathDirection { get; private set; }

	GameTile nextOnPath;

	public Vector3 ExitPoint { get; private set; }


	int distance;

	public bool IsAlternative { get; set; }

	GameTileContent content;

	public GameTile NextTileOnPath => nextOnPath;

	public GameTileContent Content
	{
		get => content;
		set
		{
			Debug.Assert(value != null, "Null assigned to content!");
			if (content != null)
			{
				content.Recycle();
			}
			content = value;
			content.transform.localPosition = transform.localPosition;
		}
	}

	public void ClearPath()
    {
		nextOnPath = null;
		distance = int.MaxValue;
    }

	public void BecomeDestination()
    {
		distance = 0;
		nextOnPath = null;
		ExitPoint = transform.localPosition;
	}


	public GameTile GrowPathNorth() => GrowPathTo(north, Direction.South);

	public GameTile GrowPathEast() => GrowPathTo(east, Direction.West);

	public GameTile GrowPathSouth() => GrowPathTo(south, Direction.North);

	public GameTile GrowPathWest() => GrowPathTo(west, Direction.East);

	GameTile GrowPathTo(GameTile neighbour, Direction direction)
	{
		if (neighbour == null || neighbour.HasPath)
		{
			return null;
		}
		neighbour.nextOnPath = this;
		neighbour.distance = distance + 1;

		//neighbour.ExitPoint =
		//	(neighbour.transform.localPosition + transform.localPosition) * 0.5f;

		neighbour.ExitPoint =
			neighbour.transform.localPosition + direction.GetHalfVector();

		neighbour.PathDirection = direction;

		return neighbour.Content.BlocksPath ? null : neighbour; 
	}

	public bool HasPath => distance != int.MaxValue;

	public static void MakeEastWestNeighbors(GameTile east, GameTile west)
	{
		Debug.Assert(west.east == null && east.west == null, "Redefined neighbors!");
		west.east = east;
		east.west = west;
	}
	public static void MakeNorthSouthNeighbors(GameTile north, GameTile south)
	{
		Debug.Assert(
			south.north == null && north.south == null, "Redefined neighbors!");
		south.north = north;
		north.south = south;
	}

	public void ShowPath()
	{
		if (distance == 0)
		{
			arrow.gameObject.SetActive(false);
			return;
		}
		arrow.gameObject.SetActive(true);
		arrow.localRotation =
			nextOnPath == north ? Direction.North.GetArrowRotation() :
			nextOnPath == east ? Direction.East.GetArrowRotation() :
			nextOnPath == south ? Direction.South.GetArrowRotation() :
			Direction.West.GetArrowRotation();
	}

	public void HidePath()
	{
		arrow.gameObject.SetActive(false);
	}

}




