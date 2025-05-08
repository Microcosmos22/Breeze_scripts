using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using Mirror;
using System.Collections;
using UnityEngine.SceneManagement;


public class CloudFinder : NetworkBehaviour
{
    private GameObject player;
    private Rigidbody rb;
    private PlaneControl pc;
    private GliderControl gc;

    public Vector3 atm_wind_server, dist;
    public float thermals;
    public float wind_step;
    public float thermal_step;
    public float cloud_exp, relX, relZ, r, vX, vZ, vY, total_cloud_overhead_player, cpdistance, Cs, slope;


    public GameObject[] instantiatedClouds; // Array to store instantiated clouds
    private MeshCollider cloudCollider;

    public float cloudbase;
    public Terrain terrain;             // Reference to the Terrain
    private float mapWidth;             // Width of the map
    private float mapLength;            // Length of the maps

    private float clouds_overhead_player;

    public float cloud_sucksize = 100;
    public int i = 0;
    public int i_update_atm = 600;
    public ParticleForceField[] forcefields;

    private bool tornado_present;
    private WindZone windZone;

    // Start is called before the first frame update
    void Start(){
        StartCoroutine(FindTerrainInScenes());
        windZone = GetComponentInChildren<WindZone>();

        GameObject terrainObject = null;

        if (terrain == null){
            print(" Couldnt find the terrain Component");
        }

        forcefields = FindObjectsOfType<ParticleForceField>();

        mapWidth = terrain.terrainData.size.x;
        mapLength = terrain.terrainData.size.z;

        instantiatedClouds = GameObject.FindGameObjectsWithTag("Clouds");
        //Debug.Log("Clouds found: " + instantiatedClouds.Length);


        //print("Setting atm wind in pcs and particles");

        // Set wind for planes and gliders
    }

    // Update is called once per frame
    void Update(){
        //clouds_overhead_player = is_pc_undercloud()* 5f;

        //set_CloudBase(cloudbase, clouds_overhead_player);
        //set_quickatm_winds(atm_wind);
        if (terrain != null && NetworkServer.active && isServer){
            UpdateCloudLift();
            UpdatePlayersAtmWind();
        }
    }


    IEnumerator FindTerrainInScenes(){
        Debug.Log("🔍 Plane Co-searching for terrain in loaded scenes...");

        for (int i = 0; i < SceneManager.sceneCount; i++){
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded){
                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach (GameObject obj in rootObjects){
                    if (obj.CompareTag("Terrain")){  // Make sure your terrain has this tag set!
                        terrain = obj.GetComponent<Terrain>();
                        if (terrain != null){
                            Debug.Log("✅ Terrain found and assigned.");
                            yield break;
                        }
            }}}}
        Debug.LogWarning("❌ Plane found no terrain with 'Terrain' tag found in loaded scenes.");
    }


    private GameObject FindTerrainInNetwork()
    {
        foreach (var netId in NetworkServer.spawned)
        {
            GameObject obj = netId.Value.gameObject;

            // Check if the object is the terrain (e.g., by tag, name, or component)
            if (obj.CompareTag("Terrain")) // Assuming the terrain has a "Terrain" tag
            {
                return obj;
            }
        }

        return null; // Terrain not found
    }

    [Server]
    public void UpdatePlayersAtmWind(){
      atm_wind_server = new Vector3(1.7f,0f,-1.9f);

      windZone.transform.rotation = Quaternion.LookRotation(atm_wind_server);

      foreach (var netId in NetworkServer.spawned){
            player = netId.Value.gameObject;
            rb = player.GetComponent<Rigidbody>();
            pc = player.GetComponent<PlaneControl>();
            gc = player.GetComponent<GliderControl>();
            //print($"RpcSetAtmWind CALLED on {player.name}, Wind: {atm_wind_server}");



            if (player != null){
                if (pc != null){pc.RpcSetAtmWind(atm_wind_server);}
                if (gc != null){gc.RpcSetAtmWind(atm_wind_server);}

            }
        }
    }

    public void set_tornado_present(bool init_tornado){
        tornado_present = init_tornado;
    }

    [Server]
    private void UpdateCloudLift(){

        foreach (var netId in NetworkServer.spawned){
          player = netId.Value.gameObject;
          rb = player.GetComponent<Rigidbody>();
          pc = player.GetComponent<PlaneControl>();
          gc = player.GetComponent<GliderControl>();

          if (rb == null){
              continue; // Skip this player and move to the next one
          }
             total_cloud_overhead_player = 0f;

            foreach (GameObject cloud in instantiatedClouds){
                if (cloud == null) continue;

                 r = (Mathf.Sqrt(Mathf.Pow(cloud.transform.lossyScale.x,2f)+Mathf.Pow(cloud.transform.lossyScale.y,2f)+Mathf.Pow(cloud.transform.lossyScale.z,2f))) * 1.85f; //radius of the cloud
                 dist = cloud.transform.position - rb.position; // vector player cloud
                 cpdistance = (float)Math.Sqrt((float)Math.Pow(dist.x, 2f) + (float)Math.Pow(dist.z, 2f));

                if (cpdistance < r){
                     Cs = cloud.transform.lossyScale.x;
                     slope = -Cs / (2 * r);

                    total_cloud_overhead_player += Cs * 5 / 2 + cpdistance * slope; //returns size/strength of the cloud

                    // Now are we also INSIDE THE CLOUD?
                    cloudCollider = cloud.GetComponent<MeshCollider>();

                }
            }

            if (player != null){
                if (pc != null){pc.RpcSetCloudLift(cloudbase, total_cloud_overhead_player);}
                if (gc != null){gc.RpcSetCloudLift(cloudbase, total_cloud_overhead_player);}

            }

    }}


    public Vector3 CalculateTornadoVelocity(Vector3 tornadoCenter, float x, float y, float z, float k = 1000000.0f, float alpha = 100.0f, float beta = 0.0f, float H = 300.0f)
        {                                   // Computes the tornado component, depending on the plane's position
            // Calculate the relative position
             relX = x - tornadoCenter.x;
             relZ = z - tornadoCenter.z;

            // Calculate radial distance
             r = Mathf.Sqrt(relX * relX + relZ * relZ);


            // Calculate velocity components
             vX = k * (relZ / (r*r));  // Radial component in X
             vZ = -k * (relX / (r*r));    // Radial component in Z
             vY = Mathf.Clamp(alpha * Mathf.Exp(-beta * z) / r, 0f, 7f);
             ; // Vertical component, decaying with height

            return new Vector3(vX, vY, vZ);
        }

}
