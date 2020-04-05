using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Ship_Spawner : MonoBehaviour {
   #region Public Variables

   // The Type of Ship to spawn
   public Ship.Type shipType;

   #endregion

   public void setExtraInfo (string extraInfo) {
      // If a specific type of Ship was specified, then set the type to that
      if (!Util.isEmpty(extraInfo)) {
         shipType = (Ship.Type) System.Enum.Parse(typeof(Ship.Type), extraInfo, true);
      }
   }

   #region Private Variables

   #endregion
}
