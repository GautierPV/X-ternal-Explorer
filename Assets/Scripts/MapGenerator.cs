using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] enum DrawMode { NoiseMap, ColorMap, Mesh, FalloffMap, Object};
    [SerializeField] DrawMode drawMode;

    public Noise.NormalizeMode normalizeMode;

    public bool useFlatShading;

    [Range(0,6)]
    [SerializeField] int editorPreviewLOD;
    [SerializeField] float noiseScale;

    [SerializeField] int octaves;
    [Range(0,1)]
    [SerializeField] float persistance;
    [SerializeField] float lacunarity;

    [SerializeField] int seed;
    [SerializeField] Vector2 offset;

    public bool useFalloff;

    [SerializeField] float meshHeighMultiplier;
    [SerializeField] AnimationCurve meshHeightCurve;

    [SerializeField] public bool autoUpdate;
    
    [SerializeField] TerrainType[] regions;

    [SerializeField] GameObject[] rocks;
    [SerializeField] GameObject[] trees;

    float[,] falloffMap;
    static MapGenerator instance;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Awake()
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunckSize);
    }

    public static int mapChunckSize
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MapGenerator>();
            }
            if (instance.useFlatShading)
            {
                return 95;
            }
            else
            {
                return 239;
            }
        }
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunckSize, mapChunckSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeighMultiplier, meshHeightCurve, editorPreviewLOD, useFlatShading), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunckSize, mapChunckSize));
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunckSize)));
        }
        else if(drawMode == DrawMode.Object)
        {
            
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock(mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeighMultiplier, meshHeightCurve, lod, useFlatShading);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if(mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunckSize + 2, mapChunckSize + 2, seed, noiseScale, octaves, persistance, lacunarity, center + offset, normalizeMode);

        Color[] colorMap = new Color[mapChunckSize * mapChunckSize];

        Dictionary<GameObject, List<Vector2>> prefabPos = new Dictionary<GameObject, List<Vector2>>();
        for (int y = 0; y < mapChunckSize; y++)
        {
            for (int x = 0; x < mapChunckSize; x++)
            {
                if(useFalloff)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }

                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                {
                    if(currentHeight >= regions[i].height)
                    {
                        colorMap[y * mapChunckSize + x] = regions[i].color;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        prefabPos = GeneratePrefabPos(noiseMap);

        return new MapData(noiseMap, colorMap, prefabPos);
    }

    Dictionary<GameObject, List<Vector2>> GeneratePrefabPos(float[,] noiseMap)
    {
        Dictionary<GameObject, List<Vector2>> prefabPos = new Dictionary<GameObject, List<Vector2>>();

        for (int i = 0; i < rocks.Length; i++)
        {
            prefabPos.Add(rocks[i], new List<Vector2>());
        }

        for (int y = 5; y < mapChunckSize - 5; y+=5)
        {
            for (int x = 5; x < mapChunckSize - 5; x+=5)
            {
                System.Random prng = new System.Random();
                float rand = (float)prng.NextDouble();
                
                float chanceTree = (float)(0.2 * (1.3 - noiseMap[x, y]));

                int randPrefab = prng.Next(3);

                if (rand < 0.75)
                {
                    if(0.75 < rand && rand <(0.75+chanceTree))
                    {

                    }
                    else if (0.75 + chanceTree < rand)
                    {
                        prefabPos[rocks[randPrefab]].Add(new Vector2(x,y));
                    }
                }
            }
        }

        return prefabPos;
    }

    private void OnValidate()
    {
        if(lacunarity < 1)
        {
            lacunarity = 1;
        }
        if(octaves < 0)
        {
            octaves = 0;
        }

        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunckSize);
    }

    struct MapThreadInfo<T>
    {
        public  readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;
    public readonly Dictionary<GameObject, List<Vector2>> prefabPos;

    public MapData(float [,] heightMap, Color[] colorMap, Dictionary<GameObject, List<Vector2>> prefabPos)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
        this.prefabPos = prefabPos;
    }
}
