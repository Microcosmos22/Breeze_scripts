using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Mirror;
using System;

public class MinimapController : MonoBehaviour
{
    public RectTransform minimapPanel;
    private Scene scene;


    private Dictionary<Transform, RectTransform> enemyIcons = new();
    public RectTransform playerIcon;

    public List<Transform> enemies;

    public Transform player;
    public Transform camera;
    public GameObject enemyDotPrefab;

    private GameObject[] players;
    private GameObject[] AIs;
    private List<GameObject> planes;

    private float gamma, alpha, beta;


    public float mapScale = 0.1f; // Adjust based on your world size
    private float getEnemiesTimer, getEnemiesTime = 2f;

    void Start()
    {
        planes = new List<GameObject>();
        foreach (var enemy in enemies)
        {
            GameObject dot = Instantiate(enemyDotPrefab, minimapPanel);
            enemyIcons[enemy] = dot.GetComponent<RectTransform>();
        }
    }

    void Update()
    {
        getEnemiesTimer += Time.deltaTime;
        if (getEnemiesTimer > getEnemiesTime){
            FindAllPlanesInAllScenes();
            getEnemiesTimer = 0f;
        }

        gamma = player.rotation.eulerAngles.y;
        beta = camera.rotation.eulerAngles.y;
        alpha = gamma - beta;
        playerIcon.rotation = Quaternion.Euler(0, 0, -alpha);


        foreach (var enemy in enemies)
        {
            Vector3 offset = enemy.position - player.position;
            Vector2 minimapPos = new Vector2(offset.x, offset.z) * mapScale;
            enemyIcons[enemy].anchoredPosition = minimapPos;
        }
    }

    private void FindAllPlanesInAllScenes(){

        for (int i = 0; i < SceneManager.sceneCount; i++){
             scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded){
                 players = GameObject.FindGameObjectsWithTag("Player");
                 AIs = GameObject.FindGameObjectsWithTag("AI");
                 planes.AddRange(AIs);
                 planes.AddRange(players);
            }
        }

    }
}
