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
   public List<BattlerData> rawMonsterDataList;

   // Direct reference to ability manage for enemy skills
   public AbilityManager abilityManager;

   // Direct reference to ability inventory for player skills
   public AbilityInventory abilityInventory;

   // Direct reference to battleManager
   public BattleManager battleManager;

   #endregion

   public void Awake () {
      self = this;

#if IS_SERVER_BUILD// && !UNITY_EDITOR
      initializeCraftCache();
#else
      monsterDataAssets = null;
#endif
   }

   public void translateRawDataToBattlerData (Enemy.Type enemyType, BattlerData mainData) {
      BattlerData rawData = rawMonsterDataList.Find(_ => _.enemyType == enemyType);
      if(rawData == null) {
         return;
      }

      mainData.enemyName = rawData.enemyName;
      mainData.baseHealth = rawData.baseHealth;
      mainData.baseDefense = rawData.baseDefense;
      mainData.baseDamage = rawData.baseDamage;
      mainData.baseGoldReward = rawData.baseGoldReward;
      mainData.baseXPReward = rawData.baseXPReward;

      mainData.damagePerLevel = rawData.damagePerLevel;
      mainData.defensePerLevel = rawData.defensePerLevel;
      mainData.healthPerlevel = rawData.healthPerlevel;

      RawGenericLootData newLootData = new RawGenericLootData();
      newLootData.defaultLoot = rawData.battlerLootData.defaultLoot;
      newLootData.lootList = rawData.battlerLootData.lootList;
      newLootData.maxQuantity = rawData.battlerLootData.maxQuantity;
      newLootData.minQuantity = rawData.battlerLootData.minQuantity;
      mainData.battlerLootData = (newLootData);

      mainData.physicalDefenseMultiplier = rawData.physicalDefenseMultiplier;
      mainData.fireDefenseMultiplier = rawData.fireDefenseMultiplier;
      mainData.earthDefenseMultiplier = rawData.earthDefenseMultiplier;
      mainData.airDefenseMultiplier = rawData.airDefenseMultiplier;
      mainData.waterDefenseMultiplier = rawData.waterDefenseMultiplier;
      mainData.allDefenseMultiplier = rawData.allDefenseMultiplier;

      mainData.physicalAttackMultiplier = rawData.physicalAttackMultiplier;
      mainData.fireAttackMultiplier = rawData.fireAttackMultiplier;
      mainData.earthAttackMultiplier = rawData.earthAttackMultiplier;
      mainData.airAttackMultiplier = rawData.airAttackMultiplier;
      mainData.waterAttackMultiplier = rawData.waterAttackMultiplier;
      mainData.allAttackMultiplier = rawData.allAttackMultiplier;

      mainData.preContactLength = rawData.preContactLength;
      mainData.preMagicLength = rawData.preMagicLength;
   }

   public BattlerData getMonster (Enemy.Type enemyType, int itemType) {
      return _monsterData[enemyType];
   }

   public void receiveListFromServer (BattlerData[] rawDataList) {
      if (!hasInitialized) {
         hasInitialized = true;
         rawMonsterDataList = new List<BattlerData>();
         foreach (BattlerData rawData in rawDataList) {
            rawMonsterDataList.Add(rawData);
         }
      }
   }

   public List<BattlerData> getAllMonsterData () {
      List<BattlerData> monsterList = new List<BattlerData>();
      foreach (KeyValuePair<Enemy.Type, BattlerData> item in _monsterData) {
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
            BattlerData monsterData = Util.xmlLoad<BattlerData>(textAsset);
            Enemy.Type typeID = (Enemy.Type) monsterData.enemyType;

            foreach(BasicAbilityData basicAbility in monsterData.battlerAbilities.BasicAbilityDataList) {
               if (typeID == Enemy.Type.Humanoid) {
                  abilityInventory.addPlayerAbility(basicAbility);
               } else {
                  abilityManager.addNewAbility(basicAbility);
               }
            }

            // Save the monster data in the memory cache
            if (!_monsterData.ContainsKey(typeID)) {
               _monsterData.Add(typeID, monsterData);
            }
            battleManager.registerBattler(monsterData);
         }

         rawMonsterDataList = getAllMonsterData();
      }
   }

   #region Private Variables

   // The cached monster data 
   private Dictionary<Enemy.Type, BattlerData> _monsterData = new Dictionary<Enemy.Type, BattlerData>();

   #endregion
}
