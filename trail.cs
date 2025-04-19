using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneTrailController : MonoBehaviour
{
    public TrailRenderer trail1;
    public TrailRenderer trail2;
    
    public Gradient trailColorGradient;

    void Start()
    {
        // Initialize or modify trail properties if needed
        trail1.time = 9999999.0f;
        trail2.time = 9999999.0f;
        
        trail1.colorGradient = trailColorGradient;
        trail2.colorGradient = trailColorGradient;
        
    }

    void Update()
    {
        // Example: Change trail width based on speed
        float speed = GetComponent<Rigidbody>().linearVelocity.magnitude;
        trail1.startWidth = Mathf.Lerp(0.1f, 0.5f, speed / 100f);
        trail2.startWidth = Mathf.Lerp(0.1f, 0.5f, speed / 100f);
    }
}
