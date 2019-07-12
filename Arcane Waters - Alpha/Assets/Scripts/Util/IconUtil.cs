using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class IconUtil : MonoBehaviour {
   #region Public Variables
      
   #endregion

   public static string getCoins () {
      return string.Format("<voffset=6><size=200%><sprite={0}></size></voffset>", "aw_icons_15");
   }

   public static string getCrop (Crop.Type cropType) {
      return string.Format("<voffset=6><size=200%><sprite name=\"{0}\"></size></voffset>", "aw_icons_" + (((int) cropType) + 1));
   }

   #region Private Variables
      
   #endregion
}
