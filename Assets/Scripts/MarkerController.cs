using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class MarkerController : MonoBehaviour
{
    private const string CristianoRonaldoString = "Cristiano Ronaldo";
    private const string GunString = "Gun";
    private const float MinFingerDistance = 0.1f; // Add a minimum distance to prevent accidental scale

    private ARTrackedImageManager _TrackedImageManager;
    private Camera _arCamera;

    private GameObject _CristianoRonaldo;
    private GameObject _Gun;
    private GameObject _instantiatedPrefab;
    private XRReferenceImageLibrary _referenceImageLibrary;

    private readonly float settleTime = 1.0f;
    private bool allowSpawn;
    private float initialFingerDistance;
    private Vector3 initialScale;
    private Vector3 latestScale;
    private Vector2 lastTouchPosition;
    private Vector3 position;
    private GameObject prefabToSpawn;
    private bool objectSpawned;
    private bool rotating;
    private Quaternion rotation;
    private Vector2 touchEndPos;
    private Vector2 touchStartPos;

    private void Awake()
    {
        _arCamera = GameObject.Find("Main Camera").GetComponent<Camera>() ?? throw new ArgumentNullException("Main Camera not found");

        _referenceImageLibrary = Resources.Load("ReferenceImageLibrary") as XRReferenceImageLibrary;
        _TrackedImageManager = GetComponent<ARTrackedImageManager>() ?? throw new ArgumentNullException("ARTrackedImageManager not found");
        _TrackedImageManager.referenceLibrary = _referenceImageLibrary;
        _TrackedImageManager.enabled = true;

        _CristianoRonaldo = Resources.Load("Prefabs/CristianoRonaldoBust") as GameObject;
        _Gun = Resources.Load("Prefabs/Gun") as GameObject;
    }

    private void Start()
    {
        prefabToSpawn = GameState.selectedPrefab.Equals(CristianoRonaldoString) ? _CristianoRonaldo : _Gun;
        _Gun = Resources.Load("Prefabs/Gun") as GameObject;
        allowSpawn = true;
        objectSpawned = false;
        latestScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void Update()
    {
        if (_instantiatedPrefab && Input.touchCount == 2) // Scaling
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
            else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
            {
                var scaleFactor = currentFingerDistance / initialFingerDistance;
                var potentiallyResultingScale = initialScale * scaleFactor;
                if (potentiallyResultingScale.x > 0.25)
                {
                    latestScale = initialScale * scaleFactor;
                    _instantiatedPrefab.transform.localScale = initialScale * scaleFactor;
                }
            }
        }
        else if (_instantiatedPrefab && Input.touchCount == 1) // Rotation
        {
            var touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch.position;
                rotating = true;
            }
            else if (touch.phase == TouchPhase.Moved && rotating)
            {
                var newTouchPosition = touch.position;
                var delta = newTouchPosition - lastTouchPosition;

                var rotationAmount = delta.x * 0.25f;
                _instantiatedPrefab.transform.Rotate(Vector3.up, -rotationAmount);

                lastTouchPosition = newTouchPosition;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                rotating = false;
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe
        _TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        _TrackedImageManager.enabled = true;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe
        _TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        _TrackedImageManager.enabled = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset your state variables here
        allowSpawn = true;
        objectSpawned = false;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImg in eventArgs.added.Concat(eventArgs.updated))
            switch (trackedImg.trackingState)
            {
                case TrackingState.Tracking when !allowSpawn:
                    continue;
                case TrackingState.Tracking:
                    allowSpawn = false;
                    StartCoroutine(PlaceObjectAfterDelay(trackedImg.transform));
                    break;
                case TrackingState.Limited: // Only update position
                    StartCoroutine(PlaceObjectAfterDelay(trackedImg.transform));
                    break;
                case TrackingState.None:
                    allowSpawn = true;
                    break;
                default:
                    allowSpawn = true;
                    break;
            }
    }

    private IEnumerator PlaceObjectAfterDelay(Transform transform)
    {
        yield return new WaitForSeconds(settleTime);
        
        rotation = GetRotation(transform);

        if (objectSpawned) // Only update position, tracking is limited, perhaps
        {
            _instantiatedPrefab.transform.position = transform.position;
            _instantiatedPrefab.transform.localScale = latestScale;
        }
        else
        {
            _instantiatedPrefab = Instantiate(prefabToSpawn, transform.position, rotation);
            objectSpawned = true;
        }
    }

    private Quaternion GetRotation(Transform transform)
    {
        var toCamera = _arCamera.transform.position - transform.position;
        toCamera.y = 0; // Zero out the y-component to ignore height differences
        return Quaternion.LookRotation(toCamera);
    }
}