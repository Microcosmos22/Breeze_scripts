using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class tools : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public float NextGaussian(float mean = 0, float standardDeviation = 1)
    {
        System.Random random = new System.Random();
        double u1 = random.NextDouble(); // Uniform(0,1) random doubles
        double u2 = random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // Random normal(0,1)
        float randNormal = mean + standardDeviation * (float)randStdNormal; // Random normal(mean,stdDev)
        return (float)randNormal;
    }
    
}


