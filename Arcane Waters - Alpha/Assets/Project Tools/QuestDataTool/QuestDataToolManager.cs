using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class QuestDataToolManager : XmlDataToolManager {
   #region Public Variables

   // Quest Scene Reference
   public QuestDataToolScene questToolScene;

   // Reference to self
   public static QuestDataToolManager instance;

   // If the equipment data is loaded
   public bool equipmentLoaded;

   // AbilityData list
   public List<BasicAbilityData> basicAbilityList;

   #endregion

   protected override void Awake () {
      base.Awake();
      instance = this;
   }

   private void Start () {
      // Initialize all craftable item data after equipment data is setup
      EquipmentXMLManager.self.finishedDataSetup.AddListener(() => {
         equipmentLoaded = true;
         Invoke("loadXMLData", MasterToolScene.loadDelay);
      });

      fetchRecipe();
      EquipmentXMLManager.self.initializeDataCache();
   }

   public void saveData (QuestData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      int uniqueID = data.questId;
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateNPCQuestXML(longString, uniqueID, data.questGroupName, data.isActive ? 1 : 0);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteDataFile (int xmlId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteNPCQuestXML(xmlId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateData (QuestData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateNPCQuestXML(longString, -1, data.questGroupName, 0);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      XmlLoadingPanel.self.startLoading();
      _questDataList = new List<QuestXMLContent>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<AbilityXMLContent> abilityContentList = DB_Main.getBattleAbilityXML();
         List<XMLPair> rawXMLData = DB_Main.getNPCQuestXML();
         userIdData = DB_Main.getSQLDataByID(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

            foreach (AbilityXMLContent abilityXML in abilityContentList) {
               AbilityType abilityType = (AbilityType) abilityXML.abilityType;
               TextAsset newTextAsset = new TextAsset(abilityXML.abilityXML);

               switch (abilityType) {
                  case AbilityType.Standard:
                     AttackAbilityData attackAbilityData = Util.xmlLoad<AttackAbilityData>(newTextAsset);
                     attackAbilityData.itemID = abilityXML.abilityId;
                     basicAbilityList.Add(attackAbilityData);
                     break;
                  case AbilityType.Stance:
                     AttackAbilityData stancebilityData = Util.xmlLoad<AttackAbilityData>(newTextAsset);
                     stancebilityData.itemID = abilityXML.abilityId;
                     basicAbilityList.Add(stancebilityData);
                     break;
                  case AbilityType.BuffDebuff:
                     BuffAbilityData buffAbilityData = Util.xmlLoad<BuffAbilityData>(newTextAsset);
                     buffAbilityData.itemID = abilityXML.abilityId;
                     basicAbilityList.Add(buffAbilityData);
                     break;
                  default:
                     BasicAbilityData abilityData = Util.xmlLoad<BasicAbilityData>(newTextAsset);
                     abilityData.itemID = abilityXML.abilityId;
                     basicAbilityList.Add(abilityData);
                     break;
               }
            }

            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               QuestData questData = Util.xmlLoad<QuestData>(newTextAsset);
               questData.questId = xmlPair.xmlId;

               // Save the data to cache
               if (!_questDataList.Exists(_ => _.xmlId == xmlPair.xmlId)) {
                  QuestXMLContent newXmlContent = new QuestXMLContent {
                     xmlId = xmlPair.xmlId,
                     isEnabled = xmlPair.isEnabled,
                     xmlData = questData
                  };
                  _questDataList.Add(newXmlContent);
               }
            }

            questToolScene.loadData(_questDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Cached xml list
   [SerializeField] private List<QuestXMLContent> _questDataList = new List<QuestXMLContent>();

   #endregion
}
