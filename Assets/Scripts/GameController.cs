using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the game state and transitions between scenes.
/// </summary>
public class GameController : MonoBehaviour
{
    public string prefabSelected; 

    /// <summary>
    /// Initializes the controller and sets up the initial game state.
    /// </summary>
    void Start()
    {
        // Initializes the selected prefab based on global game state.
        prefabSelected = GameState.selectedPrefab;
    }

    /// <summary>
    /// Update is called once per frame. Reserved for future use.
    /// </summary>
    void Update()
    {
        return;
    }

    /// <summary>
    /// Navigates to the HomeScreen scene.
    /// </summary>
    public void GoBack()
    {
        // Loads the HomeScreen scene.
        SceneManager.LoadScene("HomeScreen");
    }
}