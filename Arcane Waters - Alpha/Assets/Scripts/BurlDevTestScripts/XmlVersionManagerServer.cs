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
   public static string WEB_DIRECTORY = "http://arcanewaters.com/";//;"http://localhost/";

   // The sub directories
   public static string ABILITIES_POST = "setXml_Abilities_v2.php?version=";
   public static string ZIP_POST = "setXml_Zip.php?version=";
   public static string XML_VERSION_GET = "getXmlVersion.php";
   public static string LAST_UPDATED_GET = "getLastUpdates.php";

   public static string XML_PAIR_GET = "setXml_TypePair_v2.php?tableName=";
   public static string TEXT_FILE_NAME = "textFileName=";

   // TABLES (Can be changed)
   public static string CROPS_TABLE = "crops_xml_v1";

   public static string ARMOR_TABLE = "equipment_armor_xml_v3";
   public static string WEAPON_TABLE = "equipment_weapon_xml_v3";
   public static string HELM_TABLE = "equipment_helm_xml_v2";
   public static string LAND_MONSTER_TABLE = "land_monster_xml_v3";
   public static string NPC_TABLE = "npc_xml";

   public static string CLASS_TABLE = "player_class_xml";
   public static string FACTION_TABLE = "player_faction_xml";
   public static string JOB_TABLE = "player_job_xml";
   public static string SPECIALTY_TABLE = "player_specialty_xml";
   public static string SEA_MONSTER_TABLE = "sea_monster_xml_v2";

   public static string SHIP_TABLE = "ship_xml_v2";
   public static string SHOP_TABLE = "shop_xml_v2";
   public static string TUTORIAL_TABLE = "tutorial_xml";
   public static string SHIP_ABILITY_TABLE = "ship_ability_xml_v2";

   // TEXT FILE NAMES (Do not Modify)
   public static string CROPS_FILE = "crops";
   public static string ABILITIES_FILE = "abilities";

   public static string ARMOR_FILE = "equipment_armor";
   public static string WEAPON_FILE = "equipment_weapon";
   public static string HELM_FILE = "equipment_helm";
   public static string LAND_MONSTER_FILE = "land_monsters";
   public static string NPC_FILE = "npc";

   public static string CLASS_FILE = "player_class";
   public static string FACTION_FILE = "player_faction";
   public static string JOB_FILE = "player_job";
   public static string SPECIALTY_FILE = "player_specialty";
   public static string SEA_MONSTER_FILE = "sea_monsters";

   public static string SHIP_FILE = "ships";
   public static string SHOP_FILE = "shops";
   public static string TUTORIAL_FILE = "tutorials";
   public static string SHIP_ABILITY_FILE = "ships_abilities";

   // Progress indicators
   public int targetProgress;
   public int currentProgress;

   // Logs the progress of the file setup
   public bool logProgress;

   // Stops the server from initializing
   public bool forceDisable;

   #endregion

   private void Awake () {
      self = this;
   }

   public void initializeServerData () {
      #if !UNITY_EDITOR
         forceDisable = false;
      #endif
      if (!forceDisable) {
         StartCoroutine(CO_GetXmlData());
      }
   }

   private IEnumerator CO_GetXmlData () {
      D.editorLog("Preparing to Process Files...", Color.cyan);
      yield return new WaitForSeconds(5);
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

         int index = 1;
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
                     D.editorLog(xmlTableName + "-" + dateTime, Color.blue);
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

                  if (logProgress) {
                     D.editorLog(xmlTableName + "  There are saved files: Old:" + savedDate + " - New: " + serverDate, Color.cyan);
                  }

                  if (serverDate > savedDate) {
                     shouldZipNewFiles = true;
                     PlayerPrefs.SetString(xmlTableName, serverDate.ToString());
                     if (logProgress) {
                        D.editorLog("Server updated recently updates: " + (index + "/" + (xmlGroup.Length - 1)), Color.blue);
                     }
                  } else {
                     if (logProgress) {
                        D.editorLog("NO updates: " + (index + "/" + (xmlGroup.Length - 1)), Color.blue);
                     }
                  }
               }
            }
            index++;
         }

         // Get the latest version
         int databaseVersion = 0;
         www = UnityWebRequest.Get(WEB_DIRECTORY + XML_VERSION_GET);
         yield return www.SendWebRequest();
         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            rawData = www.downloadHandler.text;
            try {
               databaseVersion = int.Parse(rawData);
               newServerVersion = databaseVersion + 1;
            } catch {
               D.editorLog("Failed to parse version: " + rawData, Color.red);
            }
         }

         string serverMessage = "";
         if (shouldZipNewFiles) {
            serverMessage = "Version is valid: Writing data to server, new version from: " + databaseVersion + " to " + newServerVersion;
            targetProgress = 0;
            currentProgress = 0;
            StartCoroutine(CO_PostXmlData(EditorToolType.Crops));
            StartCoroutine(CO_PostXmlData(EditorToolType.BattlerAbility));

            StartCoroutine(CO_PostXmlData(EditorToolType.Equipment_Armor));
            StartCoroutine(CO_PostXmlData(EditorToolType.Equipment_Weapon));
            StartCoroutine(CO_PostXmlData(EditorToolType.Equipment_Helm));
            StartCoroutine(CO_PostXmlData(EditorToolType.NPC));
            StartCoroutine(CO_PostXmlData(EditorToolType.LandMonster));

            StartCoroutine(CO_PostXmlData(EditorToolType.PlayerClass));
            StartCoroutine(CO_PostXmlData(EditorToolType.PlayerFaction));
            StartCoroutine(CO_PostXmlData(EditorToolType.PlayerSpecialty));
            StartCoroutine(CO_PostXmlData(EditorToolType.PlayerJob));
            StartCoroutine(CO_PostXmlData(EditorToolType.SeaMonster));

            // TODO: Convert these tables into using xml_id as primary key
            //StartCoroutine(CO_PostXmlData(EditorToolType.Tutorial));
            StartCoroutine(CO_PostXmlData(EditorToolType.Shop));
            StartCoroutine(CO_PostXmlData(EditorToolType.Ship));
            StartCoroutine(CO_PostXmlData(EditorToolType.ShipAbility));
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
      UnityWebRequest www = null;
      switch (xmlType) {
         case EditorToolType.BattlerAbility:
            // Set xml data for abilities
            www = UnityWebRequest.Get(WEB_DIRECTORY + ABILITIES_POST + newServerVersion);
            break;
         case EditorToolType.LandMonster:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + LAND_MONSTER_TABLE + "&" + TEXT_FILE_NAME + LAND_MONSTER_FILE);
            break;
         case EditorToolType.SeaMonster:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + SEA_MONSTER_TABLE + "&" + TEXT_FILE_NAME + SEA_MONSTER_FILE);
            break;

         case EditorToolType.Crops:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + CROPS_TABLE + "&" + TEXT_FILE_NAME + CROPS_FILE);
            break;
         case EditorToolType.Equipment_Weapon:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + WEAPON_TABLE + "&" + TEXT_FILE_NAME + WEAPON_FILE);
            break;
         case EditorToolType.Equipment_Armor:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + ARMOR_TABLE + "&" + TEXT_FILE_NAME + ARMOR_FILE);
            break;
         case EditorToolType.Equipment_Helm:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + HELM_TABLE + "&" + TEXT_FILE_NAME + HELM_FILE);
            break;

         case EditorToolType.NPC:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + NPC_TABLE + "&" + TEXT_FILE_NAME + NPC_FILE);
            break;
         case EditorToolType.PlayerClass:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + CLASS_TABLE + "&" + TEXT_FILE_NAME + CLASS_FILE);
            break;
         case EditorToolType.PlayerFaction:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + FACTION_TABLE + "&" + TEXT_FILE_NAME + FACTION_FILE);
            break;
         case EditorToolType.PlayerSpecialty:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + SPECIALTY_TABLE + "&" + TEXT_FILE_NAME + SPECIALTY_FILE);
            break;
         case EditorToolType.PlayerJob:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + JOB_TABLE + "&" + TEXT_FILE_NAME + JOB_FILE);
            break;

         case EditorToolType.Ship:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + SHIP_TABLE + "&" + TEXT_FILE_NAME + SHIP_FILE);
            break;
         case EditorToolType.ShipAbility:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + SHIP_ABILITY_TABLE + "&" + TEXT_FILE_NAME + SHIP_ABILITY_FILE);
            break;
         case EditorToolType.Shop:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + SHOP_TABLE + "&" + TEXT_FILE_NAME + SHOP_FILE);
            break;
         case EditorToolType.Tutorial:
            www = UnityWebRequest.Get(WEB_DIRECTORY + XML_PAIR_GET + TUTORIAL_TABLE + "&" + TEXT_FILE_NAME + TUTORIAL_FILE);
            break;
      }

      yield return www.SendWebRequest();
      if (www.isNetworkError || www.isHttpError) {
         D.warning(www.error);
      } else {
         resultMessage = www.downloadHandler.text;
      }
      currentProgress++;

      if (logProgress) {
         try {
            D.log(resultMessage + " (" + currentProgress + "/" + targetProgress + ")");
         } catch {
            D.editorLog(resultMessage + " (" + currentProgress + "/" + targetProgress + ")", Color.cyan);
         }
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
         if (logProgress) {
            try {
               D.log(rawData);
            } catch {
               D.editorLog(rawData, Color.cyan);
            }
         }
      }
   }

   #region Private Variables

   #endregion
}