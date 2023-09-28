using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private readonly List<ARRaycastHit> mHits = new();

    [SerializeField] private string _CristianoRonaldoString = "Cristiano Ronaldo";
    [SerializeField] private float _MinFingerDistance = 0.1f;
    [SerializeField] private float _initialFingerDistance;
    
    [SerializeField] private GameObject _instantiatedPrefab;
    [SerializeField] private Camera _arCamera;
    [SerializeField] private GameObject _arPlane;
    [SerializeField] private bool _disableMovement;

    [SerializeField] private Vector3 _initialScale;
    [SerializeField] private Material _occlusionMaterial;
    [SerializeField] private ARPlaneManager _PlaneManager;
    [SerializeField] private Material _planeMaterial;
    [SerializeField] private ARRaycastManager _RaycastManager;
    [SerializeField] private GameObject _tutorialHintText;

    [SerializeField] private List<GameObject> models;

    private GameObject _prefabToSpawn;
    private void Awake()
    {
        _RaycastManager = GetComponent<ARRaycastManager>();
        _PlaneManager = GetComponent<ARPlaneManager>();
        _arCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        _arPlane = _PlaneManager.planePrefab;
        _disableMovement = false;
        
        _prefabToSpawn = GameState.selectedPrefab.Equals(_CristianoRonaldoString) ? models[0] : models[1];
        SetPlaneMaterial();
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

    private void SetOcclusionMaterial()
    {
        _arPlane.GetComponent<MeshRenderer>().material = _occlusionMaterial;
        foreach (var plane in _PlaneManager.trackables) plane.GetComponent<MeshRenderer>().material = _occlusionMaterial;
    }

    private void SetPlaneMaterial()
    {
        _arPlane.GetComponent<MeshRenderer>().material = _planeMaterial;
        foreach (var plane in _PlaneManager.trackables) plane.GetComponent<MeshRenderer>().material = _planeMaterial;
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
                _disableMovement = true;
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
        if (currentFingerDistance < _MinFingerDistance) return; // Prevent accidental scale

        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
        {
            _initialFingerDistance = Vector2.Distance(touch1.position, touch2.position);
            _initialScale = _instantiatedPrefab.transform.localScale;
        }
        else if ((touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved) &&
                 _instantiatedPrefab.transform.localScale.magnitude > 0.1f)
        {
            currentFingerDistance = Vector2.Distance(touch1.position, touch2.position);
            var scaleFactor = currentFingerDistance / _initialFingerDistance;
            _instantiatedPrefab.transform.localScale = _initialScale * scaleFactor;
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
        if (!_disableMovement)
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
        _instantiatedPrefab = Instantiate(_prefabToSpawn, spawnPosition, rotation);

        // Disable hint, given the player knows how to do it
        _tutorialHintText.GetComponent<TextMeshProUGUI>().enabled = false;

        // Change plane material
        SetOcclusionMaterial();
    }
}