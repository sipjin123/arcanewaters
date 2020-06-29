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

public class TreasureDropsToolManager : MonoBehaviour {
   #region Public Variables

   // Cached drops list
   public Dictionary<int, LootGroupData> treasureDropsCollection = new Dictionary<int, LootGroupData>();

   // Reference to self
   public static TreasureDropsToolManager instance;

   // Reference to the tool scene
   public TreasureDropsToolScene toolScene;

   #endregion

   private void Awake () {
      instance = this;
   }

   private void Start () {
      // Initialize equipment data first
      Invoke("initializeData", MasterToolScene.loadDelay);
      XmlLoadingPanel.self.startLoading();
   }

   private void initializeData () {
      EquipmentXMLManager.self.finishedDataSetup.AddListener(() => {
         loadAllDataFiles();
      });

      EquipmentXMLManager.self.initializeDataCache();
   }

   public void saveDataFile (int xmlId, Biome.Type biomeType, LootGroupData lootGroupData) {
      XmlLoadingPanel.self.startLoading();

      XmlSerializer ser = new XmlSerializer(lootGroupData.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, lootGroupData);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateBiomeTreasureDrops(xmlId, longString, biomeType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public void loadAllDataFiles () {
      XmlLoadingPanel.self.startLoading();
      treasureDropsCollection = new Dictionary<int, LootGroupData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> xmlPairList = DB_Main.getBiomeTreasureDrops();
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in xmlPairList) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               LootGroupData treasureCollectionData = Util.xmlLoad<LootGroupData>(newTextAsset);
               int uniqueId = xmlPair.xmlId;

               if (!treasureDropsCollection.ContainsKey(uniqueId)) {
                  treasureDropsCollection.Add(uniqueId, treasureCollectionData);
               }
            }

            toolScene.cacheDatabaseContents(treasureDropsCollection);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   #endregion
}
