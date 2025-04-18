using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror; // Import Mirror

public class GliderControl : NetworkBehaviour, IVehicleControl
{
    public float slopelift;
    public Vector3 drag;
    public Vector3 lift;
    public Vector3 airspeed = new Vector3(0f,0f,0f); // rb.velocity in plane frame ()
    public Vector3 restoring_torque = new Vector3(0f,0f,0f);
    public Vector3 aerodyn_force = new Vector3(0f,0f,0f);
    public Vector3 cloud_suction;
    public float rollRate, distance_tobase, thermals, cloud_exp, slope_exp, slope_const, wind_step;
    public float h_overterrain;
    public GameObject explosionPrefabs;
    private GameObject explosionInstance;
    private BulletManager bulletManager;

    public Vector3 atm_wind;
    public Vector3 total_vel;
    public Vector3 exp_slopew;
    public Rigidbody rb;
    private BoxCollider collider;
    int i;
    int i_update_atm;
    public float thermal_step;
    public Terrain land;

    public float restore_coeff_pitch;
    public float restore_coeff_yaw;
    public float yaw_damping_factor, healingTimer, healingTime = 1f;
    public float healthBar = 100f;
    public bool crashed;

    private Vector3 gravity;
    private Vector3 total_force;

    private Vector3 drag_vec = new Vector3(0f,0f,-1f);
    private Vector3 lift_vec = new Vector3(0f, 0.98f, 0.17f);

    private Vector3 velangles_in_planeframe = new Vector3(0f,0f,0f);


    public Vector3 tornado;

    public float drag_coeff;
    public float lift_coeff;
    public float init_velocity;
    public float aerodyn_const;
    public float control_torque;
    private NetworkIdentity netIdentity;


    // Start is called before the first frame update
    void Start(){
        bulletManager = GetComponent<BulletManager>();
        netIdentity = GetComponent<NetworkIdentity>();
        StartCoroutine(WaitForTerrainInNetwork());

        transform.position = new Vector3(500f, 100f, 500f);

        // Only initialize physics on the server and local player
        if (isServer || isLocalPlayer)
        {

            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.None;
            rb.isKinematic = false;
            rb.centerOfMass = Vector3.zero;

            // Only set initial velocity on server
            if (isServer)
            {
                rb.linearVelocity = transform.forward.normalized * init_velocity;
                rb.inertiaTensor = new Vector3(40f, 40f, 40f);
            }
        }

        Camera.main.enabled = true;
        print("INIT VELOCITY IN GLIDER FRAME AND WORLD FRAME:");
        print(Quaternion.Inverse(transform.rotation) * transform.forward*rb.linearVelocity.magnitude);
        print(rb.linearVelocity);
    }

    void FixedUpdate(){
        if (isLocalPlayer){
          healingTimer += Time.deltaTime;
          if (healthBar < 100f && healingTimer > healingTime){
              healthBar += 1f;
              TargetTakeDamage(netIdentity.connectionToClient, -1f);
              healingTimer = 0f;
              }
          else if(healthBar<0f){

              healthBar = 100f;
              explosionInstance = Instantiate(explosionPrefabs);
              explosionInstance.transform.position = transform.position;
              StartCoroutine(bulletManager.DestroyExplosionAfterTime(explosionInstance, 1f));
              set_initpos();
          }

        //    DRAG & LIFT ONLY DEPEND ON WINDSPEED, NOT ON TOTAL VEL
        airspeed = rb.linearVelocity; // - exp_slopew - cloud_suction ;
        float v_forw = Vector3.Dot(airspeed, transform.forward); // velocity projected on the forward

        drag = drag_vec*drag_coeff*(float)Math.Pow(v_forw,2);
        if (v_forw > 0){ // travelling forward
            lift = lift_vec*(lift_coeff*(float)Math.Pow(v_forw,2));
            if (lift.magnitude > 5.0f)
                lift = lift*(1/lift.magnitude)*5.0f;
        }else{lift = lift*0;
        }

        gravity = new Vector3(0f, -5f, 0f);


        //+transform.TransformDirection(total_force)
        rb.AddRelativeForce(total_force, ForceMode.Acceleration);
        rb.AddForce(gravity, ForceMode.Acceleration);
        aerodyn_force = calc_aerodynamics();
        rb.AddRelativeForce(aerodyn_force, ForceMode.Acceleration);
        total_force = drag+lift+aerodyn_force;

        // Input applies a torque on the local system, rotating the lift vector
        float roll = Input.GetAxisRaw("Horizontal");
        float pitch = Input.GetAxisRaw("Vertical");
        float yaw = Input.GetAxisRaw("Yaw");
        float rollRate = rb.angularVelocity.z; // Roll rate around the forward axis
        float yawCorrection = -rollRate * yaw_damping_factor;
        Vector3 torque = new Vector3(1.5f*pitch, 1f*yaw+yawCorrection, roll*(-1f))*control_torque;

        restoring_torque = calc_restoring_torque();
        rb.AddRelativeTorque(torque, ForceMode.Acceleration);
        rb.AddRelativeTorque(restoring_torque, ForceMode.Acceleration);


        if (rb.linearVelocity.magnitude > 1000){
            Application.Quit();
        }
        if (land != null){
        //exp_slopew = slopewind(); // contains atm + slopewind
        //slopelift = exp_slopew[1];
        //rb.velocity = rb.velocity + ;
        //rb.AddForce((exp_slopew + cloud_suction), ForceMode.Acceleration);
        }
        //rb.velocity = rb.velocity; + cloud_suction;
        //rb.position += rb.position+(exp_slopew+cloud_suction)*Time.deltaTime;

        bool breaks = Input.GetKey(KeyCode.B);
        if((breaks == true) && (v_forw > 0.1f)){
            rb.AddRelativeForce(new Vector3(0f, 0f, -3f), ForceMode.Acceleration);
        }

        cloud_suction = new Vector3(0f,0f,0f); // set zero because CloudParents will += cloud_suction
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
          }
      }else{                        // AI
          if (collision.gameObject.CompareTag("Terrain")){
              healthBar -= 102f; // Damage in server, then damage the client
              print(" AI hitting terrain");
          }
    }}

    public void set_initpos(){

        //transform.position = new Vector3(5f, 15f, 1015f);
        transform.position = new Vector3(500f, 100f, 500f);
        rb.linearVelocity = transform.forward * 5f;
        healthBar = 100f;

    }


    [TargetRpc]
    public void TargetTakeDamage(NetworkConnection target, float hp_damage) {
        healthBar -= hp_damage;
        //print($"Damaging Player in client. Health: {healthBar}");
    }

    private IEnumerator WaitForTerrainInNetwork(){
        Debug.Log("🔁 Waiting for Terrain to appear in NetworkServer.spawned...");

        while (land == null){
            foreach (var netId in NetworkServer.spawned){
                GameObject obj = netId.Value.gameObject;

                Debug.Log($"🌐 Checking: {obj.name}, Tag: {obj.tag}");

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
