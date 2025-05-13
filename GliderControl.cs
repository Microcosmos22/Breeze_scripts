using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror; // Import Mirror
using UnityEngine.SceneManagement;

public class GliderControl : NetworkBehaviour, IVehicleControl
{
    public float slopelift, v, stepSize;
    private TerrainData terrainData;
    public Vector3 drag, posRight, posLeft, posForward, posBackward;
    public Vector3 lift;
    public Vector3 airspeed = new Vector3(0f,0f,0f); // rb.velocity in plane frame ()
    public Vector3 restoring_torque = new Vector3(0f,0f,0f);
    public Vector3 aerodyn_force = new Vector3(0f,0f,0f);
    public Vector3 slope_vel = new Vector3(0f,0f,0f);
    public Vector3 cloud_suction;
    float v_forw, roll, pitch, yaw, yawCorrection;
    public float rollRate, distance_tobase, thermals, cloud_exp, slope_exp, slope_const, wind_step;
    public float h_overterrain;
    public GameObject explosionPrefabs;
    private GameObject explosionInstance;
    private BulletManager bulletManager;
    public Vector3 w_exp, e_w, tornado, forward_airspeed, particlePos, newVelocity, gradient, e_grad;
    [SyncVar] public int lives;

    private float[,] heightMap; // Cached height data
    private int terrainWidth, terrainHeight;
    private float terrainSizeX, terrainSizeZ, wy;
    private float steepness;
    private Vector3 terrainPosition;
    public Vector3 atm_wind, torque;
    public Vector3 total_vel;
    public Vector3 exp_slopew;
    public Rigidbody rb;
    private BoxCollider collider;
    int i, x, z;
    int i_update_atm;
    public float thermal_step;
    public Terrain land;
    public float gunCoolTimer;
    public float gunUptime;

    public float restore_coeff_pitch;
    public float restore_coeff_yaw;
    public float yaw_damping_factor, healingTimer, healingTime = 1f;
    public float healthBar = 100f;
    public bool crashed, isCoolingDown, breaks;

    private Vector3 gravity;
    private Vector3 total_force;

    private Vector3 drag_vec = new Vector3(0f,0f,-1f);
    private Vector3 lift_vec = new Vector3(0f, 0.98f, 0.17f);

    private Vector3 velangles_in_planeframe = new Vector3(0f,0f,0f);

    private float w_strength;
    private Vector3 pos;
    float ho, hxRight, hxLeft, hzForward, hzBackward, gradientX, gradientZ;

    public float drag_coeff;
    public float lift_coeff;
    public float init_velocity;
    public float aerodyn_const;
    public float control_torque;
    private NetworkIdentity netIdentity;
    private CamFollower camFollow;

    public AudioClip shootSound;
    float mouseX, mouseY, mouseSensitivity;
    private AudioSource audioSource;

    // Start is called before the first frame update
    public void Start(){
        healthBar = 100f;
        StartCoroutine(FindTerrainInScenes());
        StartCoroutine(Wait4Terrain_ThenCache());
        audioSource = GetComponent<AudioSource>();

        gunUptime = 2f;
        gunCoolTimer = UnityEngine.Random.Range(0f, 4f);
        set_initpos();

        bulletManager = GetComponent<BulletManager>();
        netIdentity = GetComponent<NetworkIdentity>();
        StartCoroutine(WaitForTerrainInNetwork());
        camFollow = GetComponentInChildren<CamFollower>();

        if (isServer || isLocalPlayer)
        {

            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.None;
            rb.isKinematic = false;
            rb.centerOfMass = Vector3.zero;

            if (isServer){
                rb.linearVelocity = transform.forward.normalized * init_velocity;
                rb.inertiaTensor = new Vector3(40f, 40f, 40f);
            }
        }

        Camera.main.enabled = true;
        print(Quaternion.Inverse(transform.rotation) * transform.forward*rb.linearVelocity.magnitude);
        print(rb.linearVelocity);
    }

