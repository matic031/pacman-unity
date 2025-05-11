using UnityEngine;

namespace MazeTemplate
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }
        
        [SerializeField] private int currentScore = 0;
        
        public int CurrentScore => currentScore;

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void AddPoints(int points)
        {
            currentScore += points;
            Debug.Log($"Score: {currentScore}");
        }

        public void ResetScore()
        {
            currentScore = 0;
            Debug.Log("Score reset to 0");
        }
    }
}