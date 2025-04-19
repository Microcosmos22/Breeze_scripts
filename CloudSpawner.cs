using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class CloudSpawner : MonoBehaviour
{
    public Vector3 atm_wind;
    public float thermals;
    public float wind_step;
    public float thermal_step;
    public float cloud_exp;
    public int meancloudnumber;
    public int stdcloudnumber;
    public float meancloudsize;
    public float stdcloudsize;

    public GameObject[] hugeCloudPrefabs;   // Array for huge cloud prefabs
    private GameObject[] instantiatedClouds; // Array to store instantiated clouds

    public GameObject cloudParent;      // The parent GameObject for all clouds
    public GameObject[] planeplayers;              // Reference to the Player script
    public GameObject[] gliderplayers;              // Reference to the Player script

    public List<PlaneControl> pcs = new List<PlaneControl>();
    public List<GliderControl> gcs = new List<GliderControl>();

    private float cloudbase;    // Height at which clouds will be spawned
    public Terrain terrain;             // Reference to the Terrain
    private int numberOfClouds;     // Number of clouds to spawn
    public float renderDistance = 500f; // Render distance for clouds

    private float mapWidth;             // Width of the map
    private float mapLength;            // Length of the map

    private float clouds_overhead_player;
    public Material cloudMaterial;

    public float cloud_sucksize;
    public int i = 0;
    public int i_update_atm = 600;
    public ParticleForceField[] forcefields;

    void Start()
    {
        print("Starting weather systemm");

        GameObject terrainObject = GameObject.FindWithTag("Terrain");
        //land = terrainObject.Terrain;
        terrain = terrainObject.GetComponent<Terrain>();

        // Find all players in Scene
        PlaneControl[] planeControls = FindObjectsOfType<PlaneControl>();
        pcs = planeControls.ToList();
        GliderControl[] gliderControls = FindObjectsOfType<GliderControl>();
        gcs = gliderControls.ToList();
        forcefields = FindObjectsOfType<ParticleForceField>();

        if (terrain == null)
        {
            Debug.LogError("Terrain reference is not set!");
            return;}

        // Get the dimensions of the terrain
        mapWidth = terrain.terrainData.size.x;
        mapLength = terrain.terrainData.size.z;
        print(mapWidth);


    }

    void SpawnClouds(int number_clouds, float size_clouds)
    {
        System.Random r = new System.Random();
        for (int i = 0; i < number_clouds; i++)
        {
            float dheight = (float)r.NextDouble() * 40f;
            Vector3 spawnPosition = new Vector3(
                (float)r.NextDouble() * mapWidth,
                cloudbase,
                (float)r.NextDouble() * mapLength
            );

            GameObject cloudPrefab = hugeCloudPrefabs[0];
            GameObject newCloud = Instantiate(cloudPrefab, spawnPosition, Quaternion.identity, cloudParent.transform);
            newCloud.layer = 3;

            // RANDOM ORIENTATION AND SIZES
            newCloud.transform.rotation = Quaternion.Euler(new Vector3((float)r.NextDouble() * 360f, (float)r.NextDouble() * 360f, (float)r.NextDouble() * 360f));
            float scaling = (float)r.NextDouble() * size_clouds;
            newCloud.transform.localScale = newCloud.transform.localScale * scaling;

            instantiatedClouds[i] = newCloud;

            // Set the render distance for the cloud
            SetCloudRenderDistance(instantiatedClouds[i]);

            // Assign material to cloud
            MeshRenderer cloudRenderer = newCloud.GetComponent<MeshRenderer>();
            if (cloudRenderer != null && cloudMaterial != null)
                cloudRenderer.material = cloudMaterial;

            Collider collider = newCloud.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false; // Disable the collider
        }
    }

    void Update()
    {
        i += 1;
        if (i % i_update_atm == 0)
        {
            atm_wind = update_weather(atm_wind);
        }

    }

    private float is_pc_undercloud(PlaneControl pc)
    {
        float total_cloud_overhead_player = 0f;

        foreach (GameObject cloud in instantiatedClouds)
        {
            if (cloud == null) continue;

            float r = cloud.transform.lossyScale.x * cloud_sucksize; //radius of the cloud
            Vector3 dist = cloud.transform.position - pc.rb.position; // vector player cloud
            float cpdistance = (float)Math.Sqrt((float)Math.Pow(dist.x, 2f) + (float)Math.Pow(dist.z, 2f));

            if (cpdistance < r)
            {
                float Cs = cloud.transform.lossyScale.x;
                float slope = -Cs / (2 * r);

                total_cloud_overhead_player += Cs * 5 / 4 + cpdistance * slope; //returns size/strength of the cloud
            }
        }
        return total_cloud_overhead_player;
    }

    private float is_gc_undercloud(GliderControl gc)
    {
        float total_cloud_overhead_player = 0f;

        foreach (GameObject cloud in instantiatedClouds)
        {
            if (cloud == null) continue;

            float r = cloud.transform.lossyScale.x * cloud_sucksize; //radius of the cloud
            Vector3 dist = cloud.transform.position - gc.rb.position; // vector player cloud
            float cpdistance = (float)Math.Sqrt((float)Math.Pow(dist.x, 2f) + (float)Math.Pow(dist.z, 2f));

            if (cpdistance < r)
            {
                float Cs = cloud.transform.lossyScale.x;
                float slope = -Cs / r;

                total_cloud_overhead_player += Cs * 3 / 2 + cpdistance * slope; //returns size/strength of the cloud
            }
        }
        return total_cloud_overhead_player;
    }

    public Vector3 update_weather(Vector3 atm_wind)
    {
        System.Random r = new System.Random();

        float dx = ((float)r.NextDouble() - 0.5f) * 2f * wind_step;
        float dz = ((float)r.NextDouble() - 0.5f) * 2f * wind_step;

        atm_wind += new Vector3(dx, 0f, dz); // Update atmospheric wind vector
        atm_wind.y = 0f;

        thermals += ((float)r.NextDouble() - 0.5f) * 2f * thermal_step;

        return atm_wind;
    }


    void SetCloudRenderDistance(GameObject cloud)
    {
        // Example method to adjust cloud render distance
        if (cloud != null)
        {
            Renderer cloudRenderer = cloud.GetComponent<Renderer>();
            if (cloudRenderer != null)
            {
                cloudRenderer.enabled = true;
            }
        }
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
