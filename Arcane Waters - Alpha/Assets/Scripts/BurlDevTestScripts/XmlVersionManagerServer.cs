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
   public int serverVersion;

   // The server directory
   public static string WEB_DIRECTORY = "http://localhost/";

   // The sub directories
   public static string LAND_MONSTER_POST = "setXml_LandMonsters.php?version=";
   public static string SEA_MONSTER_POST = "setXml_SeaMonsters.php?version=";
   public static string ABILITIES_POST = "setXml_Abilities.php?version=";
   public static string ZIP_POST = "setXml_Zip.php?version=";
   public static string XML_VERSION_GET = "getXmlVersion.php";

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
      UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + XML_VERSION_GET);
      yield return www.SendWebRequest();
      if (www.isNetworkError || www.isHttpError) {
         D.warning(www.error);
      } else {
         string rawData = www.downloadHandler.text;

         // Web request will return the xml version of the server
         int databaseVersion = int.Parse(rawData);
         string serverMessage = "";

         if (serverVersion < databaseVersion) {
            serverMessage = "You cannot downgrade versions! The database Ver is: " + databaseVersion + " while your declared version is : " + serverVersion;
         } else if (serverVersion > databaseVersion) {
            serverMessage = "Version is valid: Writting data to server";
            targetProgress = 0;
            currentProgress = 0;
            StartCoroutine(CO_PostXmlData(EditorToolType.LandMonster));
            StartCoroutine(CO_PostXmlData(EditorToolType.SeaMonster));
            StartCoroutine(CO_PostXmlData(EditorToolType.BattlerAbility));
         } else {
            serverMessage = "Version in the database and the declared version is the same: " + databaseVersion;
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
            UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + LAND_MONSTER_POST + serverVersion);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError) {
               D.warning(www.error);
            } else {
               resultMessage = www.downloadHandler.text;
            }
            break;
         case EditorToolType.SeaMonster:
            // Set xml data for Sea monsters
            www = UnityWebRequest.Get(WEB_DIRECTORY + SEA_MONSTER_POST + serverVersion);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError) {
               D.warning(www.error);
            } else {
               resultMessage = www.downloadHandler.text;
            }
            break;
         case EditorToolType.BattlerAbility:
            // Set xml data for abilities
            www = UnityWebRequest.Get(WEB_DIRECTORY + ABILITIES_POST + serverVersion);
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
      UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + ZIP_POST + serverVersion);
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