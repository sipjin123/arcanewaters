using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class GameVersionManager : MonoBehaviour
{
   #region Public Variables

   // The minimum client version, below which the client is asked to download the new build
   public int minClientGameVersion = int.MaxValue;

   // Self
   public static GameVersionManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public void scheduleMinimumGameVersionUpdate () {
      InvokeRepeating("updateMinimumClientGameVersion", 0, 60);
   }

   private void updateMinimumClientGameVersion () {
      if (NetworkServer.active) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            minClientGameVersion = DB_Main.getMinimumClientGameVersion();
         });
      }
   }

   #region Private Variables

   #endregion
}
