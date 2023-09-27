using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class SceneBuilder : MonoBehaviour
{
    GameObject xrOriginGameObject;
    GameObject arSessionGameObject;
    
    // Start is called before the first frame update
    void Start()
    {
        if (GameState.modeSelected.Equals("Marker"))
        {
            xrOriginGameObject = Resources.Load("Prefabs/XR Origin Marker") as GameObject;
        }
        else
        {
            xrOriginGameObject = Resources.Load("Prefabs/XR Origin Plane") as GameObject;
        }
        
        arSessionGameObject = Resources.Load("Prefabs/AR Session") as GameObject;
        Instantiate(xrOriginGameObject);
        Instantiate(arSessionGameObject);
    }

    private void OnDisable()
    {
        xrOriginGameObject.SetActive(false);
        arSessionGameObject.SetActive(false);
    }
    
    private void OnEnable()
    {
        arSessionGameObject.GetComponent<ARSession>().Reset();
        xrOriginGameObject.SetActive(true);
        arSessionGameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
