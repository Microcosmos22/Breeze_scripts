using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GoalChecker : MonoBehaviour
{
    //public static UIManager Instance; // Singleton instance
    private string scenename;
    // Defines a world volume, once entered -> check_passed = true
    private Vector3 check_level;// (x,y,z)_min , (x,y,z,)_max
    private float r0_level = 0f;
    private bool check_passed = false;
    
    
    void Start(){}
    
    public void set_level(int chosenLevel){
        
        
        if (chosenLevel == 1){         // Mountain climb
            check_level = new Vector3(578f, 5000f, 369f);  // objective
            r0_level = 4760f;                              // spheric distance to objective
        }else if (chosenLevel == 2){    // Mountain Airport
            check_level = new Vector3(746f, 215f, 264f);
            r0_level = 40f;
        }else if (chosenLevel == 3){ // Dunes 1 Airport
            check_level = new Vector3(917f, 20f, 285f);
            r0_level = 40f;
        }else if (chosenLevel == 4){  // Dunes  Climb
            check_level = new Vector3(500f, 5000f, 500f);
            r0_level = 4700f;
        }else if (chosenLevel == 5){  // Wake Island Airport
            check_level = new Vector3(354f, 50f, 382f);
            r0_level = 50f;
        }else if (chosenLevel == 6){    // Wake island climb
            check_level = new Vector3(1000f, 5000f, 500f);
            r0_level = 4550f;
        }
        
        print("Level goal is at:");
        print(check_level);
        
    }
    
    // Is aircraft at less than a distance r0 from the check_level goal?
    public bool goal_reached(Vector3 aircraft){
        float distance = Vector3.Distance(aircraft, check_level);
        
        if (distance < r0_level){
            return true;
        }else{
            return false;
        }
    }
    
    public void set_checkpoint_passed(Transform aircraft_transform){ // Every checkpoint must have been passed.
    
        // If ever passed (OR)
        check_passed = (check_passed || goal_reached(aircraft_transform.position));
    
    }
    
    public bool get_checkpoint_passed(){
        return check_passed;
    }
    
    }