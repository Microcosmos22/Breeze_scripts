using UnityEngine;
using Mirror;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class BulletManager : NetworkBehaviour
{

    public GameObject player;
    public float directHitDamage = 33f;
    private Vector3 gun_xyz;
    private float explosionRadius = 40f; // Radius of the explosion
    private float lastFireTime = 0f;
    public GameObject bullet_trail_prefab, explosionInstance, bullet;
    public float bulletspeed, dist, hp_damage;
    public float fireRate = 0.7f; // Fire rate in seconds
    public float explosionTime = 3f;
    public GameObject explosionPrefabs;

    private float fireRateTimer = 0f;
    private PlaneControl pc;
    private Rigidbody rb;
    private GameObject aircraft;
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

    void Start()
    {
        offset = Random.Range(0f, 0.7f);
        // Get the PlaneControl component (which should be attached to the same GameObject)
        pc = GetComponent<PlaneControl>();
        rb = GetComponent<Rigidbody>();
        aircraft = transform.gameObject;

        if (pc.networkIdentity.isOwned){ // pc.enabled &&
            chatScrollRect = chatScroll.GetComponent<ScrollRect>();
            chatInputField = chatInput.GetComponent<TMP_InputField>();}
            chatManager = FindObjectOfType<ChatManager>();
        }

    void Update()
    {
         if(pc.networkIdentity.isOwned){ // is pc.enabled

          if (Input.GetKeyDown(KeyCode.Return)){
              chatInputField.Select();
              chatInputField.ActivateInputField();
          }
          if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrWhiteSpace(chatInputField.text)){
              string msg = chatInputField.text;
              chatInputField.text = "";
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
    [Server]
    public IEnumerator DestroyExplosionAfterTime(GameObject explosionInstance, float time){
        yield return new WaitForSeconds(time);
        NetworkServer.Destroy(explosionInstance);
    }

    private IEnumerator ActivateDamageCross(){
        DealtDamageCross.SetActive(true);  // Activate the object
        yield return new WaitForSeconds(0.3f);  // Wait for 0.3 seconds
        DealtDamageCross.SetActive(false);  // Deactivate the object
    }

    [Server]
    public void Expl_damage(Vector3 explosion){
        // Explosion checks if any planes are nearby.

        foreach (var netId in NetworkServer.spawned){
            var obj = netId.Value.gameObject;
            var plane = obj.GetComponent<PlaneControl>();

            if (plane != null){
                dist = Vector3.Distance(explosion, plane.transform.position);

                if (dist < (explosionRadius)){
                    hp_damage = Mathf.Clamp((-dist+40f)/2,0f,25f); //

                    if (pc.isOwned){
                        StartCoroutine(ActivateDamageCross());}


                    if (!plane.networkIdentity.isOwned){ // AI
                        plane.healthBar -= hp_damage;
                        //print($"🚨 Health: {plane.healthBar} to {plane.Username} at distance {dist} < {explosionRadius}");

                        if (plane.healthBar < 0f){
                            kills += 1;
                            plane.GetComponent<BulletManager>().deaths += 1;
                            //print($" {pc.Username} killed {plane.Username} and has now {kills} kills");
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

                  explosionInstance = Instantiate(explosionPrefabs);
                  explosionInstance.transform.position = bullet.transform.position;
                  StartCoroutine(DestroyExplosionAfterTime(explosionInstance, 1f));
                  NetworkServer.Spawn(explosionInstance);
                  Expl_damage(bullet.transform.position);
                  NetworkServer.Destroy(bullet);


                  yield break;
              }
              yield return null;
          }
      }



    [Command]
    void CmdStartTrail(GameObject bullet){
        StartCoroutine(Ballistics(bullet));
    }

    [Server]
    void AICmdStartTrail(GameObject bullet){
        StartCoroutine(Ballistics(bullet));
    }

    [Server]
    public void AICmdShootBullet(Quaternion passed_aim){

        if (Time.time - lastFireTime < fireRate + offset) return;

        if (!isServer) return;

        lastFireTime = Time.time;

         bullet = Instantiate(bullet_trail_prefab);
        NetworkServer.Spawn(bullet);
        Physics.IgnoreCollision(bullet.GetComponent<Collider>(), aircraft.GetComponent<Collider>());

        bullet.transform.position = transform.position;
         gun_xyz = new Vector3();
        gun_xyz = passed_aim * Vector3.forward;
        bullet.GetComponent<Rigidbody>().linearVelocity = rb.linearVelocity + gun_xyz * bulletspeed;

        AICmdStartTrail(bullet);
        fireRateTimer = 0f;

    }


    [Command]
    public void CmdShootBullet(Quaternion gun_quaternion){
        if (Time.time - lastFireTime < fireRate) return; // Prevent shooting too fast
        if (!isServer) return;

        lastFireTime = Time.time; // Update last fire time
         bullet = Instantiate(bullet_trail_prefab);
        NetworkServer.Spawn(bullet);
        Physics.IgnoreCollision(bullet.GetComponent<Collider>(), aircraft.GetComponent<Collider>());

        bullet.transform.position = transform.position;
         gun_xyz = gun_quaternion * Vector3.forward;

        bullet.GetComponent<Rigidbody>().linearVelocity = rb.linearVelocity + gun_xyz * bulletspeed;

        CmdStartTrail(bullet);
    }

}
