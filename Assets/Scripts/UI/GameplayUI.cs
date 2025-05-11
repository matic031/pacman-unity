using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeTemplate
{
    public class GameplayUI : MonoBehaviour
    {
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject winPanel;
        [SerializeField] public GameObject losePanel; 
        [SerializeField] LevelManager levelManager;

        public void PauseButton()
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0;
        }

        public void ContinueButton()
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1;
        }

        public void MenuButton()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Time.timeScale = 1;
        }

        public void NextLevelButton()
        {
            levelManager.NextLevel();
        }

        public void LevelWin()
        {
            winPanel.SetActive(true);
            Time.timeScale = 0;
        }

        public void HideWinPanel()
        {
            winPanel.SetActive(false);
        }

        public void ShowLosePanel()
        {
            if (losePanel != null)
            {
                losePanel.SetActive(true);
            }
            Time.timeScale = 0; // Ustavi igro
        }

        public void RetryButton()
        {

            if (levelManager != null)
            {
                levelManager.RestartCurrentLevel();
            }
            else
            {
                Debug.LogError("LevelManager ni dodeljen v GameplayUI! Ne morem ponovno zagnati levela.");
            }

            if (losePanel != null)
            {
                losePanel.SetActive(false);
            }
        }
    
        public void HideLosePanel()
        {
            if (losePanel != null) losePanel.SetActive(false);
        }

    }
}