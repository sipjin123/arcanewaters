using System;
using Mirror;
using UnityEngine;

public class RestartManager : MonoBehaviour
{
   private void Awake () {
      // Continually check if a Restart has been scheduled in the database
      InvokeRepeating("checkPendingServerRestarts", 0.0f, 60.0f);
   }

   private void checkPendingServerRestarts () {
      try {
         // We only do this check on the server
         if (!NetworkServer.active) {
            return;
         }

         // Background thread to check the database
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DeployScheduleInfo info = DB_Main.getDeploySchedule();

            // Back to the Unity thread to handle the database info
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (info == null) {
                  return;
               }

               // Figure out when the restart is scheduled to take place
               DateTime timePoint = new DateTime(info.DateAsTicks);

               // Send a message to all players about the pending Restart
               ServerNetwork.self.server.SendGlobalChat($"The Game Server will restart at: {timePoint.ToShortTimeString()} {timePoint.ToShortTimeString()}", 0);
            });
         });
      } catch (Exception ex) {
         D.warning("RestartManager: Couldn't check for pending server restarts: " + ex);
      }
   }
}