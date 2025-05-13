using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Mirror;
using System;

public class AIManager : MonoBehaviour
{
    private List<GameObject> combinedPlayers;
    private Vector3 hitPosition, paral_hangL, paral_hangR, forw, error, simpleLead;
    public GameObject[] aircraftPrefabs;
    private GameObject[] scenePlayers;
    public List<GameObject> dummyAircraft = new List<GameObject>();
    public List<Rigidbody> dummyRb = new List<Rigidbody>();
    public List<PlaneControl> dummyPlaneControl = new List<PlaneControl>();
    private Rigidbody rb;
    public List<GameObject> targetPlayer = new List<GameObject>();
    public List<Rigidbody> targetRb = new List<Rigidbody>();
    private List<Vector3> target_direction = new List<Vector3>();
    private float detectionThreshold, a,b,c, T1, T2, T, theta, detectionDistance, dist, dist_min, steerInput, steerDirection, steerMagnitude, steer;
    private Vector3 newPosition, pos, paral_hang;
    private List<GameObject> players;
    private RaycastHit hit;
    private bool outsideMap;
    public int NAIs, j_target;
    private NetworkConnectionToClient dummyConnection;

    private List<float> timeOutside = new List<float>();
    private List<float> flightlevel = new List<float>();

    private float distance, std, dot, angle, heightovert, randomAngle, x, z, targetDistance;
    private float targetSetTimer;
    private List<bool> alr_outside = new List<bool>(new bool[5]);// Moved outside the Update method
    private bool alr_avoidingTerrain = false; // Moved outside the Update method
    private bool alr_detected = false, detected;
    private bool alr_detectionLost = false;
    public List<PlaneControl> targetPlaneControl = new List<PlaneControl>();
    private int chosenTarget;
    private Scene scene;
    private Quaternion rotation;
    private Vector3 direction, cross, grad_up, posdiff;
    private float terr_height, u1, u2, randStdNormal;
    private GameObject explosionInstance, vfx;

    void Start(){

        NAIs = 10;
        for (int i = 0; i < NAIs; i++) {
            dummyAircraft.Add(null); // Fills the list with nulls
            dummyPlaneControl.Add(null);
            dummyRb.Add(null);
            targetPlayer.Add(null);
            targetRb.Add(null);
            timeOutside.Add(0f);
            alr_outside.Add(false);
            target_direction.Add(Vector3.zero);
        }
        print($"Initializing AI list with count: {dummyAircraft.Count}");
      }


    void Update(){

        targetSetTimer += Time.deltaTime;

        if (targetSetTimer > 4f){ // global timer to find players, set Target and Detected
            targetSetTimer = 0f;
            players = FindAllPlayersInAllScenes();
            setTarget();

        }
        setPlayerDetected();
        set_aimAIQ();

        for (int i = 0; i < NAIs; i++){

          // dying
          if (dummyPlaneControl[i] != null && dummyPlaneControl[i].healthBar < 0f){

                dummyPlaneControl[i].ServerHandleAIDeath();
                SetAIPosition(i);
            }



            if (dummyAircraft[i] != null){
                 pos = dummyAircraft[i].transform.position; // Get the position of the dummy aircraft
                 heightovert = pos.y - dummyPlaneControl[i].GetCachedHeight(pos);
                 outsideMap = ((pos.z < 200) || (pos.z > 800) || (pos.x < 200) || (pos.x > 800));

                 if (!outsideMap){
                    alr_outside[i] = false;
                 }
                 //print($"AI {i} at {dummyAircraft[i].transform.position}");

                    // FLYING TOWARDS TERRAIN
                 if(CheckIfFlyingTowardsTerrain(i)){
                     grad_up = dummyPlaneControl[i].getGradientDirection(pos).normalized*(-1f);
                     target_direction[i] = grad_up;

                     // OUSIDE MAP
                }else if (outsideMap){
                    if (!alr_outside[i]){ // First time outside map
                        timeOutside[i] = 0f;
                        alr_outside[i] = true;
                        target_direction[i] = setCourse_outsidemap(pos); // Calculate target direction
                    }
                    timeOutside[i] += Time.deltaTime; //

                    if (timeOutside[i] >= 0.5f){
                        target_direction[i] = setCourse_outsidemap(pos);
                        timeOutside[i] = 0f; // Reset the timer
                    }

                      // FOLLOWING HANG
                }else if((heightovert < 100f) && (heightovert > 10f)){
                  //print("Following terrain");

                   paral_hangL = RotateVector(dummyPlaneControl[i].getGradientDirection(hitPosition).normalized, Vector3.up, 90f); // kurs: parallel zum Hang
                   paral_hangR = RotateVector(dummyPlaneControl[i].getGradientDirection(hitPosition).normalized, Vector3.up, -90f); // kurs: parallel zum Hang
                   forw = dummyAircraft[i].transform.forward;

                   if (Vector3.Dot(forw, paral_hangL) > Vector3.Dot(forw, paral_hangR)){
                       target_direction[i] = paral_hangL;
                   }else{
                       target_direction[i] = paral_hangR;}

                 }else{
                    target_direction[i] = Vector3.zero;

                 }

                if (target_direction[i] != Vector3.zero){
                     steer = CalculateAISteer(target_direction[i], dummyPlaneControl[i].transform.forward);
                    dummyPlaneControl[i].AIsteer = steer;
                  }else{

                    dummyPlaneControl[i].AIsteer = 0;
                  }
            }
        }}

