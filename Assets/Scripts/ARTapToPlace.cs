using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
///     Manages the spawning and interaction of AR objects.
///     - Allows for spawning an object once, and scale it.
///     - Disallows further spawns and plane detection, while maintaining existing planes
///     - Existing planes with different heights may occlude objects (see: OcclusionMaterial in ARFeatheredPlane prefab)
/// </summary>
[RequireComponent(typeof(ARPlaneManager))]
[RequireComponent(typeof(ARRaycastManager))]
public class SpawnableManager : MonoBehaviour
{
    [SerializeField] private GameObject spawnablePrefab1;
    [SerializeField] private GameObject spawnablePrefab2;
    
    private const string CristianoRonaldo = "Cristiano Ronaldo";
    private const float MinFingerDistance = 0.1f; // Add a minimum distance to prevent accidental scale
    
    private readonly List<ARRaycastHit> mHits = new();
    private ARRaycastManager arRaycastManager;
    private Camera arCam;
    private ARPlaneManager arPlaneManager;
    private bool disableMovement; // To control wether or not the object should be moved upon TouchPhase.Moved

    private float initialFingerDistance; // Allows to compute the scaling factor
    private Vector3 initialScale;
    private GameObject spawnedObject;
    private GameObject tutorialHintText; // Used to remove hint text upon asset spawn
    
    /// <summary>
    ///     Resets the AR Environment, so that the modified ARPlaneManager component behaves correctly
    /// </summary>
    private void Awake()
    {
        arCam = GameObject.Find("Main Camera").GetComponent<Camera>() ?? throw new ArgumentNullException("Main Camera not found");
        arRaycastManager = GetComponent<ARRaycastManager>() ?? throw new ArgumentNullException("ARRaycastManager not found");
        arPlaneManager = GetComponent<ARPlaneManager>() ?? throw new ArgumentNullException("ARPlaneManager not found");
        tutorialHintText = GameObject.FindWithTag("Tutorial") ?? throw new ArgumentNullException("Tutorial not found");
        disableMovement = false;
        ResetAREnvironment();
    }

    /// <summary>
    ///     Initializes variables and sets up the AR environment.
    /// </summary>
    private void Start()
    {
        arCam = GameObject.Find("Main Camera").GetComponent<Camera>();
        arRaycastManager = GameObject.Find("XR Origin").GetComponent<ARRaycastManager>();
        tutorialHintText = GameObject.FindWithTag("Tutorial");
    }

    /// <summary>
    ///     Handles touch inputs for both single and multi-touch.
    ///     Delegates method call depending on how many touch points are on the touchscreen
    /// </summary>
    private void Update()
    {
        if (Input.touchCount > 1 && spawnedObject)
            HandleTwoFingerTouch();
        else
            HandleSingleFingerTouch();
    }
    
    /// <summary>
    ///     Making sure previously instanced prefabs are utterly destroyed before instancing one again
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        OnDestroyPrefab();
    }
    
    /// <summary>
    ///     Broadcast the scene has been disabled
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    ///     Only destroy what has been created...
    /// </summary>
    private void OnDestroyPrefab()
    {
        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
        }
    }

    /// <summary>
    ///     Makes sure both the arPlaneManager, and trackables are viewable
    ///     SpawnPrefab disables the former, and makes the latter transparent upon object placement
    /// </summary>
    public void ResetAREnvironment()
    {
        arPlaneManager.enabled = true;
        foreach (var plane in arPlaneManager.trackables)
            plane.gameObject.GetComponent<MeshRenderer>().materials[0].color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
    }


    /// <summary>
    ///     Handles single-finger touch interactions.
    ///     Allows for object movement
    /// </summary>
    private void HandleSingleFingerTouch()
    {
        if (Input.touchCount == 0) return;

        var touch = Input.GetTouch(0);
        var ray = arCam.ScreenPointToRay(touch.position);
        if (!PerformRaycast(touch.position)) return;

        switch (touch.phase)
        {
            case TouchPhase.Began when !spawnedObject:
                HandleTouchBegin(ray);
                break;
            case TouchPhase.Moved when spawnedObject:
                HandleTouchMove();
                break;
            case TouchPhase.Ended when spawnedObject:
                disableMovement = true;
                break;
            case TouchPhase.Stationary:
                break;
            case TouchPhase.Canceled:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    ///     Handles two-finger touch for scaling objects.
    /// </summary>
    private void HandleTwoFingerTouch()
    {
        var touch1 = Input.GetTouch(0);
        var touch2 = Input.GetTouch(1);
        
        var currentFingerDistance = Vector2.Distance(touch1.position, touch2.position);
        if (currentFingerDistance < MinFingerDistance) return; // Prevent accidental scale
        
        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
        {
            initialFingerDistance = Vector2.Distance(touch1.position, touch2.position);
            initialScale = spawnedObject.transform.localScale;
        }
        else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            currentFingerDistance = Vector2.Distance(touch1.position, touch2.position);
            var scaleFactor = currentFingerDistance / initialFingerDistance;
            spawnedObject.transform.localScale = initialScale * scaleFactor;
        }
    }

    /// <summary>
    ///     Performs raycast to detect AR planes.
    /// </summary>
    /// <param name="touchPosition">The 2D position of the touch.</param>
    /// <returns>True if a hit is found, otherwise false.</returns>
    private bool PerformRaycast(Vector2 touchPosition)
    {
        return arRaycastManager.Raycast(touchPosition, mHits, TrackableType.PlaneWithinPolygon);
    }

    /// <summary>
    ///     Handles the start of a touch event.
    /// </summary>
    /// <param name="ray">Ray from the camera to the touch point.</param>
    private void HandleTouchBegin(Ray ray)
    {
        var hitPose = mHits[0].pose;
        SpawnPrefab(hitPose.position, GetHorizontalUpRotation(hitPose.position));
    }

    /// <summary>
    ///     Calculates rotation to align object with the horizontal plane.
    /// </summary>
    /// <param name="position">The 3D position for rotation calculation.</param>
    /// <returns>The calculated rotation.</returns>
    private Quaternion GetHorizontalUpRotation(Vector3 position)
    {
        var direction = arCam.transform.position - position;
        direction.y = 0;
        return Quaternion.LookRotation(direction);
    }

    /// <summary>
    ///     Handles object movement during a touch event.
    ///     Movement is disabled upon first touch has ended, we need to check for that.
    /// </summary>
    private void HandleTouchMove()
    {
        if (!disableMovement)
        {
            spawnedObject.transform.position = mHits[0].pose.position;
            spawnedObject.transform.rotation = GetHorizontalUpRotation(mHits[0].pose.position);
        }
    }

    /// <summary>
    ///     Instantiates a prefab at a given position and rotation.
    ///     arPlaneManager is disabled to prevent existing planes from changing position.
    ///     arPlaneManager's planes' material at index 0 is made invisible to impede any distraction from the experience
    /// </summary>
    /// <param name="spawnPosition">The 3D position to spawn the prefab.</param>
    /// <param name="rotation">The rotation for the spawned object.</param>
    private void SpawnPrefab(Vector3 spawnPosition, Quaternion rotation)
    {
        spawnedObject = Instantiate(GameState.selectedPrefab.Equals("Cristiano Ronaldo") ? spawnablePrefab1 : spawnablePrefab2, spawnPosition, rotation);

        Destroy(tutorialHintText);

        arPlaneManager.enabled = false;
        foreach (var plane in arPlaneManager.trackables)
            plane.gameObject.GetComponent<MeshRenderer>().materials[0].color = Color.clear;
    }
}