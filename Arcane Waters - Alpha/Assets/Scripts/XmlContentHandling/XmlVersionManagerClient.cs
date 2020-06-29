using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Networking;
using static EditorSQLManager;
using System.IO;
using System;
using static ShopDataToolManager;
using UnityEngine.Events;
using NubisDataHandling;
using BackgroundTool;

public class XmlVersionManagerClient : MonoBehaviour {
   #region Public Variables

   // The player pref key for clients
   public static string XML_VERSION = "xml_version";

   // Path of the streaming files
   public static string ZIP_PATH = Application.streamingAssetsPath + "/XmlZip/XmlContent.zip";
   public static string TEXT_PATH = Application.streamingAssetsPath + "/XmlTexts/";

   // Error directory for log purposes
   public static string ERROR_DIRECTORY = "C:/XmlErrorLog/";
   public static string ERROR_FILENAME = "ErrorFile.txt";

   // Progress indicators
   public int targetProgress;
   public int currentProgress;

   // The web directory
   public string webDirectory = "";

   // Self
   public static XmlVersionManagerClient self;

   // Blocks the character panel while loading data
   public GameObject loadBlocker;

   // Resets the cached xml version
   public bool resetXmlPrefs;

   // The space key for the xml content
   public static string SPACE_KEY = "[space]";

   // Logs the progress of the file setup
   public bool includeProgressInEditorLog;

   // Event that notifies completion of xml loadup
   public UnityEvent finishedLoadingXmlData = new UnityEvent();

   // Determines if this is initialized
   public bool isInitialized;

   // Event that notifies if streaming asset files are complete of not
   public Util.BoolEvent finishedCheckingStreamingAsset = new Util.BoolEvent();

   // Event that notifies if streaming asset files are complete of not
   public UnityEvent initializeLoadingXmlData = new UnityEvent();

   // Popup notice if nubis failed to return zip file data
   public GameObject failedToFetchDataPanel;
   public Button exitButton;

   #endregion

   private void Awake () {
      self = this;
      webDirectory = "http://" + Global.getAddress(MyNetworkManager.ServerType.AmazonVPC) + ":7900/";
   }

   public void initializeClient () {
      if (resetXmlPrefs) {
         PlayerPrefs.SetInt(XML_VERSION, 0);
      }
      loadBlocker.SetActive(true);

      initializeLoadingXmlData.RemoveAllListeners();
      finishedCheckingStreamingAsset.RemoveAllListeners();
      finishedLoadingXmlData.RemoveAllListeners();

      NubisDataFetcher.self.xmlVersionEvent.AddListener(_ => {
         processClientData(_);
         NubisDataFetcher.self.xmlVersionEvent.RemoveAllListeners();
      });

      NubisDataFetcher.self.fetchXmlVersion();
   }

   private void processClientData (int serverVersion) {
      int clientXmlVersion = PlayerPrefs.GetInt(XML_VERSION, 0);
      string clientMessage = "";

      finishedCheckingStreamingAsset.AddListener(isCompleteData => {
         if (!isCompleteData) {
            clientMessage = "Missing Files! Initialize Redownload";
            D.debug(clientMessage);

            // Reset version cache since file integrity might be compromised
            PlayerPrefs.SetInt(XML_VERSION, 0);

            // Force redownload zip data due to possible missing files
            downloadClientData(serverVersion);
         } else {
            clientMessage = "All files are existing, continue version checking";
            D.debug(clientMessage);

            if (serverVersion > clientXmlVersion) {
               clientMessage = "Client is outdated ver: " + clientXmlVersion + ", downloading new version: " + serverVersion;
               D.debug(clientMessage);
               downloadClientData(serverVersion);
            } else {
               clientMessage = "Client is up to date: Ver = " + clientXmlVersion;
               D.debug(clientMessage);
               processClientXml();
            }
         }
         finishedCheckingStreamingAsset.RemoveAllListeners();
      });
      confirmStreamingAssets();
   }

