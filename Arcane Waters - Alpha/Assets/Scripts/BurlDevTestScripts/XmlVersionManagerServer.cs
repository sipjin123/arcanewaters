using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Networking;
using static EditorSQLManager;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System;

public class XmlVersionManagerServer : MonoBehaviour {
   #region Public Variables

   // Self
   public static XmlVersionManagerServer self;

   // The current version of the server
   public int newServerVersion;

   // The server directory
   public static string WEB_DIRECTORY = "http://localhost/";

   // The sub directories
   public static string LAND_MONSTER_POST = "setXml_LandMonsters.php?version=";
   public static string SEA_MONSTER_POST = "setXml_SeaMonsters.php?version=";
   public static string ABILITIES_POST = "setXml_Abilities.php?version=";
   public static string ZIP_POST = "setXml_Zip.php?version=";
   public static string XML_VERSION_GET = "getXmlVersion.php";
   public static string LAST_UPDATED_GET = "getLastUpdates.php";

   // Progress indicators
   public int targetProgress;
   public int currentProgress;

   #endregion

   private void Awake () {
      self = this;
   }

   public void initializeServerData () {
      StartCoroutine(CO_GetXmlData());
   }

   private IEnumerator CO_GetXmlData () {
      UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + LAST_UPDATED_GET);
      yield return www.SendWebRequest();
      if (www.isNetworkError || www.isHttpError) {
         D.warning(www.error);
      } else {
         string rawData = www.downloadHandler.text;
         rawData = rawData.Replace("\n", "");

         // Split each entry data
         string splitter = "[next]";
         string[] xmlGroup = rawData.Split(new string[] { splitter }, StringSplitOptions.None);

         bool shouldZipNewFiles = false; 
         foreach (string subgroup in xmlGroup) {
            string subSplitter = "[space]";
            string[] newGroup = subgroup.Split(new string[] { subSplitter }, StringSplitOptions.None);
            if (newGroup.Length == 2) {
               string xmlTableName = newGroup[0];
               if (PlayerPrefs.GetString(xmlTableName, "") == "") {
                  D.editorLog("There are no saved files, create new ones", Color.cyan);
                  shouldZipNewFiles = true;

                  try {
                     DateTime dateTime = Convert.ToDateTime(newGroup[1]);
                     D.editorLog(xmlTableName + "-" + dateTime, Color.cyan);
                  } catch {
                     D.editorLog("Fail: " + newGroup[1], Color.red);
                  }

                  PlayerPrefs.SetString(xmlTableName, newGroup[1]);
               } else {
                  DateTime savedDate = Convert.ToDateTime(PlayerPrefs.GetString(xmlTableName));
                  DateTime serverDate = DateTime.UtcNow;

                  try {
                     serverDate = Convert.ToDateTime(newGroup[1]);
                  } catch {
                     D.editorLog("Failed to convert: " + newGroup[1], Color.red);
                  }

                  D.editorLog("There are saved files: Old:" + savedDate + " - New: " + serverDate, Color.cyan);
                  if (serverDate > savedDate) {
                     shouldZipNewFiles = true;
                     PlayerPrefs.SetString(xmlTableName, serverDate.ToString());
                     D.editorLog("Server updated recently updates", Color.blue);
                  } else {
                     D.editorLog("NO updates", Color.blue);
                  }

               }
            }
         }

         // Get the latest version
         int databaseVersion = 0;
         www = UnityWebRequest.Get(WEB_DIRECTORY + XML_VERSION_GET);
         yield return www.SendWebRequest();
         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            rawData = www.downloadHandler.text;
            databaseVersion = int.Parse(rawData);
            newServerVersion = databaseVersion + 1;
         }

         string serverMessage = "";
         if (shouldZipNewFiles) {
            serverMessage = "Version is valid: Writting data to server, new version from: " + databaseVersion + " to " + newServerVersion;
            targetProgress = 0;
            currentProgress = 0;
            StartCoroutine(CO_PostXmlData(EditorToolType.LandMonster));
            StartCoroutine(CO_PostXmlData(EditorToolType.SeaMonster));
            StartCoroutine(CO_PostXmlData(EditorToolType.BattlerAbility));
         } else {
            serverMessage = "No new version, set as cached version: " + databaseVersion;
         }

         try {
            D.log(serverMessage);
         } catch {
            D.editorLog(serverMessage, Color.cyan);
         }
      }
   }

   private IEnumerator CO_PostXmlData (EditorToolType xmlType) {
      targetProgress++;
      string resultMessage = "";
      switch (xmlType) {
         case EditorToolType.LandMonster:
            // Set xml data for Land monsters
            UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + LAND_MONSTER_POST + newServerVersion);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError) {
               D.warning(www.error);
            } else {
               resultMessage = www.downloadHandler.text;
            }
            break;
         case EditorToolType.SeaMonster:
            // Set xml data for Sea monsters
            www = UnityWebRequest.Get(WEB_DIRECTORY + SEA_MONSTER_POST + newServerVersion);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError) {
               D.warning(www.error);
            } else {
               resultMessage = www.downloadHandler.text;
            }
            break;
         case EditorToolType.BattlerAbility:
            // Set xml data for abilities
            www = UnityWebRequest.Get(WEB_DIRECTORY + ABILITIES_POST + newServerVersion);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError) {
               D.warning(www.error);
            } else {
               resultMessage = www.downloadHandler.text;
            }
            break;
      }
      currentProgress++;

      try {
         D.log(resultMessage + " (" + currentProgress + "/" + targetProgress + ")");
      } catch {
         D.editorLog(resultMessage + " (" + currentProgress + "/" + targetProgress + ")", Color.cyan);
      }
      checkUploadProgress();
   }

   private void checkUploadProgress () {
      if (currentProgress >= targetProgress) {
         StartCoroutine(CO_CompileXmlData());
      }
   }

   private IEnumerator CO_CompileXmlData () {
      // Set sql version and zip files
      UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + ZIP_POST + newServerVersion);
      yield return www.SendWebRequest();
      if (www.isNetworkError || www.isHttpError) {
         D.warning(www.error);
      } else {
         // Grab the map data from the request
         string rawData = www.downloadHandler.text;
         try {
            D.log(rawData);
         } catch {
            D.editorLog(rawData, Color.cyan);
         }
      }
   }

   #region Private Variables

   #endregion
}