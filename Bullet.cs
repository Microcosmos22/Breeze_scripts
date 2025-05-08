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

    public GameObject smallExplosion;
    private GameObject explosionInstance;

    void Start(){}

    [Server]
    public IEnumerator DestroyExplosionAfterTime(GameObject explosionInstance, float time){
        yield return new WaitForSeconds(time);
        NetworkServer.Destroy(explosionInstance);
    }

    void OnCollisionEnter(Collision collision){
      if (!isServer) return;
      
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
        }else if( collision.gameObject.CompareTag("Terrain")){
          explosionInstance = Instantiate(smallExplosion);
          explosionInstance.transform.position = transform.position;
          StartCoroutine(DestroyExplosionAfterTime(explosionInstance, 0.05f));
          NetworkServer.Spawn(explosionInstance);
        }
      NetworkServer.Destroy(gameObject);
      }
}
