using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    [SerializeField] Renderer textureRenderer;
    [SerializeField] MeshFilter meshFilter;
    [SerializeField] MeshRenderer meshRenderer;

    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

    public void DrawObject(MapData mapData, GameObject[] rocks)
    {
        for (int i = 0; i < rocks.Length; i++)
        {
            for (int j = 0; j < mapData.prefabPos[rocks[i]].Count; j++)
            {
                GameObject track = Instantiate(rocks[i]);
                track.transform.SetParent(GameObject.Find("Mesh").transform, true);
                track.transform.position = mapData.prefabPos[rocks[i]][j];
            }
        }
    }
}
