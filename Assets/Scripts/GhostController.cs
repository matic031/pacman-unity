using UnityEngine;

namespace MazeTemplate
{
    public enum GhostState
    {
        Chase,
        Scatter,
        Frightened
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
        private SpriteRenderer spriteRenderer;
        private Vector2 currentDirection;
        private Vector2[] possibleDirections = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        private float modeTimer;
        private Vector2 lastPosition;
        private bool isStuck = false;
        private float stuckCheckTimer = 0f;
        private float stuckCheckInterval = 0.5f;
        
        // Tile-based movement variables
        private Vector3 targetPosition;
        private bool isMoving = false;

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Try to find the player if not assigned in the inspector
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
            }

            // Set ghost color
            if (spriteRenderer != null)
                spriteRenderer.color = ghostColor;
            
            // Ensure the ghost is aligned to the grid
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
            modeTimer -= Time.deltaTime;
            if (modeTimer <= 0)
            {
                SwitchMode();
            }
            
            // Check if the ghost is stuck
            stuckCheckTimer += Time.deltaTime;
            if (stuckCheckTimer >= stuckCheckInterval)
            {
                CheckIfStuck();
                stuckCheckTimer = 0f;
            }
            
            // Tile-based movement
            if (isMoving)
            {
                // Move smoothly towards the target tile
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                
                // Check if we've reached the target position
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    // Snap to exact position to avoid floating point errors
                    transform.position = targetPosition;
                    isMoving = false;
                    
                    // Choose next direction when reaching a tile center
                    DecideNextDirection();
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
            // Check if we collided with the player
            if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Debug.Log("Game Over");
                // Optional: You could disable the player movement here
                // collision.gameObject.GetComponent<PlayerController>().enabled = false;
            }
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
            // This would be called when Pacman eats a power pellet
            currentState = GhostState.Frightened;
            spriteRenderer.color = Color.blue;
            modeTimer = duration;
            
            // Reverse direction when entering frightened mode
            currentDirection = -currentDirection;
            if (!isMoving)
            {
                TryMove(currentDirection);
            }
        }

        public void ResetToNormalState()
        {
            // Return to normal state (called after frightened mode ends)
            currentState = GhostState.Chase;
            spriteRenderer.color = ghostColor;
            modeTimer = chaseModeTime;
        }
    }
}