using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MonsterManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static MonsterManager self;

   // The files containing the crafting data
   public TextAsset[] monsterDataAssets;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data
   public List<MonsterRawData> rawMonsterDataList;

   #endregion

   public void Awake () {
      self = this;

#if IS_SERVER_BUILD && !UNITY_EDITOR
      initializeCraftCache();
#else
      monsterDataAssets = null;
#endif
   }

   public void translateRawDataToBattlerData (Enemy.Type enemyType, BattlerData mainData) {
      MonsterRawData rawData = rawMonsterDataList.Find(_ => _.battlerID == enemyType);
      if(rawData == null) {
         return;
      }

      mainData.setEnemyName(rawData.enemyName);
      mainData.setBaseHealth(rawData.baseHealth);
      mainData.setBaseDefense(rawData.baseDefense);
      mainData.setBaseDamage(rawData.baseDamage);
      mainData.setBaseGoldReward(rawData.baseGoldReward);
      mainData.setBaseXPReward(rawData.baseXPReward);

      mainData.setDamagePerLevel(rawData.damagePerLevel);
      mainData.setDefensePerLevel(rawData.defensePerLevel);
      mainData.setHealthPerLevel(rawData.healthPerlevel);

      GenericLootData newLootData = GenericLootData.CreateInstance<GenericLootData>();
      newLootData.defaultLoot = rawData.battlerLootData.defaultLoot;
      newLootData.lootList = new List<LootInfo>(rawData.battlerLootData.lootList);
      newLootData.maxQuantity = rawData.battlerLootData.maxQuantity;
      newLootData.minQuantity = rawData.battlerLootData.minQuantity;
      mainData.setLootData(newLootData);

      mainData.setPhysicalDefMultiplier(rawData.physicalDefenseMultiplier);
      mainData.setFireDefMultiplier(rawData.fireDefenseMultiplier);
      mainData.setEarthDefMultiplier(rawData.earthDefenseMultiplier);
      mainData.setAirDefMultiplier(rawData.airDefenseMultiplier);
      mainData.setWaterDefMultiplier(rawData.waterDefenseMultiplier);
      mainData.setAllDefMultiplier(rawData.allDefenseMultiplier);

      mainData.setPhysicalAtkMultiplier(rawData.physicalAttackMultiplier);
      mainData.setFireAtkMultiplier(rawData.fireAttackMultiplier);
      mainData.setEarthAtkMultiplier(rawData.earthAttackMultiplier);
      mainData.setAirAtkMultiplier(rawData.airAttackMultiplier);
      mainData.setWaterAtkMultiplier(rawData.waterAttackMultiplier);
      mainData.setAllAtkMultiplier(rawData.allAttackMultiplier);

      mainData.setPreContactLength(rawData.preContactLength);
      mainData.setPreMagicLength(rawData.preMagicLength);
   }

   public MonsterRawData getMonster (Enemy.Type enemyType, int itemType) {
      return _monsterData[enemyType];
   }

   public void receiveListFromServer (MonsterRawData[] rawDataList) {
      if (!hasInitialized) {
         hasInitialized = true;
         rawMonsterDataList = new List<MonsterRawData>();
         foreach (MonsterRawData rawData in rawDataList) {
            rawMonsterDataList.Add(rawData);
         }
      }
   }

   public List<MonsterRawData> getAllMonsterData () {
      List<MonsterRawData> monsterList = new List<MonsterRawData>();
      foreach (KeyValuePair<Enemy.Type, MonsterRawData> item in _monsterData) {
         monsterList.Add(item.Value);
      }
      return monsterList;
   }

   private void initializeCraftCache () {
      if (!hasInitialized) {
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in monsterDataAssets) {
            // Read and deserialize the file
            MonsterRawData monsterData = Util.xmlLoad<MonsterRawData>(textAsset);
            Enemy.Type typeID = (Enemy.Type) monsterData.battlerID;

            // Save the monster data in the memory cache
            if (!_monsterData.ContainsKey(typeID)) {
               _monsterData.Add(typeID, monsterData);
            }
         }

         rawMonsterDataList = getAllMonsterData();
         /////RewardManager.self.craftableDataList = getAllCraftableData();
      }
   }

#region Private Variables

   // The cached monster data 
   private Dictionary<Enemy.Type, MonsterRawData> _monsterData = new Dictionary<Enemy.Type, MonsterRawData>();

#endregion
}
