using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

/// <summary>
///     Manages scene transitions and updates global game state.
/// </summary>
public class SceneController : MonoBehaviour
{
    IEnumerator StartXR(string scene)
    {
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
        }
        else
        {
            Debug.Log("Starting XR...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            yield return null;
            
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
        }
    }
 
    void StopXR()
    {
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            Camera.main.ResetAspect();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }
    }
    
    /// <summary>
    ///     Switches to the AR Scene and updates the selected prefab in the global game state.
    /// </summary>
    /// <param name="selectedPrefab">Name of the prefab to be selected.</param>
    public void SwitchSceneToModeSelection(string selectedPrefab)
    {
        // Update the global game state with the selected prefab.
        GameState.selectedPrefab = selectedPrefab;

        // Proceed to mode selection menu
        StopXR();
        StartCoroutine(StartXR("Home Screen Mode Selection"));
    }

    public void SwitchSceneToAR(string selectedMode)
    {
        // Update the global game state with the selected mode.
        GameState.modeSelected = selectedMode;

        // Proceed to AR mode.
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
        string scene = selectedMode.Equals("Marker") ? "AR Scene Marker" : "AR Scene Plane";
        StopXR();
        StartCoroutine(StartXR(scene));
    }
}