using UnityEngine;

[SelectionBase]
public class GameTileContent : MonoBehaviour
{
	GameTileContentFactory originFactory;

	public GameTileContentFactory OriginFactory
	{
		get => originFactory;
		set
		{
			Debug.Assert(originFactory == null, "Redefined origin factory!");
			originFactory = value;
		}
	}

	public void Recycle()
	{
		originFactory.Reclaim(this);
	}

	public virtual void GameUpdate() { }

	[SerializeField]
	GameTileContentType type = default;

	public GameTileContentType Type => type;

	public bool BlocksPath =>
		Type == GameTileContentType.Wall || Type == GameTileContentType.Tower;
}