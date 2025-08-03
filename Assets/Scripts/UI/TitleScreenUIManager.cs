using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenUIManager : MonoBehaviour
{
    public string gameSceneName = "GameScene"; // Set this to your game scene name

    void Update()
    {
        // On any mouse click or screen touch, load the game scene
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
