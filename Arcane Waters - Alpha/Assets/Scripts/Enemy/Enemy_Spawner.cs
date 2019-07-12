using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Enemy_Spawner : MonoBehaviour {
   #region Public Variables

   // The Type of Enemy to spawn
   public Enemy.Type enemyType;

   #endregion

   public void setExtraInfo (string extraInfo) {
      // If a specific type of Enemy was specified, then set the type to that
      if (!Util.isEmpty(extraInfo)) {
         this.enemyType = (Enemy.Type) System.Enum.Parse(typeof(Enemy.Type), extraInfo, true);
      }
   }

   #region Private Variables

   #endregion
}
