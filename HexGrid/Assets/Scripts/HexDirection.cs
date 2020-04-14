using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HexDirection
{
    NE,E,SE,SW,W,NW
}


public static class HexDirectionExtensions
{
    public static HexDirection Opposite(this HexDirection direction)
    {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }

    public static HexDirection Previous(this HexDirection direction)
    {
        //return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
        return Adjust(direction, -1);
    }

    public static HexDirection Next(this HexDirection direction)
    {
        //return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
        return Adjust(direction, 1);
    }

    public static HexDirection Previous2(this HexDirection direction)
    {
        //direction -= 2; 
        //return direction >= HexDirection.NE ? HexDirection.NW : (direction - 1);
        return Adjust(direction, -2);
    }

    public static HexDirection Next2(this HexDirection direction)
    {
        //direction += 2;
        //return direction <= HexDirection.NW ? direction : (direction - 61);
        return Adjust(direction, 2);
    }

    public static HexDirection Adjust(HexDirection dir,int val)
    {
        int dirVal = (int)dir;
        dirVal += val;
        if(dirVal < 0)
        {
            dirVal += 6;
        }
        if(dirVal > 5)
        {
            dirVal -= 6;
        }

        return (HexDirection)dirVal;
    }


}