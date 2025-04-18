using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Mirror;

public class Bullet : NetworkBehaviour
{
    public BoxCollider bulletCollider;
    private float directHitDam = 20f;
    private NetworkIdentity playerIdentity;
    private PlaneControl pc;

    void Start(){}

    void OnCollisionEnter(Collision collision){
        pc = collision.gameObject.GetComponent<PlaneControl>();

        if (collision.gameObject.CompareTag("AI")){
            pc.healthBar -= directHitDam;
            //print($" 📌 Direct hit to AI, health: {pc.healthBar}");
        }else if ( collision.gameObject.CompareTag("Player")){
            pc.healthBar -= directHitDam;
            playerIdentity = collision.gameObject.GetComponent<NetworkIdentity>();

            if (playerIdentity != null && playerIdentity.connectionToClient != null){
                //print($" 📌 Direct hit to Player, health: {pc.healthBar}");
                pc.TargetTakeDamage(playerIdentity.connectionToClient, directHitDam);
            }else{
                Debug.LogWarning("❌ No NetworkIdentity or connection found on collided player.");
            }
        }
      NetworkServer.Destroy(gameObject);
      }
}
