using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;

public partial class mapGen : MonoBehaviour
{
    private Vector2Int[] seeds;
    private Vector3[] colors;
    Vector3[] jar;

    private ComputeBuffer seedBuffer;
    private ComputeBuffer colorBuffer;

    private int seedNumber = 20;
    private int initSeedsKernel;
    private int jfaKernel;
    private int floodMapKernel;

    public RenderTexture inputTexture;
    public RenderTexture outputTexture;

    public void createvMap(){
        seeds = genPtsInt();
        genSdColors();

        seedBuffer = new ComputeBuffer(seeds.Length, sizeof(int)*2);
        seedBuffer.SetData(seeds);
        colorBuffer = new ComputeBuffer(seeds.Length, sizeof(float)*3);
        colorBuffer.SetData(colors);
        initSeedsKernel = JFAShader.FindKernel("initSeeds");
        jfaKernel = JFAShader.FindKernel("jfaCS");
        floodMapKernel = JFAShader.FindKernel("floodMap");
        rendVMAP();
    }

    private void rendVMAP()
    {
        RenderTexture source = new RenderTexture(mapSize.x, mapSize.y, 24);
        InitRenderTexture(source);

        seedBuffer.SetData(seeds);
        JFAShader.SetBuffer(initSeedsKernel, "seeds", seedBuffer);
        JFAShader.SetTexture(initSeedsKernel, "Source", inputTexture);
        JFAShader.SetInt("mapWidth", source.width);
        JFAShader.SetInt("mapHeight", source.height);
        JFAShader.Dispatch(initSeedsKernel, seeds.Length, 1, 1);

        int stepAmount = (int)Mathf.Log(Mathf.Max(source.width, source.height), 2);
    
        int threadGroupsX = Mathf.CeilToInt(source.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(source.height / 8.0f);
        for (int i = 0; i < stepAmount; i++)
        {
            int step = (int)Mathf.Pow(2, stepAmount - i - 1);
            //Debug.Log("step:" + step);
            JFAShader.SetInt("step", step);
            JFAShader.SetTexture(jfaKernel, "Source", inputTexture);
            JFAShader.SetTexture(jfaKernel, "Result", outputTexture);

            JFAShader.Dispatch(jfaKernel, threadGroupsX, threadGroupsY, 1);
            Graphics.Blit(outputTexture, inputTexture);
        }

        JFAShader.SetInt("mapWidth", source.width);
        JFAShader.SetInt("mapHeight", source.height);
        JFAShader.SetBuffer(floodMapKernel, "colors", colorBuffer);

        JFAShader.SetTexture(floodMapKernel, "Source", inputTexture);
        JFAShader.SetTexture(floodMapKernel, "Result", outputTexture);
        JFAShader.Dispatch(floodMapKernel, threadGroupsX, threadGroupsY, 1);

        seedBuffer.Dispose();
        colorBuffer.Dispose();
    }

    private void InitRenderTexture(RenderTexture source)
    {
        if (inputTexture == null || inputTexture.width != source.width || inputTexture.height != source.height)
        {
            // Release render texture if we already have one
            if (inputTexture != null)
                inputTexture.Release();
            if (outputTexture != null)
                outputTexture.Release();
            // Get a render target for Ray Tracing
            inputTexture = new RenderTexture(source.width, source.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            inputTexture.enableRandomWrite = true;
            inputTexture.Create();

            outputTexture = new RenderTexture(source.width, source.height, 0,
               RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            outputTexture.filterMode = FilterMode.Point;
            outputTexture.enableRandomWrite = true;
            outputTexture.Create();
        }
    }

    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(mapSize.x, mapSize.y, TextureFormat.RGB24, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    /*
    #############################
           Color Gen Code        
    #############################
    */


    //Color Generation for seeds
    public void genSdColors(){
        colors = new Vector3[seeds.Length];
        for(int i = 0; i < seeds.Length; i++){
            colors[i] = getColor();
        }
    }

    public Vector3 getColor()
    {
        Vector3 color = new Vector3Int(0,0,0);

        bool check = false;
        while (check == false)
        {
            color = new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            if (!colors.Contains(color))
                check = true;
        }
        return color;
    }
}
