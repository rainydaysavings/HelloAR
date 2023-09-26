using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages scene transitions and updates global game state.
/// </summary>
public class SceneController : MonoBehaviour
{
    /// <summary>
    /// Switches to the AR Scene and updates the selected prefab in the global game state.
    /// </summary>
    /// <param name="selectedPrefab">Name of the prefab to be selected.</param>
    public void SwitchScenes(string selectedPrefab)
    {
        // Update the global game state with the selected prefab.
        GameState.selectedPrefab = selectedPrefab;
        
        // Load the AR Scene.
        SceneManager.LoadScene("AR Scene");
    }
}