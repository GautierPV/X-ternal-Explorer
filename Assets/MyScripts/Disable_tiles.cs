using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Disable_tiles : MonoBehaviour
{
    // Calculate the planes from the main camera's view frustum
    Plane[] planes;
    
    // Start is called before the first frame update
    void Start()
    {
        planes= GeometryUtility.CalculateFrustumPlanes(Camera.main);
    }

    // Update is called once per frame
    void Update()
    {
        foreach(Transform children in transform)
        {
            var childMR=children.gameObject.GetComponent<MeshRenderer>();
            
            if(GeometryUtility.TestPlanesAABB(planes, children.GetComponent<BoxCollider>().bounds))
            {
                childMR.enabled=true;
            }
            else
            {
                childMR.enabled=false;
            }
        }
        
        planes= GeometryUtility.CalculateFrustumPlanes(Camera.main);
    }
}
