using UnityEngine;

public class SunMovement : MonoBehaviour
{
    public float dayLengthInSeconds = 120f; // Length of a full day in seconds
    public Transform sunTransform; // Reference to the sun object's Transform

    void Update()
    {
        // Calculate current rotation angle based on time of day
        float angle = Mathf.Repeat(Time.time / dayLengthInSeconds, 1f) * 360f;
        
        // Set sun's rotation based on the calculated angle
        sunTransform.rotation = Quaternion.Euler(angle, 0f, 0f);
    }
}
