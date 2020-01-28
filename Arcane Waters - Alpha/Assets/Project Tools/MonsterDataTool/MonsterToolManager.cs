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

public class MonsterToolManager : XmlDataToolManager {
   #region Public Variables

   // Reference to the tool scene
   public MonsterDataScene monsterToolScreen;

   // Holds the path of the folder
   public const string FOLDER_PATH = "MonsterStats";

   // Cache all abilities
   public List<BasicAbilityData> basicAbilityList = new List<BasicAbilityData>();

   // Cache offensive abilities only
   public List<AttackAbilityData> attackAbilityList = new List<AttackAbilityData>();

   // Cache buff abilities only
   public List<BuffAbilityData> buffAbilityList = new List<BuffAbilityData>();

   // Self
   public static MonsterToolManager self;

   // Crafting Data to be rewarded
   public List<CraftableItemRequirements> craftingDataList = new List<CraftableItemRequirements>();

   public class BattlerXMLContent
   {
      public int xml_id;
      public BattlerData battler;
      public bool isEnabled;
   }

   #endregion

   private void Awake () {
      self = this;
      fetchRecipe();
   }

   private void fetchRecipe () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getCraftingXML();
         userNameData = DB_Main.getSQLDataByName(EditorSQLManager.EditorToolType.Crafting);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               CraftableItemRequirements craftingData = Util.xmlLoad<CraftableItemRequirements>(newTextAsset);

               // Save the Crafting data in the memory cache
               craftingDataList.Add(craftingData);
            }
         });
      });
   }

   private void Start () {
      Invoke("loadAllDataFiles", MasterToolScene.loadDelay);
   }

   public void loadAllDataFiles () {
      XmlLoadingPanel.self.startLoading();
      basicAbilityList = new List<BasicAbilityData>();
      attackAbilityList = new List<AttackAbilityData>();
      buffAbilityList = new List<BuffAbilityData>();
      monsterDataList = new List<BattlerXMLContent>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> xmlPairList = DB_Main.getLandMonsterXML();
         List<AbilityXMLContent> abilityContentList = DB_Main.getBattleAbilityXML();
         userIdData = DB_Main.getSQLDataByID(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in xmlPairList) {
               TextAsset newTextAsset = new TextAsset(xmlPair.raw_xml_data);
               BattlerData monsterData = Util.xmlLoad<BattlerData>(newTextAsset);

               // Save the Monster data in the memory cache
               if (!monsterDataList.Exists(_=>_.xml_id == xmlPair.xml_id)) {
                  BattlerXMLContent newXmlContent = new BattlerXMLContent {
                     xml_id = xmlPair.xml_id,
                     battler = monsterData,
                     isEnabled = xmlPair.is_enabled
                  };
                  monsterDataList.Add(newXmlContent);
               }
            }

            foreach (AbilityXMLContent abilityXML in abilityContentList) {
               AbilityType abilityType = (AbilityType) abilityXML.abilityType;
               TextAsset newTextAsset = new TextAsset(abilityXML.abilityXML);

               switch (abilityType) {
                  case AbilityType.Standard:
                     AttackAbilityData attackAbilityData = Util.xmlLoad<AttackAbilityData>(newTextAsset);
                     attackAbilityList.Add(attackAbilityData);
                     basicAbilityList.Add(attackAbilityData);
                     break;
                  case AbilityType.Stance:
                     AttackAbilityData stancebilityData = Util.xmlLoad<AttackAbilityData>(newTextAsset);
                     attackAbilityList.Add(stancebilityData);
                     basicAbilityList.Add(stancebilityData);
                     break;
                  case AbilityType.BuffDebuff:
                     BuffAbilityData buffAbilityData = Util.xmlLoad<BuffAbilityData>(newTextAsset);
                     buffAbilityList.Add(buffAbilityData);
                     basicAbilityList.Add(buffAbilityData);
                     break;
                  default:
                     BasicAbilityData abilityData = Util.xmlLoad<BasicAbilityData>(newTextAsset);
                     basicAbilityList.Add(abilityData);
                     break;
               }
            }

            monsterToolScreen.updatePanelWithBattlerData(monsterDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void deleteMonsterDataFile (int xml_id) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteLandmonsterXML(xml_id);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public void saveDataToFile (BattlerData data, int xml_id, bool isActive) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateLandMonsterXML(longString, xml_id, data.enemyType, data.enemyName, isActive);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public void duplicateData (BattlerData data) {
      data.enemyName += "_copy";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateLandMonsterXML(longString, -1, data.enemyType, data.enemyName, false);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   #region Private Variables

   // Cached monster list
   [SerializeField] private List<BattlerXMLContent> monsterDataList = new List<BattlerXMLContent>();

   #endregion
}