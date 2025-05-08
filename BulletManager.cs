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

    public int kills, deaths;
    private bool isOwned;

    public VehicleSwitch vehicleSwitch;
    private Collider collider;

    void Awake() {
        vehicleSwitch = GetComponent<VehicleSwitch>();
    }

    void Start()
    {
        print($" Initializing BulletManager: {isLocalPlayer}, {isOwned}, {isServer}");

        aircraft = transform.gameObject;
        collider = aircraft.GetComponent<Collider>();
        offset = Random.Range(0f, 0.7f);
        // Get the PlaneControl component (which should be attached to the same GameObject)
        pc = GetComponent<PlaneControl>();
        rb = GetComponent<Rigidbody>();

        vehicleSwitch = GetComponent<VehicleSwitch>();

        if (pc.networkIdentity.isLocalPlayer){ // pc.enabled &&
            chatScrollRect = chatScroll.GetComponent<ScrollRect>();
            chatInputField = chatInput.GetComponent<TMP_InputField>();}
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
              print($" Player tryina send message, isOwned: {isOwned}");
              chatManager.CmdSendMessage(msg, pc.Username);
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

            if (plane != null){
                dist = Vector3.Distance(explosion, plane.transform.position);

                if (dist < (explosionRadius)){
                    hp_damage = Mathf.Clamp((-dist+40f)/2,0f,25f);

                    if (pc.isOwned){
                        StartCoroutine(ActivateDamageCross());}


                    if (plane.isAI){ // AI
                        plane.healthBar -= hp_damage;
                        //print($"🚨 Health: {plane.healthBar} to {plane.Username} at distance {dist} < {explosionRadius}");

                        if (plane.healthBar < 0f){
                            kills += 1;
                            plane.GetComponent<BulletManager>().deaths += 1;
                            print($" {pc.Username} killed {plane.Username} and has now {kills} kills");
                            killmsg = $"{pc.Username} killed {plane.Username}";
                            chatManager.CmdSendMessage(killmsg, "void");
                        }
                    } else {

                        plane.healthBar -= hp_damage;
                        //print($"🚨 Health: {plane.healthBar} to {obj.name} at distance {dist} < {explosionRadius}");
                        //print($" within range: {dist} m, dealing {hp_damage} damage");

                        plane.TargetTakeDamage(plane.connectionToClient, hp_damage);
                        if (plane.healthBar < 0) {
                            plane.set_initpos();
                            plane.TargetSetInitpos(plane.connectionToClient);
                            plane.healthBar = 100f;
                            plane.TargetResetHealth(plane.connectionToClient);

                            kills += 1;
                            plane.GetComponent<BulletManager>().deaths += 1;
                            print($" {pc.Username} killed {plane.Username} and has now {kills} kills");
                            killmsg = $"{pc.Username} killed {plane.Username}";
                            chatManager.CmdSendMessage(killmsg, "void");
                        }
                    }
                }
            }
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
                  print("server bullet explode");
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
        print("cmdstart trail");
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
        bullet.GetComponent<Rigidbody>().linearVelocity = gun_xyz * bulletspeed;
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
