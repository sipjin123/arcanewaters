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

   #endregion

   private void Awake () {
      instance = this;
   }

   private void Start () {
      // Initialize all craftable item data after equipment data is setup
      EquipmentXMLManager.self.finishedDataSetup.AddListener(() => {
         equipmentLoaded = true;
         Invoke("loadXMLData", MasterToolScene.loadDelay);
      });

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

   public void loadXMLData () {
      XmlLoadingPanel.self.startLoading();
      _questDataList = new List<QuestXMLContent>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getNPCQuestXML();
         userIdData = DB_Main.getSQLDataByID(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               QuestData questData = Util.xmlLoad<QuestData>(newTextAsset);

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
