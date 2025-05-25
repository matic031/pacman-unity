using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MazeTemplate
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] public bool canMove;
        [SerializeField] private GameplayUI gameplayUI;
        private Rigidbody2D rb;
        private float speed = 5f;

        private Vector2 nextDirection = Vector2.zero;      // Smer, ki jo želi igralec
        private Vector2 currentDirection = Vector2.zero;   // Smer, v katero se trenutno premika Pacman

        private LevelManager levelManager;

        private void Start()
        {
            gameplayUI = GameObject.Find("Gameplay").GetComponent<GameplayUI>();
            levelManager = FindObjectOfType<LevelManager>();
            if (levelManager == null) Debug.LogError("LevelManager ni najden v PlayerController!");

            rb = GetComponent<Rigidbody2D>();
            canMove = true;
            nextDirection = Vector2.zero;
            currentDirection = Vector2.left; // Začni v levo (ali po želji)
        }

        private void Update()
        {
            if (!canMove) return;

            HandleInput();
            TryChangeDirection();
            Move();
        }

        // Sprejme input in shrani želeno smer
        private void HandleInput()
        {
            // Tipkovnica
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                nextDirection = Vector2.up;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                nextDirection = Vector2.down;
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                nextDirection = Vector2.left;
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                nextDirection = Vector2.right;

            // Touch swipe
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                startTouchPosition = Input.GetTouch(0).position;
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                endTouchPosition = Input.GetTouch(0).position;
                Vector2 inputVector = endTouchPosition - startTouchPosition;
                if (Mathf.Abs(inputVector.x) > Mathf.Abs(inputVector.y))
                    nextDirection = inputVector.x > 0 ? Vector2.right : Vector2.left;
                else
                    nextDirection = inputVector.y > 0 ? Vector2.up : Vector2.down;
            }
        }

        // Če je v želeni smeri prost prehod, spremeni smer
        private void TryChangeDirection()
        {
            if (nextDirection != Vector2.zero && CanMoveInDirection(nextDirection))
            {
                currentDirection = nextDirection;
                nextDirection = Vector2.zero;
            }
        }

        // Premakni Pacmana v trenutni smeri, če je možno
        private void Move()
        {
            if (CanMoveInDirection(currentDirection))
            {
                rb.linearVelocity = currentDirection * speed;
                // Rotacija sprite-a (opcijsko)
                if (currentDirection == Vector2.up) transform.eulerAngles = new Vector3(0, 0, 90);
                else if (currentDirection == Vector2.down) transform.eulerAngles = new Vector3(0, 0, 270);
                else if (currentDirection == Vector2.left) transform.eulerAngles = new Vector3(0, 0, 180);
                else if (currentDirection == Vector2.right) transform.eulerAngles = new Vector3(0, 0, 0);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        // Preveri, če je v določeni smeri prost prehod (ni zidu)
        private bool CanMoveInDirection(Vector2 direction)
        {
            if (direction == Vector2.zero) return false;
            float distance = 0.6f; // Prilagodi glede na velikost Pacmana/tile-a
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, LayerMask.GetMask("Wall"));
            return hit.collider == null;
        }

        // --- Kolizije in točke ---
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Win"))
            {
                if (gameplayUI != null) gameplayUI.LevelWin();
                canMove = false;
                rb.linearVelocity = Vector2.zero;
            }
            else if (collision.TryGetComponent<Point>(out Point point))
            {
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddPoints(point.PointValue);
                Destroy(collision.gameObject);
                if (levelManager != null) levelManager.OnPointCollected();
            }
            else if (collision.CompareTag("Energizer"))
            {
                Energizer energizer = collision.GetComponent<Energizer>();
                if (energizer != null)
                {
                    if (ScoreManager.Instance != null) ScoreManager.Instance.AddPoints(energizer.PointValue);
                    ActivateFrightenedModeOnGhosts(energizer.FrightenedDuration);
                    Destroy(collision.gameObject);
                }
            }
        }

        private void ActivateFrightenedModeOnGhosts(float duration)
        {
            GhostController[] ghosts = FindObjectsOfType<GhostController>();
            if (ghosts.Length == 0)
            {
                Debug.LogWarning("No ghosts found to activate frightened mode.");
                return;
            }
            foreach (GhostController ghost in ghosts)
            {
                ghost.SetFrightened(duration);
            }
            Debug.Log($"Player ate Energizer! All ghosts set to frightened mode for {duration} seconds.");
        }

        // --- Klicano, ko te ujame duh ---
        public void PlayerHitByGhost()
        {
            if (!canMove)
            {
                Debug.Log("PlayerHitByGhost called, but player canMove is already false. No action taken.");
                return;
            }

            Debug.Log("PlayerHitByGhost() called successfully in PlayerController.");
            canMove = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                Debug.Log("Player Rigidbody velocity set to zero.");
            }
            else
            {
                Debug.LogWarning("Player Rigidbody (rb) is null in PlayerHitByGhost(). Cannot set velocity to zero.");
            }

            if (gameplayUI != null)
            {
                Debug.Log("Calling gameplayUI.ShowLosePanel().");
                gameplayUI.ShowLosePanel();
            }
            else
            {
                Debug.LogError("GameplayUI reference is NULL in PlayerController! Cannot show Lose Panel.");
            }
        }

        // --- Touch swipe support ---
        private Vector2 startTouchPosition;
        private Vector2 endTouchPosition;
    }
}