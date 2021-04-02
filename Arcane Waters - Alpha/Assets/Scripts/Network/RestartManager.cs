using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class RestartManager : GenericGameManager
{
   #region Public Variables

   // Singleton self reference
   public static RestartManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      // Continually check if a Restart has been scheduled in the database
      InvokeRepeating("checkPendingServerRestarts", 0.0f, 60.0f);

      self = this;
   }

   public void onScheduledServerRestart(DateTime dateTime) {
      _timeOfNextServerRestart = dateTime;
      _isServerRestartScheduled = true;
      StartCoroutine(CO_trackTimeToServerRestart());
   }

   public void onCanceledServerRestart () {
      _isServerRestartScheduled = false;
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

               if (!_isServerRestartScheduled) {
                  onScheduledServerRestart(timePoint);
               }

               // Send a message to all players about the pending Restart
               //ChatInfo chatInfo = new ChatInfo(0, $"The Game Server will restart at: {timePoint.ToShortTimeString()} {timePoint.ToShortTimeString()}",
               //   System.DateTime.UtcNow, ChatInfo.Type.Global, "Server", "", 0);
               //ServerNetworkingManager.self.sendGlobalChatMessage(chatInfo);
            });
         });
      } catch (Exception ex) {
         D.warning("RestartManager: Couldn't check for pending server restarts: " + ex);
      }
   }

   private IEnumerator CO_trackTimeToServerRestart () {
      double[] secondsDiff = { 60, 30, 10 };

      while (_isServerRestartScheduled && _timeOfNextServerRestart > DateTime.UtcNow) {
         double currSeconds = (double) (_timeOfNextServerRestart - DateTime.UtcNow).TotalSeconds;
         for (int i = 0; i < secondsDiff.Length; i++) {
            if (Math.Abs(currSeconds - secondsDiff[i]) <= 2.5f) {
               string timeUnit = "seconds";
               string message = $"Server will reboot in {secondsDiff[i]} {timeUnit}!";
               ChatInfo.Type chatType = ChatInfo.Type.Global;
               ChatInfo chatInfo = new ChatInfo(0, message, System.DateTime.UtcNow, chatType);
               ServerNetworkingManager.self?.sendGlobalChatMessage(chatInfo);
            }
         }

         if (currSeconds < 3.0f) {
            // Forcibly kick players
            _isServerRestartScheduled = false;
            foreach (NetEntity player in EntityManager.self.getAllEntities()) {
               if (player != null) {
                  player.connectionToClient.Send(new ErrorMessage(Global.netId, ErrorMessage.Type.Kicked, $"You were disconnected.\n\n Reason: Server is currently rebooting"));
               }
            }

            // Log the server stop event
            ServerHistoryManager.self.onServerStop();

            break;
         } else if (currSeconds < 9.0f) {
            yield return new WaitForSecondsRealtime(0.5f);
         } else {
            yield return new WaitForSecondsRealtime(5.0f);
         }
      }
   }

   #region Private Variables

   // Time at which server will start restarting
   private DateTime _timeOfNextServerRestart;

   // Determine whether server is scheduled for restarting
   private bool _isServerRestartScheduled = false;

   #endregion

}