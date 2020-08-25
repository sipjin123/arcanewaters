using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Networking;
using static EditorSQLManager;
using System.IO;
using System;
using System.Text;

public class XmlVersionManagerServer : MonoBehaviour {
   #region Public Variables

   // Self
   public static XmlVersionManagerServer self;

   // The current version of the server
   public int newServerVersion;

   // Text directory of the xml text files
   public static string XML_TEXT_DIRECTORY = "C:/XmlTextFiles";
   public static string SERVER_ZIP_DIRECTORY = "C:/XmlZipFiles";
   public static string TEXT_FILE_NAME = "textFileName=";
   public static string SERVER_ZIP_FILE = "ServerXmlZip.zip";

   // TABLES (Can be changed)
   public static string ABILITY_TABLE = "ability_xml_v2";
   public static string CROPS_TABLE = "crops_xml_v1";

   public static string ARMOR_TABLE = "equipment_armor_xml_v3";
   public static string WEAPON_TABLE = "equipment_weapon_xml_v3";
   public static string HAT_TABLE = "equipment_hat_xml_v1";
   public static string LAND_MONSTER_TABLE = "land_monster_xml_v3";
   public static string NPC_TABLE = "npc_xml";

   public static string SEA_MONSTER_TABLE = "sea_monster_xml_v2";

   public static string SHIP_TABLE = "ship_xml_v2";
   public static string SHOP_TABLE = "shop_xml_v2";
   public static string SHIP_ABILITY_TABLE = "ship_ability_xml_v2";
   public static string BACKGROUND_DATA_TABLE = "background_xml_v2";
   public static string TREASURE_DROPS_TABLE = "treasure_drops_xml_v2";

   public static string PERKS_DATA_TABLE = "perks_config_xml";
   public static string QUEST_DATA_TABLE = "quest_data_xml_v1";
   public static string PALETTE_DATA_TABLE = "palette";
   public static string ITEM_DEFINITIONS_TABLE = "item_definitions";

   // TEXT FILE NAMES (Do not Modify)
   public static string CROPS_FILE = "crops";
   public static string ABILITIES_FILE = "abilities";

   public static string ARMOR_FILE = "equipment_armor";
   public static string WEAPON_FILE = "equipment_weapon";
   public static string HAT_FILE = "equipment_hat";
   public static string LAND_MONSTER_FILE = "land_monsters";
   public static string NPC_FILE = "npc";

   public static string SEA_MONSTER_FILE = "sea_monsters";

   public static string SHIP_FILE = "ships";
   public static string SHOP_FILE = "shops";
   public static string SHIP_ABILITY_FILE = "ships_abilities";
   public static string BACKGROUND_DATA_FILE = "battle_bg_data";

   public static string PERKS_FILE = "perks";
   public static string PALETTE_FILE = "palettes";
   public static string TREASURE_DROPS_FILE = "treasure_drops";
   public static string QUEST_DATA_FILE = "quest_data";
   public static string ITEM_DEFINITIONS_FILE = "item_definitions";

   // Progress indicators
   public int targetProgress;
   public int currentProgress;

   // Forces the editor to zip the updated xml content
   public bool forceZipNewData;

   // Logs the progress of the file setup
   public bool includeProgressInEditorLog;

   // Stops the server from initializing
   public bool forceDisable;

   #endregion

   private void Awake () {
      #if IS_SERVER_BUILD && CLOUD_BUILD
      forceDisable = false;
      #endif
      
      // Make sure this directory exists
      if (!Directory.Exists(XML_TEXT_DIRECTORY)) {
         Directory.CreateDirectory(XML_TEXT_DIRECTORY);
      }
      if (!Directory.Exists(SERVER_ZIP_DIRECTORY)) {
         Directory.CreateDirectory(SERVER_ZIP_DIRECTORY);
      }
      confirmTextFiles();
      self = this;
   }

   public void confirmTextFiles () {
      confirmTextFile(CROPS_FILE);
      confirmTextFile(ABILITIES_FILE);

      confirmTextFile(ARMOR_FILE);
      confirmTextFile(WEAPON_FILE);
      confirmTextFile(HAT_FILE);
      confirmTextFile(LAND_MONSTER_FILE);
      confirmTextFile(NPC_FILE);

      confirmTextFile(SEA_MONSTER_FILE);

      confirmTextFile(SHIP_FILE);
      confirmTextFile(SHOP_FILE);
      confirmTextFile(SHIP_ABILITY_FILE);
      confirmTextFile(BACKGROUND_DATA_FILE);

      confirmTextFile(PERKS_FILE);
      confirmTextFile(PALETTE_FILE);
      confirmTextFile(TREASURE_DROPS_FILE);
      confirmTextFile(QUEST_DATA_FILE);
      confirmTextFile(ITEM_DEFINITIONS_FILE);
   }

