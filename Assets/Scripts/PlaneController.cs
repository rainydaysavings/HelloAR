using System;
using System.Collections.Generic;
using TMPro;
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
    private const string CristianoRonaldoString = "Cristiano Ronaldo";
    private const float MinFingerDistance = 0.1f; // Add a minimum distance to prevent accidental scale

    private GameObject _CristianoRonaldo;
    private GameObject _Gun;
    
    private readonly List<ARRaycastHit> mHits = new();
    private GameObject _instantiatedPrefab;
    private Camera _arCamera;
    private ARTrackedImageManager _TrackedImageManager;
    private ARPlaneManager _PlaneManager;
    private ARRaycastManager _RaycastManager;
    private GameObject _arPlane;
    private GameObject PlaneSetupManager;
    private Material occlusionMaterial;
    private Material planeMaterial;
    private bool disableMovement; // To control wether or not the object should be moved upon TouchPhase.Moved

    private float initialFingerDistance; // Allows to compute the scaling factor
    private Vector3 initialScale;
    private GameObject prefabToSpawn;
    private GameObject tutorialHintText; // Used to remove hint text upon asset spawn
    
    private void Awake()
    {
        _RaycastManager = GetComponent<ARRaycastManager>() ?? throw new ArgumentNullException("ARRaycastManager not found");
        _PlaneManager = GetComponent<ARPlaneManager>() ?? throw new ArgumentNullException("ARPlaneManager not found");
        _arPlane = _PlaneManager.planePrefab;
        
        occlusionMaterial = Resources.Load("OcclusionMaterial") as Material;
        planeMaterial = Resources.Load("PlaneMat") as Material;
        
        tutorialHintText = GameObject.FindWithTag("Tutorial") ?? throw new ArgumentNullException("Tutorial not found");
        
        _arCamera = GameObject.Find("Main Camera").GetComponent<Camera>() ?? throw new ArgumentNullException("Main Camera not found");
        _CristianoRonaldo = Resources.Load("Prefabs/CristianoRonaldoBust") as GameObject;
        _Gun = Resources.Load("Prefabs/Gun") as GameObject;
    }

    /// <summary>
    ///     Initializes variables and sets up the AR environment.
    /// </summary>
    private void Start()
    {
        prefabToSpawn = GameState.selectedPrefab.Equals(CristianoRonaldoString) ? _CristianoRonaldo : _Gun;
        ResetAREnvironment();
    }

    /// <summary>
    ///     Handles touch inputs for both single and multi-touch.
    ///     Delegates method call depending on how many touch points are on the touchscreen
    /// </summary>
    private void Update()
    {
        if (Input.touchCount > 1 && _instantiatedPrefab)
            HandleTwoFingerTouch();
        else
            HandleSingleFingerTouch();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    ///     Broadcast the scene has been disabled
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DestroyImmediate(this);
    }

    /// <summary>
    ///     Broadcast the scene has been loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    ///     Makes sure both the arPlaneManager, and trackables are viewable
    ///     SpawnPrefab disables the former, and makes the latter transparent upon object placement
    /// </summary>
    public void ResetAREnvironment()
    {
        tutorialHintText.GetComponent<TextMeshProUGUI>().enabled = true;
        SetPlaneMaterial();
        disableMovement = false;
        _PlaneManager.enabled = true;
        _RaycastManager.enabled = true;
    }

    public void SetOcclusionMaterial()
    {
        _arPlane.GetComponent<MeshRenderer>().material = occlusionMaterial;
        foreach (var plane in _PlaneManager.trackables)
        {
            plane.GetComponent<MeshRenderer>().material = occlusionMaterial;
        }
    }
    
    public void SetPlaneMaterial()
    {
        _arPlane.GetComponent<MeshRenderer>().material = planeMaterial;
        foreach (var plane in _PlaneManager.trackables)
        {
            plane.GetComponent<MeshRenderer>().material = planeMaterial;
        }
    }

    /// <summary>
    ///     Handles single-finger touch interactions.
    ///     Allows for object movement
    /// </summary>
    private void HandleSingleFingerTouch()
    {
        if (Input.touchCount == 0) return;

        var touch = Input.GetTouch(0);
        var ray = _arCamera.ScreenPointToRay(touch.position);
        if (!PerformRaycast(touch.position)) return;

        switch (touch.phase)
        {
            case TouchPhase.Began when !_instantiatedPrefab:
                HandleTouchBegin(ray);
                break;
            case TouchPhase.Moved when _instantiatedPrefab:
                HandleTouchMove();
                break;
            case TouchPhase.Ended when _instantiatedPrefab:
                disableMovement = true;
                break;
            case TouchPhase.Stationary:
                break;
            case TouchPhase.Canceled:
                break;
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
            initialScale = _instantiatedPrefab.transform.localScale;
        }
        else if ((touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved) && _instantiatedPrefab.transform.localScale.magnitude > 0.1f)
        {
            currentFingerDistance = Vector2.Distance(touch1.position, touch2.position);
            var scaleFactor = currentFingerDistance / initialFingerDistance;
            _instantiatedPrefab.transform.localScale = initialScale * scaleFactor;
        }
    }

    /// <summary>
    ///     Performs raycast to detect AR planes.
    /// </summary>
    /// <param name="touchPosition">The 2D position of the touch.</param>
    /// <returns>True if a hit is found, otherwise false.</returns>
    private bool PerformRaycast(Vector2 touchPosition)
    {
        return _RaycastManager.Raycast(touchPosition, mHits, TrackableType.PlaneWithinPolygon);
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
        var direction = _arCamera.transform.position - position;
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
            _instantiatedPrefab.transform.position = mHits[0].pose.position;
            _instantiatedPrefab.transform.rotation = GetHorizontalUpRotation(mHits[0].pose.position);
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
        // Make sure we only have one instance of the object
        _instantiatedPrefab = Instantiate(prefabToSpawn, spawnPosition, rotation);

        // Disable hint, given the player knows how to do it
        tutorialHintText.GetComponent<TextMeshProUGUI>().enabled = false;

        // Change plane material
        SetOcclusionMaterial();
    }
}