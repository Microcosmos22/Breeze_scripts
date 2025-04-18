using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;

public class DisplayGliderVariables : MonoBehaviour
{
    public GameObject player;  // Reference to the Plane GameObject
    public Text displayText1;  // Reference to the Text UI component
    public Text displayText2;  // Reference to the Text UI component
    public Text displayText3;  // Reference to the Text UI component
    public Text displayText4;  // Reference to the Text UI component
    public Text displayText5;  // Reference to the Text UI component
    public Text displayText6;  // Reference to the Text UI component
    private GliderControl gc;

    void Start()
    {
        gc = player.GetComponent<GliderControl>();
        
        
    }
    void Update(){
        
        displayText1.text = "Drag: "+gc.drag.magnitude.ToString();
        displayText2.text = "Lift: "+gc.lift.magnitude.ToString();
        
    }
}