   private void confirmTextFile (string fileName) {
      string fileDirectory = XML_TEXT_DIRECTORY + "/" + fileName + ".txt";
      if (!File.Exists(fileDirectory)) {
         File.Create(fileDirectory).Close();
      }
   }

   public void initializeServerData () {
      if (!forceDisable) {
         getXmlData();
      }
   }

   private void getXmlData () {
      debugLog("Preparing to Process Files...", Color.cyan);
      string compiledData = "";
      int databaseVersion = 0;

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         compiledData += DB_Main.getLastUpdate(EditorToolType.BattlerAbility);
         compiledData += DB_Main.getLastUpdate(EditorToolType.LandMonster);
         compiledData += DB_Main.getLastUpdate(EditorToolType.SeaMonster);
         compiledData += DB_Main.getLastUpdate(EditorToolType.NPC);
         
         compiledData += DB_Main.getLastUpdate(EditorToolType.Ship);
         compiledData += DB_Main.getLastUpdate(EditorToolType.ShipAbility);

         compiledData += DB_Main.getLastUpdate(EditorToolType.Shop);
         compiledData += DB_Main.getLastUpdate(EditorToolType.Achievement);
         compiledData += DB_Main.getLastUpdate(EditorToolType.Crops);
         compiledData += DB_Main.getLastUpdate(EditorToolType.Background);

         compiledData += DB_Main.getLastUpdate(EditorToolType.Crafting);
         compiledData += DB_Main.getLastUpdate(EditorToolType.Equipment_Armor);
         compiledData += DB_Main.getLastUpdate(EditorToolType.Equipment_Weapon);
         compiledData += DB_Main.getLastUpdate(EditorToolType.Equipment_Hat);

         compiledData += DB_Main.getLastUpdate(EditorToolType.Perks);
         compiledData += DB_Main.getLastUpdate(EditorToolType.Palette);
         compiledData += DB_Main.getLastUpdate(EditorToolType.Treasure_Drops);
         compiledData += DB_Main.getLastUpdate(EditorToolType.Quest);
         compiledData += DB_Main.getLastUpdate(EditorToolType.ItemDefinitions);

         databaseVersion = DB_Main.getLatestXmlVersion();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            compiledData = compiledData.Replace("\n", "");

            // Split each entry data
            string splitter = "[next]";
            string[] xmlGroup = compiledData.Split(new string[] { splitter }, StringSplitOptions.None);

            int index = 1;
            bool shouldZipNewFiles = false;
            foreach (string subgroup in xmlGroup) {
               string subSplitter = "[space]";
               string[] newGroup = subgroup.Split(new string[] { subSplitter }, StringSplitOptions.None);
               if (newGroup.Length == 2) {
                  string xmlTableName = newGroup[0];
                  if (PlayerPrefs.GetString(xmlTableName, "") == "") {
                     shouldZipNewFiles = true;

                     try {
                        DateTime dateTime = Convert.ToDateTime(newGroup[1]);
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

                     debugLog(xmlTableName + "  There are saved files: Old:" + savedDate + " - New: " + serverDate, Color.cyan);

                     if (serverDate > savedDate) {
                        shouldZipNewFiles = true;
                        PlayerPrefs.SetString(xmlTableName, serverDate.ToString());
                        debugLog("Server updated recently updates: " + (index + "/" + (xmlGroup.Length - 1)), Color.blue);
                     } else {
                        debugLog("NO updates: " + (index + "/" + (xmlGroup.Length - 1)), Color.blue);
                     }
                  }
               }
               index++;
            }

            // Set the latest version
            newServerVersion = databaseVersion + 1;

            if (forceZipNewData) {
               shouldZipNewFiles = true;
            }

            string serverMessage = "";
            if (shouldZipNewFiles) {
               serverMessage = "Version is valid: Writing data to server, new version from: " + databaseVersion + " to " + newServerVersion;
               targetProgress = 0;
               currentProgress = 0;

               Debug.Log("Zipping Process has begun");
               beginZipProcess();
            } else {
               serverMessage = "No new version, set as cached version: " + databaseVersion;
            }

            debugLog(serverMessage, Color.cyan);
         });
      });
   }

   private void beginZipProcess () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get contents from DB_Main
         string landMonsterData = DB_Main.getXmlContent(LAND_MONSTER_TABLE);
         string seaMonsterData = DB_Main.getXmlContent(SEA_MONSTER_TABLE);

         string npcData = DB_Main.getXmlContent(NPC_TABLE);
         string cropsData = DB_Main.getXmlContent(CROPS_TABLE);
         string abilityData = DB_Main.getXmlContent(ABILITY_TABLE);

         string armorData = DB_Main.getXmlContent(ARMOR_TABLE);
         string weaponData = DB_Main.getXmlContent(WEAPON_TABLE);
         string hatData = DB_Main.getXmlContent(HAT_TABLE);

         string shopData = DB_Main.getXmlContent(SHOP_TABLE);
         string shipData = DB_Main.getXmlContent(SHIP_TABLE);
         string shipAbilityData = DB_Main.getXmlContent(SHIP_ABILITY_TABLE);
         string battleBGData = DB_Main.getXmlContent(BACKGROUND_DATA_TABLE);

         string perkData = DB_Main.getXmlContent(PERKS_DATA_TABLE);
         string paletteData = DB_Main.getXmlContent(PALETTE_DATA_TABLE, EditorToolType.Palette);
         string treasureDropsData = DB_Main.getXmlContent(TREASURE_DROPS_TABLE, EditorToolType.Treasure_Drops);
         string questData = DB_Main.getXmlContent(QUEST_DATA_TABLE, EditorToolType.Quest);
         string itemDefinitionsData = DB_Main.getXmlContent(ITEM_DEFINITIONS_TABLE, EditorToolType.ItemDefinitions);

         // Write data to text files
         writeAndCache(XML_TEXT_DIRECTORY + "/" + LAND_MONSTER_FILE + ".txt", landMonsterData);
         writeAndCache(XML_TEXT_DIRECTORY + "/" + SEA_MONSTER_FILE + ".txt", seaMonsterData);

         writeAndCache(XML_TEXT_DIRECTORY + "/" + NPC_FILE + ".txt", npcData);
         writeAndCache(XML_TEXT_DIRECTORY + "/" + CROPS_FILE + ".txt", cropsData);
         writeAndCache(XML_TEXT_DIRECTORY + "/" + ABILITIES_FILE + ".txt", abilityData);

         writeAndCache(XML_TEXT_DIRECTORY + "/" + ARMOR_FILE + ".txt", armorData);
         writeAndCache(XML_TEXT_DIRECTORY + "/" + WEAPON_FILE + ".txt", weaponData);
         writeAndCache(XML_TEXT_DIRECTORY + "/" + HAT_FILE + ".txt", hatData);

         writeAndCache(XML_TEXT_DIRECTORY + "/" + SHOP_FILE + ".txt", shopData);
         writeAndCache(XML_TEXT_DIRECTORY + "/" + SHIP_FILE + ".txt", shipData);
         writeAndCache(XML_TEXT_DIRECTORY + "/" + SHIP_ABILITY_FILE + ".txt", shipAbilityData);
         writeAndCache(XML_TEXT_DIRECTORY + "/" + BACKGROUND_DATA_FILE + ".txt", battleBGData);

         writeAndCache(XML_TEXT_DIRECTORY + "/" + PERKS_FILE + ".txt", perkData);
         writeAndCache(XML_TEXT_DIRECTORY + "/" + PALETTE_FILE + ".txt", paletteData);
         writeAndCache(XML_TEXT_DIRECTORY + "/" + TREASURE_DROPS_FILE + ".txt", treasureDropsData);
         writeAndCache(XML_TEXT_DIRECTORY + "/" + QUEST_DATA_FILE + ".txt", questData);
         writeAndCache(XML_TEXT_DIRECTORY + "/" + ITEM_DEFINITIONS_FILE + ".txt", itemDefinitionsData);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            string zipDirectory = SERVER_ZIP_DIRECTORY + "/" + SERVER_ZIP_FILE;
            GZipUtility.compressDirectory(XML_TEXT_DIRECTORY + "/", zipDirectory, (fileName) => { debugLog("Compressing ..." + fileName, Color.gray); });

            finalizeZipData();
         });
      });
   }

   private void writeAndCache (string fileName, string fileContent) {
      File.WriteAllText(fileName, fileContent);
      _filenamesAndData.Add(fileName, fileContent);
   }

   private void finalizeZipData () {
      debugLog("Init Zip data sending", Color.green);
      string zipDirectory = SERVER_ZIP_DIRECTORY + "/" + SERVER_ZIP_FILE;
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         byte[] zipData = File.ReadAllBytes(zipDirectory);
         DB_Main.writeZipData(zipData, (int) XmlSlotIndex.Default);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Debug.Log("Zip Upload Complete: Windows:" + zipData.Length);
         });
      });
   }

   private void debugLog (string message, Color color) {
      if (includeProgressInEditorLog) {
         try {
            D.debug(message);
         } catch {
            D.editorLog(message, Color.cyan);
         }
      }
   }

   #region Private Variables

   // The file data gathered for zipping
   private Dictionary<string, string> _filenamesAndData = new Dictionary<string, string>();

   #endregion
}