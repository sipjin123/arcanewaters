using System;
using System.IO;
using UnityEngine;

public static class MapCache
{
   #region Public Variables

   // Path to maps directory
   public static string MAP_FOLDER_PATH = Application.persistentDataPath + "/MapData";

   #endregion

   private static string getMapPath (string areaKey, int version) {
      return MAP_FOLDER_PATH + "/" + areaKey + " (" + version + ").json";
   }

   public static bool hasMap (string areaKey, int version) {
      return File.Exists(getMapPath(areaKey, version));
   }

   public static string getMapData (string areaKey, int version) {
      Directory.CreateDirectory(MAP_FOLDER_PATH);

      if (File.Exists(getMapPath(areaKey, version))) {
         return File.ReadAllText(getMapPath(areaKey, version));
      } else {
         return "";
      }
   }

   public static void storeMapData (string areaKey, int version, string mapData) {
      Directory.CreateDirectory(MAP_FOLDER_PATH);

      try {
         File.WriteAllText(getMapPath(areaKey, version), mapData);
      } catch (Exception ex) {
         D.error($"Caught an exception when storing map data in player prefs. Map key - { areaKey }, version - { version }, data symbol count - { mapData.Length }, exception - { ex }");
         ChatPanel.self.addChatInfo(new ChatInfo(0, "WARNING: Failed to cache map data.", System.DateTime.Now, ChatInfo.Type.System));
      }
   }

   #region Private Variables

   #endregion
}
