using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class CamFollower : NetworkBehaviour
{
    public GameObject player;
    private Vector3 offset = new Vector3(-10f, 5f, 10f);

    public float mouseX;
    public float mouseY;
    public float scrollInput;
    public float distance; // Distance from the target
    public float minDistance = 5f; // Minimum allowed distance
    public float maxDistance = 50f; // Maximum allowed distance
    public float zoomSpeed = 2f; // Speed of zoom adjustment
    public float mouseSensitivity = 300f; // Mouse sensitivity
    private float xRotation = 0f; // Vertical rotation
    private float yRotation = 0f; // Horizontal rotation
    public bool firstPerson = false;

    public Quaternion cam_rotation;
    public Vector3 position;
    public Vector3 v_offset;
    private Camera camera;


    public GameObject gunCrosshair;
    private RawImage gunImg;
    private RectTransform crossTransf;
    private float crossSize;

    public VehicleSwitch vehicleSwitch;

    // Start is called before the first frame update
    void Start()
    {
        camera = GetComponent<Camera>();

        player = transform.parent.gameObject;
        crossTransf = gunCrosshair.GetComponent<RectTransform>();
        gunImg = gunCrosshair.GetComponent<RawImage>();
        vehicleSwitch = GetComponentInParent<VehicleSwitch>();

        // This will be called on start. If no UIManager is present, this values will be used (for testing purposes)
        // Initial offset vector  (camera-player) will be kept for testing.
        if (GameObject.Find("EZGliderPlanePrefab 1") != null)
        {
            offset = transform.position - GameObject.Find("EZGliderPlanePrefab 1").GetComponent<Rigidbody>().transform.position;
        }
        distance = offset.magnitude;

        Camera.main.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
      // All of this logs are fine, its only not "created", whatever that means.

      //Debug.Log("Camera is enabled: " + camera.enabled);
      //Debug.Log("RenderTexture assigned: " + (camera.targetTexture != null));
      //Debug.Log("RenderTexture is created: " + (camera.targetTexture?.IsCreated() ?? false));

      /*if (isClient) {
        Debug.LogWarning("camera running as a client!");
      }*/

        if (player != null)
        {
            v_offset = new Vector3(0f, 0f, 0f);
            // Handle camera zoom with mouse wheel
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            Camera.main.fieldOfView -= scrollInput * zoomSpeed * 10f; // Adjust FOV
            Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView, 40f, 80f); // Clamp to avoid extreme zoom
            crossSize += scrollInput*zoomSpeed/2f;
            crossSize = Mathf.Clamp(crossSize, 1f, 2.5f);

            crossTransf.localScale = new Vector3(crossSize, crossSize, crossSize);
            //distance -= scrollInput * zoomSpeed;
            //distance = Mathf.Clamp(distance, minDistance, maxDistance); // Ensure distance is within bounds

            // Calculate mouse movement for rotation
            if (Application.platform == RuntimePlatform.WebGLPlayer) {
                mouseX = Input.GetAxis("Mouse X") * mouseSensitivity / 4 * Time.deltaTime;
                mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity / 4 * Time.deltaTime;
            }else{
                mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
                mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
            }

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -80f, 80f); // Limit vertical rotation to prevent flipping

            // Calculate the rotation and position of the camera
            cam_rotation = Quaternion.Euler(xRotation, yRotation, 0);

            if (Input.GetMouseButtonDown(1) && firstPerson){
                firstPerson = false;
            }else if(Input.GetMouseButtonDown(1) && !firstPerson){
                firstPerson = true;
            }

            if(firstPerson){

                gunImg.enabled = true;
                if (vehicleSwitch.vehicletype == "pc"){
                    position = player.transform.position + player.transform.up*1f;

                }else if (vehicleSwitch.vehicletype == "gc"){
                    position = player.transform.position + player.transform.up*1f - player.transform.forward*2.5f;
                    cam_rotation = transform.parent.rotation; //Quaternion.LookRotation(transform.parent.forward);

                }else if ( vehicleSwitch == null){
                    print(" cam couldnt find vehicle type");
                }
            }else{
                Camera.main.fieldOfView = 60f;
                position = player.transform.position - cam_rotation * Vector3.forward * 10f + v_offset;
                gunImg.enabled = false;
            }


            // Set camera position and rotation
            transform.position = position;
            transform.rotation = cam_rotation; // Look at the target
        }
    }

    public Quaternion get_camera_quaternion(){
      return cam_rotation;
    }

    public Quaternion get_camera_quaternion_with_error(float maxAngleErrorDegrees = 2.5f)
{
    // Original rotation
    Quaternion baseRotation = cam_rotation;

    // Create a small random rotation
    Vector3 randomAxis = Random.onUnitSphere; // random direction
    float angleError = Random.Range(-maxAngleErrorDegrees, maxAngleErrorDegrees);

    Quaternion errorRotation = Quaternion.AngleAxis(angleError, randomAxis);

    // Apply the error to the original rotation
    return errorRotation * baseRotation;
}

    public void set_camera(Vector3 offset_i, Vector3 rotation_i)
    {
        offset = offset_i;

        Quaternion newRotation = Quaternion.Euler(rotation_i);

        // Apply the new rotation to the transform
        transform.rotation = newRotation;
    }
}
