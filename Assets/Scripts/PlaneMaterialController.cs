using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PlaneMaterialController : MonoBehaviour
{
    [SerializeField] private Material occlusionMaterial;
    [SerializeField] private Material planeMaterial;
    private XROrigin xrOrigin;
    private ARPlaneManager planeManager;
    private GameObject planePrefab;
    
    public void Setup()
    {
        planeManager = GameObject.FindWithTag("ARSessionOrigin").GetComponent<ARPlaneManager>();
        planePrefab =  GameObject.FindWithTag("ARPlanePrefab");
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
