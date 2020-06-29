using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System.Linq;
using System;

public class SeaMonsterToolManager : XmlDataToolManager
{
   #region Public Variables

   // Reference to the tool scene
   public SeaMonsterDataScene monsterToolScreen;

   // Holds the path of the folder
   public const string FOLDER_PATH = "SeaMonsterStats";

   // Reference to self
   public static SeaMonsterToolManager instance;

   // List of abilities
   public List<ShipAbilityPair> shipSkillList = new List<ShipAbilityPair>();

   // Reference to the loot group data
   public Dictionary<int, LootGroupData> lootGroupDataCollection = new Dictionary<int, LootGroupData>();

   // Checks if requirements are loaded
   public bool equipmentLoaded, abilitiesLoaded;

   #endregion

   private void Awake () {
      instance = this;
   }

   private void Start () {
      // Initialize equipment data first
      Invoke("initializeEquipmentData", MasterToolScene.loadDelay);
      XmlLoadingPanel.self.startLoading();
   }

   private void initializeEquipmentData () {
      // Initialize all craftable item data after equipment data is setup
      EquipmentXMLManager.self.finishedDataSetup.AddListener(() => {
         equipmentLoaded = true;
         checkRequirements();
      });

      fetchRecipe();
      EquipmentXMLManager.self.initializeDataCache();

      lootGroupDataCollection = new Dictionary<int, LootGroupData>();
      shipSkillList = new List<ShipAbilityPair>();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawSeaEntityAbilityXMLData = DB_Main.getShipAbilityXML();
         List<XMLPair> lootDropXmlData = DB_Main.getBiomeTreasureDrops();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Loads all the loot drops xml
            foreach (XMLPair xmlPair in lootDropXmlData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               LootGroupData lootDropData = Util.xmlLoad<LootGroupData>(newTextAsset);
               int uniqueId = xmlPair.xmlId;

               if (!lootGroupDataCollection.ContainsKey(uniqueId)) {
                  lootGroupDataCollection.Add(uniqueId, lootDropData);
               }
            }

            // Ability loading
            foreach (XMLPair seaEntityAbilityText in rawSeaEntityAbilityXMLData) {
               TextAsset newTextAsset = new TextAsset(seaEntityAbilityText.rawXmlData);
               ShipAbilityData shipAbility = Util.xmlLoad<ShipAbilityData>(newTextAsset);
               shipSkillList.Add(new ShipAbilityPair {
                  abilityName = shipAbility.abilityName,
                  abilityId = seaEntityAbilityText.xmlId
               });
            }

            abilitiesLoaded = true;
            checkRequirements();
         });
      });
   }

   private void checkRequirements () {
      if (abilitiesLoaded && equipmentLoaded) {
         loadAllDataFiles();
      }
   }

   public void loadAllDataFiles () {
      XmlLoadingPanel.self.startLoading();
      _monsterDataList = new List<SeaMonsterXMLContent>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getSeaMonsterXML();
         userIdData = DB_Main.getSQLDataByID(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               SeaMonsterEntityData newSeaData = Util.xmlLoad<SeaMonsterEntityData>(newTextAsset);

               // Save the Monster data in the memory cache
               if (!_monsterDataList.Exists(_=>_.xmlId == xmlPair.xmlId)) {
                  SeaMonsterXMLContent xmlContent = new SeaMonsterXMLContent { 
                     xmlId = xmlPair.xmlId,
                     isEnabled = xmlPair.isEnabled,
                     seaMonsterData = newSeaData
                  };
                  _monsterDataList.Add(xmlContent);
               }
            }
            monsterToolScreen.updatePanelWithData(_monsterDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void deleteMonsterDataFile (int xmlID) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteSeamonsterXML(xmlID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public void saveDataToFile (SeaMonsterEntityData data, int xml_id, bool isActive) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateSeaMonsterXML(longString, xml_id, data.seaMonsterType, data.monsterName, isActive);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public void duplicateFile (SeaMonsterEntityData data) {
      data.monsterName = MasterToolScene.UNDEFINED;
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateSeaMonsterXML(longString, -1, data.seaMonsterType, data.monsterName, false);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   #region Private Variables

   // Cached sea monster list
   [SerializeField] private List<SeaMonsterXMLContent> _monsterDataList = new List<SeaMonsterXMLContent>();

   #endregion
}
