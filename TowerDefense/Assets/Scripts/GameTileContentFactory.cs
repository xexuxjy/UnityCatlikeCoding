using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu]
public class GameTileContentFactory : GameObjectFactory
{
	//Scene contentScene;

	[SerializeField]
	GameTileContent destinationPrefab = default;

	[SerializeField]
	GameTileContent emptyPrefab = default;

	[SerializeField]
	GameTileContent wallPrefab = default;

	[SerializeField]
	GameTileContent spawnPointPrefab = default;

	//[SerializeField]
	//GameTileContent towerPrefab = default;

	[SerializeField]
	Tower[] towerPrefabs = default;


	public void Reclaim(GameTileContent content)
	{
		Debug.Assert(content.OriginFactory == this, "Wrong factory reclaimed!");
		Destroy(content.gameObject);
	}

	GameTileContent Get(GameTileContent prefab)
	{
		GameTileContent instance = CreateGameObjectInstance(prefab);
		instance.OriginFactory = this;
		return instance;
	}

	T Get<T>(T prefab) where T : GameTileContent
	{
		T instance = CreateGameObjectInstance(prefab);
		instance.OriginFactory = this;
		return instance;
	}
	//public GameTileContent Get(TowerType type)
	//{
	//	Debug.Assert((int)type < towerPrefabs.Length, "Unsupported tower type!");
	//	Tower prefab = towerPrefabs[(int)type];
	//	Debug.Assert(type == prefab.TowerType, "Tower prefab at wrong index!");
	//	return Get(prefab);
	//}

	public GameTileContent Get(GameTileContentType type)
	{
		switch (type)
		{
			case GameTileContentType.Destination: return Get(destinationPrefab);
			case GameTileContentType.Empty: return Get(emptyPrefab);
			case GameTileContentType.Wall: return Get(wallPrefab);
			case GameTileContentType.SpawnPoint: return Get(spawnPointPrefab);
		}
		Debug.Assert(false, "Unsupported type: " + type);
		return null;
	}

	public GameTileContent Get(TowerType type)
	{
		Debug.Assert((int)type < towerPrefabs.Length, "Unsupported tower type!");
		Tower prefab = towerPrefabs[(int)type];
		Debug.Assert(type == prefab.TowerType, "Tower prefab at wrong index!");
		return Get(prefab);
	}

}


public enum GameTileContentType
{
	Empty, Destination, Wall, SpawnPoint, Tower
}