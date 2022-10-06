using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using vDiagramGen;

public class pfgMain : MonoBehaviour
{
    public Vector2Int mapSize = new Vector2Int(512, 512);
    public int pointCount = 1000;

    public Texture2D outImg;
    public GameObject quad;
    public GameObject cam;

    List<Vector2> vPt;
    List<Color> colors;

    void Start()
    {
        pfgRun();
    }

    public void pfgRun()
    {
        var vTotal = new Stopwatch();
        var sw = new Stopwatch();
        vTotal.Start();
        sw.Start();

        vPt = new List<Vector2>();
        colors = new List<Color>();

        var voronoiDiagram = new VoronoiDiagram(new Rect(0f, 0f, mapSize.x, mapSize.y));    
        var points = new List<VoronoiDiagramSite>();

        while(points.Count < pointCount)
        {
            int randX = Random.Range(0, mapSize.x - 1);
            int randY = Random.Range(0, mapSize.y - 1);

            var point = new Vector2(randX, randY);
            if(!points.Any(item => item.Coordinate == point))
            {
                points.Add(new VoronoiDiagramSite(point, new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f))));
            }
        }

        voronoiDiagram.AddSites(points);
        voronoiDiagram.GenerateSites();
        UnityEngine.Debug.Log($"Voronoi Sites Generated in {sw.Elapsed}");
        sw.Restart();

        outImg = new Texture2D(mapSize.x, mapSize.y);
        var arr = voronoiDiagram.Get1DSampleArray();
        UnityEngine.Debug.Log($"TextureArray Got in {sw.Elapsed}");
        sw.Restart();

        outImg.SetPixels(arr);
        UnityEngine.Debug.Log($"Texture Pixels Set in {sw.Elapsed}");
        sw.Restart();
        
        outImg.Apply();
        displayTex();
        UnityEngine.Debug.Log($"Texture Apllied and Displayed in {sw.Elapsed}");
        UnityEngine.Debug.Log($"Total Process Finished in {vTotal.Elapsed} with {vPt.Count} cells");
        sw.Stop();
        vTotal.Stop();
    }

    void displayTex(){
        quad.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", outImg);
        quad.GetComponent<MeshRenderer>().material.SetTexture("_RemapTex", outImg); 
        quad.GetComponent<MeshRenderer>().material.SetTexture("_PaletteTex", outImg);

        //quad.GetComponent<MeshRenderer>().material.mainTexture.filterMode = FilterMode.Point;
        quad.transform.localScale = new Vector3(mapSize.x, mapSize.y, 1);
        cam.GetComponent<Camera>().orthographicSize = mapSize.x/2 + 50;
        quad.GetComponent<Mapshower>().runMapShower();
    }

    public Color getColor()
    {
        Color color = new Color(0, 0, 0, 1);

        bool check = false;
        while (check == false)
        {
            color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            if (!colors.Contains(color))
            {
                check = true;
                colors.Add(color);
            }
        }
        return color;
    }
}
