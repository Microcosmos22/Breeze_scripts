using Mirror;
using UnityEngine;

public class KeepAlive : NetworkBehaviour
{
    private float pingInterval = 10f;
    private float timer = 0f;

    void Update()
    {
        if (!isLocalPlayer) return;

        timer += Time.deltaTime;
        if (timer >= pingInterval)
        {
            CmdPingServer();
            timer = 0f;
        }
    }

    [Command]
    void CmdPingServer()
    {
        // This command keeps the connection alive
    }
}
