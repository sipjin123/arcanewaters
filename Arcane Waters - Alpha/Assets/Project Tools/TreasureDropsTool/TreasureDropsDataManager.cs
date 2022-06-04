using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using UnityEngine.Events;

public class TreasureDropsDataManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static TreasureDropsDataManager self;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Cached drops list
   public Dictionary<int, LootGroupData> lootDropsCollection = new Dictionary<int, LootGroupData>();

   // Unity event after finishing data setup
   public UnityEvent finishedDataSetup = new UnityEvent();

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
               newTreasureDropsData = validateLootContents(newTreasureDropsData, lootDropCollection.Key);
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
      hasInitialized = true;
      finishedDataSetup.Invoke();
   }

   private List<TreasureDropsData> validateLootContents (List<TreasureDropsData> existingList, int xmlId) {
      if (existingList == null) {
         D.debug("Missing list for treasure data {" + xmlId + "}!");
         return new List<TreasureDropsData>();
      }
      List<TreasureDropsData> fetchedList = existingList.FindAll(_ => _.item != null && _.item.category == Item.Category.Blueprint && _.item.data.Length < 1);
      int invalidContentCount = fetchedList.Count;
      if (fetchedList == null) {
         D.debug("Null list for treasure data {" + xmlId + "}!");
         return new List<TreasureDropsData>();
      }

      foreach (TreasureDropsData bluePrintItem in fetchedList) {
         CraftableItemRequirements craftingData = CraftingManager.self.getCraftableData(bluePrintItem.item.itemTypeId);
         if (craftingData != null) {
            switch (craftingData.resultItem.category) {
               case Item.Category.Weapon:
                  bluePrintItem.item.data = Blueprint.WEAPON_DATA_PREFIX;
                  break;
               case Item.Category.Armor:
                  bluePrintItem.item.data = Blueprint.ARMOR_DATA_PREFIX;
                  break;
               case Item.Category.Hats:
                  bluePrintItem.item.data = Blueprint.HAT_DATA_PREFIX;
                  break;
               case Item.Category.Ring:
                  bluePrintItem.item.data = Blueprint.RING_DATA_PREFIX;
                  break;
               case Item.Category.Necklace:
                  bluePrintItem.item.data = Blueprint.NECKLACE_DATA_PREFIX;
                  break;
               case Item.Category.Trinket:
                  bluePrintItem.item.data = Blueprint.TRINKET_DATA_PREFIX;
                  break;
            }
         }
      }
      return existingList;
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

   public string getLootGroupName (int groupId) {
      if (lootDropsCollection.ContainsKey(groupId)) {
         LootGroupData groupDataLoots = lootDropsCollection[groupId];
         return groupDataLoots.lootGroupName;
      }
      return "";
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
            // This block of code makes sure to fetch items that matchest the next rarity group of this current rarity, loop through all rarity until reaches common
            if (rarity != Rarity.Type.Common && rarity != Rarity.Type.None) {
               int treasureDropCount = groupDataLoots.treasureDropsCollection.Count;
               if (treasureDropCount > 0) {
                  int nextRarityValue = (int) rarity;
                  int nextItemCountValue = 0;
                  while (nextItemCountValue == 0 && ((int) rarity) > 1) {
                     nextRarityValue -= 1;
                     List<TreasureDropsData> nextTreasureGroup = groupDataLoots.treasureDropsCollection.FindAll(_ => _.rarity == (Rarity.Type) nextRarityValue);
                     nextItemCountValue = nextTreasureGroup.Count;
                     if (nextItemCountValue > 0) {
                        return nextTreasureGroup;
                     }
                  }
               }
            }
            
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

                     newTreasureDropsData = validateLootContents(newTreasureDropsData, uniqueKey);
                     lootDropsCollection.Add(uniqueKey, lootGroupData);

                     // TODO: Remove after successful implementation
                     _lootDropsList.Add(lootGroupData);
                  } catch {
                     D.debug("Failed to process Loot drops: {" + uniqueKey + "}");
                  }
               }
            }

            hasInitialized = true;
            finishedDataSetup.Invoke();
         });
      });
   }

   #region Private Variables

   // The list of treasure drops collection for editor preview
   [SerializeField]
   private List<LootGroupData> _lootDropsList = new List<LootGroupData>();

   #endregion
}
