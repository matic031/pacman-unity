using UnityEngine;
using UnityEngine.UI;

public class ImageSequencePlayer : MonoBehaviour
{
    public Image imageComponent;  // Reference to the UI Image component
    public Sprite[] frames;       // Array of frames (images) to play
    public float frameDuration = 0.1f;  // Duration of each frame

    public int currentFrameIndex = 0;
    private float timeSinceLastFrame = 0.0f;

    private void Update()
    {
        // Check if it's time to switch to the next frame
        timeSinceLastFrame += Time.deltaTime;
        if (timeSinceLastFrame >= frameDuration)
        {
            // Update the image to the next frame
            imageComponent.sprite = frames[currentFrameIndex];

            // Move to the next frame index
            currentFrameIndex = (currentFrameIndex + 1) % frames.Length;

            // Reset the timer
            timeSinceLastFrame = 0.0f;
        }
    }
}





