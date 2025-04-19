using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class CloudWake : MonoBehaviour
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
    private GameObject[] instantiatedSinks;

    public List<PlaneControl> pcs = new List<PlaneControl>();
    public List<GliderControl> gcs = new List<GliderControl>();

    public float cloudbase;    // Height at which clouds will be spawned
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
        // Find all players in Scene
        PlaneControl[] planeControls = FindObjectsOfType<PlaneControl>();
        pcs = planeControls.ToList();
        GliderControl[] gliderControls = FindObjectsOfType<GliderControl>();
        gcs = gliderControls.ToList();
        forcefields = FindObjectsOfType<ParticleForceField>();

        // Get the dimensions of the terrain
        mapWidth = terrain.terrainData.size.x;
        mapLength = terrain.terrainData.size.z;
        print(mapWidth);



        string logFilePath = "Assets/Logs/parameters_log.txt"; // Adjust the path as needed

        SpawnClouds();




        // Assuming Player script is attached to a GameObject in the scene
        //SpawnClouds(numberOfClouds);
    }

    public void SpawnClouds()
    {

        //Debug.Log("Spawning Clouds for parent: " + transform.parent.name);

        int numberOfClouds = (int)Math.Round(NextGaussian(meancloudnumber, stdcloudnumber));

        print("WEATHER GENERATION: ");
        print("Cloud number: " + numberOfClouds.ToString());
        print("Cloud mean size: " + meancloudsize.ToString());
        print("Cloud base: " + cloudbase.ToString());

        // Initialize the array for instantiated clouds
        instantiatedClouds = new GameObject[numberOfClouds];
        instantiatedSinks = new GameObject[numberOfClouds];

        System.Random r = new System.Random();
        for (int i = 0; i < numberOfClouds; i++)
        {

            Vector3 spawnPosition = new Vector3(
                ((float)r.NextDouble() * mapWidth) + transform.position.x,
                cloudbase,
                ((float)r.NextDouble() * mapLength) + transform.position.z
            );


            GameObject cloudPrefab = hugeCloudPrefabs[0];
            GameObject newCloud = Instantiate(cloudPrefab, spawnPosition, Quaternion.identity, transform);
            newCloud.layer = 3;

            // RANDOM ORIENTATION AND SIZES
            newCloud.transform.rotation = Quaternion.Euler(new Vector3((float)r.NextDouble() * 360f, (float)r.NextDouble() * 360f, (float)r.NextDouble() * 360f));
            float size = NextGaussian(meancloudsize, stdcloudsize);
            newCloud.transform.localScale = new Vector3(size, size, size);

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

        //   For each cloud we instantiate a sink GameObject
        for (int i = 0; i < numberOfClouds; i++)
        {
            GameObject sink = new GameObject("Sink");
            sink.transform.SetParent(transform);

            float min_clouddistance = 0f;


            while(min_clouddistance < 70f){ // Find a sink position that is not too close
                sink.transform.position = new Vector3(
                        ((float)r.NextDouble() * mapWidth) + transform.position.x,
                        cloudbase,
                        ((float)r.NextDouble() * mapLength) + transform.position.z);

                min_clouddistance = 2000f;
                foreach (GameObject cloud in instantiatedClouds)                            /// CLOSEST DISTANCE TO A CLOUD
                    {
                    Vector3 dist = (cloud.transform.position) - sink.transform.position; // vector player cloud offsetted by parent position
                    float cs_distance = (float)Math.Sqrt((float)Math.Pow(dist.x, 2f) + (float)Math.Pow(dist.z, 2f));  // distance scalar

                    if (cs_distance < min_clouddistance){
                        min_clouddistance = cs_distance;}
                    }
                }

            float size = 0.8f*NextGaussian(meancloudsize, stdcloudsize);
            sink.transform.localScale = new Vector3(size, size, size);

            instantiatedSinks[i] = sink;
        }
    }

    public void set_all_atm_winds(Vector3 scene_atm_wind){
        atm_wind = scene_atm_wind;

    }

    public void set_quickatm_winds(Vector3 scene_atm_wind){
        atm_wind = scene_atm_wind;

    }

    private void Update()
    {
    }

    private float is_pc_undercloud(PlaneControl pc)
    {
        float total_cloud_overhead_player = 0f;

        foreach (GameObject cloud in instantiatedClouds)                            /// CLOUDS
        {
            if (cloud == null) continue;

            float r = cloud.transform.lossyScale.x * cloud_sucksize; //radius of the cloud
            Vector3 dist = (cloud.transform.position) - pc.rb.position; // vector player cloud offsetted by parent position
            float cpdistance = (float)Math.Sqrt((float)Math.Pow(dist.x, 2f) + (float)Math.Pow(dist.z, 2f));  // distance scalar

            if (cpdistance < r)
            {
                float Cs = cloud.transform.lossyScale.x;
                float slope = -Cs / (r);

                total_cloud_overhead_player += Clip(Cs * 5 / 4 + cpdistance * slope, 0, 1000f); //returns size/strength of the cloud
            }
        }

        foreach (GameObject sink in instantiatedSinks)                              /// SINKS
        {
            if (sink == null) continue;

            float r = sink.transform.lossyScale.x * cloud_sucksize; //radius of the cloud
            Vector3 dist = (sink.transform.position) - pc.rb.position; // vector player cloud
            float cpdistance = (float)Math.Sqrt((float)Math.Pow(dist.x, 2f) + (float)Math.Pow(dist.z, 2f)); // distance scalar

            if (cpdistance < r)
            {
                float Cs = sink.transform.lossyScale.x;
                float slope = -Cs / (r);

                total_cloud_overhead_player -= Clip(Cs * 5 / 4 + cpdistance * slope, 0, 1000f); //returns size/strength of the cloud
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
            Vector3 dist = (cloud.transform.position + transform.position) - gc.rb.position; // vector player cloud
            float cpdistance = (float)Math.Sqrt((float)Math.Pow(dist.x, 2f) + (float)Math.Pow(dist.z, 2f));

            if (cpdistance < r)
            {
                float Cs = cloud.transform.lossyScale.x;
                float slope = -Cs / r;

                total_cloud_overhead_player += Cs * 3 / 2 + cpdistance * slope; //returns size/strength of the cloud
            }
        }

        foreach (GameObject sink in instantiatedSinks)                              /// SINKS
        {
            if (sink == null) continue;

            float r = sink.transform.lossyScale.x * cloud_sucksize; //radius of the cloud
            Vector3 dist = (sink.transform.position + transform.position) - gc.rb.position; // vector player cloud
            float cpdistance = (float)Math.Sqrt((float)Math.Pow(dist.x, 2f) + (float)Math.Pow(dist.z, 2f));

            if (cpdistance < r)
            {
                float Cs = sink.transform.lossyScale.x;
                float slope = -Cs / (2 * r);

                total_cloud_overhead_player -= Cs * 5 / 4 + cpdistance * slope; //returns size/strength of the cloud
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

    void UpdateCloudPositions()
    {
        foreach (GameObject cloud in instantiatedClouds)
        {
            if (cloud == null) continue;

            // Move the cloud based on wind direction
            cloud.transform.position += atm_wind * Time.deltaTime;
        }

        // PERIODIC BOUNDARIES
        foreach (GameObject cloud in instantiatedClouds)
        {
            if (cloud == null) continue;

            Transform cloudTransform = cloud.transform;
            Vector3 cloudPosition = cloudTransform.position;

            // Get terrain size
            float terrainSizeX = terrain.terrainData.size.x;
            float terrainSizeZ = terrain.terrainData.size.z;

            /*
            // Apply periodic boundary conditions
            while (cloudPosition.x < 0 || cloudPosition.x > terrainSizeX || cloudPosition.z < 0 || cloudPosition.z > terrainSizeZ)
            {
                if (cloudPosition.x < 0)
                    cloudPosition.x += terrainSizeX;
                else if (cloudPosition.x > terrainSizeX)
                    cloudPosition.x -= terrainSizeX;

                if (cloudPosition.z < 0)
                    cloudPosition.z += terrainSizeZ;
                else if (cloudPosition.z > terrainSizeZ)
                    cloudPosition.z -= terrainSizeZ;
            }
            */
            // Assign the corrected position back to the cloud
            //cloud.transform.position = cloudPosition;
        }
    }

    public void find_planecontrolscript(){
        PlaneControl[] planeControls = FindObjectsOfType<PlaneControl>();
        pcs = planeControls.ToList();
    }

    public static float Clip(float value, float min, float max)
    {
        if (value < min)
        {
            return min;
        }
        else if (value > max)
        {
            return max;
        }
        return value;
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
