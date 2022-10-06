using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

public class FloodFill : MonoBehaviour
{
    public Vector2Int mapSize = new Vector2Int(1920, 1080);

    public GameObject quad;
    [SerializeField] private Texture2D tex;
	private Color[] texCol;
    
    private List<Color> colors;
	private Color baseCol = new Color(1f, 1f, 1f, 1f);
	Dictionary<Color, MapCell> cellRef = new Dictionary<Color, MapCell>();

    void Start()
    {
        floodRunner();
    }

    public void floodRunner()
    {
		var sw = new Stopwatch();
		var swTotal = new Stopwatch();
		sw.Start();
		swTotal.Start();

		colors = new List<Color>();
        tex = new Texture2D(1920, 1080);
		tex.filterMode = FilterMode.Point;

		texCol = tex.GetPixels();
		for(int i = 0; i < texCol.Length; i++){
			texCol[i] = baseCol;
		}
		UnityEngine.Debug.Log($"Get Pixels Done In: {sw.Elapsed}");
		sw.Restart();

		var seedList = GeneratePoints(mapSize);
		UnityEngine.Debug.Log($"Poisson Done In: {sw.Elapsed}");
		sw.Restart();

		var seedRef = new Dictionary<Color, Vector2Int>();
        var floodQ = new Queue<Vector2Int>();
        foreach(var s in seedList){
			var c = getColor();
			texCol[s.x + (s.y * mapSize.x)] = c;
			seedRef.Add(c, s);
			//cellRef.Add(c, new MapCell(c, s));
            floodQ.Enqueue(s);
        }
		UnityEngine.Debug.Log($"Seeds Generated In: {sw.Elapsed}");
		sw.Restart();
        
        while(floodQ.Count > 0)
        {
            var cQ = floodQ.Dequeue();
			var cI = cQ.x + (cQ.y * mapSize.x);
			var cC = texCol[cI];
			if((cQ.x+1) <= (mapSize.x-1)){
				var right = new Vector2Int(cQ.x+1, cQ.y);
				var rI = right.x + (right.y * mapSize.x);
				var rC = texCol[rI];
				if(rC == baseCol){
					texCol[rI] = cC;
					floodQ.Enqueue(right);
				}
				else{
					var oPS = seedRef[rC];
					var tPS = seedRef[rC];
					if(GetDistance(tPS, right) < GetDistance(oPS, right)){
						texCol[rI] = cC;
						floodQ.Enqueue(right);
					}
					// seedRef[rC].neighbors.Add(seedRef[cC]);
					// seedRef[cC].neighbors.Add(seedRef[rC]);
				}
			}

			if((cQ.x-1) >= 0){
				var left = new Vector2Int(cQ.x-1, cQ.y);
				var lI = left.x + (left.y * mapSize.x);
				var lC = texCol[lI];
				if(lC == baseCol){
					texCol[lI] = cC;
					floodQ.Enqueue(left);
				}
				else{
					var oPS = seedRef[lC];
					var tPS = seedRef[lC];
					if(GetDistance(tPS, left) < GetDistance(oPS, left)){
						texCol[lI] = cC;
						floodQ.Enqueue(left);
					}
					//seedRef[lC].neighbors.Add(seedRef[cC]);
					//seedRef[cC].neighbors.Add(seedRef[lC]);
				}
			}
			if((cQ.y+1) <= (mapSize.y-1)){
				var up = new Vector2Int(cQ.x, cQ.y+1);
				var uI = up.x + (up.y * mapSize.x);
				var uC = texCol[uI];
				if(uC == baseCol){
					texCol[uI] = cC;
					floodQ.Enqueue(up);
				}
				else{
					var oPS = seedRef[uC];
					var tPS = seedRef[cC];
					if(GetDistance(tPS, up) < GetDistance(oPS, up)){
						texCol[uI] = cC;
						floodQ.Enqueue(up);
					}
					//seedRef[uC].neighbors.Add(seedRef[cC]);
					//seedRef[cC].neighbors.Add(seedRef[uC]);
				}
			}
			if((cQ.y-1) >= 0){
				var dwn = new Vector2Int(cQ.x, cQ.y-1);
				var dI = dwn.x + (dwn.y * mapSize.x);
				var dC = texCol[dI];
				if(dC == baseCol){
					texCol[dI] = cC;
					//seedRef[cC].pixs.Add(dwn);
					floodQ.Enqueue(dwn);
				}
				else{
					var oPS = seedRef[dC];
					var tPS = seedRef[cC];
					if(GetDistance(tPS, dwn) < GetDistance(oPS, dwn)){
						texCol[dI] = cC;
						floodQ.Enqueue(dwn);
					}
					//seedRef[dC].neighbors.Add(seedRef[cC]);
					//seedRef[cC].neighbors.Add(seedRef[dC]);
				}
			}
        }
		tex.SetPixels(texCol);
		tex.Apply();
		quad.GetComponent<Renderer>().material.mainTexture = tex;
		UnityEngine.Debug.Log($"Flood Time: {sw.Elapsed}");
		UnityEngine.Debug.Log($"Total Run Time: {swTotal.Elapsed} with {colors.Count} cells generated.");
    }

