using UnityEngine;

public class VehicleSwitch : MonoBehaviour
{
    public PlaneControl pc;
    public GliderControl gc;

    private Vector3 gliderCatch;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gliderCatch = new Vector3( 711f, 283f, 321f );
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(transform.position, gliderCatch) < 5f){

          if (pc != null) pc.enabled = false;
          if (gc != null) gc.enabled = true;

        }
    }
}
