using UnityEngine;

public class WindParticleMovement : MonoBehaviour
{
    public Vector3 windDirection = new Vector3(10, 0, 0); // Set the desired wind direction
    public float windStrength = 1f; // Adjust the strength of the wind

    private ParticleSystem particleSystem;
    private ParticleSystem.Particle[] particles;

    void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
    }

    void Update()
    {
        if (particles != null){
            int particleCount = particleSystem.GetParticles(particles);

            for (int i = 0; i < particleCount; i++)
            {
                // Apply wind direction
                particles[i].velocity += windDirection.normalized * windStrength * Time.deltaTime;
            }

            particleSystem.SetParticles(particles, particleCount);
        }
    }
}
