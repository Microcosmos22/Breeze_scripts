using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collscr : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public bool OnTriggerEnter(Collider other){
    print("terrain collision");
    return true;
    }
}
