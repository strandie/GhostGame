using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUIManager : MonoBehaviour
{
    public void RestartGame()
    {
        SceneManager.LoadScene("GameScene"); // Replace with your actual gameplay scene name
    }

    public void GoToTitleScreen()
    {
        SceneManager.LoadScene("TitleScreen");
    }
}
