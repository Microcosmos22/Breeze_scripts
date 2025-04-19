
using UnityEngine;
using UnityEngine.UI;

public class VelocitySlider : MonoBehaviour
{
    public Rigidbody playerRigidbody; // Reference to the player's Rigidbody
    public Slider velocitySlider; // Reference to the Slider component

    void Start()
    {
        if (velocitySlider == null)
        {
            velocitySlider = GetComponent<Slider>();
        }
    }

    void Update()
    {
        if (playerRigidbody != null)
        {
            // Get the player's velocity magnitude
            float velocity = playerRigidbody.linearVelocity.y;

            // Update the slider value with the current velocity
            velocitySlider.value = velocity;
        }
    }
}