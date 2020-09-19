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
      try {
         PlayerPrefs.SetString(areaKey + version, mapData);
      } catch (PlayerPrefsException ex) {
         D.error($"Caught an exception when storing map data in player prefs. Map key - { areaKey }, version - { version }, data symbol count - { mapData.Length }, exception - { ex }");
         ChatPanel.self.addChatInfo(new ChatInfo(0, "WARNING: Failed to cache map data.", System.DateTime.Now, ChatInfo.Type.System));
      }
   }

   #region Private Variables
      
   #endregion
}