    public void SpawnDummyPlayer(){
        // Fills the airplane list if one has been destroyed.

        for (int i = 0; i < NAIs; i++){
            if (dummyAircraft[i] == null) {

                dummyConnection = new NetworkConnectionToClient(i, "AI Connection");

                // Instantiate the aircraft prefab for the dummy player
                dummyAircraft[i] = Instantiate(aircraftPrefabs[0]);

                //NetworkServer.AddPlayerForConnection(dummyConnection, dummyAircraft);
                NetworkServer.Spawn(dummyAircraft[i], dummyConnection);
                dummyPlaneControl[i] = dummyAircraft[i].GetComponent<PlaneControl>();
                dummyPlaneControl[i].isAI = true;
                dummyPlaneControl[i].ispaused = false;
                dummyPlaneControl[i].glide_desc = 0;
                dummyPlaneControl[i].Username = "AI n" + i.ToString();
                dummyRb[i] = dummyAircraft[i].GetComponent<Rigidbody>();
                dummyPlaneControl[i].ispaused = false;

                SetAIPosition(i);
                print($" 🧠 Spawning {i}th AI slot, pos: {dummyAircraft[i].transform.position}");
                }
          }
        }


    private Vector3 GetGaussianNoise(float mean, float stdDev)
    {
        u1 = 1.0f - UnityEngine.Random.value; // Uniform(0,1] random value
        u2 = 1.0f - UnityEngine.Random.value;
        randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);

