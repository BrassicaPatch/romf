using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapCell : MonoBehaviour
{
    public Color color;

    public List<MapCell> neighbors;

    public Vector2Int seed;
    public List<Vector2Int> pixs;
    public List<Vector2Int> verticies;

    public MapCell(Color c, Vector2Int s){
        color = c;
        seed = s;
        neighbors = new List<MapCell>();
        pixs = new List<Vector2Int>();
    }
}