    public void FixedUpdate(){
        // HEALTH AND DYING
        if (isLocalPlayer && land != null){
          healingTimer += Time.deltaTime;

          if (healthBar < 100f && healingTimer > healingTime){
              healthBar += 1f;
              TargetTakeDamage(netIdentity.connectionToClient, -1f);
              healingTimer = 0f;
              print("healing");
              }
          else if(healthBar<0f){

              healthBar = 100f;
              explosionInstance = Instantiate(explosionPrefabs);
              explosionInstance.transform.position = transform.position;
              StartCoroutine(bulletManager.DestroyExplosionAfterTime(explosionInstance, 1f));
              set_initpos();
          }

        //    DRAG, LIFT, GRAVITY AND AERODYNAMIC FORCE
        airspeed = rb.linearVelocity; // - exp_slopew - cloud_suction ;
        v_forw = Vector3.Dot(airspeed, transform.forward); // velocity projected on the forward
        drag = drag_vec*drag_coeff*(float)Math.Pow(v_forw,2);
        if (v_forw > 0){ // travelling forward
            lift = lift_vec*(lift_coeff*(float)Math.Pow(v_forw,2));
            if (lift.magnitude > 5.0f)
                lift = lift*(1/lift.magnitude)*5.0f;
        }else{lift = lift*0;
        }

        //print($"aer forces: {drag}, {lift}, {aerodyn_force}");
        print($"ext forces: {slope_vel}, {cloud_suction}");

        gravity = new Vector3(0f, -5f, 0f);
        aerodyn_force = calc_aerodynamics();

        total_force = drag+lift+aerodyn_force;
        rb.AddRelativeForce(total_force, ForceMode.Acceleration);
        rb.AddForce(gravity, ForceMode.Acceleration);


        // Input applies a torque on the local system, rotating the lift vector
        if (camFollow.firstPerson){mouseSensitivity = 1.5f;}else{mouseSensitivity = 0f;}

        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y"); // Combine both inputs
        roll = Input.GetAxisRaw("Horizontal");
        pitch = Input.GetAxisRaw("Vertical") - mouseY * mouseSensitivity;
        yaw = Input.GetAxisRaw("Yaw") + mouseX * mouseSensitivity;;

        rollRate = rb.angularVelocity.z; // Roll rate around the forward axis
        yawCorrection = -rollRate * yaw_damping_factor;
        v = airspeed.magnitude;
        torque = new Vector3(1f*pitch, 1f*yaw+yawCorrection, roll*(-0.7f))*control_torque* Mathf.Clamp(((v+6.5f)*(v+6.5f))/(200f), 0f, 5.5f);

        restoring_torque = calc_restoring_torque();
        rb.AddRelativeTorque(torque, ForceMode.Acceleration);
        rb.AddRelativeTorque(restoring_torque, ForceMode.Acceleration);


        slope_vel = calculate_slopewind(atm_wind, transform.position); // contains atm + slopewind
        //rb.linearVelocity = rb.linearVelocity + slope_vel + cloud_suction;
        rb.AddForce((slope_vel + cloud_suction), ForceMode.Acceleration);

        //rb.velocity = rb.velocity; + cloud_suction;
        //rb.position += rb.position+(exp_slopew+cloud_suction)*Time.deltaTime;

        breaks = Input.GetKey(KeyCode.B);
        if((breaks == true) && (v_forw > 0.1f)){
            rb.AddRelativeForce(new Vector3(0f, 0f, -3f), ForceMode.Acceleration);
        }


        // SHOOTING
        if(gunCoolTimer > 0f && !Input.GetMouseButton(0)){ // Counts down in both cases, isCoolingDown and !isCoolingDown
            gunCoolTimer -= Time.deltaTime;
        }else if(gunCoolTimer > gunUptime){
            isCoolingDown = true;
        }else if (Input.GetMouseButton(0) && !isCoolingDown){
            gunCoolTimer += Time.deltaTime;
            audioSource.Play();
            if (isLocalPlayer && NetworkClient.isConnected && isOwned){
                bulletManager.CmdShootBullet(camFollow.get_camera_quaternion_with_error());
            }

        }else if(isCoolingDown && gunCoolTimer < 0f){
            isCoolingDown = false;
        }
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
          print(" GetCachedHeight found no land");
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


    public void OnCollisionEnter(Collision collision){
      if (!enabled) return;
      if (!isServer) return;

      Debug.Log($"{gameObject.name} | isServer: {isServer}, isClient: {isClient}");
      Debug.Log($" isOwned: {netIdentity.isOwned}, isLocalPlayer: {isLocalPlayer}");

      if (netIdentity.isOwned){ // Player
          if (collision.gameObject.CompareTag("Terrain")){
              healthBar -= 102f; // Damage in server, then damage the client
              TargetTakeDamage(netIdentity.connectionToClient, 102f);
              print(" HITTING TERRAIN");
          }
      }else{                        // AI
          if (collision.gameObject.CompareTag("Terrain")){
              healthBar -= 102f; // Damage in server, then damage the client
              print(" AI hitting terrain");
          }
    }}

    public Vector3 getGradientDirection(Vector3 pos){
         stepSize = 6.0f; // Larger step size

        // Get neighboring positions in all 4 directions
         posRight = pos + new Vector3(stepSize, 0.0f, 0.0f);
         posLeft  = pos - new Vector3(stepSize, 0.0f, 0.0f);
         posForward = pos + new Vector3(0.0f, 0.0f, stepSize);
         posBackward = pos - new Vector3(0.0f, 0.0f, stepSize);

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

    public void set_initpos(){

        //transform.position = new Vector3(5f, 15f, 1015f);
        transform.position = new Vector3(711f, 283f, 321f);
        rb.linearVelocity = transform.forward * 5f;
        healthBar = 100f;

    }


    [TargetRpc]
    public void TargetTakeDamage(NetworkConnection target, float hp_damage) {
        healthBar -= hp_damage;
        print($"Damaging Player in client. Health: {healthBar}");
    }

    [ClientRpc]
    public void RpcSetAtmWind(Vector3 atm_wind_server){
        atm_wind = atm_wind_server;
    }

    private IEnumerator WaitForTerrainInNetwork(){
        Debug.Log("🔁 Waiting for Terrain to appear in NetworkServer.spawned...");

        while (land == null){
            foreach (var netId in NetworkServer.spawned){
                GameObject obj = netId.Value.gameObject;

                if (obj.CompareTag("Terrain")){
                    land = obj.GetComponent<Terrain>();
                    Debug.Log("✅ Terrain found and assigned!");
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.5f); // Wait a bit before checking again
        }
    }

    [ClientRpc]
    public void RpcSetCloudLift(float cloudbase, float clouds_overhead){
         distance_tobase = (float)Math.Abs(cloudbase-transform.position.y);
         cloud_suction = new Vector3(0f, 0.01f*thermals*clouds_overhead*(float)Math.Exp(-cloud_exp*distance_tobase), 0f);
    }

    public void set_atm_wind(Vector3 setted_atm_wind){
        atm_wind = setted_atm_wind;
    }

     public void set_tornado_component(Vector3 tor){
        tornado = tor;
    }

    private Vector3 calc_aerodynamics(){
        airspeed = Quaternion.Inverse(transform.rotation) * rb.linearVelocity;
        float v_speed = airspeed[1];
        float side_speed = airspeed[0];

        aerodyn_force = new Vector3(-(float)side_speed, -(float)v_speed, 0f);

        return aerodyn_force*aerodyn_const;

        }

    private Vector3 calc_restoring_torque(){
        airspeed = Quaternion.Inverse(transform.rotation) * rb.linearVelocity;
        velangles_in_planeframe = Quaternion.LookRotation(airspeed.normalized).eulerAngles;
        float speed = rb.linearVelocity.magnitude;

        float alpha = velangles_in_planeframe[0];
        float beta = velangles_in_planeframe[1];
        if (alpha > 270)
            alpha = alpha - 360f;
        else if((alpha)>90 && (alpha)<270)
            alpha = 0f;
        if (beta > 270)
            beta = beta - 360f;
        else if((beta)>90 && (beta)<270)
            beta = 0f;

        // Only pitch and yaw create restoring torques
        // altitudal angle creates torque in x
        // azimutal angle creates torque in y
        restoring_torque = new Vector3(alpha*restore_coeff_pitch*speed/10f, beta*restore_coeff_yaw*speed/10f, 0f);
        //
        //-velangles_in_planeframe[0]*restore_coeff_yaw

        return restoring_torque;
        }


        public bool has_crashed(){   // Aircraft has actually crashed
            return crashed;
        }
}
