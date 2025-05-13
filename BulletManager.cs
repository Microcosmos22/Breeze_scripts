using UnityEngine;
using Mirror;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class BulletManager : NetworkBehaviour
{
    private AudioSource audioSource;

    private bool chatSelected = false;
    public GameObject player;
    public float directHitDamage = 33f;
    private Vector3 gun_xyz;
    private float explosionRadius = 40f; // Radius of the explosion
    public float lastFireTime = 0f;
    public GameObject bullet_trail_prefab, explosionInstance, bullet;
    public float bulletspeed = 100f, dist, hp_damage;
    public float fireRate = 0.7f; // Fire rate in seconds
    public float explosionTime = 3f;
    public GameObject explosionPrefabs;
    public bool whether2explode = true;

    private float fireRateTimer = 0f;
    private PlaneControl pc;
    private Rigidbody rb;
    private GameObject aircraft, vfx;
    private float offset;

    public GameObject chat;
    public GameObject lb;
    public TextMeshProUGUI chatText;
    public TextMeshProUGUI lbText;
    public GameObject chatInput; // Reference to the TMP_InputField GameObject
    private TMP_InputField chatInputField;
    private ChatManager chatManager;
    private string killmsg;
    public GameObject chatScroll;
    private ScrollRect chatScrollRect;

    public GameObject DealtDamageCross;

    public GameObject VertLayoutGroup;

    [SyncVar]
    public int kills;

    [SyncVar]
    public int deaths;

    private bool isOwned;

    public VehicleSwitch vehicleSwitch;
    private Collider collider;

    void Start()
    {
        aircraft = transform.gameObject;
        collider = aircraft.GetComponent<Collider>();
        offset = Random.Range(0f, 0.7f);
        // Get the PlaneControl component (which should be attached to the same GameObject)
        pc = GetComponent<PlaneControl>();
        rb = GetComponent<Rigidbody>();

        vehicleSwitch = GetComponent<VehicleSwitch>();

        if (pc.networkIdentity.isLocalPlayer){ // pc.enabled &&
          if (chatScroll != null)
              chatScrollRect = chatScroll.GetComponent<ScrollRect>();
          else
              Debug.LogWarning("chatScroll not assigned on BulletManager!");

          if (chatInput != null)
              chatInputField = chatInput.GetComponent<TMP_InputField>();
          else
              Debug.LogWarning("chatInput not assigned on BulletManager!");
          }
              chatManager = FindObjectOfType<ChatManager>();
        }

    void Update()
    {

         if(pc.networkIdentity.isOwned){ // is pc.enabled

           if (Input.GetKeyDown(KeyCode.Return) && !chatSelected) {
               chatInputField.Select();
               chatInputField.ActivateInputField();
               chatSelected = true; // flag so we know it's active
           } else if (Input.GetKeyDown(KeyCode.Return) && chatSelected) {

               chatInputField.DeactivateInputField(); // stops capturing input
               chatSelected = false; // reset the flag
           }


          if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrWhiteSpace(chatInputField.text)){
              string msg = chatInputField.text;
              chatInputField.text = "";

              if (isLocalPlayer && NetworkClient.isConnected){
                  chatManager.CmdSendMessage(msg, pc.Username);}
          }

          if (VertLayoutGroup.transform.childCount > 6)
          {
              // Remove the top item (first child)
              Transform firstChild = VertLayoutGroup.transform.GetChild(0);
              Text firstText = firstChild.GetComponent<Text>();
              if (firstText != null)
              {
                  Debug.Log("Text of first element: " + firstText.text);
              }
              Destroy(firstChild.gameObject);
          }

      }
        fireRateTimer += Time.deltaTime;
    }

    public IEnumerator DestroyExplosionAfterTime(GameObject explosionInstance, float time){
        yield return new WaitForSeconds(time);
        Destroy(explosionInstance);
    }

    [TargetRpc]
    public void TargetShowDamageCross(NetworkConnection target){
        StartCoroutine(ActivateDamageCross());
    }

    private IEnumerator ActivateDamageCross(){
        DealtDamageCross.SetActive(true);  // Activate the object
        yield return new WaitForSeconds(0.3f);  // Wait for 0.3 seconds
        DealtDamageCross.SetActive(false);  // Deactivate the object
    }

    [Server]
    public void Expl_damage(Vector3 explosion){
      if (!isServer) return;
        // Explosion checks if any planes are nearby.

        foreach (var netId in NetworkServer.spawned){
            var obj = netId.Value.gameObject;
            var plane = obj.GetComponent<PlaneControl>();
            var glider = obj.GetComponent<GliderControl>();

            if (plane != null){
                dist = Vector3.Distance(explosion, plane.transform.position);

                if (dist < (explosionRadius)){
                    hp_damage = Mathf.Clamp((-dist+40f)/2,0f,25f);

                    if (!pc.isAI){
                        TargetShowDamageCross(connectionToClient);}
                    //ApplyExplosionForce(plane, explosion, dist);

                    plane.healthBar -= hp_damage;
                    if (plane.connectionToClient != null){
                        plane.TargetTakeDamage(plane.connectionToClient, hp_damage);}

                    if (plane.healthBar < 0) {

                        kills += 1;
                        plane.GetComponent<BulletManager>().deaths += 1;

                        killmsg = $"{pc.Username} killed {plane.Username}";

                        if (pc.isAI){
                            chatManager.AISendMessage(killmsg, " ");
                        }else{
                            chatManager.CmdSendMessage(killmsg, " ");
                        }

                    }
                }
            }
        }
    }

    // Helper method to apply the drift force
    private void ApplyExplosionForce(PlaneControl plane, Vector3 explosionPosition, float distance)
    {
        // Calculate direction away from the explosion
        Vector3 directionAwayFromExplosion = plane.transform.position - explosionPosition;

        // Normalize the direction to avoid uneven force
        directionAwayFromExplosion.Normalize();

        // Calculate a force based on distance (the closer the plane, the stronger the force)
        float forceStrength = Mathf.Clamp((explosionRadius - distance) * 500f, 0f, 200f); // You can adjust the multiplier for desired force strength
        Debug.Log($"Applying force with strength {forceStrength}");

        // Apply the force if the plane has a Rigidbody
        Rigidbody rb = plane.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(directionAwayFromExplosion * forceStrength, ForceMode.VelocityChange);
        }
    }


    [Server]
    public IEnumerator Ballistics(GameObject bullet){
        float spawnTime = Time.time;
        bool exploded = false;
        float elapsedTime = 0f;

        while (true){
            if (bullet == null) yield break;
            elapsedTime = Time.time - spawnTime;

            if (elapsedTime >= explosionTime && !exploded){
                exploded = true; // Ensure we don't trigger an explosion more than once

                if (whether2explode){
                  Expl_damage(bullet.transform.position);
                  RpcSpawnExplosion(bullet.transform.position);
                }
                NetworkServer.Destroy(bullet);

                yield break;
            }
            yield return null;
        }
    }

    [ClientRpc]
    void RpcSpawnExplosion(Vector3 position) {
        vfx = Instantiate(explosionPrefabs, position, Quaternion.identity);
        StartCoroutine(DestroyExplosionAfterTime(vfx, 1f));
    }

    [Server]
    void SrvStartTrail(GameObject bullet){
        if (!isServer) return;

        StartCoroutine(Ballistics(bullet));
    }

    [Server]
    public void AICmdShootBullet(Quaternion passed_aim){
        if (!isServer) return;

        lastFireTime = Time.time;

        bullet = Instantiate(bullet_trail_prefab);
        Physics.IgnoreCollision(bullet.GetComponent<Collider>(), collider);

        bullet.transform.position = transform.position;
        gun_xyz = new Vector3();
        gun_xyz = passed_aim * Vector3.forward;
        bullet.GetComponent<Rigidbody>().linearVelocity = rb.linearVelocity + gun_xyz * bulletspeed;
        NetworkServer.Spawn(bullet);

        SrvStartTrail(bullet);
        //RpcStartTrail( transform.position, rb.linearVelocity + gun_xyz * bulletspeed);
        fireRateTimer = 0f;

    }

    [Command]
    public void CmdShootBullet(Quaternion gun_quaternion){

        bullet = Instantiate(bullet_trail_prefab);
        Physics.IgnoreCollision(bullet.GetComponent<Collider>(), collider);

        bullet.transform.position = transform.position;
        gun_xyz = gun_quaternion * Vector3.forward;
        bullet.GetComponent<Rigidbody>().linearVelocity = rb.linearVelocity + gun_xyz * bulletspeed;
        NetworkServer.Spawn(bullet);

        SrvStartTrail(bullet);
        //RpcStartTrail( transform.position, rb.linearVelocity + gun_xyz * bulletspeed);
    }

}
