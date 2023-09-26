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
    [SerializeField] private GameObject spawnablePrefab1;
    [SerializeField] private GameObject spawnablePrefab2;

    private const string CristianoRonaldo = "Cristiano Ronaldo";
    private const float MinFingerDistance = 0.1f; // Add a minimum distance to prevent accidental scale

    private ARTrackedImageManager m_TrackedImageManager;
    private Camera arCamera;
    private bool allowSpawm;
    private GameObject prefabToSpawn;
    private GameObject _instantiatedPrefab;
    private Vector3 position;
    private Quaternion rotation;
    
    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private float initialFingerDistance;
    private Vector3 initialScale;
    private Vector2 lastTouchPosition;
    private bool rotating = false;
    private float settleTime = 1.0f;
    
    private void Awake()
    {
        arCamera = GameObject.Find("Main Camera").GetComponent<Camera>() ?? throw new ArgumentNullException("Main Camera not found");
        m_TrackedImageManager = GetComponent<ARTrackedImageManager>() ?? throw new ArgumentNullException("ARTrackedImageManager not found");
        allowSpawm = true;
    }

    private void Start()
    {
        prefabToSpawn = GameState.selectedPrefab.Equals(CristianoRonaldo) ? spawnablePrefab1 : spawnablePrefab2;
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
                _instantiatedPrefab.transform.localScale = initialScale * scaleFactor;
            }
        }
        else if (_instantiatedPrefab && Input.touchCount == 1) // Rotation
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch.position;
                rotating = true;
            }
            else if (touch.phase == TouchPhase.Moved && rotating)
            {
                Vector2 newTouchPosition = touch.position;
                Vector2 delta = newTouchPosition - lastTouchPosition;

                float rotationAmount = delta.x * 0.2f; // Adjust the multiplier for sensitivity
                _instantiatedPrefab.transform.Rotate(Vector3.up, -rotationAmount);

                lastTouchPosition = newTouchPosition;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                rotating = false;
            }
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        m_TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        m_TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }
    
    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImg in eventArgs.added.Concat(eventArgs.updated))
        {
            switch (trackedImg.trackingState)
            {
                case TrackingState.Tracking when !allowSpawm:
                    continue;
                case TrackingState.Tracking:
                    allowSpawm = false;
                    StartCoroutine(PlaceObjectAfterDelay(trackedImg.transform.position));
                    break;
                case TrackingState.Limited:
                    position = trackedImg.transform.position;
                    rotation = GetRotationTowardsPlayer(position);
                    prefabToSpawn.transform.position = position;
                    prefabToSpawn.transform.rotation = rotation;
                    break;
                case TrackingState.None:
                    if (_instantiatedPrefab) Destroy(_instantiatedPrefab);;
                    allowSpawm = true;
                    break;
                default:
                {
                    if (_instantiatedPrefab) Destroy(_instantiatedPrefab);;
                    allowSpawm = true;
                    break;
                }
            }
        }
    }
    
    private IEnumerator PlaceObjectAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(settleTime);
        rotation = GetRotationTowardsPlayer(position);
        _instantiatedPrefab = Instantiate(prefabToSpawn, position, rotation);
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        OnDestroyPrefab();
    }

    private void OnDestroyPrefab()
    {
        if (_instantiatedPrefab != null)
        {
            Destroy(_instantiatedPrefab);
        }
    }

    private Quaternion GetRotationTowardsPlayer(Vector3 trackedPosition)
    {
        var toCamera = arCamera.transform.position - trackedPosition;
        toCamera.y = 0; // Zero out the y-component to ignore height differences
        return Quaternion.LookRotation(toCamera);
    }

}