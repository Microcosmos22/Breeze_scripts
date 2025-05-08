using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Mirror; // Import Mirror
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

public class PlaneControl : NetworkBehaviour, IVehicleControl{
    public Rigidbody rb;
    public BoxCollider collider;
    public Vector3 airspeed;
    private Vector3 newTotalVel;
    private int x;
    private int z;
    private float wy, hx, hz, kappa, dv_hor, my_angular_vel, my_climb_vel, climb, v_hor, distance_tobase, trailSpeed, elapsedTime;
    private bool exploded, breaks;
    float ho, hxRight, hxLeft, hzForward, hzBackward, gradientX, gradientZ;
    public Camera playerCamera;

    private ParticleSystem windParticles;

    private Quaternion aimAIQ = Quaternion.identity; // Default rotation
    public bool playerDetected;

    public float scrollInput, steer, pitch, omega, climb_vel, sensitivity, wind_step, thermal_step,h_overterrain, roll_angularvel;

    public float thermals = 6.56f;
    public float cloud_exp = 0.005f;
    public float slope_exp = 0.04f;
    public float slope_const = 4.2f;
    public float glide_desc { get; set; } = -0.15f;
    public Vector3 cloud_suction;
    public Vector3 atm_wind = new Vector3(0f, 0f, 0f);
    private Vector3 pos;
    private Vector3 pospx, pospy, pospz;
    private bool isMouseLocked;

    public Vector3 w_exp, e_w, tornado, forward_airspeed, particlePos, newVelocity, gradient, e_grad;
    public Terrain land;
    public Vector3 slope_vel = new Vector3(0f,0f,0f);
    private Vector3 glide_vec = new Vector3(0.0f, -0.15f, 0.0f);
    private float local_roll_angle;
    private float local_phi;

    public Quaternion gun_quaternion;
    public float maxHealth = 100f; // Maximum health of the plane
    [SyncVar] private float currentHealth; // Current health (synchronized)
    public float healthBar; // UI Health Bar

    private UIVirtualJoystick virtualJoystick;
    private float steepness;
    private float w_strength;

    private CamFollower camFollow;
    private bool isGuideComplete = false; // Track if the user has clicked "fly"
    public bool crashed = false;

    private float syncRate = 0.2f; // Update frequency in seconds (0.2s)
    private float timeSinceLastSync = 0f; // Timer to track elapsed time

    private float[,] heightMap; // Cached height data
    private int terrainWidth, terrainHeight;
    private float terrainSizeX, terrainSizeZ;
    private TerrainData terrainData;
    private Vector3 terrainPosition;
    private Vector3 breakvec = new Vector3(0f,-4f,0f);
    private Vector3 elevvec = new Vector3(0f, 5f, 0f);

    public float AIsteer { get; set; }
    public float AIpitch { get; set; }
    public bool AIbreaks { get; set; }
    public bool isAI { get; set; }

    public float gunCoolTimer;
    private float slopeCalcTimer = 0f;
    private float healingTimer = 0f;
    public float healingTime;
    public float gunUptime;
    public bool isCoolingDown = false;
    public string Username;

    public GameObject explosionPrefabs, vfx;
    private GameObject explosionInstance;


    public NetworkIdentity networkIdentity;
    public BulletManager bulletManager;
    public PanelManager PanelManager;
    public List<GameObject> ownBullets = new List<GameObject>();
    public bool ispaused = true;

    private AudioSource audioSource;
    public AudioClip shootSound;

