using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public static class MapCache {
   #region Public Variables
      
   #endregion

   public static bool hasMap (string areaKey, int version) {
      return PlayerPrefs.HasKey(areaKey + version);
   }

   public static string getMapData (string areaKey, int version) {
      return PlayerPrefs.GetString(areaKey + version, "");
   }

   public static void storeMapData (string areaKey, int version, string mapData) {
      PlayerPrefs.SetString(areaKey + version, mapData);
   }

   #region Private Variables
      
   #endregion
}
