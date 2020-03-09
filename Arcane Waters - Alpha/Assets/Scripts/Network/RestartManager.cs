using System;
using Mirror;
using UnityEngine;

public class RestartManager : MonoBehaviour
{
    private void Awake()
    {
        InvokeRepeating("checkPendingServerRestarts",0.0f,60.0f);
    }

    private void checkPendingServerRestarts()
    {
        try
        {
            if (!NetworkServer.active) return;

            var info = DB_Main.getDeploySchedule();

            if (info == null) return;

            var timePoint = DateTime.FromBinary(info.DateAsTicks);

            ServerNetwork.self.server.SendGlobalChat(0, $"The Game Server will restart at: {timePoint.ToShortTimeString()} {timePoint.ToShortTimeString()}",
                                                     info.DateAsTicks, "", 0);
        }
        catch (Exception ex)
        {
            Debug.Log("RestartManager: Couldn't check for pending server restarts.");
        }
    }
    
}