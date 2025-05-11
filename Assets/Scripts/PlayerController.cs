using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MazeTemplate
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private bool canMove;
        [SerializeField] private GameplayUI gameplayUI;
        private Rigidbody2D rb;
        private float speed = 5;
        private Vector2 startTouchPosition;
        private Vector2 endTouchPosition;
        private Vector2 nextDirection;

        private void Start()
        {
            gameplayUI = GameObject.Find("Gameplay").GetComponent<GameplayUI>();
            rb = GetComponent<Rigidbody2D>();
            canMove = true;
            nextDirection = Vector2.zero;
        }

        private void Update()
        {
            if (canMove)
            {
                WASDAndArrowsMove();
                SwipeMove();
            }
        }

        private void SwipeMove()
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                startTouchPosition = Input.GetTouch(0).position;
            }
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                endTouchPosition = Input.GetTouch(0).position;
                Vector2 inputVector = endTouchPosition - startTouchPosition;
                if (Mathf.Abs(inputVector.x) > Mathf.Abs(inputVector.y))
                {
                    if (inputVector.x > 0)
                    {
                        if (canMove)
                        {
                            rb.linearVelocity = Vector2.right * speed;
                            transform.eulerAngles = new Vector3(0, 0, 0);
                            DoSomething();
                        }
                        else
                        {
                            nextDirection = Vector2.right;
                        }
                    }
                    else
                    {
                        if (canMove)
                        {
                            rb.linearVelocity = Vector2.left * speed;
                            transform.eulerAngles = new Vector3(0, 0, 180);
                            DoSomething();
                        }
                        else
                        {
                            nextDirection = Vector2.left;
                        }
                    }
                }
                else
                {
                    if (inputVector.y > 0)
                    {
                        if (canMove)
                        {
                            rb.linearVelocity = Vector2.up * speed;
                            transform.eulerAngles = new Vector3(0, 0, 90);
                            DoSomething();
                        }
                        else
                        {
                            nextDirection = Vector2.up;
                        }
                    }
                    else
                    {
                        if (canMove)
                        {
                            rb.linearVelocity = Vector2.down * speed;
                            transform.eulerAngles = new Vector3(0, 0, 270);
                            DoSomething();
                        }
                        else
                        {
                            nextDirection = Vector2.down;
                        }
                    }
                }
            }
        }

        void DoSomething()
        {
            canMove = false;
            AudioManager.instance.PlayFirstSound();
        }

        private void WASDAndArrowsMove()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                if (canMove)
                {
                    rb.linearVelocity = Vector2.up * speed;
                    transform.eulerAngles = new Vector3(0, 0, 90);
                    DoSomething();
                }
                else
                {
                    nextDirection = Vector2.up;
                }
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                if (canMove)
                {
                    rb.linearVelocity = Vector2.down * speed;
                    transform.eulerAngles = new Vector3(0, 0, 270);
                    DoSomething();
                }
                else
                {
                    nextDirection = Vector2.down;
                }
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                if (canMove)
                {
                    rb.linearVelocity = Vector2.left * speed;
                    transform.eulerAngles = new Vector3(0, 0, 180);
                    DoSomething();
                }
                else
                {
                    nextDirection = Vector2.left;
                }
            }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                if (canMove)
                {
                    rb.linearVelocity = Vector2.right * speed;
                    transform.eulerAngles = new Vector3(0, 0, 0);
                    DoSomething();
                }
                else
                {
                    nextDirection = Vector2.right;
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            canMove = true;
            var xValue = Math.Round(gameObject.transform.position.x, 1);
            var yValue = Math.Round(gameObject.transform.position.y, 1);
            gameObject.transform.position = new Vector2((float)xValue, (float)yValue);
            if (nextDirection != Vector2.zero)
            {
                rb.linearVelocity = nextDirection * speed;
                if (nextDirection == Vector2.up) transform.eulerAngles = new Vector3(0, 0, 90);
                if (nextDirection == Vector2.down) transform.eulerAngles = new Vector3(0, 0, 270);
                if (nextDirection == Vector2.left) transform.eulerAngles = new Vector3(0, 0, 180);
                if (nextDirection == Vector2.right) transform.eulerAngles = new Vector3(0, 0, 0);
                DoSomething();
                nextDirection = Vector2.zero;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Win"))
            {
                gameplayUI.LevelWin();
                Destroy(gameObject, 3);
            }
            else if (collision.TryGetComponent<Point>(out Point point))
            {
                // Collect the point
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddPoints(point.PointValue);
                }
                // Destroy the point object
                Destroy(collision.gameObject);
            }
        }
    }
}
