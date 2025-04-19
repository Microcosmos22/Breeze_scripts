using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticCamControl : MonoBehaviour
{
private Rigidbody rb;
public float sensitivity = 4.0f;
public Vector3 offset;
private bool camera_frozen = false;
private float turnrate = 0.0035f;
private float omega;
// Start is called before the first frame update
void Start()
{
    rb = GetComponentInParent<Rigidbody>();
    offset = transform.position - rb.position;
    
    
}

// Update is called once per frame
void Update()
{
    
    
    float steer = Input.GetAxisRaw("Horizontal");
    print(steer);
    
    //transform.rotation.z = 0.0f;
    // Get the current rotation
    
    float oldheight = transform.position.y;
    
    offset = transform.position - rb.position;
    transform.position = rb.position + offset;
    transform.Translate(new Vector3(0.0f, 0.0f, -transform.position.y+oldheight));
    //transform.position.y = oldheight;
    
    
    
    float steerold = steer;
    Quaternion currentRotation = transform.rotation;
    
    omega = turnrate*steer*(-1);

    // Calculate the new quaternion with the desired z rotation
    Quaternion newRotation = Quaternion.Euler(currentRotation.eulerAngles.x, currentRotation.eulerAngles.y + omega, 0);

    // Apply the new rotation to the transform
    transform.rotation = newRotation;
    
    
    
    //transform.rotation.y = transform.rotation.y - omega;
    
}
public void set_camerafreeze(){
    camera_frozen = true;
}
public void set_cameraunfreeze(){
    camera_frozen = false;
}

}
