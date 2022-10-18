using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeVisualisation : MonoBehaviour
{
    public MazeGenerator MazeGenerator;
    public GameObject FloorPrefab;
    public GameObject WallPrefab;

    
    private bool m_haveBuilt;
    private GameObject m_mazeRoot;


    public void Update()
    {
        if(!m_haveBuilt)
        {
            DrawMaze();
            m_haveBuilt = true;
        }
    }


    public void DrawMaze()
    {
        m_mazeRoot = new GameObject("MazeRoot");
        Maze maze = MazeGenerator.Maze;
        for(int y = 0;y<maze.Height;++y)
        {
            GameObject row = new GameObject("Row-"+y);
            row.transform.parent = m_mazeRoot.transform;
            row.transform.localPosition = new Vector3(0,0,y);

            for(int x=0;x<maze.Width;++x)
            {
                MazeSquare mazeSquare = maze.GetSquare(x,y);

                GameObject square =  Instantiate(FloorPrefab);
                square.name = ""+mazeSquare.Location;
                square.transform.parent = row.transform;
                square.transform.localPosition = new Vector3(x,0,0);
                if(mazeSquare.SquareColour != Color.black)
                {
                    square.GetComponent<Renderer>().material.color = mazeSquare.SquareColour;
                }

                if(mazeSquare.MazeRoom != null)
                {
                    square.GetComponent<Renderer>().material.color = Color.yellow;
                }
                
                for(int i=0;i<(int)Direction.NumDirections;++i)
                {
                    Direction direction = (Direction)i;
                    if(!mazeSquare.HasConnection(direction))
                    {
                        PlaceWall(square,direction);
                    }
                }
            }
        }
    }

    private void PlaceWall(GameObject square,Direction direction)
    {
        float wallWidth = 0.1f;
        float wallHeight =1f;
        float wallAngle = 90f;
        float wallOffset = (wallHeight - wallWidth)/2f;
        float wallMid = wallHeight/2f;

        GameObject wall = Instantiate(WallPrefab);
        wall.transform.parent = square.transform;
        wall.transform.localScale = new Vector3(wallWidth,1,1);
        
        Color color = Color.white;    
        if(direction == Direction.North)
        {
            wall.transform.localPosition = new Vector3(0,wallHeight,wallOffset);
            color = Color.green;
            wall.transform.localRotation = Quaternion.Euler(0,wallAngle,0);
        }
        else if(direction == Direction.South)
        {
            wall.transform.localPosition = new Vector3(0,wallHeight,-wallOffset);
            wall.transform.localRotation = Quaternion.Euler(0,-wallAngle,0);
            color = Color.blue;
        }
        else if(direction == Direction.East)
        {
            wall.transform.localPosition = new Vector3(wallOffset,wallHeight,0);
            color = Color.red;
            //wall.transform.localRotation = Quaternion.Euler(0,wallAngle,0);
            
        }
        else if(direction == Direction.West)
        {
            wall.transform.localPosition = new Vector3(-wallOffset,wallHeight,0);
            color = Color.yellow;
            //wall.transform.localRotation = Quaternion.Euler(90,wallAngle,0);
            
        }
        wall.GetComponent<Renderer>().material.color = color;
    }



}
