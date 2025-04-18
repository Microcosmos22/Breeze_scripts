using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class BasicPlayerController : MonoBehaviour
{
    private Rigidbody rb;
    public GameObject cameraTarget;
    public float movementIntensity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
 
    void Update()
    {
        var ForwardDirection = cameraTarget.transform.forward;
        var RightDirection = cameraTarget.transform.right;

        // Move Forwards
        if (Input.GetKey(KeyCode.W)) 
        {
            rb.AddForce (ForwardDirection * movementIntensity);
            /* You may want to try using velocity rather than force.
            This allows for a more responsive control of the movement
            possibly better suited to first person controls, eg: */
            //rb.velocity = ForwardDirection * movementIntensity;
        }
        // Move Backwards
        if (Input.GetKey(KeyCode.S))
        {
            // Adding a negative to the direction reverses it
            rb.AddForce (-ForwardDirection * movementIntensity);
        }
        // Move Rightwards (eg Strafe. *We are using A & D to swivel)
        if (Input.GetKey(KeyCode.E))
        {
           rb.AddForce (RightDirection * movementIntensity);
        }
        // Move Leftwards
        if (Input.GetKey(KeyCode.Q))
        {
           rb.AddForce (-RightDirection * movementIntensity);
        }
    }
}