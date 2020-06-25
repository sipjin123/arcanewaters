using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TreasureDropsDataManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static TreasureDropsDataManager self;

   // Cached drops list
   public Dictionary<Biome.Type, List<TreasureDropsData>> treasureDropsCollection = new Dictionary<Biome.Type, List<TreasureDropsData>>();

   #endregion

   private void Awake () {
      self = this;
   }

   public void receiveListFromZipData (Dictionary<Biome.Type, List<TreasureDropsData>> zipData) {
      treasureDropsCollection = new Dictionary<Biome.Type, List<TreasureDropsData>>();
      _treasureDropsList = new List<TreasureDropsCollection>();
      foreach (KeyValuePair<Biome.Type, List<TreasureDropsData>> biomeDropCollection in zipData) {
         if (!treasureDropsCollection.ContainsKey(biomeDropCollection.Key)) {
            List<TreasureDropsData> newTreasureDropsData = new List<TreasureDropsData>();
            foreach (TreasureDropsData treasureDrop in biomeDropCollection.Value) {
               newTreasureDropsData.Add(treasureDrop);
            }
            treasureDropsCollection.Add(biomeDropCollection.Key, newTreasureDropsData);

            // TODO: Remove after successful implementation
            TreasureDropsCollection newTreasureCollection = new TreasureDropsCollection {
               biomeType = biomeDropCollection.Key,
               treasureDropsCollection = newTreasureDropsData
            };
            _treasureDropsList.Add(newTreasureCollection);
         }
      }
   }

   public List<TreasureDropsData> getTreasureDropsFromBiome (Biome.Type biomeType) {
      if (treasureDropsCollection.ContainsKey(biomeType)) {
         return treasureDropsCollection[biomeType];
      }
      return new List<TreasureDropsData>(); 
   }

   public void initializeServerDataCache () {
      treasureDropsCollection = new Dictionary<Biome.Type, List<TreasureDropsData>>();
      _treasureDropsList = new List<TreasureDropsCollection>();
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

                  // TODO: Remove after successful implementation
                  treasureCollectionData.biomeType = newBiomeType;
                  _treasureDropsList.Add(treasureCollectionData);
               }
            }
         });
      });
   }

   #region Private Variables

   // The list of treasure drops collection for editor preview
   [SerializeField]
   private List<TreasureDropsCollection> _treasureDropsList = new List<TreasureDropsCollection>();

   #endregion
}
