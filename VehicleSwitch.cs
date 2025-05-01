using UnityEngine;
using Mirror;

public class VehicleSwitch : NetworkBehaviour
{
    public PlaneControl pc;
    public GliderControl gc;
    public string vehicletype;

    private Vector3 gliderCatch;
    public BulletManager bulletManager;

    public GameObject PlaneModel;
    public GameObject GliderModel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bulletManager = GetComponent<BulletManager>();
        gliderCatch = new Vector3( 711f, 283f, 321f );
        vehicletype = "pc";
        //gc.enabled = true;
        //pc.enabled = false;

    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(transform.position, gliderCatch) < 5f){

            if (pc != null) pc.enabled = false;
            if (gc != null) gc.enabled = true;
            vehicletype = "gc";
          }else if(!pc.enabled && gc.enabled){
            vehicletype = "gc";
          }

        if (vehicletype == "gc" ){

          bulletManager.fireRate = 0.05f;
          bulletManager.explosionTime = 1f;
          bulletManager.bulletspeed = 300f;
          bulletManager.whether2explode = false;
          GliderModel.SetActive(true);
          PlaneModel.SetActive(false);

          } else{
            PlaneModel.SetActive(true);
            GliderModel.SetActive(false);
          }
        }
    }
