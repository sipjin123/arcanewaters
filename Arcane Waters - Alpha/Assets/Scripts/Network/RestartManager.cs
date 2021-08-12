using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class RestartManager : GenericGameManager
{

   #region Public Variables

   // The number of seconds left at which point the server restart event is logged and clients cannot login to the server anymore
   public static float SECONDS_LEFT_FOR_RESTART_LOG = 15f;

   // Singleton self reference
   public static RestartManager self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;

      if (!Application.isEditor && !Util.isStressTesting()) {
         // Continually check if a Restart has been scheduled in the database
         InvokeRepeating("checkPendingServerRestarts", 0.0f, 60.0f);
      }

   }

   public void onScheduledServerRestart (DateTime dateTime) {
      _timeOfNextServerRestart = dateTime;
      _isServerRestartScheduled = true;
      StopAllCoroutines();
      StartCoroutine(CO_trackTimeToServerRestart());
      StartCoroutine(CO_LogServerRestartEvent());
   }

   public void onCanceledServerRestart () {
      if (_isServerRestartScheduled) {
         // Check if the server restart event has already been logged
         float secondsRemaining = (float) (_timeOfNextServerRestart - DateTime.UtcNow).TotalSeconds;
         if (secondsRemaining < SECONDS_LEFT_FOR_RESTART_LOG) {
            // Log the cancel event
            ServerHistoryManager.self.logServerEvent(ServerHistoryInfo.EventType.RestartCanceled);
         }
      }

      _isServerRestartScheduled = false;
   }

   public void onServerShutdown () {
      StopAllCoroutines();
      
      // Forcibly kick players
      foreach (NetEntity player in EntityManager.self.getAllEntities()) {
         if (player != null) {
            player.connectionToClient.Send(new ErrorMessage(Global.netId, ErrorMessage.Type.Kicked, $"You were disconnected.\n\n Reason: Server is currently shutting down"));
         }
      }
      
      // Quit application
      Invoke("QuitWithDelay", 10);
   }

   private void checkPendingServerRestarts () {
      try {
         // We only do this check on the server
         if (!NetworkServer.active) {
            return;
         }

         // Background thread to check the database
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            bool serverShutdown = DB_Main.getServerShutdown();
            DeployScheduleInfo info = DB_Main.getDeploySchedule();

            // Back to the Unity thread to handle the database info
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // If server is shutting down - trigger shutdown function and exit 
               if (serverShutdown) {
                  onServerShutdown();
                  return;
               }
               
               if (info == null || info.DateAsTicks < 0) {
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

      // Reset server restart sheduled flag if restart time is expired
      if (_isServerRestartScheduled && _timeOfNextServerRestart < DateTime.UtcNow) {
         _isServerRestartScheduled = false;
      }

      while (_isServerRestartScheduled && _timeOfNextServerRestart > DateTime.UtcNow) {
         double currSeconds = (double) (_timeOfNextServerRestart - DateTime.UtcNow).TotalSeconds;
         
         // Send chat global notification from master server only 
         if (ServerNetworkingManager.self.server.isMasterServer()) {
            for (int i = 0; i < secondsDiff.Length; i++) {
               if (Math.Abs(currSeconds - secondsDiff[i]) <= 2.5f) {
                  string timeUnit = "seconds";
                  string message = $"Server will reboot in {secondsDiff[i]} {timeUnit}!";
                  ChatInfo.Type chatType = ChatInfo.Type.Global;
                  ChatInfo chatInfo = new ChatInfo(0, message, System.DateTime.UtcNow, chatType);
                  ServerNetworkingManager.self?.sendGlobalChatMessage(chatInfo);
               }
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

            // Do db updates from master server only 
            if (ServerNetworkingManager.self.server.isMasterServer()) {
               // Log the server stop event
               ServerHistoryManager.self.logServerEvent(ServerHistoryInfo.EventType.ServerStop);
               
               // Update schedule_date to -1 to let jenkins job know that server release job can be started
               DB_Main.finishDeploySchedule();
               
               // Quit application
               // Invoke("QuitWithDelay", 10);
            }

            break;
         } else if (currSeconds < 9.0f) {
            yield return new WaitForSecondsRealtime(0.5f);
         } else {
            yield return new WaitForSecondsRealtime(5.0f);
         }
      }
   }

   private void QuitWithDelay () {
      Application.Quit();
   }

   private IEnumerator CO_LogServerRestartEvent () {
      if (_timeOfNextServerRestart < DateTime.UtcNow + TimeSpan.FromSeconds(3f)) {
         yield break;
      }

      float secondsRemaining = (float) (_timeOfNextServerRestart - DateTime.UtcNow).TotalSeconds;

      // Wait until the countdown is close to be finished
      yield return new WaitForSecondsRealtime(secondsRemaining - SECONDS_LEFT_FOR_RESTART_LOG);

      if (_isServerRestartScheduled) {
         // Log the server restart event
         ServerHistoryManager.self.logServerEvent(ServerHistoryInfo.EventType.RestartRequested);
      }
   }

   #region Private Variables

   // Time at which server will start restarting
   private DateTime _timeOfNextServerRestart;

   // Determine whether server is scheduled for restarting
   private bool _isServerRestartScheduled = false;

   #endregion

}