    void FixedUpdate(){


        if (!ispaused && land != null && ((isLocalPlayer) || (isAI))){

            if (isLocalPlayer){
              //healthBar = 100f;

              healingTimer += Time.deltaTime;
              if (healthBar < 100f && healingTimer > healingTime){
                  healthBar += 1f;
                  TargetTakeDamage(networkIdentity.connectionToClient, -1f);
                  healingTimer = 0f;
                  }
              else if(healthBar<0f){

                  healthBar = 100f;
                  RpcSpawnExplosion(transform.position);

                  set_initpos();
              }

              scrollInput = Input.GetAxis("Mouse ScrollWheel");
              bulletManager.explosionTime += scrollInput * 2f; // Adjust FOV
              bulletManager.explosionTime = Mathf.Clamp(bulletManager.explosionTime, 52f/bulletManager.bulletspeed, 360f/bulletManager.bulletspeed);
              CmdUpdateExplosionTime(bulletManager.explosionTime);

              if(gunCoolTimer > 0f && !Input.GetMouseButton(0)){ // Counts down in both cases, isCoolingDown and !isCoolingDown
                  gunCoolTimer -= Time.deltaTime;
              }else if(gunCoolTimer > gunUptime){
                  isCoolingDown = true;
              }else if (Input.GetMouseButton(0) && !isCoolingDown){
                  gunCoolTimer += Time.deltaTime;

                  if (Time.time - bulletManager.lastFireTime > bulletManager.fireRate){
                    if (isClient && audioSource != null)
                    {
                      audioSource.PlayOneShot(shootSound);
                    }

                      bulletManager.lastFireTime = Time.time; // Update last fire time
                      if (isLocalPlayer && NetworkClient.isConnected && isOwned){

                          bulletManager.CmdShootBullet(camFollow.get_camera_quaternion());
                      }}

              }else if(isCoolingDown && gunCoolTimer < 0f){
                  isCoolingDown = false;
              }


              steer = Input.GetAxisRaw("Horizontal");
              pitch = Input.GetAxisRaw("Vertical");
              breaks = Input.GetKey(KeyCode.B);

              airspeed = steer_plane(airspeed, steer, pitch);
              //print($"player height over terrain {transform.position.y-GetCachedHeight(transform.position)}");


              slopeCalcTimer += Time.deltaTime;
              if ((slopeCalcTimer>0.1f)){
                  slope_vel = calculate_slopewind(atm_wind, transform.position);
                  slopeCalcTimer = 0f;
              }

              if(breaks == true){
                  newTotalVel = airspeed + slope_vel + cloud_suction + glide_vec + tornado + breakvec;
              }else{
                  newTotalVel = airspeed + slope_vel + cloud_suction + glide_vec + tornado;
              }


              UpdateParticleTrajectories();

            }else if(isAI){ // For AI

                if(gunCoolTimer > 0f && !playerDetected){ // Cooling down, each frame
                    gunCoolTimer -= Time.deltaTime;
                }else if(gunCoolTimer > gunUptime){
                    isCoolingDown = true;
                    playerDetected = false;
                }else if (playerDetected && !isCoolingDown){

                    gunCoolTimer += Time.deltaTime;

                    if (Time.time - bulletManager.lastFireTime > bulletManager.fireRate){
                        if (audioSource != null){
                          audioSource.PlayOneShot(shootSound);
                        }

                        bulletManager.lastFireTime = Time.time; // Update last fire time

                        print(" shooting bullet timer");
                        bulletManager.AICmdShootBullet(aimAIQ);
                        /*if (NetworkClient.isConnected){
                            bulletManager.AICmdShootBullet(aimAIQ);
                        }*/
                      }
                }else if(isCoolingDown && gunCoolTimer < 0f){
                    isCoolingDown = false;
                    playerDetected = true;
                }

                 steer = AIsteer;
                 pitch = AIpitch;
                 breaks = AIbreaks;
                 //print($"Steering AI PlaneControl: {steer}");
                airspeed = steer_plane(airspeed, steer, pitch);
                newTotalVel = airspeed;
              }

            if (!(float.IsNaN(newTotalVel.x) || float.IsNaN(newTotalVel.y) || float.IsNaN(newTotalVel.z))){
                rb.linearVelocity = newTotalVel;

            }

            forward_airspeed = Vector3.Normalize(airspeed);
            set_plane_orientation(steer, pitch, forward_airspeed);
            //cloud_suction = new Vector3(0f,0f,0f);
            }
        }

    [ClientRpc]
    void RpcSpawnExplosion(Vector3 position) {
        vfx = Instantiate(explosionPrefabs, position, Quaternion.identity);
        StartCoroutine(DestroyExplosionAfterTime(vfx, 1f));
    }

    public IEnumerator DestroyExplosionAfterTime(GameObject explosionInstance, float time){
        yield return new WaitForSeconds(time);
        Destroy(explosionInstance);
    }

    [Command]
    void CmdUpdateExplosionTime(float time) {
        bulletManager.explosionTime = time;
    }

    public void setAim(Quaternion newAim){
        aimAIQ = newAim;
    }

    public override void OnStartClient()
        {
            base.OnStartClient();

            // Check if this object is owned by the local player
            if (!isLocalPlayer && playerCamera != null)
            {
                // Disable the camera or any other components for non-local players
                playerCamera.enabled = false;
            }
        }

    void Start()
    {
      audioSource = GetComponent<AudioSource>();


      StartCoroutine(FindTerrainInScenes());
      gunCoolTimer = UnityEngine.Random.Range(0f, 8f);

      bulletManager = GetComponent<BulletManager>();
      networkIdentity = GetComponent<NetworkIdentity>();

      if (isLocalPlayer || isAI){ //
          if (healthBar != null){
              healthBar = maxHealth;
          }


        StartCoroutine(Wait4Terrain_ThenCache());

        camFollow = GetComponentInChildren<CamFollower>();

        bulletManager.explosionTime = 60f/bulletManager.bulletspeed;
        Application.targetFrameRate = -1;

        if (GetComponentInChildren<ParticleSystem>() != null){
            windParticles = GetComponentInChildren<ParticleSystem>();
        }



        local_roll_angle = 0f;
        roll_angularvel = 0.35f; // degrees/frame

        rb = this.GetComponent<Rigidbody> ();
        collider = this.GetComponent<BoxCollider> ();
        climb_vel = 2.0f;
        sensitivity = 0.5f;

      if (!isAI){
          set_initpos();}
      }
    }



