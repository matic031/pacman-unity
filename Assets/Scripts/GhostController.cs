using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace MazeTemplate
{
    public enum GhostState
    {
        Chase,
        Scatter,
        Frightened,
        Eaten 
    }

    public class GhostController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Color ghostColor = Color.red;
        [SerializeField] private GhostState currentState = GhostState.Chase;
        [SerializeField] private float scatterModeTime = 7f;
        [SerializeField] private float chaseModeTime = 20f;
        [SerializeField] private Transform scatterTarget;
        [SerializeField] private float tileSize = 1f; // Size of each tile in the grid
        
        private Rigidbody2D rb;
        //private SpriteRenderer spriteRenderer;
        private Image imageComponent;
        private Vector2 currentDirection;
        private Vector2[] possibleDirections = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        private float modeTimer;
        private Vector2 lastPosition;
        private bool isStuck = false;
        private float stuckCheckTimer = 0f;
        private float stuckCheckInterval = 0.5f;

        [SerializeField] private float frightenedSpeedMultiplier = 0.75f; 
        [SerializeField] private float eatenSpeedMultiplier = 1.5f;   
        [SerializeField] private Transform ghostBasePosition;         
        [SerializeField] private Color frightenedColor = Color.blue;  
        [SerializeField] private Color eatenColor = new Color(0.8f, 0.8f, 1f, 0.7f);

        private Color originalColorActual; // Za shranjevanje dejanske originalne barve ob Start()
        private Coroutine frightenedTimerCoroutine; // Za upravljanje timerja Frightened stanja
        
        // Tile-based movement variables
        private Vector3 targetPosition;
        private bool isMoving = false;

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            //spriteRenderer = GetComponent<SpriteRenderer>();
            imageComponent = GetComponent<Image>();
            //originalColorActual = spriteRenderer.color;

            if (ghostBasePosition == null) 
            {
                GameObject tempBase = new GameObject(gameObject.name + "_AutoBasePos");
                tempBase.transform.position = transform.position;
                ghostBasePosition = tempBase.transform;
                Debug.LogWarning($"Ghost {gameObject.name}: GhostBasePosition not assigned! Using initial position. Assign a Transform in Inspector.");
            }
            
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
            }

            // Set ghost color
            if (imageComponent != null) originalColorActual = imageComponent.color; 
            else originalColorActual = ghostColor; // Fallback na ghostColor, če Image ni najden

            if (imageComponent != null) imageComponent.color = originalColorActual; 

            SnapToGrid();
            targetPosition = transform.position;
            
            // Choose a random starting direction
            ChooseRandomDirection();
            
            // Initialize mode timer
            modeTimer = currentState == GhostState.Chase ? chaseModeTime : scatterModeTime;
            
            // Store initial position for stuck detection
            lastPosition = transform.position;
        }

        private void SnapToGrid()
        {
            // Round position to align with grid
            float x = Mathf.Round(transform.position.x / tileSize) * tileSize;
            float y = Mathf.Round(transform.position.y / tileSize) * tileSize;
            transform.position = new Vector3(x, y, transform.position.z);
        }

        private void Update()
        {
            // Update mode timer and switch modes if needed
            if (currentState != GhostState.Frightened && currentState != GhostState.Eaten)
            {
                modeTimer -= Time.deltaTime;
                if (modeTimer <= 0)
                {
                    SwitchMode(); // Samo za Chase/Scatter
                }
            }
            
            stuckCheckTimer += Time.deltaTime;
            if (stuckCheckTimer >= stuckCheckInterval)
            {
                CheckIfStuck();
                stuckCheckTimer = 0f;
            }
            
            if (isMoving)
            {
                float currentSpeed = moveSpeed;

                if (currentState == GhostState.Frightened) currentSpeed *= frightenedSpeedMultiplier;
                else if (currentState == GhostState.Eaten) currentSpeed *= eatenSpeedMultiplier;

                transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);
                
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    transform.position = targetPosition;
                    isMoving = false;
                    

                    if (currentState == GhostState.Eaten && ghostBasePosition != null &&
                        Vector3.Distance(transform.position, ghostBasePosition.position) < tileSize * 0.1f)
                    {
                        ResetFromEatenState();
                    }
                    else
                    {
                        DecideNextDirection();
                    }
                }
            }
            else if (!isMoving)
            {
                // If not currently moving, start moving in the current direction
                TryMove(currentDirection);
            }
        }

        private bool TryMove(Vector2 direction)
        {
            // Calculate the next position (exactly one tile away)
            Vector3 nextPos = transform.position + new Vector3(direction.x, direction.y, 0) * tileSize;
            
            // Check if there's a wall in that direction
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, tileSize, LayerMask.GetMask("Wall"));
            if (hit.collider == null)
            {
                // Path is clear, set target position to the next tile
                targetPosition = nextPos;
                isMoving = true;
                // Disable rigidbody velocity to avoid physics interference
                rb.linearVelocity = Vector2.zero;
                return true;
            }
            
            // Path blocked, can't move in this direction
            return false;
        }

         private void OnCollisionEnter2D(Collision2D collision)
        {
            Debug.Log($"{gameObject.name} OnCollisionEnter2D triggered with: {collision.gameObject.name} which has tag: {collision.gameObject.tag}");
            if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Debug.Log($"{gameObject.name} collided with Player tagged object."); 

                PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                Debug.Log($"{gameObject.name}: PlayerController found on collided object. Player canMove: {playerController.canMove}, Ghost currentState: {currentState}"); // DEBUG Log

    
            if (playerController.canMove && currentState != GhostState.Frightened && currentState != GhostState.Eaten)
            {
                Debug.Log($"{gameObject.name} is calling PlayerHitByGhost()."); 
                playerController.PlayerHitByGhost(); // Duh ujame Pacmana
            }
            else if (currentState == GhostState.Frightened && playerController.canMove)
            {
                 GetEaten(); // Pacman poje duha
                Debug.Log($"{gameObject.name} is Frightened. Player collided, but GetEaten() logic is currently handled here or later.");
            }
            }
            else
            {
                Debug.LogError($"{gameObject.name} collided with Player tagged object, but PlayerController component is missing!");
            }
            }
            else
            {
            
            }
        }

        private void GetEaten()
        {
            Debug.Log($"{gameObject.name} got eaten!");
            currentState = GhostState.Eaten;
            
            // Disable the entire ghost GameObject when eaten
            gameObject.SetActive(false);

            if (frightenedTimerCoroutine != null)
            {
                StopCoroutine(frightenedTimerCoroutine);
                frightenedTimerCoroutine = null;
            }
            
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddPoints(200);
            }
            
            // Start a coroutine to re-enable the ghost after a delay
            StartCoroutine(ReenableAfterDelay());
        }

        private IEnumerator ReenableAfterDelay()
        {
            // Wait a short time before reactivating at the base position
            yield return new WaitForSeconds(2f);
            
            // Set position to ghost base before enabling
            if (ghostBasePosition != null)
            {
                transform.position = ghostBasePosition.position;
            }
            
            // Re-enable the ghost and reset state
            gameObject.SetActive(true);
            ResetFromEatenState();
        }

        private void ResetFromEatenState()
        {
            Debug.Log($"{gameObject.name} returned to base and is resetting.");
            SnapToGrid();
            transform.position = ghostBasePosition.position; // Postavi točno na bazo
            currentState = GhostState.Chase; 
            if (imageComponent != null) 
            {
                imageComponent.color = originalColorActual;
                // Make ghost visible again
                imageComponent.enabled = true;
            }
            modeTimer = chaseModeTime; // Ali scatterModeTime
            isMoving = false;
            ChooseRandomDirection(); // Izberi novo smer iz baze
        }

        private void DecideNextDirection()
        {
            // Choose the next direction based on the current mode
            switch (currentState)
            {
                case GhostState.Chase:
                    ChasePlayer();
                    break;
                case GhostState.Scatter:
                    HeadToScatterTarget();
                    break;
                case GhostState.Frightened:
                    ChooseRandomDirection();
                    break;
                    case GhostState.Eaten: // Novo
                    if (ghostBasePosition != null)
                    {
                        
                        Vector2 toBase = ((Vector2)ghostBasePosition.position - (Vector2)transform.position).normalized;
                
                        Vector2[] dirs = GetAvailableDirections(); // Uporabi obstoječo, ki preprečuje obračanje
                        if (dirs.Length > 0) {
                            currentDirection = dirs[0]; // Zelo osnovno, samo za prikaz
                            float bestDot = -2;
                            foreach(Vector2 d in dirs) {
                                float dot = Vector2.Dot(d, toBase);
                                if (dot > bestDot) {
                                    bestDot = dot;
                                    currentDirection = d;
                                }
                            }
                        } else if (TryMove(-currentDirection)) {} // Če ni poti, se obrni
                        
                        TryMove(currentDirection);

                    }
                    else ResetFromEatenState(); // Če ni baze, se takoj resetiraj
                    break;
            
            }
        }

        private void ChasePlayer()
        {
            if (playerTransform == null)
            {
                ChooseRandomDirection();
                return;
            }

            // Find available directions (not walls)
            Vector2[] availableDirections = GetAvailableDirections();
            
            if (availableDirections.Length == 0)
            {
                // If no directions are available, just reverse
                currentDirection = -currentDirection;
                TryMove(currentDirection);
                return;
            }

            // Choose the direction that gets closer to the player
            Vector2 toPlayer = (Vector2)playerTransform.position - (Vector2)transform.position;
            float bestScore = float.MinValue;
            Vector2 bestDirection = currentDirection;
            
            // Don't go back the way we came unless it's the only option
            Vector2 oppositeDirection = -currentDirection;
            
            foreach (Vector2 dir in availableDirections)
            {
                // Try to avoid reversing direction
                if (dir == oppositeDirection && availableDirections.Length > 1)
                    continue;
                    
                float score = Vector2.Dot(dir.normalized, toPlayer.normalized);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = dir;
                }
            }
            
            currentDirection = bestDirection;
            TryMove(currentDirection);
        }

        private void HeadToScatterTarget()
        {
            if (scatterTarget == null)
            {
                ChooseRandomDirection();
                return;
            }

            // Similar to chase but target the scatter point
            Vector2[] availableDirections = GetAvailableDirections();
            
            if (availableDirections.Length == 0)
            {
                currentDirection = -currentDirection;
                TryMove(currentDirection);
                return;
            }

            Vector2 toTarget = (Vector2)scatterTarget.position - (Vector2)transform.position;
            float bestScore = float.MinValue;
            Vector2 bestDirection = currentDirection;
            
            Vector2 oppositeDirection = -currentDirection;
            
            foreach (Vector2 dir in availableDirections)
            {
                if (dir == oppositeDirection && availableDirections.Length > 1)
                    continue;
                    
                float score = Vector2.Dot(dir.normalized, toTarget.normalized);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = dir;
                }
            }
            
            currentDirection = bestDirection;
            TryMove(currentDirection);
        }

        private void ChooseRandomDirection()
        {
            Vector2[] availableDirections = GetAvailableDirections();
            
            if (availableDirections.Length == 0)
            {
                // If no directions are available, just reverse
                currentDirection = -currentDirection;
                TryMove(currentDirection);
                return;
            }
            
            // Choose a random available direction
            currentDirection = availableDirections[Random.Range(0, availableDirections.Length)];
            TryMove(currentDirection);
        }

        private Vector2[] GetAvailableDirections()
        {
            // Check which directions don't have walls at tile-distance
            System.Collections.Generic.List<Vector2> available = new System.Collections.Generic.List<Vector2>();
            
            foreach (Vector2 dir in possibleDirections)
            {
                // Don't check the opposite direction (don't want to go back)
                if (dir == -currentDirection && !isStuck)
                    continue;
                    
                if (!Physics2D.Raycast(transform.position, dir, tileSize, LayerMask.GetMask("Wall")))
                {
                    available.Add(dir);
                }
            }
            
            return available.ToArray();
        }

        private void SwitchMode()
        {
            if (currentState == GhostState.Chase)
            {
                currentState = GhostState.Scatter;
                modeTimer = scatterModeTime;
            }
            else
            {
                currentState = GhostState.Chase;
                modeTimer = chaseModeTime;
            }
            
            // When switching modes, ghosts typically reverse direction
            currentDirection = -currentDirection;
            // Only try to move if we're not already moving
            if (!isMoving)
            {
                TryMove(currentDirection);
            }
        }

        private void CheckIfStuck()
        {
            float distanceMoved = Vector2.Distance(lastPosition, transform.position);
            isStuck = distanceMoved < 0.1f;
            
            if (isStuck && !isMoving)
            {
                // Try to unstick by choosing a random direction
                ChooseRandomDirection();
            }
            
            lastPosition = transform.position;
        }

        public void SetFrightened(float duration)
        {
            if (currentState == GhostState.Eaten) return; // Ne postani Frightened, če si že pojeden

            if (frightenedTimerCoroutine != null)
            {
                StopCoroutine(frightenedTimerCoroutine);
            }
            currentState = GhostState.Frightened;
            if(imageComponent != null) imageComponent.color = frightenedColor; // Uporabi frightenedColor
            
            frightenedTimerCoroutine = StartCoroutine(FrightenedTimer(duration));
            
            // Duhovi se obrnejo, ko postanejo frightened
            if (!isMoving) // Če se ne premika že
            {
                Vector2 reverseDirection = -currentDirection;
                if(TryMove(reverseDirection)) // Poskusi se obrniti
                {
                    currentDirection = reverseDirection;
                } else { 
                    DecideNextDirection();
                }
            }
        }

        private IEnumerator FrightenedTimer(float duration)
        {
            float timeLeft = duration;
            while (timeLeft > 0)
            {
                timeLeft -= Time.deltaTime;
                if (timeLeft < duration * 0.4f) // Utripaj zadnjih 40% časa
                {
                    float flashSpeed = 0.5f; // Sekunde na cikel utripa
                    imageComponent.color = (Mathf.FloorToInt(timeLeft / (flashSpeed / 2f)) % 2 == 0) ? originalColorActual : frightenedColor;
                }
                else
                {
                    imageComponent.color = frightenedColor;
                }
                yield return null;
            }

            if (currentState == GhostState.Frightened) // Če ga ni vmes pojedel Pacman
            {
                ResetToNormalState();
            }
            frightenedTimerCoroutine = null;
        }

        public void ResetToNormalState()
        {
            // Return to normal state 
            currentState = GhostState.Chase;
            if(imageComponent != null) imageComponent.color = originalColorActual;
            modeTimer = chaseModeTime;
        }
    }
}