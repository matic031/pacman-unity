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
            // Poskusimo dobiti GameplayUI komponento iz gameplayPanel-a
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

        public void SelectLevel(int levelNumberForSelection) // Preimenoval sem parameter, da se ne zamenja s fieldom
        {
            // Indeks je levelNumberForSelection - 1
            int levelIndex = levelNumberForSelection - 1;

            if (levelIndex >= levels.Length || levelIndex < 0)
            {
                Debug.LogWarning($"Invalid level index requested: {levelIndex}. Max index is {levels.Length - 1}");
                return;
            }

            // Dejansko nastavi trenutni level šele tukaj
            currentLevelNumber = levelIndex; // Shrani indeks trenutno izbranega levela

            // Počisti prejšnji level, če obstaja
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
                currentLevelNumber = 0; // Vrni se na prvi level, če smo na koncu
                // Alternativno: Pokaži "Game Won" zaslon ali vrni v meni
                Debug.Log("All levels completed! Restarting from level 1.");
            }
            currentLevelPrefab = Instantiate(levels[currentLevelNumber]);
            InitializeLevelData(); // Kličemo inicializacijo podatkov za level

            if (gameplayUIComponent != null)
            {
                gameplayUIComponent.HideWinPanel();
            }
            else if (gameplayPanel != null) // Fallback, če gameplayUIComponent ni bil najden v Awake
            {
                gameplayPanel.GetComponent<GameplayUI>()?.HideWinPanel();
            }
        }

        public void RestartCurrentLevel()
        {
            if (currentLevelNumber < 0 || currentLevelNumber >= levels.Length)
            {
                Debug.LogError("Cannot restart level, currentLevelNumber is invalid or no level loaded.");
                // Mogoče vrni v meni ali naloži privzeti level
                // Za zdaj samo izpiši napako.
                // Če se to zgodi, je verjetno, da 'Retry' pritisneš preden je level sploh naložen.
                return;
            }

            Debug.Log($"Restarting level: {currentLevelNumber + 1}");

            // Počisti obstoječi level prefab
            if (currentLevelPrefab != null)
            {
                Destroy(currentLevelPrefab);
            }

            // Ponovno instanciraj isti level
            currentLevelPrefab = Instantiate(levels[currentLevelNumber]);
            InitializeLevelData(); // Ponovno preštej točke itd.

            // Ponastavi igralca (če ni del level prefaba in ga ne uničimo/ustvarimo z levelom)
            // Tvoj igralec je verjetno del scene ali pa ga spawnas ločeno.
            // Če je Pacman del prefaba levela, se bo samodejno ponastavil.
            // Če ni, ga moraš ponastaviti ročno:
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                // Primer, kako bi lahko ponastavil Pacmana:
                // 1. Ponastavi pozicijo (na neko začetno točko, ki jo moraš definirati)
                //    playerObject.transform.position = GetLevelStartPosition(currentLevelNumber);
                // 2. Ponastavi stanje Pacmana
                PlayerController pc = playerObject.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.canMove = true; // Omogoči premikanje
                    // Morda ponastavi tudi hitrost, smer, itd., če je potrebno
                    // pc.ResetState(); // Idealno bi imel PlayerController metodo za to
                }
                // 3. Ponovno aktiviraj SpriteRenderer/Collider, če si jih skril ob smrti
                // playerObject.GetComponent<SpriteRenderer>().enabled = true;
                // playerObject.GetComponent<Collider2D>().enabled = true;
            }
            else
            {
                Debug.LogWarning("Player object not found on RestartCurrentLevel. If player is part of level prefab, this is fine.");
            }


            // Ponastavi duhce (podobno kot Pacmana, če niso del level prefaba)
            // Za vsakega duha:
            // ghost.ResetToStartPosition();
            // ghost.ResetState();

            // Skrij Win/Lose panele in aktiviraj gameplay UI
            if (gameplayUIComponent != null)
            {
                gameplayUIComponent.HideWinPanel();
                // Dodaj še metodo za skrivanje Lose Panela v GameplayUI, če še nimaš
                // gameplayUIComponent.HideLosePanel(); // In jo pokliči tukaj
                // Za zdaj lahko kar direktno:
                if (gameplayUIComponent.losePanel != null) gameplayUIComponent.losePanel.SetActive(false);
            }
            if (gameplayPanel != null) gameplayPanel.SetActive(true); // Zagotovi, da je gameplay viden
            if (levelsMenu != null) levelsMenu.SetActive(false); // Zagotovi, da je meni skrit

            // Ponastavi ScoreManager, če je potrebno in če se to ne zgodi avtomatsko
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }

            // Zagotovi, da je Time.timeScale = 1
            Time.timeScale = 1;
        }

        private void InitializeLevelData()
        {
            pointsCollectedThisLevel = 0;
            if (currentLevelPrefab != null)
            {
                // Poiščemo vse Point komponente znotraj trenutnega levela
                Point[] pointsInScene = currentLevelPrefab.GetComponentsInChildren<Point>(true);
                totalPointsInLevel = pointsInScene.Length;
                Debug.Log($"Level initialized with {totalPointsInLevel} points.");

                if (totalPointsInLevel == 0)
                {
                    Debug.LogWarning("Level started with 0 points. Win condition might trigger immediately if not handled.");
                    // Če je 0 točk, takoj sproži zmago (ali pa dodaj logiko, da level rabi vsaj 1 točko)
                    // Za zdaj bomo pustili, da se zmaga lahko sproži, če ni točk.
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
            if (totalPointsInLevel < 0) return; // Level je že končan/zmagan

            pointsCollectedThisLevel++;
            Debug.Log($"Point collected. Total: {pointsCollectedThisLevel}/{totalPointsInLevel}");
            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            if (pointsCollectedThisLevel >= totalPointsInLevel && totalPointsInLevel >= 0) // totalPointsInLevel >= 0 prepreči zmago, če je bil level že zmagan (-1)
            {
                Debug.Log("All points collected! Level Win!");
                if (gameplayUIComponent != null)
                {
                    gameplayUIComponent.LevelWin();
                }
                else
                {
                     // Poskusi najti GameplayUI dinamično, če ni bila nastavljena
                    GameplayUI ui = FindObjectOfType<GameplayUI>();
                    if (ui != null) ui.LevelWin();
                    else Debug.LogError("GameplayUI component could not be found to show Win Panel!");
                }

                // Onemogoči nadaljnje premikanje igralca ali ga uniči
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    PlayerController pc = playerObject.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        // pc.enabled = false; // Možnost 1: Onemogoči skripto
                        pc.canMove = false; // Možnost 2: Onemogoči premikanje preko obstoječe spremenljivke
                        // Destroy(playerObject, 3f); // Možnost 3: Uniči igralca (kot pri "Win" tagu)
                                                // Če želiš uničenje, odkomentiraj zgornjo vrstico.
                                                // Zaenkrat samo ustavimo premikanje.
                    }
                }
                totalPointsInLevel = -1; // Označi, da je level zmagan, da se ne sproži večkrat
            }
        }
    }
}