	async void CreateCellsHostAsync(){
		

		List<Task> tasks = new List<Task>();
        for(int i = 0; i < texCol.Length; i++)
        {
            tasks.Add(Task.Run(() => CreateCell(i, texCol[i])));
        }
        await Task.WhenAll(tasks);
	}

	void CreateCell(int index, Color color){
		
		var pos = new Vector2Int(index/mapSize.x, index%mapSize.x);
	}


	float GetDistance(Vector2Int pt1, Vector2Int pt2){
		return ((pt1.x - pt2.x) * (pt1.x - pt2.x)) + ((pt1.y - pt2.y) * (pt1.y - pt2.y));
	}

    public static List<Vector2Int> GeneratePoints(Vector2 sampleRegionSize, float radius = 10, int numSamplesBeforeRejection = 15) {
		float cellSize = radius/Mathf.Sqrt(2);

		int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x/cellSize), Mathf.CeilToInt(sampleRegionSize.y/cellSize)];
		List<Vector2Int> points = new List<Vector2Int>();
		List<Vector2> spawnPoints = new List<Vector2>();

		spawnPoints.Add(sampleRegionSize/2);
		while (spawnPoints.Count > 0) {
			int spawnIndex = Random.Range(0,spawnPoints.Count);
			Vector2 spawnCentre = spawnPoints[spawnIndex];
			bool candidateAccepted = false;

			for (int i = 0; i < numSamplesBeforeRejection; i++)
			{
				float angle = Random.value * Mathf.PI * 2;
				Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				Vector2Int candidate = Vector2Int.RoundToInt(spawnCentre + dir * Random.Range(radius, 2*radius));
				if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid)) {
					points.Add(candidate);
					spawnPoints.Add(candidate);
					grid[(int)(candidate.x/cellSize),(int)(candidate.y/cellSize)] = points.Count;
					candidateAccepted = true;
					break;
				}
			}
			if (!candidateAccepted) {
				spawnPoints.RemoveAt(spawnIndex);
			}

		}

		return points;
	}

	static bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius, List<Vector2Int> points, int[,] grid) {
		if (candidate.x >=0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y) {
			int cellX = (int)(candidate.x/cellSize);
			int cellY = (int)(candidate.y/cellSize);
			int searchStartX = Mathf.Max(0,cellX -2);
			int searchEndX = Mathf.Min(cellX+2,grid.GetLength(0)-1);
			int searchStartY = Mathf.Max(0,cellY -2);
			int searchEndY = Mathf.Min(cellY+2,grid.GetLength(1)-1);

			for (int x = searchStartX; x <= searchEndX; x++) {
				for (int y = searchStartY; y <= searchEndY; y++) {
					int pointIndex = grid[x,y]-1;
					if (pointIndex != -1) {
						float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
						if (sqrDst < radius*radius) {
							return false;
						}
					}
				}
			}
			return true;
		}
		return false;
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
