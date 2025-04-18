using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;  // Add this to use Network functionalities
using Mirror;
using Mirror.SimpleWeb; // <-- This is necessary to access SimpleWebTransport
using System;
using System.Threading;

public class UIManager : NetworkManager
{
    public static UIManager Instance; // Singleton instance
    public static Dictionary<NetworkIdentity, GameObject> aircrafts = new();
    public GameObject aiManager;
    private float TimerSpawnDummies = 15f;

    public TMP_InputField nameInputField;
    public Button flyButton;

    public int aircraftIndex;
    public int chosenLevel;
    public string sceneName;

    private GameObject plane, aircraft;
    private Instr_update InstrumentUpdate;
    private string playerName;

    public GameObject goalobj;
    public Text lvlInfo;
    private IVehicleControl controller;

    public bool[] saveState = new bool[12];
    private string loadedSceneName = ""; // Track the last loaded scene name
    public GameObject cloudFinder, terrain;
    public GameObject bullet_trail_prefab;
    public GameObject MountainRace;


    void Awake(){
        if (Instance == null){
            Instance = this;
        }
        else{
            Destroy(gameObject);
            return;
        }
    }

    IEnumerator DelayedHostStart(){

        NetworkManager.singleton.sendRate = 30;

        #if UNITY_EDITOR
        Debug.Log("🛠 Running in Editor - attempting to start Host...");

    if (NetworkManager.singleton == null)
    {
        Debug.LogError("❌ NetworkManager.singleton is null!");
        yield break;
    }

    var transport = NetworkManager.singleton.transport;
    if (transport == null)
    {
        Debug.LogError("❌ Transport is null!");
        yield break;
    }

    NetworkManager.singleton.networkAddress = "localhost";

    try
    {
        NetworkManager.singleton.StartHost();
    }
    catch (System.Exception ex)
    {
        Debug.LogError("🚨 Exception during StartHost(): " + ex.Message);
        yield break;
    }

    yield return new WaitUntil(() => NetworkServer.localConnection != null);
    Debug.Log("✅ Local connection is ready.");


        #elif UNITY_SERVER && UNITY_STANDALONE_OSX
            Debug.Log("🍎🧠 Running as Mac Server - starting Server...");
            var transport = (SimpleWebTransport)NetworkManager.singleton.transport;
            transport.sslEnabled = false;

            NetworkManager.singleton.StartServer();
            yield return new WaitUntil(() => NetworkServer.active);
            Debug.Log("✅ Server is active.");
            yield break;

        #elif UNITY_SERVER && !UNITY_STANDALONE_OSX
            Debug.Log("🧠 Running as Dedicated Server - starting Server...");
            var transport = (SimpleWebTransport)NetworkManager.singleton.transport;
            transport.sslEnabled = true;
            transport.sslCertJson = "/etc/letsencrypt/live/v2202501113287307394.goodsrv.de/cert.json";

            NetworkManager.singleton.StartServer();
            yield return new WaitUntil(() => NetworkServer.active);
            Debug.Log("✅ Server is active.");
            yield break;

        #elif UNITY_STANDALONE_OSX && !UNITY_SERVER
            Debug.Log("🍎 Running macOS Client - starting Client...");
            var transport = (SimpleWebTransport)NetworkManager.singleton.transport;
            transport.sslEnabled = false;
            NetworkManager.singleton.networkAddress = "localhost";
            NetworkManager.singleton.StartClient();
            //NetworkManager.singleton.StartHost();
            yield return new WaitUntil(() => NetworkServer.localConnection != null);
            Debug.Log("✅ Local connection is ready.");
            yield break;
        #elif UNITY_WEBGL
            Debug.Log("🌐 Running in WebGL - starting Client...");
            var transport = (SimpleWebTransport)NetworkManager.singleton.transport;

            transport.sslEnabled = true;
            NetworkManager.singleton.networkAddress = "v2202501113287307394.goodsrv.de";

            NetworkManager.singleton.StartClient();
            // Retry connection in case of failure
            yield return new WaitUntil(() => NetworkClient.isConnected);

            if (NetworkClient.isConnected){
                Debug.Log("✅ Client connected.");
            }
            else{
                Debug.LogError("❌ Failed to connect.");
                // Optionally retry here or notify the user
            }
            yield break;
        #endif
        Debug.LogWarning("⚠️ No build type matched. Exiting DelayedHostStart.");
        yield break;
    }


