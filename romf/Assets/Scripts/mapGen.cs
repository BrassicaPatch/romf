using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class mapGen : MonoBehaviour
{

    public Vector2Int mapSize;
    public GameObject quad;
    public Camera cam;

    public cell[] mapCells;
    public Dictionary<Color, cell> cellColorReference;

    [Header("jfa")]
    public ComputeShader JFAShader;
    public Texture2D displayTexture;

    void Start()
    {
        createvMap();
        displayTex();
        cRHost();
        fnHost();
        //await mgAsync();
    }

    void Update()
    {
        
    }

    async Task mgAsync(){
        List<Task> mgTasks = new List<Task>();
        //mgTasks.Add(Task.Run(() => fnTaskHostASync()));

        await Task.WhenAll(mgTasks);
    }


    void cRHost(){
        cellColorReference = new Dictionary<Color, cell>();
        for(int x = 0; x < mapSize.x; x++){
            for(int y = 0; y < mapSize.y; y++){
                Color pxCol = displayTexture.GetPixel(x, y);
                if(!cellColorReference.ContainsKey(pxCol)){
                    cell c = new cell();
                    c.color = pxCol;
                    // int index = System.Array.IndexOf(colors, new Vector3(pxCol.r, pxCol.g, pxCol.b));
                    // Debug.Log($"Seed Count: {seeds.Length} || Index: {index}");
                    // c.seed = seeds[index];
                    c.pixies = new List<Vector2Int>();
                    c.pixies.Add(new Vector2Int(x, y));
                    c.verticies = new List<Vector2Int>();
                    c.neighbors = new List<cell>();
                    cellColorReference.Add(pxCol, c);
                }
                else
                    cellColorReference[pxCol].pixies.Add(new Vector2Int(x, y));
            }
        } 
    }

    async void fnHost(){;
        List<Task> fnHostTasks = new List<Task>();
        for(int x = 0; x < mapSize.x; x++){
            for(int y = 0; y < mapSize.y; y++){
                fnHostTasks.Add(Task.Run(() => findNeighbor(new Vector2Int(x,y))));
            }
        }
        await Task.WhenAll(fnHostTasks);
    }

    async Task findNeighbor(Vector2Int px){
        Color pxCol = displayTexture.GetPixel(px.x, px.y);
        if((px.x-1) !< 0){
            Color checkCol = displayTexture.GetPixel(px.x-1, px.y);
            if(pxCol != checkCol){
                cellColorReference[pxCol].neighbors.Add(cellColorReference[checkCol]);
                cellColorReference[checkCol].neighbors.Add(cellColorReference[pxCol]);
            }
        }
        if((px.x+1) !> mapSize.x){
            Color checkCol = displayTexture.GetPixel(px.x+1, px.y);
            if(pxCol != checkCol){
                cellColorReference[pxCol].neighbors.Add(cellColorReference[checkCol]);
                cellColorReference[checkCol].neighbors.Add(cellColorReference[pxCol]);
            }
        }
        if((px.y-1) !< 0){
            Color checkCol = displayTexture.GetPixel(px.x, px.y-1);
            if(pxCol != checkCol){
                cellColorReference[pxCol].neighbors.Add(cellColorReference[checkCol]);
                cellColorReference[checkCol].neighbors.Add(cellColorReference[pxCol]);
            }
        }
        if((px.y+1) !> mapSize.y){
            Color checkCol = displayTexture.GetPixel(px.x, px.y+1);
            if(pxCol != checkCol){
                cellColorReference[pxCol].neighbors.Add(cellColorReference[checkCol]);
                cellColorReference[checkCol].neighbors.Add(cellColorReference[pxCol]);
            } 
        }
    }

    //Debug Methods
    void displayTex(){
        displayTexture = toTexture2D(outputTexture);
        displayTexture.filterMode = FilterMode.Point;
        quad.GetComponent<MeshRenderer>().material.mainTexture = displayTexture;
        quad.transform.localScale = new Vector3(mapSize.x, mapSize.y, 1);
        cam.orthographicSize = mapSize.x/2 + 100;
    }
}

public struct cell{
    public Color color;
    public Vector2Int seed;
    public List<Vector2Int> pixies;
    public List<Vector2Int> verticies;
    public List<cell> neighbors;
}