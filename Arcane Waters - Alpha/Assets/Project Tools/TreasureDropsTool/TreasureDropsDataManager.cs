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
            try {
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
            } catch {
               D.debug("Failed to process Loot drops: {" + lootDropCollection.Key + "}");
            }
         }
      }
   }

   public List<TreasureDropsData> getTreasureDropsFromBiome (Biome.Type biomeType, Rarity.Type rarity) {
      List<LootGroupData> biomeLoots = lootDropsCollection.Values.ToList().FindAll(_ => _.biomeType == biomeType);
      if (biomeLoots.Count < 1) {
         return LootGroupData.DEFAULT_LOOT_GROUP.treasureDropsCollection;
      }

      List<TreasureDropsData> newTreasureDropList = new List<TreasureDropsData>();

      // Collect all data from loot groups of the same biome
      if (biomeLoots.Count > 0) {
         foreach (LootGroupData lootGroups in biomeLoots) {
            foreach (TreasureDropsData lootData in lootGroups.treasureDropsCollection) {
               if (lootData.rarity == rarity) { 
                  newTreasureDropList.Add(lootData);
               }
            }
         }

         if (newTreasureDropList.Count < 1) {
            return LootGroupData.DEFAULT_LOOT_GROUP.treasureDropsCollection;
         }
         return newTreasureDropList;
      }
      return LootGroupData.DEFAULT_LOOT_GROUP.treasureDropsCollection; 
   }

   public List<TreasureDropsData> getTreasureDropsById (int groupId) {
      if (lootDropsCollection.ContainsKey(groupId)) {
         LootGroupData groupDataLoots = lootDropsCollection[groupId];
         return groupDataLoots.treasureDropsCollection;
      }
      return LootGroupData.DEFAULT_LOOT_GROUP.treasureDropsCollection;
   }

   public List<TreasureDropsData> getTreasureDropsById (int groupId, Rarity.Type rarity) {
      if (lootDropsCollection.ContainsKey(groupId)) {
         LootGroupData groupDataLoots = lootDropsCollection[groupId];

         // Collect all data from loot groups of the same biome
         int dropsCount = groupDataLoots.treasureDropsCollection.FindAll(_ => _.rarity == rarity).Count;
         if (dropsCount > 0) {
            if (groupDataLoots != null && groupDataLoots.treasureDropsCollection.Count > 0) {
               D.adminLog("Found loot group Id: {" + groupId + "} "
                  + "Loot group name: {" + groupDataLoots.lootGroupName
                  + "} Total Loot Drops: {" + dropsCount + "}", D.ADMIN_LOG_TYPE.Treasure);
               return groupDataLoots.treasureDropsCollection.FindAll(_ => _.rarity == rarity);
            }
         } else {
            D.adminLog("This treasure drops data " +
               "{" + groupId + " : " + groupDataLoots.lootGroupName + "} " +
               "does not contain the rarity {" + rarity + "}", D.ADMIN_LOG_TYPE.Treasure);
            return new List<TreasureDropsData>();
         }
      } else {
         D.adminLog("No loot group with id {" + groupId + "} found", D.ADMIN_LOG_TYPE.Treasure);
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
                  try {
                     foreach (TreasureDropsData treasureDrop in lootGroupData.treasureDropsCollection) {
                        newTreasureDropsData.Add(treasureDrop);
                     }

                     lootDropsCollection.Add(uniqueKey, lootGroupData);

                     // TODO: Remove after successful implementation
                     _lootDropsList.Add(lootGroupData);
                  } catch {
                     D.debug("Failed to process Loot drops: {" + uniqueKey + "}");
                  }
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
