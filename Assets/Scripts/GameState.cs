using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages global game state data.
/// </summary>
public class GameState : MonoBehaviour
{
    /// <summary>
    /// Static variable to hold the selected prefab name.
    /// This variable is accessible across different scenes and used by GameController to initialize its prefabSelected variable.
    /// </summary>
    public static string selectedPrefab;
}