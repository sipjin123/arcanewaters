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
   public Dictionary<Biome.Type, List<TreasureDropsData>> treasureDropsCollection = new Dictionary<Biome.Type, List<TreasureDropsData>>();

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

   public void saveDataFile (Biome.Type biomeType, List<TreasureDropsData> treasureData) {
      XmlLoadingPanel.self.startLoading();
      TreasureDropsCollection newTreasureCollection = new TreasureDropsCollection();
      newTreasureCollection.treasureDropsCollection = treasureData;

      XmlSerializer ser = new XmlSerializer(newTreasureCollection.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, newTreasureCollection);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateBiomeTreasureDrops(biomeType, longString);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public void loadAllDataFiles () {
      XmlLoadingPanel.self.startLoading();
      treasureDropsCollection = new Dictionary<Biome.Type, List<TreasureDropsData>>();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> xmlPairList = DB_Main.getBiomeTreasureDrops();
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in xmlPairList) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               TreasureDropsCollection treasureCollectionData = Util.xmlLoad<TreasureDropsCollection>(newTextAsset);
               Biome.Type newBiomeType = (Biome.Type) xmlPair.xmlId;

               if (!treasureDropsCollection.ContainsKey(newBiomeType)) {
                  List<TreasureDropsData> newTreasureDropsData = new List<TreasureDropsData>();
                  foreach (TreasureDropsData treasureDrop in treasureCollectionData.treasureDropsCollection) {
                     newTreasureDropsData.Add(treasureDrop);
                  }
                  treasureDropsCollection.Add(newBiomeType, newTreasureDropsData);
               }
            }

            // Makes sure to populate the biomes that does not exist in the database
            foreach (Biome.Type biomeType in Enum.GetValues(typeof(Biome.Type))) {
               if (!treasureDropsCollection.ContainsKey(biomeType)) {
                  treasureDropsCollection.Add(biomeType, new List<TreasureDropsData>());
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
