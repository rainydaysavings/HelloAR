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
    public void SwitchSceneToModeSelection(string selectedPrefab)
    {
        // Update the global game state with the selected prefab.
        GameState.selectedPrefab = selectedPrefab;
        
        // Proceed to mode selection menu
        SceneManager.LoadScene("Home Screen Mode Selection", LoadSceneMode.Single);
    }
    
    public void SwitchSceneToAR(string selectedMode)
    {
        // Update the global game state with the selected mode.
        GameState.modeSelected = selectedMode;
        
        // Proceed to AR mode.
        SceneManager.LoadScene(selectedMode.Equals("Marker") ? "AR Scene Marker" : "AR Scene Plane", LoadSceneMode.Single);
    }
}