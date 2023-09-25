using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void SwitchScenes(string selectedPrefab)
    {
        GameState.selectedPrefab = selectedPrefab;
        SceneManager.LoadScene("AR Scene");
    }
}
