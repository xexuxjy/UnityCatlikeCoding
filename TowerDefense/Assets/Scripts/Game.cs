using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField]
    Vector2Int boardSize = new Vector2Int(11, 11);

    [SerializeField]
    GameBoard board = default;

    [SerializeField]
    GameTileContentFactory tileContentFactory = default;

    [SerializeField]
    WarFactory warFactory = default;

    [SerializeField]
    GameScenario scenario = default;

    [SerializeField, Range(-10, 100)]
    int startingPlayerHealth = 10;

    int playerHealth;

    const float pausedTimeScale = 0f;

    [SerializeField, Range(1f, 10f)]
    float playSpeed = 1f;

    public bool TestLevel;

    GameScenario.State activeScenario;


    GameBehaviourCollection enemies = new GameBehaviourCollection();
    GameBehaviourCollection nonEnemies = new GameBehaviourCollection();


    Ray TouchRay => Camera.main.ScreenPointToRay(Input.mousePosition);

    TowerType selectedTowerType;


    void Awake()
    {
        board.Initialize(boardSize, tileContentFactory);
        board.ShowGrid = true;
        activeScenario = scenario.Begin();
        playerHealth = startingPlayerHealth;

        if(TestLevel)
        {
            SetTestLevel();
        }
		
    }


    void OnValidate()
    {
        if (boardSize.x < 2)
        {
            boardSize.x = 2;
        }
        if (boardSize.y < 2)
        {
            boardSize.y = 2;
        }
    }

    public void SetTestLevel()
    {
        for (int x = 1; x < boardSize.x; ++x)
        {
            board.ToggleWall(board.GetTile(x, 1));
        }
        for (int y = 2; y < boardSize.y; ++y)
        {
            board.ToggleWall(board.GetTile(2, y));
        }

        for (int x = 3; x < boardSize.x-3; ++x)
        {
            board.ToggleWall(board.GetTile(x, 7));
        }



        board.ToggleTower(board.GetTile(4, 4), TowerType.Laser);

        board.ToggleTower(board.GetTile(1, 3), TowerType.Laser);
        board.ToggleTower(board.GetTile(1, 4), TowerType.Mortar);
        board.ToggleTower(board.GetTile(1, 6), TowerType.Laser);
        board.ToggleTower(board.GetTile(1, 7), TowerType.Mortar);
        board.ToggleTower(board.GetTile(1, 9), TowerType.Laser);


        board.ToggleTower(board.GetTile(4, 8), TowerType.Mortar);

    }





    void Update()
    {
        instance = this;

        if (Input.GetKeyDown(KeyCode.B))
        {
            BeginNewGame();
        }

        if (playerHealth <= 0 && startingPlayerHealth > 0)
        {
            Debug.Log("Defeat!");
            BeginNewGame();
        }

        if (!activeScenario.Progress() && enemies.IsEmpty)
        {
            Debug.Log("Victory!");
            BeginNewGame();
            activeScenario.Progress();
        }

        activeScenario.Progress();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Time.timeScale =
                Time.timeScale > pausedTimeScale ? pausedTimeScale : playSpeed;
        }
        else if (Time.timeScale > pausedTimeScale)
        {
            Time.timeScale = playSpeed;
        }


        if (Input.GetMouseButtonDown(0))
        {
            HandleTouch();
        }

        if (Input.GetMouseButtonDown(1))
        {
            HandleAlternativeTouch();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            board.ShowPaths = !board.ShowPaths;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            board.ShowGrid = !board.ShowGrid;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedTowerType = TowerType.Laser;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedTowerType = TowerType.Mortar;
        }



        enemies.GameUpdate();
        nonEnemies.GameUpdate();

        Physics.SyncTransforms();
        board.GameUpdate();
    }

    static Game instance;

    public static Shell SpawnShell()
    {
        Shell shell = instance.warFactory.Shell;
        instance.nonEnemies.Add(shell);
        return shell;
    }

    void OnEnable()
    {
        instance = this;
    }

    public static void SpawnEnemy(EnemyFactory factory, EnemyType type)
    {
        GameTile spawnPoint = instance.board.GetSpawnPoint(
            Random.Range(0, instance.board.SpawnPointCount)
        );
        Enemy enemy = factory.Get(type);
        enemy.SpawnOn(spawnPoint);
        instance.enemies.Add(enemy);
    }

    public static Explosion SpawnExplosion()
    {
        Explosion explosion = instance.warFactory.Explosion;
        instance.nonEnemies.Add(explosion);
        return explosion;
    }


    void HandleTouch()
    {
        GameTile tile = board.GetTile(TouchRay);
        if (tile != null)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                board.ToggleTower(tile, selectedTowerType);
            }
            else
            {
                board.ToggleWall(tile);
            }

        }
    }

    void HandleAlternativeTouch()
    {
        GameTile tile = board.GetTile(TouchRay);
        if (tile != null)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                board.ToggleDestination(tile);
            }
            else
            {
                board.ToggleSpawnPoint(tile);
            }
        }
    }

    public static void EnemyReachedDestination()
    {
        instance.playerHealth -= 1;
    }

    void BeginNewGame()
    {
        enemies.Clear();
        nonEnemies.Clear();
        board.Clear();
        activeScenario = scenario.Begin();
        playerHealth = startingPlayerHealth;
    }
}


