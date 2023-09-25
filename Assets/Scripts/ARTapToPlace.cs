using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SpawnableManager : MonoBehaviour
{
    [SerializeField]
    ARRaycastManager m_RaycastManager;
    List<ARRaycastHit> m_Hits = new List<ARRaycastHit>();
    [SerializeField]
    GameObject spawnablePrefab1;
    [SerializeField]
    GameObject spawnablePrefab2;
    Camera arCam;
    GameObject spawnedObject;
    GameObject gameController;
    ARPlaneManager arPlaneManager;
    private GameObject tutorialHintText;
    private bool spawningEnabled = true;
    
    // Start is called before the first frame update
    void Start()
    {
        spawnedObject = null;
        arCam = GameObject.Find("Main Camera").GetComponent<Camera>();
        gameController = GameObject.Find("GameController");
        tutorialHintText = GameObject.FindWithTag("Tutorial");
        arPlaneManager = GetComponent<ARPlaneManager>();
        
        arPlaneManager.enabled = true;
        arPlaneManager.planePrefab.GetComponent<MeshRenderer>().materials[0].color = new Color(1.0f,1.0f,1.0f,0.66f);
        foreach (var plane in arPlaneManager.trackables)
        {
            plane.gameObject.GetComponent<MeshRenderer>().materials[0].color = new Color(1.0f,1.0f,1.0f,0.66f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!spawningEnabled || Input.touchCount == 0)
            return;
        
        HandleTouch();
    }
    
    void HandleTouch()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        Ray ray = arCam.ScreenPointToRay(touch.position);

        if (!PerformRaycast(touch.position)) return;

        if (touch.phase == TouchPhase.Began)
        {
            HandleTouchBegin(ray);
        }
        else if (touch.phase == TouchPhase.Moved)
        {
            HandleTouchMove();
        }
        else if (touch.phase == TouchPhase.Ended)
        {
            HandleTouchEnd();
        }
    }

    bool PerformRaycast(Vector2 touchPosition)
    {
        return m_RaycastManager.Raycast(touchPosition, m_Hits, TrackableType.PlaneWithinPolygon);
    }

    void HandleTouchBegin(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.CompareTag("Spawnable"))
            {
                spawnedObject = hit.collider.gameObject;
                return;
            }
        }

        Pose hitPose = m_Hits[0].pose;
        if (arPlaneManager.GetPlane(m_Hits[0].trackableId).alignment == PlaneAlignment.HorizontalUp)
        {
            SpawnPrefab(hitPose.position, GetHorizontalUpRotation(hitPose.position));
        }
        else
        {
            SpawnPrefab(hitPose.position, hitPose.rotation);
        }
    }

    Quaternion GetHorizontalUpRotation(Vector3 position)
    {
        Vector3 direction = arCam.transform.position - position;
        direction.y = 0;  // Constrain rotation to Y-axis
        return Quaternion.LookRotation(direction);
    }

    void HandleTouchMove()
    {
        if (spawnedObject == null) return;
        spawnedObject.transform.position = m_Hits[0].pose.position;
        spawnedObject.transform.rotation = m_Hits[0].pose.rotation;
    }

    void HandleTouchEnd()
    {
        spawnedObject = null;
    }

    private void SpawnPrefab(Vector3 spawnPosition, Quaternion rotation)
    {
        // Select the required mesh
        if (GameState.selectedPrefab.Equals("Cristiano Ronaldo"))
        {
            spawnedObject = Instantiate(spawnablePrefab1, spawnPosition, Quaternion.identity);
            spawnedObject.transform.rotation = rotation;
        }
        else
        {
            spawnedObject = Instantiate(spawnablePrefab2, spawnPosition, Quaternion.identity);
            spawnedObject.transform.rotation = rotation;
        }
        
        // We think the player understands now...
        GameObject.Destroy(tutorialHintText);
        
        // Prevent further spawns
        spawningEnabled = false;
        arPlaneManager.enabled = false;
        arPlaneManager.planePrefab.GetComponent<MeshRenderer>().materials[0].color = Color.clear;
        foreach (var plane in arPlaneManager.trackables)
        {
            plane.gameObject.GetComponent<MeshRenderer>().materials[0].color = Color.clear;
        }
    }
}