    IEnumerator FindTerrainInScenes(){
    while (land == null){
        for (int i = 0; i < SceneManager.sceneCount; i++){
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded){
                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach (GameObject obj in rootObjects){
                    if (obj.CompareTag("Terrain")){
                        land = obj.GetComponent<Terrain>();
                        if (land != null) yield break;
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.1f);
    }
}

    public float GetCachedHeight(Vector3 position){
        if (land == null){
            return 0f;
        } else if (heightMap == null){
          return 0f;
        }

        // Convert world position to height map indices
        x = Mathf.Clamp(Mathf.RoundToInt((position.x) / terrainSizeX * terrainWidth), 0, terrainWidth - 1);
        z = Mathf.Clamp(Mathf.RoundToInt((position.z) / terrainSizeZ * terrainHeight), 0, terrainHeight - 1);

        return heightMap[z, x];
    }

    IEnumerator Wait4Terrain_ThenCache(){
        while (land == null || land.terrainData == null){
            print(" PlaneControl Waiting for terrain to cache");
            yield return null; // Wait for the next frame
        }

        terrainData = land.terrainData;
        terrainPosition = land.transform.position;

        terrainWidth = terrainData.heightmapResolution;
        terrainHeight = terrainData.heightmapResolution;
        terrainSizeX = terrainData.size.x;
        terrainSizeZ = terrainData.size.z;

        heightMap = terrainData.GetHeights(0, 0, terrainWidth, terrainHeight);

        for (int x = 0; x < terrainWidth; x++){
            for (int z = 0; z < terrainHeight; z++){
                heightMap[x,z] = heightMap[x,z] * terrainData.size.y + terrainPosition.y;
            }
        }
    }

    public void set_airspeed(Vector3 air){
        airspeed = air;
    }

    public void set_initpos(){
        transform.position = new Vector3(15f, 15f, 1015f);
        //transform.position = new Vector3( 400f, 140f, 500f );
        rb.linearVelocity = transform.forward * 5f;
        healthBar = 100f;
    }

    [Server]
    public void ServerHandleAIDeath() {
        healthBar = 100f;
        RpcSpawnExplosion(transform.position);  // Sync explosion to all clients

    }

    [TargetRpc]
    public void TargetSetInitpos(NetworkConnection target){

        //transform.position = new Vector3(5f, 15f, 1015f);
        transform.position = new Vector3(500f, 100f, 600f);
        rb.linearVelocity = transform.forward * 5f;
        healthBar = 100f;
    }

    [TargetRpc]
    public void TargetTakeDamage(NetworkConnection target, float hp_damage) {
        healthBar -= hp_damage;
        //print($"Damaging Player in client. Health: {healthBar}");
    }

    [TargetRpc]
    public void TargetResetHealth(NetworkConnection target){
        healthBar = 100f;
    }

    public void OnCollisionEnter(Collision collision){
      if (!enabled) return;
      if (!isServer) return;

      Debug.Log("Collision with: " + collision.gameObject.name);

      if (!isAI){ // Player
          if (collision.gameObject.CompareTag("Terrain")){
              healthBar -= 102f; // Damage in server, then damage the client
              TargetTakeDamage(networkIdentity.connectionToClient, 102f);
              bulletManager.deaths += 1;
              print(" player against terrain");
          }
      }else{                        // AI
          if (collision.gameObject.CompareTag("Terrain")){
              healthBar -= 102f; // Damage in server, then damage the client
              bulletManager.deaths += 1;
          }
    }}

    [ClientRpc]
    public void RpcSetCloudLift(float cloudbase, float clouds_overhead){
         distance_tobase = (float)Math.Abs(cloudbase-transform.position.y);
        cloud_suction = new Vector3(0f, 0.01f*thermals*clouds_overhead*(float)Math.Exp(-cloud_exp*distance_tobase), 0f);
    }

    [ClientRpc]
    public void RpcSetAtmWind(Vector3 atm_wind_server){
        atm_wind = atm_wind_server;
    }

    void UpdateParticleTrajectories()
    {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[windParticles.particleCount];
        int numParticles = windParticles.GetParticles(particles);

        for (int i = 0; i < numParticles; i++)
            {particlePos = particles[i].position;
            newVelocity = calculate_slopewind(atm_wind, particlePos);
            particles[i].velocity = newVelocity*3f;}

    windParticles.SetParticles(particles, numParticles);
}

    public Vector3 getGradientDirection(Vector3 pos){
        float stepSize = 6.0f; // Larger step size

        // Get neighboring positions in all 4 directions
        Vector3 posRight = pos + new Vector3(stepSize, 0.0f, 0.0f);
        Vector3 posLeft  = pos - new Vector3(stepSize, 0.0f, 0.0f);
        Vector3 posForward = pos + new Vector3(0.0f, 0.0f, stepSize);
        Vector3 posBackward = pos - new Vector3(0.0f, 0.0f, stepSize);

        // Get terrain heights at each point
        ho = GetCachedHeight(pos);
         hxRight = GetCachedHeight(posRight);
         hxLeft = GetCachedHeight(posLeft);
         hzForward = GetCachedHeight(posForward);
         hzBackward = GetCachedHeight(posBackward);


        // Compute central difference for gradient
         gradientX = (hxRight - hxLeft) / (2 * stepSize);
         gradientZ = (hzForward - hzBackward) / (2 * stepSize);

    // Gradient direction (ascent vector)
    return new Vector3(gradientX, 0.0f, gradientZ);
    }


    public Vector3 calculate_slopewind(Vector3 w, Vector3 position){
        //Computes the wind-terrain effect, in a simple manner.
        // w.e_grad, where w is the wind vector and e_grad the direction of steepest terrain descent
        w_strength = w.magnitude;
        e_w = w.normalized;
        pos = position + Vector3.zero;


        gradient = getGradientDirection(pos);
        e_grad = gradient.normalized;
        steepness = gradient.magnitude;
        h_overterrain = position.y - GetCachedHeight(position);

        x = Mathf.Clamp(Mathf.RoundToInt((position.x - terrainPosition.x) / terrainSizeX * (terrainWidth - 1)),0, terrainWidth - 1);
        z = Mathf.Clamp(Mathf.RoundToInt((position.z - terrainPosition.z) / terrainSizeZ * (terrainHeight - 1)),0, terrainHeight - 1);

        wy = (float)Vector3.Dot(e_w, e_grad)*steepness;
        wy = (float)Math.Exp(-slope_exp*h_overterrain)*wy*slope_const;

        w_exp = new Vector3(w.x, wy, w.z);
        w_exp = w_exp.normalized*w_strength;

        return w_exp;
    }

    public Vector3 steer_plane(Vector3 vel_0, float steer, float pitch){

        // Transform angular_vel (my FPS) to angular_vel (target FPS)
         my_angular_vel = 0.0055f;
         omega = my_angular_vel * Time.deltaTime / 0.00625f; // TURN RATE
        omega = omega*steer*(-1);

        // Steering: Lets the horizontal pointer rotate while pressed

        vel_0 = new Vector3((float)vel_0.x * (float)Math.Cos(omega) - (float)vel_0.z * (float)Math.Sin(omega), 0, (float)vel_0.x * (float)Math.Sin(omega) + (float)vel_0.z * (float)Math.Cos(omega));

         my_climb_vel = 3.0f * Time.deltaTime / 0.00625f;
         climb = 3f * pitch * (-1);
        // Climbing changes hor. vel.

         v_hor = (float)Math.Sqrt((float)Math.Pow((float)vel_0.x,2.0f)+(float)Math.Pow((float)vel_0.z,2.0f));

        if((climb > 0) && (v_hor<7)){           // Can't climb, too slow
             dv_hor = 0;
            climb = 0;
        }else if((climb < 0) && (v_hor > 25)){  // Cant descend, too fast
             dv_hor = 0;
            climb = 0;

        }else{

             dv_hor = 0.2f*(float)Math.Sqrt((float)Time.deltaTime*climb_vel);
            vel_0.y += climb;

            if(pitch<0){
                kappa = 1-dv_hor/v_hor;
                vel_0.x = vel_0.x*kappa;
                vel_0.z = vel_0.z*kappa;
            }else if(pitch >0){
                 kappa = 1+dv_hor/v_hor;
                vel_0.x = vel_0.x*kappa;
                vel_0.z = vel_0.z*kappa;
        }}

        return vel_0;
    }
    void set_plane_orientation(float hor_input, float vert_input, Vector3 forward_airspeed){

        // Local calculations for phi and roll_angle (for the local player only)
        local_phi = -(float)Math.Atan2((float)forward_airspeed.z, (float)forward_airspeed.x) + 3.1416f / 2.0f;


        float target_roll = -hor_input*30f; // [-30 to +30]
        float adaptive_rollvel = (target_roll - local_roll_angle)/30f*roll_angularvel; //[-2 angvel to +2 angvel]

        local_roll_angle += adaptive_rollvel;


        // Update the local player's transform (rotation)
        transform.eulerAngles = new Vector3(
            0,
            local_phi * 180.0f / 3.1416f,
            local_roll_angle);

        }

        public bool has_crashed(){   // Aircraft has actually crashed
            return crashed;
        }
}
