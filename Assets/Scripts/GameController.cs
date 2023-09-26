using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the game state and transitions between scenes.
/// </summary>
public class GameController : MonoBehaviour
{
    public string SelectedPrefab { get; private set; }
    public string ModeSelected { get; private set; }

    /// <summary>
    /// Initializes the controller and sets up the initial game state.
    /// </summary>
    void Start()
    {
        ModeSelected = GameState.modeSelected ?? ModeSelected;
        SelectedPrefab = GameState.selectedPrefab ?? SelectedPrefab;
    }

    /// <summary>
    /// Navigates to the HomeScreen scene.
    /// </summary>
    public void GoBack()
    {
        // Loads the HomeScreen scene.
        SceneManager.LoadScene("Home Screen Prefab Selection");
    }
}