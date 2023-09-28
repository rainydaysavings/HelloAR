using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

/// <summary>
///     Manages global game state data.
/// </summary>
public class GameState : MonoBehaviour
{
    /// <summary>
    ///     Static variable to hold the selected prefab name.
    ///     This variable is accessible across different scenes and used by GameController to initialize its prefabSelected
    ///     variable.
    /// </summary>
    public static string selectedPrefab;

    public static string modeSelected;

    /// <summary>
    ///     Navigates to the HomeScreen scene.
    /// </summary>
    public static void GoBack()
    {
        // Loads the HomeScreen scene.
        SceneManager.LoadScene("Home Screen Prefab Selection");
    }
}