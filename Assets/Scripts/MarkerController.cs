using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class MarkerController : MonoBehaviour
{
    [SerializeField] private string _CristianoRonaldoString = "Cristiano Ronaldo";
    [SerializeField] private float _MinFingerDistance = 0.1f;
    [SerializeField] private float _settleTime = 1.0f;
    
    [SerializeField] private GameObject _instantiatedPrefab;
    [SerializeField] private bool _allowSpawn;
    [SerializeField] private bool _objectSpawned;
    [SerializeField] private bool _rotating;

    [SerializeField] private float _initialFingerDistance;
    [SerializeField] private Vector3 _initialScale;
    [SerializeField] private Vector2 _lastTouchPosition;
    [SerializeField] private Vector3 _latestScale;
    [SerializeField] private Vector3 _position;
    [SerializeField] private GameObject _prefabToSpawn;
    [SerializeField] private Quaternion _rotation;
    [SerializeField] private Vector2 _touchEndPos;
    [SerializeField] private Vector2 _touchStartPos;

    [SerializeField] private Camera _arCamera;
    [SerializeField] private XRReferenceImageLibrary _referenceImageLibrary = null;
    [SerializeField] private ARTrackedImageManager _TrackedImageManager = null;

    [SerializeField] private List<GameObject> models;
    private void Awake()
    {
        _allowSpawn = true;
        _objectSpawned = false;
        _rotating = false;
        _latestScale = new Vector3(1.0f, 1.0f, 1.0f);
        _position = new Vector3(0.0f, 0.0f, 0.0f);
        
        _referenceImageLibrary = Resources.Load("ReferenceImageLibrary") as XRReferenceImageLibrary;
        _prefabToSpawn = GameState.selectedPrefab.Equals(_CristianoRonaldoString) ? models[0] : models[1];
        _arCamera = GameObject.Find("Main Camera").GetComponent<Camera>();

        _TrackedImageManager = GetComponent<ARTrackedImageManager>();
        _TrackedImageManager.enabled = false;
        _TrackedImageManager.referenceLibrary = _TrackedImageManager.CreateRuntimeLibrary(_referenceImageLibrary);
        _TrackedImageManager.enabled = true;
    }

    private void Update()
    {
        if (_instantiatedPrefab && Input.touchCount == 2) // Scaling
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
            else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
            {
                var scaleFactor = currentFingerDistance / _initialFingerDistance;
                var potentiallyResultingScale = _initialScale * scaleFactor;
                if (potentiallyResultingScale.x > 0.25)
                {
                    _latestScale = _initialScale * scaleFactor;
                    _instantiatedPrefab.transform.localScale = _initialScale * scaleFactor;
                }
            }
        }
        else if (_instantiatedPrefab && Input.touchCount == 1) // Rotation
        {
            var touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                _lastTouchPosition = touch.position;
                _rotating = true;
            }
            else if (touch.phase == TouchPhase.Moved && _rotating)
            {
                var newTouchPosition = touch.position;
                var delta = newTouchPosition - _lastTouchPosition;

                var rotationAmount = delta.x * 0.25f;
                _instantiatedPrefab.transform.Rotate(Vector3.up, -rotationAmount);

                _lastTouchPosition = newTouchPosition;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                _rotating = false;
            }
        }
    }
    
    private void OnEnable()
    {
        _TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        _TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }
    
    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImg in eventArgs.added.Concat(eventArgs.updated))
            switch (trackedImg.trackingState)
            {
                case TrackingState.Tracking when !_allowSpawn:
                    continue;
                case TrackingState.Tracking:
                    _allowSpawn = false;
                    StartCoroutine(PlaceObjectAfterDelay(trackedImg.transform));
                    break;
                case TrackingState.Limited: // Only update position
                    StartCoroutine(PlaceObjectAfterDelay(trackedImg.transform));
                    break;
                case TrackingState.None:
                    _allowSpawn = true;
                    break;
                default:
                    _allowSpawn = true;
                    break;
            }
    }

    private IEnumerator PlaceObjectAfterDelay(Transform transform)
    {
        yield return new WaitForSeconds(_settleTime);

        _rotation = GetRotation(transform);

        if (_objectSpawned) // Only update position, tracking is limited, perhaps
        {
            _instantiatedPrefab.transform.position = transform.position;
            _instantiatedPrefab.transform.localScale = _latestScale;
        }
        else
        {
            _instantiatedPrefab = Instantiate(_prefabToSpawn, transform.position, _rotation);
            _objectSpawned = true;
        }
    }

    private Quaternion GetRotation(Transform transform)
    {
        var toCamera = _arCamera.transform.position - transform.position;
        toCamera.y = 0; // Zero out the y-component to ignore height differences
        return Quaternion.LookRotation(toCamera);
    }
}