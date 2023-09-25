using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public string prefabSelected; 
        
    // Start is called before the first frame update
    void Start()
    {
        prefabSelected = GameState.selectedPrefab;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoBack()
    {
        SceneManager.LoadScene("HomeScreen");
    }
}
