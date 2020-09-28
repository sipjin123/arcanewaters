using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class MapCache
{
   #region Public Variables

   // Path to maps directory
   public static string MAP_FOLDER_PATH = Application.persistentDataPath + "/MapData";

   // After how many maps will we start deleting old ones
   public const int MAX_MAPS = 200;

   #endregion

   public static void pruneExcessMaps () {
      int previousMapCount = -1;
      int removedMapsCount = 0;

      try {
         // Get information about files in maps directory
         DirectoryInfo dirInfo = new DirectoryInfo(MAP_FOLDER_PATH);
         FileInfo[] files = dirInfo.GetFiles().OrderBy(f => f.LastWriteTime).ToArray();
         previousMapCount = files.Length;

         // Remove excess old maps
         for (int i = 0; i < previousMapCount - MAX_MAPS; i++) {
            files[i].Delete();
            removedMapsCount++;
         }
      } catch (Exception ex) {
         D.error("There was an error pruning excess maps in MapCache: " + ex);
      }

      if (removedMapsCount != 0) {
         D.log($"Map data files pruning: had { previousMapCount }, aimed for { MAX_MAPS }, removed { removedMapsCount }.");
      }
   }

   private static string getMapPath (string areaKey, int version) {
      return MAP_FOLDER_PATH + "/" + areaKey + " (" + version + ").json";
   }

   public static bool hasMap (string areaKey, int version) {
      return File.Exists(getMapPath(areaKey, version));
   }

   public static string getMapData (string areaKey, int version) {
      Directory.CreateDirectory(MAP_FOLDER_PATH);

      string path = getMapPath(areaKey, version);
      if (File.Exists(path)) {
         // Write a white space to file to update write date so we know which files were used recently
         File.AppendAllText(path, " ");

         return File.ReadAllText(path);
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
