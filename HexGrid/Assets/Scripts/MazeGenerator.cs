using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    public int Width = 20;
    public int Height = 20;


    public int MaxRooms = 4;

    public int RoomMin = 2;
    public int RoomMax = 5;

    Maze m_maze;



    public void Awake()
    {
        BuildMaze();
    }

    public void BuildMaze()
    {
        m_maze = new Maze(Width,Height);
        m_maze.GenerateRooms(RoomMin,RoomMax,MaxRooms);
        m_maze.Generate();
    }

    public Maze Maze
    { get { return m_maze; } }

}


public class Maze
{
    private MazeSquare[] m_mazeSquares;
    private int m_width;
    private int m_height;
    private Stack<MazeSquare> m_generationStack = new  Stack<MazeSquare>();

    public int Width{ get { return m_width; } }
    public int Height{ get { return m_height; } }


    
    public Maze(int width,int height)
    {
        m_width = width;
        m_height = height;
        m_mazeSquares = new MazeSquare[width * height];
        for(int y=0;y<height;y++)
        {
            for(int x=0;x<width;x++)
            {
                m_mazeSquares[GetIndex(x,y)] = new MazeSquare(new Vector2(x,y));
            }
        }
    }

    public int GetIndex(int x,int y)
    {
        return (y * m_width) + x;
    }

    public MazeSquare GetSquare(int x,int y)
    {
        if(x >=0 && y>=0 && x<m_width && y <m_height)
        {
            return m_mazeSquares[GetIndex(x,y)];
        }
        return null;
    }




    public MazeSquare GetSquare(MazeSquare source,Direction dir)
    {
        Vector2 newVal = source.Location;
        if(dir == Direction.North)
        {
            newVal += new Vector2(0,1);
        }
        else if(dir == Direction.South)
        {
            newVal += new Vector2(0,-1);
        }
        else if (dir == Direction.West)
        {
            newVal += new Vector2(-1,0);
        }
        else if(dir == Direction.East)
        {
            newVal += new Vector2(1,0);
        }
        else
        {
            Debug.Assert(false,"Failed direction "+dir);
        }

        MazeSquare targetSquare = GetSquare((int)newVal.x,(int)newVal.y);
        return targetSquare;


    }

    public void GenerateRooms(int roomMin,int roomMax,int maxRooms)
    {
        int maxAttempts = 200;
        int attemptCount = 0;
        int roomsGenerated = 0;
        
        while(attemptCount < maxAttempts && roomsGenerated < maxRooms)
        {

            int roomWidth = Random.Range(roomMin,roomMax);
            int roomHeight = Random.Range(roomMin,roomMax);
            // pick a random spot, then try and set all points to a room.
            int randomX = Random.Range(0,m_width-roomWidth);
            int randomY = Random.Range(0,m_height-roomHeight);

            MazeSquare startSquare = m_mazeSquares[GetIndex(randomX,randomY)];
            bool validRoom = true;
            if(!startSquare.IsRoom)
            {
                for(int y=0;y<roomHeight;++y)
                {
                    for(int x=0;x<roomWidth;++x)
                    {
                        MazeSquare roomSquare = GetSquare(randomX+x,randomY+y);
                        if(roomSquare == null || roomSquare.IsRoom)
                        {
                            validRoom = false;
                            break;
                        }
                    }
                    if(!validRoom)
                    {
                        break;
                    }
                }
            }
            
            // valid room, so mark all squares and form connections
            if(validRoom)
            {
                attemptCount = 0;
                roomsGenerated++;

                for(int y=0;y<roomHeight;++y)
                {
                    for(int x=0;x<roomWidth;++x)
                    {
                        MazeSquare roomSquare = GetSquare(randomX+x,randomY+y);
                        roomSquare.IsRoom = true;
                        if(x < roomWidth-1)
                        {
                            roomSquare.JoinSquare(GetSquare(randomX+x+1,randomY+y),Direction.East);
                        }
                        if(y < roomHeight -1)
                        {
                            roomSquare.JoinSquare(GetSquare(randomX+x,randomY+y+1),Direction.North);
                        }
                    }
                }
            }
            else
            {
                attemptCount++;
            }
        }
    }


    public void Generate()
    {
        Reset();
        int randomX = Random.Range(0,m_width);
        int randomY = Random.Range(0,m_height);

        MazeSquare startSquare = m_mazeSquares[GetIndex(randomX,randomY)];
        startSquare.SquareColour = Color.magenta;
        startSquare.Visited = true;
        m_generationStack.Push(startSquare);
        

        while(m_generationStack.Count > 0)
        {
            MazeSquare currentSquare = m_generationStack.Pop();
            MazeSquare neighbour = GetUnvisitedNeighbour(currentSquare);
            if(neighbour != null)
            {
                m_generationStack.Push(currentSquare);
                Direction dir = DirectionBetweenSquare(currentSquare,neighbour);
                currentSquare.JoinSquare(neighbour,dir);

                neighbour.Visited = true;
                m_generationStack.Push(neighbour);
            }
        }

    }

    private List<MazeSquare> m_scratchPad = new List<MazeSquare>();
    MazeSquare GetUnvisitedNeighbour(MazeSquare square)
    {
        m_scratchPad.Clear();
        for(int i=0;i<(int)Direction.NumDirections;++i)
        {
            MazeSquare newSquare = GetSquare(square,(Direction)i);
            if(newSquare != null && !newSquare.Visited)
            {
                m_scratchPad.Add(newSquare);
            }
        }

        if (m_scratchPad.Count > 0)
        {
            return m_scratchPad[Random.Range(0,m_scratchPad.Count)];
        }

        return null;
    }

    public void Reset()
    {
        foreach(MazeSquare square in m_mazeSquares)
        {
            square.Reset();
        }
    }

    public static Direction DirectionBetweenSquare(MazeSquare source,MazeSquare destination)
    {
        Vector2 diff = destination.Location - source.Location;
        if(diff.x > 0)
        {
            return Direction.East;
        }
        if(diff.x < 0)
        {
            return Direction.West;
        }
        if(diff.y > 0)
        {
            return Direction.North;
        }
        return Direction.South;
    }

}



public class MazeSquare
{
    public Vector2 Location = new Vector2();
    //public MazeSquare[] Links = new MazeSquare[(int)Direction.NumDirections];
    public bool[] PathExists = new bool[(int)Direction.NumDirections];
    public bool Visited;
    public Color SquareColour = Color.black;
    public bool IsRoom;

    public MazeSquare(Vector2 location)
    {
        Location = location;
    }

    public void Reset()
    {
        Visited = false;
        //for(int i=0;i<Links.Length;i++)
        //{
        //    Links[i] = null;
        //}
    }

    public void JoinSquare(MazeSquare connection,Direction direction)
    {
        //Links[(int)direction] = connection;
        //connection.Links[(int)OppositeDirection(direction)] = this;
        PathExists[(int)direction] = true;
        connection.PathExists[(int)OppositeDirection(direction)] = true;

    }

    public bool HasConnection(Direction direction)
    {
        //return Links[(int)direction] != null;
        return PathExists[(int)direction];
    }


    public static Direction OppositeDirection(Direction dir)
    {
        switch(dir)
        {
            case Direction.North:return Direction.South;
            case Direction.South:return Direction.North;
            case Direction.East:return Direction.West;
            case Direction.West:return Direction.East;
        }
        return Direction.North;
    }
            


}

public enum Direction
{
    North=0,
    South=1,
    East=2,
    West=3,
    NumDirections=4
}