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

    public Slider Airspeedslider; // Reference to the Slider component
    public Slider verticalVelocitySlider; // Reference to the Slider component

    public GameObject GreenBar;
    private RectTransform GreenRectTrans;
    public GameObject RedBar;
    private RectTransform RedRectTrans;
    public GameObject HealthBar;
    private RectTransform healthBarTrans;
    public GameObject ExplGameObject;
    private Text explTime;
    public GameObject CoolDownBar;
    private RectTransform coolBarTrans;

    public float bulletspeed = 40f;
    public float minAltitude = 0f;    // Minimum altitude (adjust as needed)
    public float maxAltitude = 850f; // Maximum altitude (adjust as needed)
    public Image pointerImage;        // Reference to the pointer Image component
    public float currentAltitude, verticalVelocity, vel, normalizedAltitude, targetAngle;
    public Image pointer1;
    public Image pointer2;
    public Vector3 newScale;




    // Start is called before the first frame update
    void Start()
    {
        rb = player.GetComponent<Rigidbody>();
        GreenRectTrans = GreenBar.GetComponent<RectTransform>();
        RedRectTrans = RedBar.GetComponent<RectTransform>();
        pc = player.GetComponent<PlaneControl>();
        healthBarTrans = HealthBar.GetComponent<RectTransform>();
        explTime = ExplGameObject.GetComponent<Text>();
        coolBarTrans = CoolDownBar.GetComponent<RectTransform>();




        //Airspeedslider = GetComponent<Slider>();

        // Get current altitude of the parent Rigidbody

        //pointer1 = GameObject.Find("PointAlt").GetComponent<Image>(); //GetComponentInChildren<PointAlt>();
        //pointer2 = GameObject.Find("PointAlt_fast").GetComponent<Image>(); // GetComponentInChildren<PointAlt_fast>();
    }

    // Update is called once per frame
    void Update()
    {
        if (true){
            explTime.text = (Mathf.Round(pc.bulletManager.explosionTime*10f)/10f).ToString() + " s " + Mathf.Round((pc.bulletManager.explosionTime*pc.bulletManager.bulletspeed)).ToString() + " m ";

            if (rb != null){

            healthBarTrans.sizeDelta = new Vector2(pc.healthBar*2f, healthBarTrans.sizeDelta.y);
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
