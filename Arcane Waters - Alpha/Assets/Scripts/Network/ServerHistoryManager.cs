using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class ServerHistoryManager : MonoBehaviour
{
   #region Public Variables

   // The number of days until the history entries are deleted
   public static int HISTORY_ENTRIES_LIFETIME = 15;

   // Set to true to log any server event (not only cloud builds) and always show the status panel in the title screen
   public bool enableDebug = false;

   // Self
   public static ServerHistoryManager self;

   #endregion

   public void Awake() {
      self = this;
   }

   [Server]
   public void onServerStart () {
      if (!isServerHistoryActive()) {
         return;
      }
      
      _buildVersionNumber = Util.getGameVersion();
      DateTime pruneUntilDate = DateTime.UtcNow - new TimeSpan(HISTORY_ENTRIES_LIFETIME, 0, 0, 0);

      // Create the new event
      logServerEvent(ServerHistoryInfo.EventType.ServerStart);

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Delete old events
         DB_Main.pruneServerHistory(pruneUntilDate);
      });
   }

   [Server]
   public void logServerEvent (ServerHistoryInfo.EventType e) {
      if (!isServerHistoryActive()) {
         return;
      }

      Util.tryToRunInServerBackground(() =>
         DB_Main.createServerHistoryEvent(DateTime.UtcNow, e, _buildVersionNumber, MyNetworkManager.getCurrentPort()));
   }

   public bool isServerHistoryActive () {
      // If we're running a stress test, we don't need to worry about the server history
      if (Util.isStressTesting()) {
         return false;
      }

      return enableDebug || Util.isCloudBuild();
   }

   #region Private Variables

   // The build version number
   private int _buildVersionNumber = -1;

   #endregion
}
