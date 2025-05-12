using UnityEngine;
using TMPro;

namespace MazeTemplate
{
    public class ScoreDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI scoreText;
        private ScoreManager scoreManager;

        private void Start()
        {
            scoreManager = ScoreManager.Instance;
            if (scoreManager == null)
            {
                Debug.LogError("ScoreManager instance not found!");
            }

            UpdateScoreDisplay();
        }

        private void Update()
        {
            UpdateScoreDisplay();
        }

        private void UpdateScoreDisplay()
        {
            if (scoreManager != null && scoreText != null)
            {
                scoreText.text = $"Score: {scoreManager.CurrentScore}";
            }
        }
    }
}