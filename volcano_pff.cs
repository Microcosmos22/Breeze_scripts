using UnityEngine;

public class volcano_pff : MonoBehaviour
{
    public ParticleSystem particleSystem;  // Reference to the Particle System
    public float windStrength = 0.3f;  // Strength of the wind
    public static Vector3 windDirection;
    
    void Start()
    {
        if (particleSystem == null)
        {
            Debug.LogError("Particle System reference is not set!");
            return;
        }
    }

    void Update()
    {
        ApplyWindForce();
    }
    
    public void set_atm_wind(Vector3 setted_atm_wind){
        windDirection = setted_atm_wind;
    }

    void ApplyWindForce()
    {
        
        
        // Get the particles
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
        int numParticlesAlive = particleSystem.GetParticles(particles);

        // Apply wind force to each particle
        for (int i = 0; i < numParticlesAlive; i++)
        {
            particles[i].velocity += windDirection * windStrength * Time.deltaTime;
        }

        // Set the particles back to the particle system
        particleSystem.SetParticles(particles, numParticlesAlive);
    
}}
