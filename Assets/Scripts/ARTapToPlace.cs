using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Manages the spawning and interaction of AR objects.
/// - Allows for spawning an object once, and scale it.
/// - Disallows further spawns and plane detection, while maintaining existing planes
/// - Existing planes with different heights may occlude objects (see: OcclusionMaterial in ARFeatheredPlane prefab)
/// </summary>
public class SpawnableManager : MonoBehaviour
{
    [SerializeField] private ARRaycastManager m_RaycastManager;
    private List<ARRaycastHit> m_Hits = new List<ARRaycastHit>();

    [SerializeField] private GameObject spawnablePrefab1; // CR7 prefab
    [SerializeField] private GameObject spawnablePrefab2; // The Gun prefab

    private Camera arCam; 
    private GameObject spawnedObject;
    private bool disableMovement = false;   // To control wether or not the object should be moved upon TouchPhase.Moved
    private ARPlaneManager arPlaneManager;
    private GameObject tutorialHintText;    // Used to remove hint text upon asset spawn
    
    private float initialFingerDistance;    // Allows to compute the scaling factor
    private Vector3 initialScale;

    /// <summary>
    /// Resets the AR Environment, so that the modified ARPlaneManager component behaves correctly
    /// </summary>
    void Awake()
    {
        disableMovement = false;
        arPlaneManager = GetComponent<ARPlaneManager>();
        ResetAREnvironment();
    }

    /// <summary>
    /// Initializes variables and sets up the AR environment.
    /// </summary>
    void Start()
    {
        arCam = GameObject.Find("Main Camera").GetComponent<Camera>();
        tutorialHintText = GameObject.FindWithTag("Tutorial");
    }

    /// <summary>
    /// Handles touch inputs for both single and multi-touch.
    /// Delegates method call depending on how many touch points are on the touchscreen
    /// </summary>
    void Update()
    {
        if (Input.touchCount > 1 && spawnedObject)
        {
            HandleTwoFingerTouch();
        }
        else
        {
            HandleSingleFingerTouch();
        }
    }

    /// <summary>
    /// Makes sure both the arPlaneManager, and trackables are viewable
    /// SpawnPrefab disables the former, and makes the latter transparent upon object placement 
    /// </summary>
    public void ResetAREnvironment()
    {
        arPlaneManager.enabled = true;
        foreach (var plane in arPlaneManager.trackables)
        {
            plane.gameObject.GetComponent<MeshRenderer>().materials[0].color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
        }
    }


    /// <summary>
    /// Handles single-finger touch interactions.
    /// Allows for object movement
    /// </summary>
    void HandleSingleFingerTouch()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        Ray ray = arCam.ScreenPointToRay(touch.position);

        if (!PerformRaycast(touch.position)) return;

        if (touch.phase == TouchPhase.Began && !spawnedObject)
        {
            HandleTouchBegin(ray);
        }
        else if (touch.phase == TouchPhase.Moved && spawnedObject)
        {
            HandleTouchMove();
        } 
        else if (touch.phase == TouchPhase.Ended && spawnedObject)
        {
            disableMovement = true;
        }
    }
    
    /// <summary>
    /// Handles two-finger touch for scaling objects.
    /// </summary>
    void HandleTwoFingerTouch()
    {
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
        {
            initialFingerDistance = Vector2.Distance(touch1.position, touch2.position);
            initialScale = spawnedObject.transform.localScale;
        }
        else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            float currentFingerDistance = Vector2.Distance(touch1.position, touch2.position);
            float scaleFactor = currentFingerDistance / initialFingerDistance;
            spawnedObject.transform.localScale = initialScale * scaleFactor;
        }
    }

    /// <summary>
    /// Performs raycast to detect AR planes.
    /// </summary>
    /// <param name="touchPosition">The 2D position of the touch.</param>
    /// <returns>True if a hit is found, otherwise false.</returns>
    bool PerformRaycast(Vector2 touchPosition)
    {
        return m_RaycastManager.Raycast(touchPosition, m_Hits, TrackableType.PlaneWithinPolygon);
    }

    /// <summary>
    /// Handles the start of a touch event.
    /// </summary>
    /// <param name="ray">Ray from the camera to the touch point.</param>
    void HandleTouchBegin(Ray ray)
    {
        Pose hitPose = m_Hits[0].pose;
        SpawnPrefab(hitPose.position, GetHorizontalUpRotation(hitPose.position));
    }

    /// <summary>
    /// Calculates rotation to align object with the horizontal plane.
    /// </summary>
    /// <param name="position">The 3D position for rotation calculation.</param>
    /// <returns>The calculated rotation.</returns>
    Quaternion GetHorizontalUpRotation(Vector3 position)
    {
        Vector3 direction = arCam.transform.position - position;
        direction.y = 0;
        return Quaternion.LookRotation(direction);
    }

    /// <summary>
    /// Handles object movement during a touch event.
    /// Movement is disabled upon first touch has ended, we need to check for that.
    /// </summary>
    void HandleTouchMove()
    {
        if (!disableMovement)
        {
            spawnedObject.transform.position = m_Hits[0].pose.position;
            spawnedObject.transform.rotation = GetHorizontalUpRotation(m_Hits[0].pose.position);
        }
    }

    /// <summary>
    /// Instantiates a prefab at a given position and rotation.
    /// arPlaneManager is disabled to prevent existing planes from changing position.
    /// arPlaneManager's planes' material at index 0 is made invisible to impede any distraction from the experience
    /// </summary>
    /// <param name="spawnPosition">The 3D position to spawn the prefab.</param>
    /// <param name="rotation">The rotation for the spawned object.</param>
    private void SpawnPrefab(Vector3 spawnPosition, Quaternion rotation)
    {
        if (GameState.selectedPrefab.Equals("Cristiano Ronaldo"))
        {
            spawnedObject = Instantiate(spawnablePrefab1, spawnPosition, rotation);
        }
        else
        {
            spawnedObject = Instantiate(spawnablePrefab2, spawnPosition, rotation);
        }

        GameObject.Destroy(tutorialHintText);

        arPlaneManager.enabled = false;
        foreach (var plane in arPlaneManager.trackables)
        {
            plane.gameObject.GetComponent<MeshRenderer>().materials[0].color = Color.clear;
        }
    }
}