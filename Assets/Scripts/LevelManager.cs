using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MazeTemplate
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] levels;
        [SerializeField] private GameObject levelsMenu;
        [SerializeField] private GameObject gameplayPanel; // Panel, ki vsebuje GameplayUI
        private GameObject currentLevelPrefab;
        private int currentLevelNumber = -1;

        private int totalPointsInLevel;
        private int pointsCollectedThisLevel;
        private GameplayUI gameplayUIComponent; // Referenca na GameplayUI skripto

        private void Awake()
        {
            if (gameplayPanel != null)
            {
                gameplayUIComponent = gameplayPanel.GetComponent<GameplayUI>();
                if (gameplayUIComponent == null)
                {
                    Debug.LogError("GameplayUI component not found on gameplayPanel!");
                }
            }
            else
            {
                Debug.LogError("GameplayPanel is not assigned in LevelManager!");
            }
        }

        public void SelectLevel(int levelNumberForSelection) 
        {
            int levelIndex = levelNumberForSelection - 1;

            if (levelIndex >= levels.Length || levelIndex < 0)
            {
                Debug.LogWarning($"Invalid level index requested: {levelIndex}. Max index is {levels.Length - 1}");
                return;
            }

            currentLevelNumber = levelIndex;

            if (currentLevelPrefab != null)
            {
                Destroy(currentLevelPrefab);
            }

            currentLevelPrefab = Instantiate(levels[currentLevelNumber]);
            InitializeLevelData();

            if (levelsMenu != null) levelsMenu.SetActive(false);
            if (gameplayPanel != null) gameplayPanel.SetActive(true);
            if (gameplayUIComponent != null) gameplayUIComponent.HideWinPanel();
        }

        public void NextLevel()
        {
            if (currentLevelPrefab != null)
            {
                Destroy(currentLevelPrefab);
            }
            currentLevelNumber++;
            if (currentLevelNumber >= levels.Length)
            {
                currentLevelNumber = 0;
                Debug.Log("All levels completed! Restarting from level 1.");
            }
            currentLevelPrefab = Instantiate(levels[currentLevelNumber]);
            InitializeLevelData();

            if (gameplayUIComponent != null)
            {
                gameplayUIComponent.HideWinPanel();
            }
            else if (gameplayPanel != null)
            {
                gameplayPanel.GetComponent<GameplayUI>()?.HideWinPanel();
            }
        }

        public void RestartCurrentLevel()
        {
            if (currentLevelNumber < 0 || currentLevelNumber >= levels.Length)
            {
                Debug.LogError("Cannot restart level, currentLevelNumber is invalid or no level loaded.");
                return;
            }

            Debug.Log($"Restarting level: {currentLevelNumber + 1}");

            if (currentLevelPrefab != null)
            {
                Destroy(currentLevelPrefab);
            }

            currentLevelPrefab = Instantiate(levels[currentLevelNumber]);
            InitializeLevelData();

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                PlayerController pc = playerObject.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.canMove = true;
                }
            }
            else
            {
                Debug.LogWarning("Player object not found on RestartCurrentLevel. If player is part of level prefab, this is fine.");
            }

            if (gameplayUIComponent != null)
            {
                gameplayUIComponent.HideWinPanel();
                if (gameplayUIComponent.losePanel != null) gameplayUIComponent.losePanel.SetActive(false);
            }
            if (gameplayPanel != null) gameplayPanel.SetActive(true);
            if (levelsMenu != null) levelsMenu.SetActive(false);

            // ODSTRANI ali ZAKOMENTIRAJ naslednje vrstice, da se score NE resetira:
            // if (ScoreManager.Instance != null)
            // {
            //     ScoreManager.Instance.ResetScore();
            // }

            Time.timeScale = 1;
        }

        private void InitializeLevelData()
        {
            pointsCollectedThisLevel = 0;
            if (currentLevelPrefab != null)
            {
                Point[] pointsInScene = currentLevelPrefab.GetComponentsInChildren<Point>(true);
                totalPointsInLevel = pointsInScene.Length;
                Debug.Log($"Level initialized with {totalPointsInLevel} points.");

                PlayerController player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    player.canMove = true;
                }
                foreach (GhostController ghost in FindObjectsOfType<GhostController>())
                {
                    ghost.ResetToNormalState();
                }

                if (totalPointsInLevel == 0)
                {
                    Debug.LogWarning("Level started with 0 points. Win condition might trigger immediately if not handled.");
                    CheckWinCondition();
                }
            }
            else
            {
                totalPointsInLevel = 0;
                Debug.LogError("CurrentLevelPrefab is null in InitializeLevelData. Cannot count points.");
            }
        }

        public void OnPointCollected()
        {
            if (totalPointsInLevel < 0) return;

            pointsCollectedThisLevel++;
            Debug.Log($"Point collected. Total: {pointsCollectedThisLevel}/{totalPointsInLevel}");
            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            if (pointsCollectedThisLevel >= totalPointsInLevel && totalPointsInLevel >= 0)
            {
                Debug.Log("All points collected! Level Win!");
                if (gameplayUIComponent != null)
                {
                    gameplayUIComponent.LevelWin();
                }
                else
                {
                    GameplayUI ui = FindObjectOfType<GameplayUI>();
                    if (ui != null) ui.LevelWin();
                    else Debug.LogError("GameplayUI component could not be found to show Win Panel!");
                }

                // Onemogoči premikanje igralca
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    PlayerController pc = playerObject.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        pc.canMove = false;
                    }
                }
                totalPointsInLevel = -1;

                // Počakaj 1 sekundo, nato resetiraj level (točke ostanejo!)
                StartCoroutine(RestartLevelAfterDelay(1.0f));
            }
        }

        // Dodaj to funkcijo
        private IEnumerator RestartLevelAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            RestartCurrentLevel();
        }
    }
}