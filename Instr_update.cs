using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Instr_update : MonoBehaviour
{
    public GameObject player;
    private Rigidbody rb; // Reference to the parent Rigidbody
    private PlaneControl pc;
    private GliderControl gc;

    public Slider Airspeedslider; // Reference to the Slider component
    public Slider verticalVelocitySlider; // Reference to the Slider component

    public GameObject GreenBar;
    private RectTransform GreenRectTrans;
    public GameObject RedBar;
    private RectTransform RedRectTrans;
    public GameObject HealthBar;
    private RectTransform healthBarTrans;
    public GameObject ExplGameObject, explTitle;
    private Text explTime;
    public GameObject CoolDownBar;
    private RectTransform coolBarTrans;
    public GameObject energyGO, energyTitle;
    private Text energy;
    private float energyvalue;

    public float bulletspeed = 40f;
    public float minAltitude = 0f;    // Minimum altitude (adjust as needed)
    public float maxAltitude = 850f; // Maximum altitude (adjust as needed)
    public Image pointerImage;        // Reference to the pointer Image component
    public float currentAltitude, verticalVelocity, vel, normalizedAltitude, targetAngle;
    public Image pointer1;
    public Image pointer2;
    public Vector3 newScale;

    public VehicleSwitch vehicleSwitch;




    // Start is called before the first frame update
    void Start()
    {
        vehicleSwitch = GetComponentInParent<VehicleSwitch>();
        rb = player.GetComponent<Rigidbody>();
        GreenRectTrans = GreenBar.GetComponent<RectTransform>();
        RedRectTrans = RedBar.GetComponent<RectTransform>();
        pc = player.GetComponent<PlaneControl>();
        gc = player.GetComponent<GliderControl>();
        healthBarTrans = HealthBar.GetComponent<RectTransform>();
        explTime = ExplGameObject.GetComponent<Text>();
        coolBarTrans = CoolDownBar.GetComponent<RectTransform>();
        energy = energyGO.GetComponent<Text>();


        if (energyGO == null)
            Debug.LogError("energyGO is NOT assigned!");
        else if (energy == null)
            Debug.LogError("Text component not found on energyGO!");
        else
            Debug.Log("Energy text component successfully assigned.");


        //Airspeedslider = GetComponent<Slider>();

        // Get current altitude of the parent Rigidbody

        //pointer1 = GameObject.Find("PointAlt").GetComponent<Image>(); //GetComponentInChildren<PointAlt>();
        //pointer2 = GameObject.Find("PointAlt_fast").GetComponent<Image>(); // GetComponentInChildren<PointAlt_fast>();
    }

    // Update is called once per frame
    void Update()
    {
        if (vehicleSwitch.vehicletype == "pc"){
            explTime.text = (Mathf.Round(pc.bulletManager.explosionTime*10f)/10f).ToString() + " s " + Mathf.Round((pc.bulletManager.explosionTime*pc.bulletManager.bulletspeed)).ToString() + " m ";
            energy.text = " ";
            energyTitle.SetActive(false);


            if (rb != null){

            if (vehicleSwitch.vehicletype == "pc"){ healthBarTrans.sizeDelta = new Vector2(pc.healthBar*2f, healthBarTrans.sizeDelta.y);}
            else if ( vehicleSwitch.vehicletype == "gc") {healthBarTrans.sizeDelta = new Vector2(gc.healthBar*2f, healthBarTrans.sizeDelta.y);}
            coolBarTrans.sizeDelta = new Vector2((1-Mathf.Clamp(pc.gunCoolTimer/pc.gunUptime, 0f, 1f))*200f ,coolBarTrans.sizeDelta.y);

            verticalVelocity = rb.linearVelocity.y;
            verticalVelocitySlider.value = -verticalVelocity; //rectTransform.localScale;


            if (verticalVelocity > 0){

                newScale = GreenRectTrans.localScale;
                GreenRectTrans.localScale = new Vector3((float)verticalVelocity, newScale.y, newScale.z);
                RedRectTrans.localScale = new Vector3(0f, newScale.y, newScale.z);
            }else{

                newScale = RedRectTrans.localScale;
                RedRectTrans.localScale = new Vector3((float)verticalVelocity, newScale.y, newScale.z);
                GreenRectTrans.localScale = new Vector3(0f, newScale.y, newScale.z);
            }


            vel = (float)rb.linearVelocity.magnitude;
            Airspeedslider.value = vel;

            currentAltitude = rb.position.y;
            normalizedAltitude = Mathf.InverseLerp(minAltitude, maxAltitude, currentAltitude);

            targetAngle = normalizedAltitude * 360f;

            // Rotate the pointer Image around the Z-axis
            pointer1.rectTransform.rotation = Quaternion.Euler(0f, 0f, -targetAngle);
            pointer2.rectTransform.rotation = Quaternion.Euler(0f, 0f, -targetAngle*10f);

            }
        }else{

            if (rb != null){

            healthBarTrans.sizeDelta = new Vector2(pc.healthBar*2f, healthBarTrans.sizeDelta.y);
            coolBarTrans.sizeDelta = new Vector2((1-Mathf.Clamp(gc.gunCoolTimer/gc.gunUptime, 0f, 1f))*200f ,coolBarTrans.sizeDelta.y);

            verticalVelocity = rb.linearVelocity.y;
            verticalVelocitySlider.value = -gc.slope_vel[1] - gc.cloud_suction[1]; //rectTransform.localScale;

            //print($" vert. slope: {gc.slope_vel[1]}, vert. cloudSuct: {gc.cloud_suction[1]}");

            if ((gc.slope_vel[1] + gc.cloud_suction[1]) > 0){

                newScale = GreenRectTrans.localScale;
                GreenRectTrans.localScale = new Vector3((float)(gc.slope_vel[1] + gc.cloud_suction[1]), newScale.y, newScale.z);
                RedRectTrans.localScale = new Vector3(0f, newScale.y, newScale.z);
            }else{

                newScale = RedRectTrans.localScale;
                RedRectTrans.localScale = new Vector3((float)(gc.slope_vel[1] + gc.cloud_suction[1]), newScale.y, newScale.z);
                GreenRectTrans.localScale = new Vector3(0f, newScale.y, newScale.z);
            }


            vel = (float)rb.linearVelocity.magnitude;
            Airspeedslider.value = vel;

            currentAltitude = rb.position.y;
            normalizedAltitude = Mathf.InverseLerp(minAltitude, maxAltitude, currentAltitude);

            targetAngle = normalizedAltitude * 360f;

            // Rotate the pointer Image around the Z-axis
            pointer1.rectTransform.rotation = Quaternion.Euler(0f, 0f, -targetAngle);
            pointer2.rectTransform.rotation = Quaternion.Euler(0f, 0f, -targetAngle*10f);

            energyvalue = (5f*currentAltitude + rb.linearVelocity.magnitude*rb.linearVelocity.magnitude*1/2f)/1000f;
            energy.text = (Mathf.Round(energyvalue * 100f) / 100f).ToString() + " kJ";
            explTime.text = " ";
            explTitle.SetActive(false);
          }
        }
      }


    public void set_player(GameObject player)
    {
        Debug.Log("Passing player's RB to instruments");
        Debug.Log("Player has PlaneControl: " + (player.GetComponent<PlaneControl>() != null));
        Debug.Log("Player has GliderControl: " + (player.GetComponent<GliderControl>() != null));

        if (player.GetComponent<GliderControl>() != null)
        {
            this.rb = player.GetComponent<Rigidbody>();
            Debug.Log("Rigidbody set from GliderControl");
        }
        else if (player.GetComponent<PlaneControl>() != null)
        {
            this.rb = player.GetComponent<Rigidbody>();
            Debug.Log("Rigidbody set from PlaneControl");
        }
        else
        {
            Debug.LogError("Instrument found NO PLAYER SCRIPT");
        }

        if (this.rb == null)
        {
            Debug.LogError("Rigidbody is still null after attempting to set it.");
        }
        else
        {
            Debug.Log("Rigidbody successfully set.");
        }
    }
}