    void Start(){
        //aiManager = FindAIManagerInAnyScene();
        StartCoroutine(LoadSceneAndSetActive());

        StartCoroutine(DelayedHostStart());
        //NetworkManager.singleton.sendRate = 30;
        //NetworkManager.singleton.StartServer();
        //NetworkManager.singleton.StartHost();=?˛÷—  QASDF3GV BHNJM,.-PO 1Q

        terrain = GameObject.FindWithTag("Terrain");

        PrintNetworkedObjects();
        if (Input.GetKeyDown(KeyCode.F11)) {
            Screen.fullScreen = !Screen.fullScreen;  // Toggle fullscreen
        }
    }

    private GameObject FindAIManagerInAnyScene() {
        foreach (GameObject obj in GameObject.FindObjectsOfType<GameObject>()) {
            print(obj.name);
            if (obj.name == "AIManager") {
                return obj;
            }
        }
        return null;
    }

    private void WaitForServerAndTerrainThenSpawnDummy(){
        if (!NetworkServer.active){
            return;
        }
        if (aiManager == null){
          print(" AI Manager not found! ");
            return;
        }

        if (aiManager.GetComponent<AIManager>().dummyAircraft.Count == 0){
          print(" AI list not initialized");
          return;
        }


        if (terrain != null && terrain.GetComponent<NetworkIdentity>() != null && aiManager != null){
            //print("Spawning dummy AI players...");
            aiManager.GetComponent<AIManager>().SpawnDummyPlayer();
        }else{
            Debug.Log("AIManager is not assigned!");
        }
    }

    IEnumerator LoadSceneAndSetActive(){
        // Start loading the scene. When loaded, then erst get the scene by name.
        //
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Mountain Race", LoadSceneMode.Additive);
        while (!asyncLoad.isDone){
            yield return null;
        }

        Scene loadedScene = SceneManager.GetSceneByName("Mountain Race");

        if (loadedScene.IsValid()){
            SceneManager.SetActiveScene(loadedScene);
            Debug.Log("Scene 'Mountain Race' is now active!");

            yield return null;

            //StartCoroutine(waitThenSpawnTerrain());

            }

        yield return null;  // End the IEnumerator coroutine
    }

    IEnumerator waitThenSpawnTerrain(){
      yield return new WaitForSeconds(1f);


      GameObject terrainInstance = Instantiate(MountainRace);
      print(" mountain race obj instantiated");
      NetworkServer.Spawn(terrainInstance);

      print(" mountan race obj spawned");

      if (terrainInstance != null){ print( " 🔎 NetworkManager spawned the terrain ");
      }else{ print("🔎 Network manager couldnt spawn terrain");}

    }

    void OnStartServer(){
      Debug.Log(" 🚨 Server Started");
      Debug.Log($"Server active: {NetworkServer.active}");

    }

    void Update(){

        //PrintNetworkedObjects();

        TimerSpawnDummies += Time.deltaTime;
        if (TimerSpawnDummies > 20f){
            WaitForServerAndTerrainThenSpawnDummy();
            TimerSpawnDummies = 0f;}

        // Iterate through all aircraft in the dictionary
        foreach (var aircraftEntry in aircrafts){
             aircraft = aircraftEntry.Value;

            // Ensure the aircraft exists and has a component that provides the 'has_crashed' method
            if (aircraft != null && aircraft.GetComponent<IVehicleControl>() != null){

                if (aircraft.GetComponent<IVehicleControl>().has_crashed()){

                    // Handle leaderboard button click
                    Cursor.lockState = CursorLockMode.None; // or CursorLockMode.Confined
                    Cursor.visible = true;
                }
            }
        }
    }

    private void SetAircraftPosition(GameObject aircraft){
        print(" Setting aircraft position");
        aircraft.transform.position = new Vector3(500f, 150f, 500f);
        //aircraft.transform.position = new Vector3(32f, 32f, 992f);
        aircraft.transform.rotation = Quaternion.Euler(0f, 167f, 0f);
        cloudFinder = GameObject.Find("CloudFinder");

    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn){
        if(conn != null && conn.identity != null){
        if (aircrafts.ContainsKey(conn.identity)){
            GameObject aircraft = aircrafts[conn.identity];

            NetworkServer.Destroy(aircraft);

            aircrafts.Remove(conn.identity);

            Debug.Log($"Aircraft destroyed for player {conn.identity} on disconnection.");
        }

        // Always call base method to ensure proper cleanup
        base.OnServerDisconnect(conn);
      }
    }

    public void PrintNetworkedObjects(){
        if (NetworkClient.active) // Ensure the client is active
        {
            Debug.Log("Listing all networked objects on client:");

            foreach (var kvp in NetworkClient.spawned){
                uint netId = kvp.Key;
                NetworkIdentity identity = kvp.Value;

                if (identity != null)
                {
                    Debug.Log($"NetID: {netId}, Name: {identity.name}, Position: {identity.transform.position}");
                }
            }
        }
    }
}
