using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailControl : MonoBehaviour
{
    public TrailRenderer trail1;
    public TrailRenderer trail2;
    
    public Gradient trailColorGradient;
    // Start is called before the first frame update
    void Start()
    {
        // Initialize or modify trail properties if needed
        trail1.time = 9999999.0f;
        trail2.time = 9999999.0f;
        
        // Create and configure the gradient
        Gradient gradient = new Gradient();
        
        // Define the color keys and alpha keys
        GradientColorKey[] colorKey = new GradientColorKey[2];
        colorKey[0].color = Color.white; // Starting color
        colorKey[0].time = 0.0f;
        colorKey[1].color = Color.red; // Ending color
        colorKey[1].time = 1.0f;

        GradientAlphaKey[] alphaKey = new GradientAlphaKey[2];
        alphaKey[0].alpha = 1.0f; // Fully opaque at the start
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = 0.0f; // Fully transparent at the end
        alphaKey[1].time = 1.0f;

        // Assign the keys to the gradient
        gradient.SetKeys(colorKey, alphaKey);
        
        
        trail1.colorGradient = gradient;
        trail2.colorGradient = gradient;
    }

    // Update is called once per frame
    void Update()
    {
        
        // Example: Change trail width based on speed
        float speed = GetComponent<Rigidbody>().linearVelocity.magnitude;
        trail1.startWidth = Mathf.Lerp(0.1f, 0.5f, speed / 100f);
        trail2.startWidth = Mathf.Lerp(0.1f, 0.5f, speed / 100f);
    }
}