        return new Vector3(
            randStdNormal * stdDev + mean,  // X-component
            randStdNormal * stdDev + mean,  // Y-component
            randStdNormal * stdDev + mean   // Z-component
        );
    }

    private void set_aimAIQ(){
      if (dummyAircraft.Count >0){

      for (int i = 0; i < NAIs; i++){
        if (targetRb[i] != null && targetPlayer[i] != null && dummyAircraft[i] != null){
          posdiff = targetPlayer[i].transform.position - dummyAircraft[i].transform.position;
          distance = posdiff.magnitude;

          std = 4f+(distance/11f);

           error = GetGaussianNoise(0f,std);
          // Assuming a bullet travel time of 0.7 seconds, aim for the position where the target will be in 0.7 sec
           simpleLead = error + posdiff + targetRb[i].linearVelocity * 0.7f;
          //print($"Simple aim: {simpleLead}");
          dummyPlaneControl[i].setAim(Quaternion.LookRotation(simpleLead));
          // Setting an explosion time with the above assumption, clamp to 60 m - 360 m ( in time )
          dummyPlaneControl[i].bulletManager.explosionTime = Mathf.Clamp((targetPlayer[i].transform.position-dummyAircraft[i].transform.position+ targetRb[i].linearVelocity*0.7f).magnitude / dummyPlaneControl[i].bulletManager.bulletspeed, 60f/dummyPlaneControl[i].bulletManager.bulletspeed, 360f/dummyPlaneControl[i].bulletManager.bulletspeed);

        }
      }
    }
    }

    private void setPlayerDetected(){
        if (dummyAircraft.Count >0){
        for (int i = 0; i < NAIs; i++){
            if (targetPlayer[i] != null && dummyAircraft[i] != null){
                 targetDistance = (targetPlayer[i].transform.position - dummyAircraft[i].transform.position).magnitude;

                if(targetDistance < 280f && !dummyPlaneControl[i].isCoolingDown){
                    dummyPlaneControl[i].playerDetected = true;
                }else{
                    dummyPlaneControl[i].playerDetected = false;
                }
        }
    }}}

    private void setTarget(){

      combinedPlayers = new List<GameObject>(dummyAircraft);
      combinedPlayers.AddRange(players);


      for (int i = 0; i < NAIs; i++){
         dist_min = 5000f;
         dist = 5001f;

         //print($" AI {i} look through {players.Count} player targets");
          j_target = 0;

         for (int j = 0; j < combinedPlayers.Count; j++){
            if (dummyAircraft[i] != null && combinedPlayers[j] != null && i != j){
                dist = (dummyAircraft[i].transform.position - combinedPlayers[j].transform.position).magnitude;
                //print($"distance to player: {dist}");

                if ((dist < dist_min)){
                  dist_min = dist;
                  targetPlayer[i] = combinedPlayers[j];
                  targetRb[i] = combinedPlayers[j].GetComponent<Rigidbody>();
                  j_target = j;
                  }
              }
          }
          //print($" Target of {i} is {j_target}");
      }
  }

    private float CalculateAISteer(Vector3 target_direction, Vector3 forward){

        target_direction.Normalize();
        forward.Normalize();

         cross = Vector3.Cross(forward, target_direction);
         dot = Vector3.Dot(forward, target_direction);
          if (dot > 0.97f){
              return 0;
          }

         steerDirection = Mathf.Sign(cross.y); // Use the Y-component to determine left/right
         angle = Mathf.Acos(dot) * Mathf.Rad2Deg; // Angle between the vectors in degrees

         steerMagnitude = Mathf.Clamp(angle / 180f, 0f, 1f); // Normalize the angle to [0, 1]
         steerInput = steerDirection * steerMagnitude;

        //print($" Set AIPlane target direction: {target_direction}");
        //print($" Current flight direction: {forward}");

        return steerInput;
    }


    private void SetAIPosition(int i){
        // set x, z. find the terrain height. then choose a randosetam flightlevel above that terrain.
        dummyAircraft[i].transform.position = new Vector3(
            UnityEngine.Random.Range(400f, 600f),500f,  //
            UnityEngine.Random.Range(400f, 600f)
        );

        terr_height = 50f;

        flightlevel.Add(UnityEngine.Random.Range(terr_height+10f, 230f));

        dummyAircraft[i].transform.position = new Vector3(
            UnityEngine.Random.Range(400f, 600f), // 500 to 700
            flightlevel[i],  //
            UnityEngine.Random.Range(500f, 700f) // 400 to 600
        );

        randomAngle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);

        x = Mathf.Cos(randomAngle);
        z = Mathf.Sin(randomAngle);

        direction = new Vector3(x, 0f, z);

        dummyAircraft[i].transform.rotation = Quaternion.LookRotation(direction);
        dummyRb[i].linearVelocity = dummyAircraft[i].transform.forward * Mathf.Clamp((((flightlevel[i]-terr_height)-15f)/25f + 2f), 6f, 15f);
        dummyPlaneControl[i].airspeed = dummyRb[i].linearVelocity;
        dummyPlaneControl[i].healthBar = 100f;
        //print($"Init AI {dummyAircraft[i].transform.position} flying speed: {dummyRb[i].linearVelocity}");
    }

    private Vector3 RotateVector(Vector3 v, Vector3 axis, float angleDegrees) {
         rotation = Quaternion.AngleAxis(angleDegrees, axis);
        return rotation * v;
    }

    public bool CheckIfFlyingTowardsTerrain(int i)
    {
        detectionThreshold = dummyRb[i].linearVelocity.magnitude * 7f; // Max ray length based on speed
        Debug.DrawRay(dummyAircraft[i].transform.position, dummyAircraft[i].transform.forward * detectionThreshold, Color.red);
        //print($"Casting ray from {dummyAircraft[i].transform.position} forward {detectionThreshold} units");

        if (Physics.Raycast(dummyAircraft[i].transform.position, dummyAircraft[i].transform.forward, out hit, detectionThreshold)){
            if (hit.collider.CompareTag("Terrain") && hit.distance <= detectionThreshold){
                hitPosition = new Vector3(hit.point.x, 0, hit.point.z); // (x, z) for terrain positioning
                //print($"🚨 Hitting terrain in {hit.distance} meters");

                return true;
            }
        }
        return false;
    }

    private float CalculateAIPitch(Vector3 pos){
        return 0f;
    }

    private bool CalculateAIBreaks(Vector3 pos){
        return false;
    }

    private Vector3 setCourse_outsidemap(Vector3 pos){

        System.Random random = new System.Random();

        if (pos.z < 200){
             theta = (float)((random.NextDouble() * Math.PI / 2) + Math.PI/4);
            return new Vector3((float)Math.Cos(theta), 0, (float)Math.Sin(theta));
        }
        else if (pos.z > 800){
             theta = (float)((random.NextDouble() * Math.PI / 2) + Math.PI/4);
            return new Vector3((float)Math.Cos(theta), 0, -(float)Math.Sin(theta));
        }

        if (pos.x < 200){
             theta = (float)((random.NextDouble() - 0.5f) * Math.PI / 2);
            return new Vector3((float)Math.Cos(theta), 0, (float)Math.Sin(theta));
        }
        else if (pos.x > 800){
             theta = (float)((random.NextDouble() - 0.5f) * Math.PI / 2);
            return new Vector3(-(float)Math.Cos(theta), 0, (float)Math.Sin(theta));
        }
        return new Vector3(0f, 0f, 0f);
    }

    private List<GameObject> FindAllPlayersInAllScenes(){
        players = new List<GameObject>();

        for (int i = 0; i < SceneManager.sceneCount; i++){
             scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded){
                 scenePlayers = GameObject.FindGameObjectsWithTag("Player");
                players.AddRange(scenePlayers);
            }
        }

        return players;
    }
  }
