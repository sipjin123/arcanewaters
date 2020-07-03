using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class TreasureDropsDataManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static TreasureDropsDataManager self;

   // Cached drops list
   public Dictionary<int, LootGroupData> lootDropsCollection = new Dictionary<int, LootGroupData>();

   #endregion

   private void Awake () {
      self = this;
   }

   public void receiveListFromZipData (Dictionary<int, LootGroupData> zipData) {
      lootDropsCollection = new Dictionary<int, LootGroupData>();
      _lootDropsList = new List<LootGroupData>();
      foreach (KeyValuePair<int, LootGroupData> lootDropCollection in zipData) {
         if (!lootDropsCollection.ContainsKey(lootDropCollection.Key)) {
            List<TreasureDropsData> newTreasureDropsData = new List<TreasureDropsData>();
            foreach (TreasureDropsData treasureDrop in lootDropCollection.Value.treasureDropsCollection) {
               newTreasureDropsData.Add(treasureDrop);
            }
            lootDropsCollection.Add(lootDropCollection.Key, lootDropCollection.Value);

            // TODO: Remove after successful implementation
            LootGroupData newTreasureCollection = new LootGroupData {
               biomeType = lootDropCollection.Value.biomeType,
               lootGroupName = lootDropCollection.Value.lootGroupName,
               xmlId = lootDropCollection.Key,
               treasureDropsCollection = newTreasureDropsData
            };
            _lootDropsList.Add(newTreasureCollection);
         }
      }
   }

   public List<TreasureDropsData> getTreasureDropsFromBiome (Biome.Type biomeType) {
      List<LootGroupData> biomeLoots = lootDropsCollection.Values.ToList().FindAll(_ => _.biomeType == biomeType);
      List<TreasureDropsData> newTreasureDropList = new List<TreasureDropsData>();

      // Collect all data from loot groups of the same biome
      if (biomeLoots.Count > 0) {
         foreach (LootGroupData lootGroups in biomeLoots) {
            foreach (TreasureDropsData lootData in lootGroups.treasureDropsCollection) {
               newTreasureDropList.Add(lootData);
            }
         }
         return newTreasureDropList;
      }
      return LootGroupData.DEFAULT_LOOT_GROUP.treasureDropsCollection; 
   }

   public List<TreasureDropsData> getTreasureDropsById (int groupId) {
      if (lootDropsCollection.ContainsKey(groupId)) {
         LootGroupData groupDataLoots = lootDropsCollection[groupId];

         // Collect all data from loot groups of the same biome
         if (groupDataLoots != null && groupDataLoots.treasureDropsCollection.Count > 0) {
            return groupDataLoots.treasureDropsCollection;
         }
      }
      return LootGroupData.DEFAULT_LOOT_GROUP.treasureDropsCollection;
   }

   public void initializeServerDataCache () {
      lootDropsCollection = new Dictionary<int, LootGroupData>();
      _lootDropsList = new List<LootGroupData>();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> xmlPairList = DB_Main.getBiomeTreasureDrops();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in xmlPairList) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               LootGroupData lootGroupData = Util.xmlLoad<LootGroupData>(newTextAsset);
               int uniqueKey = xmlPair.xmlId;

               if (!lootDropsCollection.ContainsKey(uniqueKey)) {
                  List<TreasureDropsData> newTreasureDropsData = new List<TreasureDropsData>();
                  foreach (TreasureDropsData treasureDrop in lootGroupData.treasureDropsCollection) {
                     newTreasureDropsData.Add(treasureDrop);
                  }
                  lootDropsCollection.Add(uniqueKey, lootGroupData);

                  // TODO: Remove after successful implementation
                  _lootDropsList.Add(lootGroupData);
               }
            }
         });
      });
   }

   #region Private Variables

   // The list of treasure drops collection for editor preview
   [SerializeField]
   private List<LootGroupData> _lootDropsList = new List<LootGroupData>();

   #endregion
}
