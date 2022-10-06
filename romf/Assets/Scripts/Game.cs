using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using vDiagramGen;

[System.Serializable]
public class Game
{
    public static Game current;
    public VoronoiDiagram map;
    public Texture2D mapTexture;
}
