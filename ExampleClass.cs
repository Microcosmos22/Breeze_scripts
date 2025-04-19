using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class ExampleClass : MonoBehaviour
{
    public GameObject cam;
    public GameObject plane;

    public float speed;
    public float forw_speed;
    public float rotateSpeed;

    public float lift_coeff;
    public float lift_magn;

    public float input_ws;
    public Vector3 speedvec;
    
    public Vector3 acceleration_i;
    

    public float mass;
    public float motor_torque;
    
    public Vector3 lift_acc;
    public Vector3 gravity_acc;
    public float dt;
    
    public Collider terrain;
    
    Rigidbody rb;
    
    void Start(){
        rb = GetComponent<Rigidbody> ();
        
        //rb.AddForce(new Vector3(0, -9.8f, 0));  
        
        terrain.isTrigger = true;
    
        CharacterController controller = GetComponent<CharacterController>();
        controller.SimpleMove(new Vector3(0.0f, 0.0f, 0.0f));
        
        speed = 0.0F;
        rotateSpeed = 0.5F;

        lift_coeff = 1.0F;

        speedvec = new Vector3(0.0f, 0.0f, 0.0f);
        lift_acc = new Vector3(0.0f, 0.0F, 0.0f);
        gravity_acc = new Vector3(0.0f, 0.0f, 0.0f);

        mass = 250.0F;
        motor_torque = 0.05F;
        
        dt = 1F;
        
        
    
    }

    void FixedUpdate()
    {
        
        CharacterController controller = GetComponent<CharacterController>();

        // Rotate around y - axis
        transform.Rotate(0, Input.GetAxis("Horizontal") * rotateSpeed, 0);

        // Move forward / backward
        Vector3 e_f = plane.transform.position - cam.transform.position;
        //Vector3 forward = transform.TransformDirection(Vector3.forward);
        e_f.y = 0;
        e_f = Vector3.Normalize(e_f);
        
        
        
        input_ws = Input.GetAxis("Vertical");
        
        //print(float(lift_coeff*(speed)));
        forw_speed = Vector3.Project(speedvec, e_f).magnitude;
        lift_magn = forw_speed*lift_coeff;
        lift_acc = new Vector3(0.0f, lift_magn, 0.0f);
        
        acceleration_i = e_f * input_ws * motor_torque + lift_acc + gravity_acc;
        
        
        //speedvec = speedvec + acceleration_i*dt;
        
        //print(controller.detectCollisions);
        
        //if(OnTriggerEnter(terrain) == true)
        //{speedvec.y=0;
        //Debug.Log("Grounded aircraft");}
        
        
        
        //controller.Move(speedvec/1000);
        
        rb.position += speedvec/1000*Time.deltaTime;
        
        //Debug.Log("Forw speed");
        //Debug.Log(forw_speed);
        Debug.Log("Lift");
        Debug.Log(speedvec);
        
        
    }
    
    
    public bool OnTriggerStay(Collider other)
    {

        if(other.gameObject.name == "Terrain")
        {
            Debug.Log("Grounded aircraft");
            speedvec.y=0;
            return true;
        }else{return false;}
    }
}