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
        gliderCatch = new Vector3(711f, 283f, 321f);
        vehicletype = "pc";

        if (isServer)
        {
            // Initial setup on the server
            //gc.enabled = true;
            //pc.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isServer) // Ensure server-side logic for enabling/disabling components
        {
            if (Vector3.Distance(transform.position, gliderCatch) < 5f)
            {
                if (pc != null) pc.enabled = false;
                if (gc != null) gc.enabled = true;
                vehicletype = "gc";
                RpcUpdateVehicle("gc"); // Notify all clients about the switch
            }
            else if (!pc.enabled && gc.enabled)
            {
                vehicletype = "gc";
            }

            // Set model visibility
            if (vehicletype == "gc")
            {
                bulletManager.fireRate = 0.05f;
                bulletManager.explosionTime = 1f;
                bulletManager.bulletspeed = 300f;
                bulletManager.whether2explode = false;
                GliderModel.SetActive(true);
                PlaneModel.SetActive(false);
            }
            else
            {
                PlaneModel.SetActive(true);
                GliderModel.SetActive(false);
            }
        }
    }

    // Command: Called by the client but executed on the server
    [Command]
    void CmdSwitchToGlider()
    {
        if (pc != null) pc.enabled = false;
        if (gc != null) gc.enabled = true;

        // Notify clients to update the vehicle state
        RpcUpdateVehicle("gc");
    }

    // ClientRPC: This function is called by the server to update clients about the state
    [ClientRpc]
    void RpcUpdateVehicle(string vehicleType)
    {
        // On clients, update the vehicle type and enable/disable components
        vehicletype = vehicleType;
        if (vehicletype == "gc")
        {
            if (pc != null) pc.enabled = false;
            if (gc != null) gc.enabled = true;
            GliderModel.SetActive(true);
            PlaneModel.SetActive(false);
        }
        else
        {
            if (pc != null) pc.enabled = true;
            if (gc != null) gc.enabled = false;
            GliderModel.SetActive(false);
            PlaneModel.SetActive(true);
        }
    }
}
