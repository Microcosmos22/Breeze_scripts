using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class PanelManager : NetworkBehaviour
{
    public GameObject flyButtonGO;
    public GameObject submitButtonGO;

    private Button flyButton;
    private Button submitButton;

    private PlaneControl pc;
    private GliderControl gc;

    private Camera mainCam;
    public GameObject guidePanel, registerPanel, instrumentsPanel, gamePanel;

    public GameObject inputField;   // Assign in inspector
    private TMP_InputField inputF;      // This will hold the reference to InputField

    void Update(){

        if (gc != null && gc.enabled == true){
            instrumentsPanel.SetActive(false);
        }
    }

    void Start(){
      pc = GetComponent<PlaneControl>();
      gc = GetComponent<GliderControl>();
      mainCam = GetComponentInChildren<Camera>();
      flyButton = flyButtonGO.GetComponent<Button>();
      submitButton = submitButtonGO.GetComponent<Button>();
      inputF = inputField.GetComponent<TMP_InputField>();  // This will only work if inputField is an InputField (not a Text)



      if (flyButton != null){
          print(" add listener to fly button");
          flyButton.onClick.AddListener(OnFlyButtonClicked);
      }

      if (submitButton != null){
          submitButton.onClick.AddListener(OnSubmitButtonClicked);
      }


        // Make sure inputField is assigned with an InputField in the inspector.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (isLocalPlayer){
            mainCam = GetComponentInChildren<Camera>();

            registerPanel.SetActive(true);
            gamePanel.SetActive(false);
            instrumentsPanel.SetActive(false);
            Debug.Log("registerPanel Enabled: " + gamePanel.activeSelf);

            mainCam.enabled = true;
        }

        if (flyButton != null){
            flyButton.onClick.AddListener(OnFlyButtonClicked);
        }

        //OnFlyButtonClicked();
    }






    public void OnSubmitButtonClicked(){
      if (isLocalPlayer && inputF.text != ""){

          registerPanel.SetActive(false);
          guidePanel.SetActive(true);
          instrumentsPanel.SetActive(false);
          pc.Username = inputF.text;
          Debug.Log("Registered the user : " + pc.Username);
      }

    }


    public void OnFlyButtonClicked(){
      print(" Fly button clicked ");
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;

        if (isLocalPlayer)
        {
            registerPanel.SetActive(false);
            gamePanel.SetActive(true);
            guidePanel.SetActive(false);
            instrumentsPanel.SetActive(true);
            Debug.Log("gamePanel Enabled: " + gamePanel.activeSelf);

            mainCam.enabled = true; // Disable the guide camera if any
            //Camera.main.enabled = true; // Enable the main camera for the scene
            pc.ispaused = false;
            pc.set_initpos();

        }
    }


}
