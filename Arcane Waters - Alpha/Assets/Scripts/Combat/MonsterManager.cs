using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Linq;

public class MonsterManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static MonsterManager self;
   
   // Direct reference to ability manager for enemy skills
   public AbilityManager abilityManager;

   // Direct reference to ability inventory for player skills
   public AbilityInventory abilityInventory;

   // Direct reference to battleManager
   public BattleManager battleManager;

   // Request access to monster data list
   public List<BattlerData> monsterDataList { get { return _monsterDataDict.Values.ToList(); } }

   #endregion

   public void Awake () {
      self = this;

   }

   public BattlerData requestBattler (Enemy.Type enemyType) {
      if (_monsterDataDict.ContainsKey(enemyType)) {
         return _monsterDataDict[enemyType];
      }
      return null;
   }

   public void translateRawDataToBattlerData (Enemy.Type enemyType, BattlerData mainData) {
      BattlerData rawData = _monsterDataDict[enemyType];
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

      mainData.baseDefenseMultiplierSet.physicalDefenseMultiplier = rawData.baseDefenseMultiplierSet.physicalDefenseMultiplier;
      mainData.baseDefenseMultiplierSet.fireDefenseMultiplier = rawData.baseDefenseMultiplierSet.fireDefenseMultiplier;
      mainData.baseDefenseMultiplierSet.earthDefenseMultiplier = rawData.baseDefenseMultiplierSet.earthDefenseMultiplier;
      mainData.baseDefenseMultiplierSet.airDefenseMultiplier = rawData.baseDefenseMultiplierSet.airDefenseMultiplier;
      mainData.baseDefenseMultiplierSet.waterDefenseMultiplier = rawData.baseDefenseMultiplierSet.waterDefenseMultiplier;
      mainData.baseDefenseMultiplierSet.allDefenseMultiplier = rawData.baseDefenseMultiplierSet.allDefenseMultiplier;

      mainData.baseDamageMultiplierSet.physicalAttackMultiplier = rawData.baseDamageMultiplierSet.physicalAttackMultiplier;
      mainData.baseDamageMultiplierSet.fireAttackMultiplier = rawData.baseDamageMultiplierSet.fireAttackMultiplier;
      mainData.baseDamageMultiplierSet.earthAttackMultiplier = rawData.baseDamageMultiplierSet.earthAttackMultiplier;
      mainData.baseDamageMultiplierSet.airAttackMultiplier = rawData.baseDamageMultiplierSet.airAttackMultiplier;
      mainData.baseDamageMultiplierSet.waterAttackMultiplier = rawData.baseDamageMultiplierSet.waterAttackMultiplier;
      mainData.baseDamageMultiplierSet.allAttackMultiplier = rawData.baseDamageMultiplierSet.allAttackMultiplier;

      mainData.preContactLength = rawData.preContactLength;
      mainData.preMagicLength = rawData.preMagicLength;
   }

   public BattlerData getMonster (Enemy.Type enemyType) {
      if (!_monsterDataDict.ContainsKey(enemyType)) {
         D.warning("Enemy type is not registered: " + enemyType);
         return null;
      }

      return _monsterDataDict[enemyType];
   }

   public void receiveListFromServer (BattlerData[] battlerDataList) {
      foreach (BattlerData battlerData in battlerDataList) {
         if (!_monsterDataDict.ContainsKey(battlerData.enemyType)) {
            _monsterDataDict.Add(battlerData.enemyType, battlerData);
         }
      }
   }

   public List<BattlerData> getAllMonsterData () {
      List<BattlerData> monsterList = new List<BattlerData>();
      foreach (KeyValuePair<Enemy.Type, BattlerData> item in _monsterDataDict) {
         monsterList.Add(item.Value);
      }
      return monsterList;
   }

   public List<int> getAllEnemyTypes () {
      List<int> typeList = new List<int>();
      foreach (KeyValuePair<Enemy.Type, BattlerData> item in _monsterDataDict) {
         typeList.Add((int)item.Key);
      }
      return typeList;
   }

   public void initializeLandMonsterDataCache () {
      _monsterDataDict = new Dictionary<Enemy.Type, BattlerData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getLandMonsterXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               BattlerData monsterData = Util.xmlLoad<BattlerData>(newTextAsset);
               Enemy.Type typeID = (Enemy.Type) monsterData.enemyType;

               if (monsterData.battlerAbilities.basicAbilityDataList != null) {
                  foreach (BasicAbilityData basicAbility in monsterData.battlerAbilities.basicAbilityDataList) {
                     abilityManager.addNewAbility(basicAbility);
                  }
               }

               // Save the monster data in the memory cache
               if (!_monsterDataDict.ContainsKey(typeID)) {
                  _monsterDataDict.Add(typeID, monsterData);
               }

               if (battleManager != null) {
                  battleManager.registerBattler(monsterData);
               }
            }

            RewardManager.self.initLandMonsterLootList();
         });
      });
   }

   #region Private Variables

   // The cached monster data 
   private Dictionary<Enemy.Type, BattlerData> _monsterDataDict = new Dictionary<Enemy.Type, BattlerData>();

   #endregion
}
