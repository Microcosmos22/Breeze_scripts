using UnityEngine;

public class TrailColorChanger : MonoBehaviour
{
    public TrailRenderer trail1; // Assign in the Inspector
    public TrailRenderer trail2; // Assign in the Inspector
    private Color brighterGreen = new Color(0.5f, 1f, 0.5f); // Higher brightness
    private Color brighterRed = new Color(1f, 0.3f, 0.3f);

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Get the vertical velocity
        float verticalVelocity = rb.linearVelocity.y;

        // Calculate the color based on vertical velocity
        Color trailColor = CalculateTrailColor(verticalVelocity);

        // Set the color of the trails
        SetTrailColor(trail1, trailColor);
        SetTrailColor(trail2, trailColor);
    }

    Color CalculateTrailColor(float velocity)
    {
        // For [-4, +4], transforms to  [0, 1] for the color interpolation
        float velocity_coeff = Mathf.Clamp(velocity, -4f, 4f)/8f + 0.5f;

        // Calculate color based on the clamped vertical velocity
        return Color.Lerp(brighterRed, brighterGreen, velocity_coeff);
        /*if (clampedVelocity < 0) // Descending
        {
            return Color.Lerp(Color.red, Color.clear, -clampedVelocity / 4f);
        }
        else // Ascending
        {
            return Color.Lerp(Color.green, Color.clear, clampedVelocity / 4f);
        }*/
    }

    void SetTrailColor(TrailRenderer trail, Color color)
    {
        // Set the trail color
        trail.startColor = color;
        trail.endColor = color; // You can change this if needed
    }
}
