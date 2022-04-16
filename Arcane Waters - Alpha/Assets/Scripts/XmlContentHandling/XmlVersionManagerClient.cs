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
using MapCreationTool.Serialization;

public class XmlVersionManagerClient : GenericGameManager {
   #region Public Variables

   // Path of the streaming files
   public static string ZIP_PATH = Application.streamingAssetsPath + "/XmlZip/XmlContent.zip";
   public static string TEXT_PATH = Application.streamingAssetsPath + "/XmlTexts/";

   // Error directory for log purposes
   public static string ERROR_DIRECTORY = "C:/XmlErrorLog/";
   public static string ERROR_FILENAME = "ErrorFile.txt";

   // Version text file
   public static string VERSION_FILE = "version_xml";

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

   // File name of the tooltip xml
   public const string XML_BASE_TOOLTIP = "xml_tooltip_base";

   // The minimum valid file content size
   public const int FILE_SIZE_MIN = 10;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
      webDirectory = "http://" + Global.getAddress(MyNetworkManager.ServerType.AmazonVPC) + ":7900/";
   }

   private void Start () {
      loadBaseTooltipCache();
   }

   public void initializeClient () {
      D.debug("Initializing Client!");
      if (!isInitialized) {
         if (resetXmlPrefs) {
            File.WriteAllText(TEXT_PATH + VERSION_FILE + ".txt", "0");
         }
         loadBlocker.SetActive(true);

         // Set initialization progress to 0
         _downloadProgress = 0;
         _extractProgress = 0;
         _writeProgress = 0;

         // Add progress to loading screen if it is showing already
         if (PanelManager.self.loadingScreen.isShowing()) {
            PanelManager.self.loadingScreen.show(LoadingScreen.LoadingType.XmlExtraction);
            updateLoadingProgress();
         }

         initializeLoadingXmlData.RemoveAllListeners();
         finishedCheckingStreamingAsset.RemoveAllListeners();
         finishedLoadingXmlData.RemoveAllListeners();

         D.debug("Nubis Client Fetch has started!");
         NubisDataFetcher.self.xmlVersionEvent.AddListener(_ => {
            processClientData(_);
            NubisDataFetcher.self.xmlVersionEvent.RemoveAllListeners();
         });

         NubisDataFetcher.self.fetchXmlVersion();
      }
   }

   private void processClientData (int serverVersion) {
      int clientXmlVersion = 0;
      D.debug("Processing server version: " + serverVersion);
      string fileDirectory = TEXT_PATH + VERSION_FILE + ".txt";
      if (!File.Exists(fileDirectory)) {
         D.debug("Missing file! Creating now: " + fileDirectory);
         File.Create(fileDirectory).Close();
         File.WriteAllText(fileDirectory, "0");
      } else {
         // Read the text from directly from the txt file
         StreamReader reader = new StreamReader(fileDirectory);
         string versionText = reader.ReadToEnd();
         reader.Close();

         try {
            clientXmlVersion = int.Parse(versionText);
            D.debug("Client version is {" + clientXmlVersion + "}");
         } catch {
            D.debug("Failed to parse version {" + versionText + "} from file {" + fileDirectory + "}");
         }
      }

      string clientMessage = "";

      finishedCheckingStreamingAsset.AddListener(isCompleteData => {
         if (!isCompleteData) {
            clientMessage = "Missing Files! Initialize Redownload";
            _downloadProgress = 0f;
            D.debug(clientMessage);

            // Reset version cache since file integrity might be compromised
            File.WriteAllText(TEXT_PATH + VERSION_FILE + ".txt", "0");

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
               _downloadProgress = 1f;
               _writeProgress = 1f;
               updateLoadingProgress();
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
      checkStreamingAssetFile(XmlVersionManagerServer.CRAFTING_FILE);

      checkStreamingAssetFile(XmlVersionManagerServer.ARMOR_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.WEAPON_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.HAT_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.RING_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.NECKLACE_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.TRINKET_FILE);

      checkStreamingAssetFile(XmlVersionManagerServer.LAND_MONSTER_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.NPC_FILE);

      checkStreamingAssetFile(XmlVersionManagerServer.SEA_MONSTER_FILE);

      checkStreamingAssetFile(XmlVersionManagerServer.SHIP_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.SHOP_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.SHIP_ABILITY_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.BACKGROUND_DATA_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.LAND_POWERUPS_FILE);

      checkStreamingAssetFile(XmlVersionManagerServer.PERKS_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.TREASURE_DROPS_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.QUEST_DATA_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.ITEM_DEFINITIONS_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.PALETTE_FILE, true);
      checkStreamingAssetFile(XmlVersionManagerServer.TOOL_TIP_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.PROJECTILES_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.TUTORIAL_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.MAP_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.SFX_FILE);

      checkStreamingAssetFile(XmlVersionManagerServer.HAIRCUTS_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.DYES_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.GEMS_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.SHIP_SKINS_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.CONSUMABLES_FILE);
      checkStreamingAssetFile(XmlVersionManagerServer.QUEST_ITEMS_FILE);
   }

   private void checkStreamingAssetFile (string fileName, bool isLastEntry = false) {
      string fileDirectory = TEXT_PATH + fileName + ".txt";
      if (!File.Exists(fileDirectory)) {
         D.debug("Missing file! Creating now: " + fileDirectory);
         File.Create(fileDirectory).Close();
         finishedCheckingStreamingAsset.Invoke(false);
      }

      if (isLastEntry) {
         finishedCheckingStreamingAsset.Invoke(true);
      }
   }
   
   private async void downloadClientData (int targetVersion) {
      string zipDataRequest = await NubisClient.call<string>(nameof(DB_Main.fetchZipRawData));
      D.debug("ZipDownloadComplete: " + zipDataRequest.Length);
      _downloadProgress = 1f;
      updateLoadingProgress();
      writeData(zipDataRequest, targetVersion);
   }
   
   private void writeData (string zipDataRequest, int targetVersion) {
      if (zipDataRequest.Length < 10) {
         // If the result string is less than expected, the zip blob download has failed then call out the error panel which contains an exit button
         loadBlocker.SetActive(true);
         PanelManager.self.noticeScreen.show("Failed to fetch data from server!");
         PanelManager.self.noticeScreen.confirmButton.onClick.AddListener(() => Application.Quit());
         return;
      } else {
         try {
            byte[] bytes = Convert.FromBase64String(zipDataRequest);
            D.debug("Successfully written zip bytes: " + bytes.Length);
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
            D.debug("Successfully written zip bytes: " + zipDataRequest.Length);
         }

         GZipUtility.decompressToDirectory(ZIP_PATH, TEXT_PATH, (fileName) => {
            debugLog("Decompressing: ..." + fileName);
         });

         D.editorLog("Finished Extracting Zip", Color.green);
         File.WriteAllText(TEXT_PATH + VERSION_FILE + ".txt", targetVersion.ToString());
         D.debug("New xml version is: " + targetVersion + " to " + TEXT_PATH + VERSION_FILE + ".txt");
         _writeProgress = 1f;
         updateLoadingProgress();
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
      extractXmlType(EditorToolType.Equipment_Ring);
      extractXmlType(EditorToolType.Equipment_Necklace);
      extractXmlType(EditorToolType.Equipment_Trinket);
      extractXmlType(EditorToolType.Crafting);

      extractXmlType(EditorToolType.LandMonster);
      extractXmlType(EditorToolType.LandPowerups);
      extractXmlType(EditorToolType.SeaMonster);
      extractXmlType(EditorToolType.NPC);

      extractXmlType(EditorToolType.Shop);
      extractXmlType(EditorToolType.Ship);
      extractXmlType(EditorToolType.ShipAbility);

      extractXmlType(EditorToolType.Background);
      extractXmlType(EditorToolType.Perks);
      extractXmlType(EditorToolType.Palette);
      extractXmlType(EditorToolType.Treasure_Drops);
      extractXmlType(EditorToolType.Quest);
      extractXmlType(EditorToolType.ItemDefinitions);
      extractXmlType(EditorToolType.Tool_Tip);
      extractXmlType(EditorToolType.Projectiles);
      extractXmlType(EditorToolType.Tutorial);
      extractXmlType(EditorToolType.Map_Keys);
      extractXmlType(EditorToolType.SFX);

      extractXmlType(EditorToolType.Haircuts);
      extractXmlType(EditorToolType.Dyes);
      extractXmlType(EditorToolType.Gems);
      extractXmlType(EditorToolType.ShipSkins);
      extractXmlType(EditorToolType.Consumables);
      extractXmlType(EditorToolType.QuestItems);

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
         case EditorToolType.Equipment_Ring:
            path = TEXT_PATH + XmlVersionManagerServer.RING_FILE + ".txt";
            break;
         case EditorToolType.Equipment_Necklace:
            path = TEXT_PATH + XmlVersionManagerServer.NECKLACE_FILE + ".txt";
            break;
         case EditorToolType.Equipment_Trinket:
            path = TEXT_PATH + XmlVersionManagerServer.TRINKET_FILE + ".txt";
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
         case EditorToolType.LandPowerups:
            path = TEXT_PATH + XmlVersionManagerServer.LAND_POWERUPS_FILE + ".txt";
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
         case EditorToolType.Quest:
            path = TEXT_PATH + XmlVersionManagerServer.QUEST_DATA_FILE + ".txt";
            break;
         case EditorToolType.ItemDefinitions:
            path = TEXT_PATH + XmlVersionManagerServer.ITEM_DEFINITIONS_FILE + ".txt";
            break;
         case EditorToolType.Crafting:
            path = TEXT_PATH + XmlVersionManagerServer.CRAFTING_FILE + ".txt";
            break;
         case EditorToolType.Tool_Tip:
            path = TEXT_PATH + XmlVersionManagerServer.TOOL_TIP_FILE + ".txt";
            break;
         case EditorToolType.Projectiles:
            path = TEXT_PATH + XmlVersionManagerServer.PROJECTILES_FILE + ".txt";
            break;
         case EditorToolType.Tutorial:
            path = TEXT_PATH + XmlVersionManagerServer.TUTORIAL_FILE + ".txt";
            break;
         case EditorToolType.Map_Keys:
            path = TEXT_PATH + XmlVersionManagerServer.MAP_FILE + ".txt";
            break;
         case EditorToolType.SFX:
            path = TEXT_PATH + XmlVersionManagerServer.SFX_FILE + ".txt";
            break;
         case EditorToolType.Haircuts:
            path = TEXT_PATH + XmlVersionManagerServer.HAIRCUTS_FILE + ".txt";
            break;
         case EditorToolType.Dyes:
            path = TEXT_PATH + XmlVersionManagerServer.DYES_FILE + ".txt";
            break;
         case EditorToolType.Gems:
            path = TEXT_PATH + XmlVersionManagerServer.GEMS_FILE + ".txt";
            break;
         case EditorToolType.ShipSkins:
            path = TEXT_PATH + XmlVersionManagerServer.SHIP_SKINS_FILE + ".txt";
            break;
         case EditorToolType.Consumables:
            path = TEXT_PATH + XmlVersionManagerServer.CONSUMABLES_FILE + ".txt";
            break;
         case EditorToolType.QuestItems:
            path = TEXT_PATH + XmlVersionManagerServer.QUEST_ITEMS_FILE + ".txt";
            break;
      }

      // Read the text from directly from the txt file
      StreamReader reader = new StreamReader(path);
      content = reader.ReadToEnd();
      reader.Close();

      assignDataToManagers(xmlType, content);
      currentProgress++;
      checkTextExtractionProgress();
   }

   private void assignDataToManagers (EditorToolType xmlType, string content) {
      D.adminLog("Assigning data to manager {" + xmlType + "}" + " " + content.Length, D.ADMIN_LOG_TYPE.Client_AccountLogin);

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
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.xmlName + " - " + ((Crop.Type) actualData.cropsType);
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
                     attackAbility.itemID = abilityId;
                     attackAbilityList.Add(attackAbility);
                  } else if (abilityType == AbilityType.BuffDebuff) {
                     BuffAbilityData buffAbility = Util.xmlLoad<BuffAbilityData>(xmlSubGroup[abilityXmlContentIndex]);
                     buffAbility.itemID = abilityId;
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
               if (xmlSubGroup.Length == 3) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  bool isActive = int.Parse(xmlSubGroup[1]) == 1 ? true : false;
                  if (isActive) {
                     ArmorStatData actualData = Util.xmlLoad<ArmorStatData>(xmlSubGroup[2]);
                     if (actualData.armorType > 0) {
                        actualData.sqlId = dataId;
                        armorList.Add(actualData);
                        message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.equipmentName + " - " + actualData.sqlId + " - " + actualData.armorType;
                     } else {
                        D.debug("WARNING! An armor has no assigned armor type!" + dataId + " : " + actualData.armorType);
                     }
                  } else {
                     D.debug("Skip add entry for Armor:" + dataId);
                  }
               }
            }
            EquipmentXMLManager.self.receiveArmorDataFromZipData(armorList);
            break;
         case EditorToolType.Equipment_Weapon:
            List<WeaponStatData> weaponList = new List<WeaponStatData>();
            string weaponIdsSkipped = "";
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 3) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  bool isActive = int.Parse(xmlSubGroup[1]) == 1 ? true : false;
                  if (isActive) {
                     WeaponStatData actualData = Util.xmlLoad<WeaponStatData>(xmlSubGroup[2]);
                     if (actualData.weaponType > 0) {
                        actualData.sqlId = dataId;
                        weaponList.Add(actualData);
                        message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.equipmentName + " - " + actualData.sqlId + " - " + actualData.weaponType;
                     } else {
                        D.debug("WARNING! A weapon has no assigned weapon type! " + dataId + " : " + actualData.weaponType);
                     }
                  } else {
                     weaponIdsSkipped += dataId + " ";
                  }
               }
            }

            D.debug("Skip add entry for Weapons: {" + weaponIdsSkipped + "}");
            EquipmentXMLManager.self.receiveWeaponDataFromZipData(weaponList);
            break;
         case EditorToolType.Equipment_Hat:
            List<HatStatData> hatList = new List<HatStatData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);
               try {
                  // Extract the segregated data and assign to the xml manager
                  if (xmlSubGroup.Length == 3) {
                     int dataId = int.Parse(xmlSubGroup[0]);
                     bool isActive = int.Parse(xmlSubGroup[1]) == 1 ? true : false;
                     if (isActive) {
                        HatStatData actualData = Util.xmlLoad<HatStatData>(xmlSubGroup[2]);
                        if (actualData.hatType > 0) {
                           actualData.sqlId = dataId;
                           hatList.Add(actualData);
                           message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.equipmentName + " - " + actualData.sqlId + " - " + actualData.hatType;
                        } else {
                           D.debug("WARNING! A hat has no assigned hat type! " + dataId + " : " + actualData.hatType);
                        }
                     } else {
                        D.debug("Skip add entry for Hat:" + dataId);
                     }
                  }
               } catch {
                  D.editorLog("Cant process hat data: " + xmlSubGroup[0] + " : " + xmlSubGroup[2] + " : ", Color.yellow);
               }
            }
            EquipmentXMLManager.self.receiveHatFromZipData(hatList);
            break;
         case EditorToolType.Equipment_Ring:
            List<RingStatData> ringList = new List<RingStatData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);
               try {
                  // Extract the segregated data and assign to the xml manager
                  if (xmlSubGroup.Length == 3) {
                     int dataId = int.Parse(xmlSubGroup[0]);
                     bool isActive = int.Parse(xmlSubGroup[1]) == 1 ? true : false;
                     if (isActive) {
                        RingStatData actualData = Util.xmlLoad<RingStatData>(xmlSubGroup[2]);
                        if (actualData.ringType > 0) {
                           actualData.sqlId = dataId;
                           ringList.Add(actualData);
                           message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.equipmentName + " - " + actualData.sqlId + " - " + actualData.ringType;
                        } else {
                           D.debug("WARNING! A ring has no assigned ring type! " + dataId + " : " + actualData.ringType);
                        }
                     } else {
                        D.debug("Skip add entry for ring:" + dataId);
                     }
                  }
               } catch {
                  D.editorLog("Cant process ring data: " + xmlSubGroup[0] + " : " + xmlSubGroup[2] + " : ", Color.yellow);
               }
            }
            EquipmentXMLManager.self.receiveRingDataFromZipData(ringList);
            break;
         case EditorToolType.Equipment_Necklace:
            List<NecklaceStatData> necklaceList = new List<NecklaceStatData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);
               try {
                  // Extract the segregated data and assign to the xml manager
                  if (xmlSubGroup.Length == 3) {
                     int dataId = int.Parse(xmlSubGroup[0]);
                     bool isActive = int.Parse(xmlSubGroup[1]) == 1 ? true : false;
                     if (isActive) {
                        NecklaceStatData actualData = Util.xmlLoad<NecklaceStatData>(xmlSubGroup[2]);
                        if (actualData.necklaceType > 0) {
                           actualData.sqlId = dataId;
                           necklaceList.Add(actualData);
                           message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.equipmentName + " - " + actualData.sqlId + " - " + actualData.necklaceType;
                        } else {
                           D.debug("WARNING! A necklace has no assigned necklace type! " + dataId + " : " + actualData.necklaceType);
                        }
                     } else {
                        D.debug("Skip add entry for necklace:" + dataId);
                     }
                  }
               } catch {
                  D.editorLog("Cant process necklace data: " + xmlSubGroup[0] + " : " + xmlSubGroup[2] + " : ", Color.yellow);
               }
            }
            EquipmentXMLManager.self.receiveNecklaceDataFromZipData(necklaceList);
            break;
         case EditorToolType.Equipment_Trinket:
            List<TrinketStatData> trinketList = new List<TrinketStatData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);
               try {
                  // Extract the segregated data and assign to the xml manager
                  if (xmlSubGroup.Length == 3) {
                     int dataId = int.Parse(xmlSubGroup[0]);
                     bool isActive = int.Parse(xmlSubGroup[1]) == 1 ? true : false;
                     if (isActive) {
                        TrinketStatData actualData = Util.xmlLoad<TrinketStatData>(xmlSubGroup[2]);
                        if (actualData.trinketType > 0) {
                           actualData.sqlId = dataId;
                           trinketList.Add(actualData);
                           message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.equipmentName + " - " + actualData.sqlId + " - " + actualData.trinketType;
                        } else {
                           D.debug("WARNING! A trinket has no assigned trinket type! " + dataId + " : " + actualData.trinketType);
                        }
                     } else {
                        D.debug("Skip add entry for trinket:" + dataId);
                     }
                  }
               } catch {
                  D.editorLog("Cant process trinket data: " + xmlSubGroup[0] + " : " + xmlSubGroup[2] + " : ", Color.yellow);
               }
            }
            EquipmentXMLManager.self.receiveTrinketDataFromZipData(trinketList);
            break;
         case EditorToolType.NPC:
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  NPCData actualData = Util.xmlLoad<NPCData>(xmlSubGroup[1]);
                  NPCManager.self.storeNPCData(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
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
                  seaMonsterData.xmlId = monsterId;
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

         #region Group 4 (Ships/ShipAbility/Shop)
         case EditorToolType.Ship:
            Dictionary<int, ShipData> shipDataList = new Dictionary<int, ShipData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 3) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  bool isActive = int.Parse(xmlSubGroup[1]) == 1 ? true : false;
                  if (isActive) {
                     ShipData actualData = Util.xmlLoad<ShipData>(xmlSubGroup[2]);
                     actualData.shipID = dataId;
                     shipDataList.Add(dataId, actualData);
                     message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
                  }
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
            Dictionary<int, PaletteToolData> paletteData = new Dictionary<int, PaletteToolData>();

            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  PaletteToolData actualData = Util.xmlLoad<PaletteToolData>(xmlSubGroup[1]);
                  paletteData.Add(dataId, actualData);

                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            PaletteSwapManager.self.storePaletteData(paletteData);
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

         case EditorToolType.Quest:
            Dictionary<int, QuestData> questDataCollection = new Dictionary<int, QuestData>();

            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int uniqueId = int.Parse(xmlSubGroup[0]);
                  QuestData questData = Util.xmlLoad<QuestData>(xmlSubGroup[1]);
                  questData.questId = uniqueId;
                  questDataCollection.Add(uniqueId, questData);

                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            NPCQuestManager.self.receiveListFromZipData(questDataCollection);
            break;

         case EditorToolType.ItemDefinitions:
            string errors = "";
            foreach (string subGroup in xmlGroup) {
               // We might get an empty entry, remove skip if so
               if (string.IsNullOrWhiteSpace(subGroup)) {
                  continue;
               }
               try {
                  // Subgroup should have 3 entries in this structure:
                  // id <spacer> category <spacer> serializer data
                  string[] entries = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.RemoveEmptyEntries);
                  ItemDefinition itemDefinition = ItemDefinition.deserialize(entries[2], (ItemDefinition.Category) int.Parse(entries[1]));
                  ItemDefinitionManager.self.storeItemDefinition(itemDefinition);
               } catch (Exception ex) {
                  errors += ex + Environment.NewLine;
               }
            }
            ItemDefinitionManager.self.definitionsLoaded = true;
            if (!string.IsNullOrEmpty(errors)) {
               D.error("There were errors when storing item definitions:" + Environment.NewLine + errors);
            }
            break;

         case EditorToolType.Crafting:
            Dictionary<string, CraftableItemRequirements> _craftingData = new Dictionary<string, CraftableItemRequirements>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int uniqueId = int.Parse(xmlSubGroup[0]);
                  CraftableItemRequirements craftData = Util.xmlLoad<CraftableItemRequirements>(xmlSubGroup[1]);
                  string keyName = CraftingManager.getKey(craftData.resultItem.category, craftData.resultItem.itemTypeId);
                  if (craftData.isEnabled) {
                     if (_craftingData.ContainsKey(keyName)) {
                        D.editorLog("Duplicate Crafting Key: " + keyName, Color.red);
                     } else {
                        craftData.xmlId = uniqueId;
                        _craftingData.Add(keyName, craftData);
                     }

                     message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
                  }
               }
            }
            CraftingManager.self.receiveZipData(_craftingData);
            break;

         case EditorToolType.Tool_Tip:
            // Update the cached tooltip base data
            using (StreamWriter file = new StreamWriter(@"" + getTooltipBaseDirectory(), false)) {
               file.WriteLine(content);
            }

            List<TooltipSqlData> tooltipDataList = new List<TooltipSqlData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length > 1) {
                  int uniqueId = int.Parse(xmlSubGroup[0]);
                  string key1 = xmlSubGroup[1];
                  string key2 = xmlSubGroup[2];
                  string value = xmlSubGroup[3];
                  int locationId = int.Parse(xmlSubGroup[4]);

                  TooltipSqlData newSqlData = new TooltipSqlData {
                     id = uniqueId,
                     key1 = key1,
                     key2 = key2,
                     value = value,
                     displayLocation = locationId
                  };
                  tooltipDataList.Add(newSqlData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            UIToolTipManager.self.receiveZipData(tooltipDataList);
            break;

         case EditorToolType.Projectiles:
            List<ProjectileStatPair> projectileDataList = new List<ProjectileStatPair>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  ProjectileStatData actualData = Util.xmlLoad<ProjectileStatData>(xmlSubGroup[1]);
                  actualData.projectileId = dataId;
                  projectileDataList.Add(new ProjectileStatPair {
                     projectileData = actualData,
                     xmlId = dataId
                  });
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            ProjectileStatManager.self.receiveZipData(projectileDataList);
            break;
         case EditorToolType.Tutorial:
            List<Tutorial3> tutorialDataList = new List<Tutorial3>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  Tutorial3 actualData = Util.xmlLoad<Tutorial3>(xmlSubGroup[1]);
                  actualData.xmlId = dataId;
                  tutorialDataList.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            TutorialManager3.self.receiveDataFromZip(tutorialDataList);
            break;

         case EditorToolType.SFX:
            //List<SoundEffect> sfxDataList = new List<SoundEffect>();
            //foreach (string subGroup in xmlGroup) {
            //   string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

            //   // Extract the segregated data and assign to the xml manager
            //   if (xmlSubGroup.Length == 2) {
            //      int dataId = int.Parse(xmlSubGroup[0]);
            //      SoundEffect actualData = Util.xmlLoad<SoundEffect>(xmlSubGroup[1]);
            //      actualData.id = dataId;
            //      sfxDataList.Add(actualData);
            //      message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
            //   }
            //}
            //SoundEffectManager.self.receiveListFromServer(sfxDataList.ToArray());
            break;

         case EditorToolType.Haircuts:
            List<HaircutData> haircutDataList = new List<HaircutData>();
            
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  HaircutData actualData = Util.xmlLoad<HaircutData>(xmlSubGroup[1]);
                  actualData.itemID = dataId;
                  haircutDataList.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }

            HaircutXMLManager.self.receiveHaircutDataFromZipData(haircutDataList);
            break;

         case EditorToolType.Dyes:
            List<DyeData> dyesDataList = new List<DyeData>();

            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  DyeData actualData = Util.xmlLoad<DyeData>(xmlSubGroup[1]);
                  actualData.itemID = dataId;
                  dyesDataList.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }

            DyeXMLManager.self.receiveDataFromZipData(dyesDataList);
            break;

         case EditorToolType.Gems:
            List<GemsData> gemsDataList = new List<GemsData>();

            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  GemsData actualData = Util.xmlLoad<GemsData>(xmlSubGroup[1]);
                  actualData.itemID = dataId;
                  gemsDataList.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }

            GemsXMLManager.self.receiveDataFromZipData(gemsDataList);
            break;

         case EditorToolType.ShipSkins:
            List<ShipSkinData> shipSkinDataList = new List<ShipSkinData>();

            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  if (xmlSubGroup[1].Contains(EquipmentXMLManager.VALID_XML_FORMAT)) {
                     try {
                        ShipSkinData actualData = Util.xmlLoad<ShipSkinData>(xmlSubGroup[1]);
                        actualData.itemID = dataId;
                        shipSkinDataList.Add(actualData);
                        message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
                     } catch {
                     
                     }
                  }
               }
            }

            ShipSkinXMLManager.self.receiveShipSkinDataFromZipData(shipSkinDataList);
            break;

         case EditorToolType.Consumables:
            List<ConsumableData> consumableDataList = new List<ConsumableData>();

            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  ConsumableData actualData = Util.xmlLoad<ConsumableData>(xmlSubGroup[1]);
                  actualData.itemID = dataId;
                  consumableDataList.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }

            ConsumableXMLManager.self.receiveConsumableDataFromZipData(consumableDataList);
            break;

         case EditorToolType.LandPowerups:
            List<LandPowerupInfo> landPowerupList = new List<LandPowerupInfo>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length >= 2) {
                  LandPowerupInfo powerupData = Util.xmlLoad<LandPowerupInfo>(xmlSubGroup[1]);
                  powerupData.xmlId = int.Parse(xmlSubGroup[0]);
                  powerupData.isXmlEnabled = int.Parse(xmlSubGroup[2]) == 1 ? true : false;
                  landPowerupList.Add(powerupData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            LandPowerupManager.self.receiveListFromZipData(landPowerupList.ToArray());
            break;

         case EditorToolType.QuestItems:
            List<Item> questItemList = new List<Item>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               if (xmlSubGroup.Length >= 3) {
                  int.TryParse(xmlSubGroup[1], out int isEnabled);
                  if (isEnabled > 0) {
                     if (xmlSubGroup[2].Length > 0 && xmlSubGroup[2].Contains(EquipmentXMLManager.VALID_XML_FORMAT)) {
                        Item itemData = Util.xmlLoad<Item>(xmlSubGroup[2]);
                        questItemList.Add(itemData);
                        message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[2];
                     } else {
                        D.debug("Failed to deserialzie: " + xmlSubGroup[2]);
                     }
                  }
               }
            }
            EquipmentXMLManager.self.receiveQuestDataFromZipData(questItemList);
            break;

         case EditorToolType.Map_Keys:
            List<Map> mapKeyDataList = new List<Map>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);
              
               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length >= 6) {
                  int id = 0;
                  string name = "";
                  string displayName = "";
                  int specialType = 0;
                  int sourceMapId = 0;
                  int weatherEffectType = 0;
                  int biome = 0;
                  int editorType = 0;
                  int maxPlayerCount = 0;
                  int pvpGameMode = 0;
                  int pvpArenaSize = 0;
                  bool spawnSeaMonsters = false;
                  int specialState = 0;

                  id = int.Parse(xmlSubGroup[0]);
                  name = xmlSubGroup[1];
                  displayName = xmlSubGroup[2];
                  specialType = int.Parse(xmlSubGroup[3]);
                  sourceMapId = int.Parse(xmlSubGroup[4]);
                  weatherEffectType = int.Parse(xmlSubGroup[5]);
                  biome = int.Parse(xmlSubGroup[6]);

                  int.TryParse(xmlSubGroup[7], out int retEditorType);
                  if (retEditorType > 0) {
                     editorType = retEditorType;
                  } else {
                     editorType = 0;
                  }

                  int.TryParse(xmlSubGroup[8], out int retPlayerCount);
                  if (retPlayerCount > 0) {
                     maxPlayerCount = retPlayerCount;
                  } else {
                     maxPlayerCount = 0;
                  }

                  int.TryParse(xmlSubGroup[9], out int retGameMode);
                  if (retGameMode > 0) {
                     pvpGameMode = retGameMode;
                  } else {
                     pvpGameMode = 0;
                  }

                  int.TryParse(xmlSubGroup[10], out int retArenaSize);
                  if (retArenaSize > 0) {
                     pvpArenaSize = retArenaSize;
                  } else {
                     pvpArenaSize = 0;
                  }

                  if (xmlSubGroup.Length >= 12) {
                     int.TryParse(xmlSubGroup[11], out int retSpawnSeamonsters);
                     if (retSpawnSeamonsters > 0) {
                        spawnSeaMonsters = true;
                     } else {
                        spawnSeaMonsters = false;
                     }
                     spawnSeaMonsters = int.Parse(xmlSubGroup[11]) == 1 ? true : false;

                     try {
                        int.TryParse(xmlSubGroup[12], out int retSpecialState);
                        specialState = retSpecialState;
                     } catch {
                        specialState = 0;
                     }
                  } else {
                     spawnSeaMonsters = false;
                     specialState = 0;
                     D.debug("Xml subgroup has insufficient data! " + xmlSubGroup.Length);
                  }

                  Map newMapEntry = new Map {
                     id = id,
                     name = name,
                     displayName = displayName,
                     specialType = (Area.SpecialType) specialType,
                     sourceMapId = sourceMapId,
                     weatherEffectType = (WeatherEffectType) weatherEffectType,
                     biome = (Biome.Type) biome,
                     editorType = (MapCreationTool.EditorType) editorType,
                     maxPlayerCount = maxPlayerCount,
                     pvpGameMode = (PvpGameMode) pvpGameMode,
                     pvpArenaSize = (PvpArenaSize) pvpArenaSize,
                     spawnsSeaMonsters = spawnSeaMonsters,
                     specialState = specialState
                  };

                  mapKeyDataList.Add(newMapEntry);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            AreaManager.self.receiveMapDataFromServerZip(mapKeyDataList);
            break;
      }

      if (includeProgressInEditorLog) {
         D.editorLog(message, Color.cyan);
      }
   }

   private void checkTextExtractionProgress () {
      debugLog("Progress is: " + currentProgress + " / " + targetProgress);
      _extractProgress = (float) currentProgress / targetProgress;
      updateLoadingProgress();

      if (currentProgress >= targetProgress) {
         PanelManager.self.loadingScreen.hide(LoadingScreen.LoadingType.XmlExtraction);
         finishedLoadingXmlData.Invoke();
         if (!isInitialized) {
            D.debug("Finished assigning Xml Data");
         }

         isInitialized = true;
         loadBlocker.SetActive(false);
      }
   }

   public static async void downloadTooltipCacheForPreExport () {
      string dataRequest = await NubisClient.call<string>(nameof(DB_Main.getTooltipXmlContent));
      D.debug("Tooltip data downloaded, now writing to file: " + dataRequest.Length);

      string fileDirectory = TEXT_PATH + XML_BASE_TOOLTIP + ".txt";
      if (!File.Exists(fileDirectory)) {
         D.debug("Missing file! Creating now");
         File.Create(fileDirectory).Close();
      }

      using (StreamWriter file = new StreamWriter(@"" + fileDirectory, false)) {
         file.WriteLine(dataRequest);
      }
   }

   public async void downloadTooltipCache (bool assignAutomatically) {
      string dataRequest = await NubisClient.call<string>(nameof(DB_Main.getTooltipXmlContent));
      D.debug("Tooltip data downloaded, now writing to file: " + dataRequest.Length);

      StartCoroutine(CO_ProcessDataCreation(dataRequest, assignAutomatically));
   }

   private IEnumerator CO_ProcessDataCreation (string dataRequest, bool assignAutomatically) {
      string fileDirectory = getTooltipBaseDirectory();
      if (!File.Exists(fileDirectory)) {
         D.debug("Missing file! Creating now");
         File.Create(fileDirectory).Close();
         yield return new WaitForSeconds(1);
      }

      if (dataRequest.Length > FILE_SIZE_MIN) {
         using (StreamWriter file = new StreamWriter(@"" + getTooltipBaseDirectory(), false)) {
            file.WriteLine(dataRequest);
         }
         D.debug("Successfully cached data");
         if (assignAutomatically) {
            yield return new WaitForSeconds(1);
            loadBaseTooltipCache();
         }
      } else {
         D.debug("Failed to retrieve data from server: " + dataRequest.Length);
      }
   }

   public void loadBaseTooltipCache () {
      // Download initial content when text file does not exist
      if (!File.Exists(getTooltipBaseDirectory())) {
         D.debug("Missing file! Failed to generate tooltip initial data, now downloading...");
         downloadTooltipCache(true);
         return;
      }

      // Read the content directly from the .txt file
      StreamReader reader = new StreamReader(getTooltipBaseDirectory());
      string content = reader.ReadToEnd();
      reader.Close();

      // Download initial content when text file is empty
      if (content.Length < FILE_SIZE_MIN) {
         D.debug("Missing content! Failed to generate tooltip initial data, now downloading...");
         downloadTooltipCache(true);
         return;
      }

      string splitter = "[next]";
      string[] xmlGroup = content.Split(new string[] { splitter }, StringSplitOptions.None);

      List<TooltipSqlData> tooltipDataList = new List<TooltipSqlData>();
      foreach (string subGroup in xmlGroup) {
         string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

         // Extract the segregated data and assign to the xml manager
         if (xmlSubGroup.Length > 1) {
            int uniqueId = int.Parse(xmlSubGroup[0]);
            string key1 = xmlSubGroup[1];
            string key2 = xmlSubGroup[2];
            string value = xmlSubGroup[3];
            int locationId = int.Parse(xmlSubGroup[4]);

            TooltipSqlData newSqlData = new TooltipSqlData {
               id = uniqueId,
               key1 = key1,
               key2 = key2,
               value = value,
               displayLocation = locationId
            };
            tooltipDataList.Add(newSqlData);
         }
      }
      UIToolTipManager.self.receiveZipData(tooltipDataList);
   }

   private void updateLoadingProgress () {
      PanelManager.self.loadingScreen.setProgress(LoadingScreen.LoadingType.XmlExtraction, _writeProgress + _extractProgress + _downloadProgress);
   }

   private void debugLog (string message) {
      if (includeProgressInEditorLog) {
         D.debug(message);
      }
   }

   private string getTooltipBaseDirectory () {
      return TEXT_PATH + XML_BASE_TOOLTIP + ".txt";
   }

   #region Private Variables

   // Progress of downloading serialized files
   private float _downloadProgress = 0;

   // Progress of writing and unzipping serialized files to disk
   private float _writeProgress = 0;

   // Progress of extracting data from serialized files
   private float _extractProgress = 0;

   #endregion
}
