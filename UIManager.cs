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
    public GameObject cloudFinder;
    public Terrain terrain;
    public GameObject bullet_trail_prefab;
    public GameObject MountainRace;

    IEnumerator DelayedHostStart(){


        #if UNITY_EDITOR
        Debug.Log("🛠 Running in Editor - attempting to start Host...");

            var transport = NetworkManager.singleton.transport;
            NetworkManager.singleton.networkAddress = "localhost";
            NetworkManager.singleton.StartHost();

            yield return new WaitUntil(() => NetworkServer.localConnection != null);
            Debug.Log("✅ Local connection is ready.");


        #elif UNITY_SERVER && UNITY_STANDALONE_OSX //  TEST A LOCAL (MAC) SERVER
            Debug.Log("🍎🧠 Running as Mac Server - starting Server...");
            var transport = (SimpleWebTransport)NetworkManager.singleton.transport;
            transport.sslEnabled = false; // Disable SSL for local testing

            NetworkManager.singleton.StartServer();
            yield return new WaitUntil(() => NetworkServer.active);

            Debug.Log("✅ Server is active.");
            yield break;

        #elif UNITY_SERVER && !UNITY_STANDALONE_OSX // THE ACTUAL LINUX SERVER
            Debug.Log("🧠 Running as Dedicated Server - starting Server...");
            var transport = (SimpleWebTransport)NetworkManager.singleton.transport;
            transport.sslEnabled = true;
            transport.sslCertJson = "/etc/letsencrypt/live/v2202501113287307394.goodsrv.de/cert.json";

            NetworkManager.singleton.StartServer();

            yield return new WaitUntil(() => NetworkServer.active);
            Debug.Log("✅ Server is active.");
            yield break;

        #elif UNITY_WEBGL   // THE CLIENT, CONNECTS EITHER TO LOCALHOST OR TO THE SERVER
            Debug.Log("🌐 Running in WebGL - starting Client...");
            var transport = (SimpleWebTransport)NetworkManager.singleton.transport;

            // To connect to a local test server
            NetworkManager.singleton.networkAddress = "127.0.0.1";
            transport.sslEnabled = false;
            transport.port = 7777;
            transport.clientUseWss = false; //

            // To connect to the actual server
            //transport.sslEnabled = true;
            //NetworkManager.singleton.networkAddress = "v2202501113287307394.goodsrv.de";
            NetworkManager.singleton.StartClient();
            // Retry connection in case of failure
            yield return new WaitUntil(() => NetworkClient.isConnected);

            if (NetworkClient.isConnected){
                Debug.Log("✅ Client connected.");   // THIS USUALLY IS THE CASE
            }
            yield break;
        #endif
        Debug.LogWarning("⚠️ No build type matched. Exiting DelayedHostStart.");
        yield break;
    }

    void OnDestroy() {
        Debug.LogError("💥 UIManager destroyed. Singleton: " + (NetworkManager.singleton != null ? NetworkManager.singleton.name : "null"));
    }


    public override void Start(){
        //NetworkManager.singleton = this;
        Application.targetFrameRate = 40;

        aiManager = FindAIManagerInAnyScene();
        //StartCoroutine(LoadSceneAndSetActive());
        DontDestroyOnLoad(this.gameObject);
        StartCoroutine(DelayedHostStart());

        StartCoroutine(FindTerrainInScenes());
        //NetworkManager.singleton.sendRate = 30;
        //NetworkManager.singleton.StartServer();
        //NetworkManager.singleton.StartHost();=?˛÷—  QASDF3GV BHNJM,.-PO 1Q
        if (NetworkManager.singleton != this) {
            Debug.LogWarning("Duplicate NetworkManager detected. Destroying self.");
            Destroy(this.gameObject);
            return;
        }
        //terrain = GameObject.FindWithTag("Terrain");
        base.Start();

        //PrintNetworkedObjects();
        if (Input.GetKeyDown(KeyCode.F11)) {
            Screen.fullScreen = !Screen.fullScreen;  // Toggle fullscreen
        }
    }

    private GameObject FindAIManagerInAnyScene() {
        foreach (GameObject obj in GameObject.FindObjectsOfType<GameObject>()) {
            if (obj.name == "AIManager") {
                print(" found AI Manager");
                return obj;
            }
        }
        return null;
    }


    IEnumerator FindTerrainInScenes(){
    while (terrain == null){
        print(" Searching for terrain");
        for (int i = 0; i < SceneManager.sceneCount; i++){
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded){
                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach (GameObject obj in rootObjects){
                    if (obj.CompareTag("Terrain")){
                        terrain = obj.GetComponent<Terrain>();
                        if (terrain != null) {
                          print(" Found terrain");
                          yield break;
                        }
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.1f);
    }
}

    private void WaitForServerAndTerrainThenSpawnDummy(){
        if (!NetworkServer.active){
            return;
        }
        if (aiManager == null){
          print(" AI Manager not found! ");
            return;
        }
        if (terrain == null){
          print(" Terrain not found! ");
            return;
        }

        if (aiManager.GetComponent<AIManager>().dummyAircraft.Count == 0){
          print(" AI list not initialized");
          return;
        }


        if (terrain != null && aiManager != null){
            //print("Spawning dummy AI players...");
            aiManager.GetComponent<AIManager>().SpawnDummyPlayer();
            print(" Spawning dummy players");
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

    public void Update(){

        //PrintNetworkedObjects();

        TimerSpawnDummies += Time.deltaTime;
        if (TimerSpawnDummies > 5f){
            WaitForServerAndTerrainThenSpawnDummy();
            //PrintNetworkedObjects();
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

    public   void OnServerDisconnect(NetworkConnectionToClient conn){
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
