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

   // Self
   public static SeaMonsterToolManager self;

   // Crafting Data to be rewarded
   public List<CraftableItemRequirements> craftingDataList = new List<CraftableItemRequirements>();

   public class SeaMonsterXMLContent
   {
      public int xml_id;
      public SeaMonsterEntityData seaMonsterData;
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
      monsterDataList = new List<SeaMonsterXMLContent>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getSeaMonsterXML();
         userIdData = DB_Main.getSQLDataByID(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.raw_xml_data);
               SeaMonsterEntityData newSeaData = Util.xmlLoad<SeaMonsterEntityData>(newTextAsset);

               // Save the Monster data in the memory cache
               if (!monsterDataList.Exists(_=>_.xml_id == xmlPair.xml_id)) {
                  SeaMonsterXMLContent xmlContent = new SeaMonsterXMLContent { 
                     xml_id = xmlPair.xml_id,
                     isEnabled = xmlPair.is_enabled,
                     seaMonsterData = newSeaData
                  };
                  monsterDataList.Add(xmlContent);
               }
            }
            monsterToolScreen.updatePanelWithData(monsterDataList);
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
      data.monsterName += "_copy";
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
   [SerializeField] private List<SeaMonsterXMLContent> monsterDataList = new List<SeaMonsterXMLContent>();

   #endregion
}