   private void confirmStreamingAssets () {
      checkStreamingAssetFile(XmlVersionManagerServer.CROPS_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.ABILITIES_FILE);

      checkStreamingAssetFile(XmlVersionManagerServer.ARMOR_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.WEAPON_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.HAT_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.LAND_MONSTER_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.NPC_FILE);

      checkStreamingAssetFile(XmlVersionManagerServer.SEA_MONSTER_FILE);

      checkStreamingAssetFile(XmlVersionManagerServer.SHIP_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.SHOP_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.TUTORIAL_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.SHIP_ABILITY_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.BACKGROUND_DATA_FILE);

      checkStreamingAssetFile(XmlVersionManagerServer.PERKS_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.TREASURE_DROPS_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.PALETTE_FILE, true);
   }

   private void checkStreamingAssetFile (string fileName, bool isLastEntry = false) {
      string fileDirectory = TEXT_PATH + fileName + ".txt";
      if (!File.Exists(fileDirectory)) {
         D.error("Missing file! Creating now: " + fileDirectory);
         File.Create(fileDirectory).Close();
         finishedCheckingStreamingAsset.Invoke(false);
      }

      if (isLastEntry) {
         finishedCheckingStreamingAsset.Invoke(true);
      }
   }
   
   private async void downloadClientData (int targetVersion) {
      string zipDataRequest = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchXmlZipBytes), NubisRequestHandler.getSlotIndex().ToString());
      writeData(zipDataRequest, targetVersion);
   }
   
   private void writeData (string zipDataRequest, int targetVersion) {
      if (zipDataRequest.Length < 10) {
         // If the result string is less than expected, the zip blob download has failed then call out the error panel which contains an exit button
         loadBlocker.SetActive(true);
         failedToFetchDataPanel.SetActive(true);
         exitButton.onClick.AddListener(() => Application.Quit());
         return;
      } else {
         try {
            byte[] bytes = Convert.FromBase64String(zipDataRequest);
            File.WriteAllBytes(ZIP_PATH, bytes);
         } catch {
            D.editorLog("Failed to convert bytes:", Color.red);

            if (!Directory.Exists(ERROR_DIRECTORY)) {
               Directory.CreateDirectory(ERROR_DIRECTORY);
            }
            if (!File.Exists(ERROR_DIRECTORY + ERROR_FILENAME)) {
               File.Create(ERROR_DIRECTORY + ERROR_FILENAME).Close();
            }
            File.WriteAllText(ERROR_DIRECTORY + ERROR_FILENAME, zipDataRequest);
         }

         GZipUtility.decompressToDirectory(ZIP_PATH, TEXT_PATH, (fileName) => {
            debugLog("Decompressing: ..." + fileName);
         });

         D.editorLog("Finished Extracting Zip", Color.green);
         PlayerPrefs.SetInt(XML_VERSION, targetVersion);
         processClientXml();
      }
   }

   private void processClientXml () {
      targetProgress = 0;
      currentProgress = 0;

      extractXmlType(EditorToolType.Crops);
      extractXmlType(EditorToolType.BattlerAbility);

      extractXmlType(EditorToolType.Equipment_Armor);
      extractXmlType(EditorToolType.Equipment_Weapon);
      extractXmlType(EditorToolType.Equipment_Hat);

      extractXmlType(EditorToolType.LandMonster);
      extractXmlType(EditorToolType.SeaMonster);
      extractXmlType(EditorToolType.NPC);

      extractXmlType(EditorToolType.Shop);
      extractXmlType(EditorToolType.Ship);
      extractXmlType(EditorToolType.ShipAbility);

      extractXmlType(EditorToolType.Background);
      extractXmlType(EditorToolType.Perks);
      extractXmlType(EditorToolType.Palette);
      extractXmlType(EditorToolType.Treasure_Drops);

      initializeLoadingXmlData.Invoke();
   }

   private void extractXmlType (EditorToolType toolType) {
      targetProgress++;
      initializeLoadingXmlData.AddListener(() => {
         StartCoroutine(CO_ExtractXmlData(toolType));
      });
   }

   private IEnumerator CO_ExtractXmlData (EditorToolType xmlType) {
      yield return new WaitForSeconds(.25f);

      string content = "";
      string path = "";
      switch (xmlType) {
         case EditorToolType.BattlerAbility:
            path = TEXT_PATH + XmlVersionManagerServer.ABILITIES_FILE + ".txt";
            break;
         case EditorToolType.Crops:
            path = TEXT_PATH + XmlVersionManagerServer.CROPS_FILE + ".txt";
            break;

         case EditorToolType.Equipment_Armor:
            path = TEXT_PATH + XmlVersionManagerServer.ARMOR_FILE + ".txt";
            break;
         case EditorToolType.Equipment_Weapon:
            path = TEXT_PATH + XmlVersionManagerServer.WEAPON_FILE + ".txt";
            break;
         case EditorToolType.Equipment_Hat:
            path = TEXT_PATH + XmlVersionManagerServer.HAT_FILE + ".txt";
            break;
         case EditorToolType.LandMonster:
            path = TEXT_PATH + XmlVersionManagerServer.LAND_MONSTER_FILE + ".txt";
            break;
         case EditorToolType.NPC:
            path = TEXT_PATH + XmlVersionManagerServer.NPC_FILE + ".txt";
            break;

         case EditorToolType.SeaMonster:
            path = TEXT_PATH + XmlVersionManagerServer.SEA_MONSTER_FILE + ".txt";
            break;

         case EditorToolType.Ship:
            path = TEXT_PATH + XmlVersionManagerServer.SHIP_FILE + ".txt";
            break;
         case EditorToolType.ShipAbility:
            path = TEXT_PATH + XmlVersionManagerServer.SHIP_ABILITY_FILE + ".txt";
            break;
         case EditorToolType.Shop:
            path = TEXT_PATH + XmlVersionManagerServer.SHOP_FILE + ".txt";
            break;

         case EditorToolType.Background:
            path = TEXT_PATH + XmlVersionManagerServer.BACKGROUND_DATA_FILE + ".txt";
            break;
         case EditorToolType.Perks:
            path = TEXT_PATH + XmlVersionManagerServer.PERKS_FILE + ".txt";
            break;
         case EditorToolType.Palette:
            path = TEXT_PATH + XmlVersionManagerServer.PALETTE_FILE + ".txt";
            break;
         case EditorToolType.Treasure_Drops:
            path = TEXT_PATH + XmlVersionManagerServer.TREASURE_DROPS_FILE + ".txt";
            break;
      }

      // Read the text from directly from the txt file
      StreamReader reader = new StreamReader(path);
      content = reader.ReadToEnd();
      reader.Close();

      assignDataToManagers(xmlType, content);
      currentProgress++;
      checkTextExtractionProgress();
      try {
      } catch {
         D.debug("Failed to process: " + xmlType);
      }
   }

   private void assignDataToManagers (EditorToolType xmlType, string content) {
      // Split each entry data
      string splitter = "[next]";
      string[] xmlGroup = content.Split(new string[] { splitter }, StringSplitOptions.None);
      string message = "";

      switch (xmlType) {
         #region Group 1 (Crops/Abilities)
         case EditorToolType.Crops:
            List<CropsData> cropData = new List<CropsData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  CropsData actualData = Util.xmlLoad<CropsData>(xmlSubGroup[1]);
                  cropData.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.xmlName + " - " + ((Crop.Type)actualData.cropsType);
               }
            }
            CropsDataManager.self.receiveCropsFromZipData(cropData);
            break;
         case EditorToolType.BattlerAbility:
            List<AttackAbilityData> attackAbilityList = new List<AttackAbilityData>();
            List<BuffAbilityData> buffAbilityList = new List<BuffAbilityData>();

            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);
             
               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int abilityXmlContentIndex = 1;

                  int abilityId = int.Parse(xmlSubGroup[0]);
                  AbilityType abilityType = AbilityType.Standard;

                  if (xmlSubGroup[abilityXmlContentIndex].Contains("BuffAbilityData")) {
                     abilityType = AbilityType.BuffDebuff;
                  }

                  if (abilityType == AbilityType.Standard || abilityType == AbilityType.Stance) {
                     AttackAbilityData attackAbility = Util.xmlLoad<AttackAbilityData>(xmlSubGroup[abilityXmlContentIndex]);
                     attackAbilityList.Add(attackAbility);
                  } else if (abilityType == AbilityType.BuffDebuff) {
                     BuffAbilityData buffAbility = Util.xmlLoad<BuffAbilityData>(xmlSubGroup[abilityXmlContentIndex]);
                     buffAbilityList.Add(buffAbility);
                     message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[abilityXmlContentIndex];
                  }
               }
            }
            AbilityManager.self.receiveAbilitiesFromZipData(attackAbilityList.ToArray(), buffAbilityList.ToArray());
            break;
         #endregion

         #region Group 2 (Armor/Weapon/Hats/LandMonster/Npc)
         case EditorToolType.Equipment_Armor:
            List<ArmorStatData> armorList = new List<ArmorStatData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  ArmorStatData actualData = Util.xmlLoad<ArmorStatData>(xmlSubGroup[1]);
                  actualData.equipmentID = dataId;
                  armorList.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.equipmentName + " - " + actualData.equipmentID + " - " +actualData.armorType;
               }
               EquipmentXMLManager.self.receiveArmorDataFromZipData(armorList);
            }
            break;
         case EditorToolType.Equipment_Weapon:
            List<WeaponStatData> weaponList = new List<WeaponStatData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  WeaponStatData actualData = Util.xmlLoad<WeaponStatData>(xmlSubGroup[1]);
                  actualData.itemSqlId = dataId;
                  weaponList.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.equipmentName + " - " + actualData.equipmentID + " - " + actualData.weaponType;
               }
            }
            EquipmentXMLManager.self.receiveWeaponDataFromZipData(weaponList);
            break;
         case EditorToolType.Equipment_Hat:
            List<HatStatData> hatList = new List<HatStatData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);
               try {
                  // Extract the segregated data and assign to the xml manager
                  if (xmlSubGroup.Length == 2) {
                     int dataId = int.Parse(xmlSubGroup[0]);
                     HatStatData actualData = Util.xmlLoad<HatStatData>(xmlSubGroup[1]);
                     actualData.equipmentID = dataId;
                     actualData.itemSqlId = dataId;
                     hatList.Add(actualData);
                     message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.equipmentName + " - " + actualData.equipmentID + " - " + actualData.hatType;
                  }
               } catch {
                  D.editorLog("Cant process hat data: " + xmlSubGroup[0] + " : " + xmlSubGroup[1] + " : ", Color.yellow);
               }
            }
            EquipmentXMLManager.self.receiveHatFromZipData(hatList);
            break;
         case EditorToolType.NPC:
            List<NPCData> npcList = new List<NPCData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  NPCData actualData = Util.xmlLoad<NPCData>(xmlSubGroup[1]);
                  npcList.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            NPCManager.self.initializeNPCClientData(npcList.ToArray());
            break;
         case EditorToolType.LandMonster:
            List<BattlerData> battlerDataList = new List<BattlerData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int seaMonsterId = int.Parse(xmlSubGroup[0]);
                  BattlerData battlerData = Util.xmlLoad<BattlerData>(xmlSubGroup[1]);
                  battlerDataList.Add(battlerData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            MonsterManager.self.receiveListFromZipData(battlerDataList.ToArray());
            break;
         #endregion

         #region Group 3 (SeaMonster/Perks)
         case EditorToolType.SeaMonster:
            List<SeaMonsterEntityData> seaMonsterDataList = new List<SeaMonsterEntityData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int monsterId = int.Parse(xmlSubGroup[0]);
                  SeaMonsterEntityData seaMonsterData = Util.xmlLoad<SeaMonsterEntityData>(xmlSubGroup[1]);
                  seaMonsterDataList.Add(seaMonsterData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            SeaMonsterManager.self.receiveListFromZipData(seaMonsterDataList.ToArray());
            break;

         case EditorToolType.Perks:
            List<PerkData> perkDataList = new List<PerkData>();
            foreach (string subGroup in xmlGroup) {               
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               if (xmlSubGroup.Length == 2) {
                  int perkId = int.Parse(xmlSubGroup[0]);
                  PerkData data = Util.xmlLoad<PerkData>(xmlSubGroup[1]);
                  data.perkId = perkId;
                  perkDataList.Add(data);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            PerkManager.self.receiveListFromZipData(perkDataList);
            break;
            #endregion

         #region Group 4 (Ships/ShipAbility/Shop/Tutorial)
         case EditorToolType.Ship:
            List<ShipData> shipDataList = new List<ShipData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  ShipData actualData = Util.xmlLoad<ShipData>(xmlSubGroup[1]);
                  shipDataList.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            ShipDataManager.self.receiveShipDataFromZipData(shipDataList);
            break;
         case EditorToolType.ShipAbility:
            List<ShipAbilityPair> shipAbilityList = new List<ShipAbilityPair>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  ShipAbilityData actualData = Util.xmlLoad<ShipAbilityData>(xmlSubGroup[1]);
                  shipAbilityList.Add(new ShipAbilityPair { 
                     abilityId = dataId,
                     abilityName = actualData.abilityName,
                     shipAbilityData = actualData
                  });
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            ShipAbilityManager.self.receiveDataFromZipData(shipAbilityList.ToArray());
            break;
         case EditorToolType.Shop:
            List<ShopDataGroup> shopDataList = new List<ShopDataGroup>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  ShopData actualData = Util.xmlLoad<ShopData>(xmlSubGroup[1]);
                  actualData.shopId = dataId;
                  shopDataList.Add(new ShopDataGroup { 
                     shopData = actualData,
                     xmlId = dataId
                  });
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            ShopXMLManager.self.receiveDataFromZipData(shopDataList.ToArray());
            break;
         #endregion

         case EditorToolType.Background:
            List<BackgroundContentData> bgContentDataList = new List<BackgroundContentData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  BackgroundContentData actualData = Util.xmlLoad<BackgroundContentData>(xmlSubGroup[1]);
                  actualData.xmlId = dataId;
                  bgContentDataList.Add(actualData);

                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            BackgroundGameManager.self.receiveNewContent(bgContentDataList.ToArray());
            break;
         case EditorToolType.Palette:
            List<PaletteToolData> paletteData = new List<PaletteToolData>();

            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  PaletteToolData actualData = Util.xmlLoad<PaletteToolData>(xmlSubGroup[1]);
                  paletteData.Add(actualData);

                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            PaletteSwapManager.self.storePaletteData(paletteData.ToArray());
            break;

         case EditorToolType.Treasure_Drops:
            Dictionary<int, LootGroupData> lootGroupCollection = new Dictionary<int, LootGroupData>();

            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int uniqueId = int.Parse(xmlSubGroup[0]);
                  LootGroupData lootGroupData = Util.xmlLoad<LootGroupData>(xmlSubGroup[1]);
                  lootGroupCollection.Add(uniqueId, lootGroupData);

                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            TreasureDropsDataManager.self.receiveListFromZipData(lootGroupCollection);
            break;
      }

      if (includeProgressInEditorLog) {
         D.editorLog(message, Color.cyan);
      }
   }

   private void checkTextExtractionProgress () {
      debugLog("Progress is: " + currentProgress + " / " + targetProgress);

      if (currentProgress >= targetProgress) {
         isInitialized = true;
         finishedLoadingXmlData.Invoke();
         D.debug("Finished assigning Xml Data");
         loadBlocker.SetActive(false);
      }
   }

   private void debugLog (string message) {
      if (includeProgressInEditorLog) {
         D.debug(message);
      }
   }

   #region Private Variables

   #endregion
}
