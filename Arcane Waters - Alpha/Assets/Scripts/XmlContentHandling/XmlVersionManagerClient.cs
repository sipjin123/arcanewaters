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
using static ShopDataToolManager;
using UnityEngine.Events;

public class XmlVersionManagerClient : MonoBehaviour {
   #region Public Variables

   // The player pref key for clients
   public static string XML_VERSION = "xml_version";

   // The server directory
   public static string WEB_DIRECTORY = "http://arcanewaters.com/";//"http://localhost/";

   public static string XML_VERSION_GET = "getXmlVersion.php";
   public static string XML_ZIP_GET = "downloadZip.php?id=";

   // Path of the streaming files
   public static string ZIP_PATH = Application.streamingAssetsPath + "/XmlZip/XmlContent.zip";
   public static string TEXT_PATH = Application.streamingAssetsPath + "/XmlTexts/";

   // Progress indicators
   public int targetProgress;
   public int currentProgress;

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

   #endregion

   private void Awake () {
      self = this;
   }

   public void initializeClient () {
      if (resetXmlPrefs) {
         PlayerPrefs.SetInt(XML_VERSION, 0);
      }
      loadBlocker.SetActive(true);
      StartCoroutine(CO_ProcessClientData());
   }

   private IEnumerator CO_ProcessClientData () {
      int clientXmlVersion = PlayerPrefs.GetInt(XML_VERSION, 0);

      UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + XML_VERSION_GET);
      yield return www.SendWebRequest();
      if (www.isNetworkError || www.isHttpError) {
         D.warning(www.error);
      } else {
         string rawData = www.downloadHandler.text;

         // Web request will return the xml version of the server
         int serverVersion = int.Parse(rawData);
         string clientMessage = "";

         if (serverVersion > clientXmlVersion) {
            clientMessage = "Client is outdated ver: " + clientXmlVersion + ", downloading new version: " + serverVersion;
            D.debug(clientMessage);
            StartCoroutine(CO_DownloadClientData(serverVersion));
         } else {
            clientMessage = "Client is up to date: Ver = " + clientXmlVersion;
            D.debug(clientMessage);
            processClientXml();
         }
      }
   }

   private IEnumerator CO_DownloadClientData (int targetVersion) {
      yield return new WaitForSeconds(1);

      var uwr = new UnityWebRequest(WEB_DIRECTORY + XML_ZIP_GET + targetVersion, UnityWebRequest.kHttpVerbGET);
      string path = ZIP_PATH;
      string resultMessage = "";
      uwr.downloadHandler = new DownloadHandlerFile(path);
      yield return uwr.SendWebRequest();
      if (uwr.isNetworkError || uwr.isHttpError) {
         resultMessage = uwr.error;
      } else {
         resultMessage = "File successfully downloaded and saved to " + path;
      }

      D.debug(resultMessage);
      StartCoroutine(CO_ExtractTextFiles());
      PlayerPrefs.SetInt(XML_VERSION, targetVersion);
   }

   private IEnumerator CO_ExtractTextFiles () {
      yield return new WaitForSeconds(.1f);
      using (ZipInputStream s = new ZipInputStream(File.OpenRead(ZIP_PATH))) {
         ZipEntry theEntry;
         while ((theEntry = s.GetNextEntry()) != null) {
            string directoryName = Path.GetDirectoryName(theEntry.Name);
            string fileName = Path.GetFileName(theEntry.Name);

            // Create directory
            if (directoryName.Length > 0) {
               Directory.CreateDirectory(directoryName);
            }

            if (fileName != String.Empty) {
               string filename = TEXT_PATH + "/";
               filename += theEntry.Name;

               using (FileStream streamWriter = File.Create(filename)) {
                  int size = 2048;
                  byte[] fdata = new byte[2048];
                  while (true) {
                     size = s.Read(fdata, 0, fdata.Length);
                     if (size > 0) {
                        streamWriter.Write(fdata, 0, size);
                     } else {
                        break;
                     }
                  }
               }
            }
         }
      }

      D.debug("Finished Extracting Zip");
      processClientXml();
   }

   private void processClientXml () {
      D.debug("Initializing Text Extraction");
      targetProgress = 0;
      currentProgress = 0;
      StartCoroutine(CO_ExtractXmlData(EditorToolType.Crops));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.BattlerAbility));

      StartCoroutine(CO_ExtractXmlData(EditorToolType.Equipment_Armor));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.Equipment_Weapon));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.Equipment_Helm));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.LandMonster));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.NPC));

      StartCoroutine(CO_ExtractXmlData(EditorToolType.PlayerClass));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.PlayerFaction));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.PlayerSpecialty));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.PlayerJob));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.SeaMonster));

      StartCoroutine(CO_ExtractXmlData(EditorToolType.Shop));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.Ship));
      StartCoroutine(CO_ExtractXmlData(EditorToolType.ShipAbility));
   }

   private IEnumerator CO_ExtractXmlData (EditorToolType xmlType) {
      targetProgress++;
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
         case EditorToolType.Equipment_Helm:
            path = TEXT_PATH + XmlVersionManagerServer.HELM_FILE + ".txt";
            break;
         case EditorToolType.LandMonster:
            path = TEXT_PATH + XmlVersionManagerServer.LAND_MONSTER_FILE + ".txt";
            break;
         case EditorToolType.NPC:
            path = TEXT_PATH + XmlVersionManagerServer.NPC_FILE + ".txt";
            break;

         case EditorToolType.PlayerClass:
            path = TEXT_PATH + XmlVersionManagerServer.CLASS_FILE + ".txt";
            break;
         case EditorToolType.PlayerFaction:
            path = TEXT_PATH + XmlVersionManagerServer.FACTION_FILE + ".txt";
            break;
         case EditorToolType.PlayerSpecialty:
            path = TEXT_PATH + XmlVersionManagerServer.SPECIALTY_FILE + ".txt";
            break;
         case EditorToolType.PlayerJob:
            path = TEXT_PATH + XmlVersionManagerServer.JOB_FILE + ".txt";
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
      }

      // Read the text from directly from the txt file
      try {
         StreamReader reader = new StreamReader(path);
         content = reader.ReadToEnd();
         reader.Close();

         assignDataToManagers(xmlType, content);
         currentProgress++;
         checkTextExtractionProgress();
      } catch {
         D.editorLog("Failed to read: " + xmlType + " - " + path, Color.red);
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
               if (xmlSubGroup.Length == 3) {
                  int abilityId = int.Parse(xmlSubGroup[0]);
                  AbilityType abilityType = (AbilityType) int.Parse(xmlSubGroup[1]);

                  if (abilityType == AbilityType.Standard || abilityType == AbilityType.Stance) {
                     AttackAbilityData attackAbility = Util.xmlLoad<AttackAbilityData>(xmlSubGroup[2]);
                     attackAbilityList.Add(attackAbility);
                  } else if (abilityType == AbilityType.BuffDebuff) {
                     BuffAbilityData buffAbility = Util.xmlLoad<BuffAbilityData>(xmlSubGroup[2]);
                     buffAbilityList.Add(buffAbility);
                     message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
                  }
               }
            }
            AbilityManager.self.receiveAbilitiesFromZipData(attackAbilityList.ToArray(), buffAbilityList.ToArray());
            break;
         #endregion

         #region Group 2 (Armor/Weapon/Helm/LandMonster/Npc)
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
                  actualData.equipmentID = dataId;
                  weaponList.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.equipmentName + " - " + actualData.equipmentID + " - " + actualData.weaponType;
               }
            }
            EquipmentXMLManager.self.receiveWeaponDataFromZipData(weaponList);
            break;
         case EditorToolType.Equipment_Helm:
            List<HelmStatData> helmList = new List<HelmStatData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  HelmStatData actualData = Util.xmlLoad<HelmStatData>(xmlSubGroup[1]);
                  actualData.equipmentID = dataId;
                  helmList.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + actualData.equipmentName + " - " + actualData.equipmentID + " - " + actualData.helmType;
               }
            }
            EquipmentXMLManager.self.receiveHelmFromZipData(helmList);
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

         #region Group 3 (Class/Faction/Job/Specialty/SeaMonster)
         case EditorToolType.PlayerClass:
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  PlayerClassData actualData = Util.xmlLoad<PlayerClassData>(xmlSubGroup[1]);
                  ClassManager.self.addClassInfo(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            break;
         case EditorToolType.PlayerFaction:
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  PlayerFactionData actualData = Util.xmlLoad<PlayerFactionData>(xmlSubGroup[1]);
                  FactionManager.self.addFactionInfo(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            break;
         case EditorToolType.PlayerJob:
            List<PlayerJobData> jobList = new List<PlayerJobData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  PlayerJobData actualData = Util.xmlLoad<PlayerJobData>(xmlSubGroup[1]);
                  jobList.Add(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            JobManager.self.receiveDataFromServer(jobList);
            break;
         case EditorToolType.PlayerSpecialty:
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { SPACE_KEY }, StringSplitOptions.None);

               // Extract the segregated data and assign to the xml manager
               if (xmlSubGroup.Length == 2) {
                  int dataId = int.Parse(xmlSubGroup[0]);
                  PlayerSpecialtyData actualData = Util.xmlLoad<PlayerSpecialtyData>(xmlSubGroup[1]);
                  SpecialtyManager.self.addSpecialtyInfo(actualData);
                  message = xmlType + " Success! " + xmlSubGroup[0] + " - " + xmlSubGroup[1];
               }
            }
            break;
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
      }

      if (includeProgressInEditorLog) {
         D.editorLog(message, Color.cyan);
      }
   }

   private void checkTextExtractionProgress () {
      if (includeProgressInEditorLog) {
         D.debug("Progress is: " + currentProgress + " / " + targetProgress);
      }

      if (currentProgress >= targetProgress) {
         isInitialized = true;
         finishedLoadingXmlData.Invoke();
         D.debug("Finished assigning Xml Data");
         loadBlocker.SetActive(false);
      }
   }

   #region Private Variables

   #endregion
}
