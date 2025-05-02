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


    private float mapScale = 0.7f; // Adjust based on your world size
    private float getEnemiesTimer, getEnemiesTime = 0.2f;
    private Vector2 minimapCenter = new Vector2(0f, 0f);
    private Vector3 offset;
    private Vector2 minimapPos;
    private GameObject dot;
    private RectTransform dotRect;

    void Start(){
        planes = new List<GameObject>();

    }

    public void instIcons(){
      foreach (Transform child in minimapPanel){
          if (child.name.Contains("EnemyDot")){
              DestroyImmediate(child.gameObject);
          }
      }
      enemyIcons = new List<RectTransform>();


      for (int i = 0; i < planes.Count; i++){

          offset = planes[i].transform.position - player.position;
          minimapPos = new Vector2(offset.x*mapScale, offset.z*mapScale);
          if (minimapPos.magnitude < 250f){

              dot = Instantiate(enemyDotPrefab, minimapPanel);
              dotRect = dot.GetComponent<RectTransform>();
              //dotRect.anchoredPosition = minimapPos; // without camera rotation
              //   Rotate icons around center, depending on camera position

              radius = minimapPos.magnitude;
              beta = camera.rotation.eulerAngles.y;
              angleInRadians = + beta * Mathf.Deg2Rad;


              x = minimapPos.x * Mathf.Cos(angleInRadians) - minimapPos.y * Mathf.Sin(angleInRadians);
              y = minimapPos.x * Mathf.Sin(angleInRadians) + minimapPos.y * Mathf.Cos(angleInRadians);

              // Position rotated by camera azimuth
              dotRect.anchoredPosition = new Vector2(x, y);

              // rotation of the icons
              gamma = planes[i].transform.rotation.eulerAngles.y;
              beta = camera.rotation.eulerAngles.y;
              alpha = gamma - beta;
              dotRect.rotation = Quaternion.Euler(0, 0, -alpha);

              enemyIcons.Add(dotRect);
      }}
    }


    private void FindAllPlanesInAllScenes(){

        planes = new List<GameObject>();

        for (int i = 0; i < 1; i++){
             scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded){
                 players = GameObject.FindGameObjectsWithTag("Player");
                 AIs = GameObject.FindGameObjectsWithTag("AI");

                 planes.AddRange(AIs);
                 planes.AddRange(players);
                 planes.Remove(player.gameObject);

                 planeN = planes.Count;
            }
        }
    }


    void Update(){

      instIcons();

      getEnemiesTimer += Time.deltaTime;
      if (getEnemiesTimer > getEnemiesTime){
          // How many players ? Instantiate their icons.
          FindAllPlanesInAllScenes();

          getEnemiesTimer = 0f;
      }


        gamma = player.rotation.eulerAngles.y;
        beta = camera.rotation.eulerAngles.y;
        alpha = gamma - beta;
        playerIcon.rotation = Quaternion.Euler(0, 0, -alpha);
    }
}
