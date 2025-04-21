using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Mirror;
using System;

public class MinimapController : NetworkBehaviour
{
    public RectTransform minimapPanel;
    private Scene scene;

    public Transform player;
    public Transform camera;
    public GameObject enemyDotPrefab;

    private GameObject[] players;
    private GameObject[] AIs;
    private List<GameObject> planes;

    public RectTransform playerIcon;
    private List<RectTransform> enemyIcons;

    private float gamma, alpha, beta, radius, angleInRadians, x, y;
    private int planeN;


    public float mapScale = 1f; // Adjust based on your world size
    private float getEnemiesTimer, getEnemiesTime = 2f;
    private Vector2 minimapCenter = new Vector2(0f, 0f);
    private Vector3 offset;
    private Vector2 minimapPos;
    private GameObject dot;
    private RectTransform dotRect;

    void Start(){
        planes = new List<GameObject>();

    }

    [TargetRpc]
    private void instIcons(){
      foreach (Transform child in minimapPanel){
          if (child.name.Contains("EnemyDot")){
              DestroyImmediate(child.gameObject);
          }
      }
      enemyIcons = new List<RectTransform>();

      print($"N of planes {planeN}");

      for (int i = 0; i < planeN; i++){

          offset = planes[i].transform.position - player.position;

          if (offset.magnitude < 5f){
            continue;
            i -= 1;}


          minimapPos = new Vector2(offset.x, offset.z) * mapScale;

          dot = Instantiate(enemyDotPrefab, minimapPanel);
          dotRect = dot.GetComponent<RectTransform>();
          //dotRect.anchoredPosition = minimapPos; // without camera rotation
          //   Rotate icons around center, depending on camera position


          radius = minimapPos.magnitude;
          angleInRadians = + beta * Mathf.Deg2Rad;

          x = minimapPos.x + minimapCenter.x + radius * Mathf.Cos(angleInRadians);
          y = minimapPos.y + minimapCenter.y + radius * Mathf.Sin(angleInRadians);

          // Position rotated by camera azimuth
          dotRect.anchoredPosition = new Vector2(x, y);


          // rotation of the icons
          gamma = planes[i].transform.rotation.eulerAngles.y;
          beta = camera.rotation.eulerAngles.y;
          alpha = gamma - beta;
          dotRect.rotation = Quaternion.Euler(0, 0, -alpha);

          enemyIcons.Add(dotRect);
      }
    }

    void Update(){

        getEnemiesTimer += Time.deltaTime;
        if (getEnemiesTimer > getEnemiesTime){
            // How many players ? Instantiate their icons.
            FindAllPlanesInAllScenes();
            instIcons();
            getEnemiesTimer = 0f;
        }

        gamma = player.rotation.eulerAngles.y;
        beta = camera.rotation.eulerAngles.y;
        alpha = gamma - beta;
        playerIcon.rotation = Quaternion.Euler(0, 0, -alpha);
    }


    [Server]
    private void FindAllPlanesInAllScenes(){
        planes = new List<GameObject>();

        for (int i = 0; i < 1; i++){
             scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded){
                 players = GameObject.FindGameObjectsWithTag("Player");
                 AIs = GameObject.FindGameObjectsWithTag("AI");

                 foreach (GameObject ai in AIs){
                    print(ai.name);
                 }

                 planes.AddRange(AIs);
                 planes.AddRange(players);

                 planeN = planes.Count;
            }
        }

    }
}
