using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class MonsterManager : XmlManager {
   #region Public Variables

   // Self
   public static MonsterManager self;
   
   // Determines if the list is generated already
   public bool hasInitialized;

   // Direct reference to ability manager for enemy skills
   public AbilityManager abilityManager;

   // Direct reference to ability inventory for player skills
   public AbilityInventory abilityInventory;

   // Direct reference to battleManager
   public BattleManager battleManager;

   // Request access to monster data list
   public List<BattlerData> monsterDataList { get { return _monsterDataList; } }

   #endregion

   public void Awake () {
      self = this;

#if IS_SERVER_BUILD
      initializeLandMonsterDataCache();
#else
      monsterDataAssets = null;
#endif
   }

   public BattlerData requestBattler (Enemy.Type enemyType) {
      return _monsterDataList.Find(_ => _.enemyType == enemyType);
   }

   public void translateRawDataToBattlerData (Enemy.Type enemyType, BattlerData mainData) {
      BattlerData rawData = _monsterDataList.Find(_ => _.enemyType == enemyType);
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
      return _monsterDataDict[enemyType];
   }

   public void receiveListFromServer (BattlerData[] battlerDataList) {
      if (!hasInitialized) {
         hasInitialized = true;
         _monsterDataList = new List<BattlerData>();
         foreach (BattlerData battlerData in battlerDataList) {
            _monsterDataList.Add(battlerData);
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

   private void initializeLandMonsterDataCache () {
      _monsterDataDict = new Dictionary<Enemy.Type, BattlerData>();

      if (!hasInitialized) {
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in textAssets) {
            // Read and deserialize the file
            BattlerData monsterData = Util.xmlLoad<BattlerData>(textAsset);
            Enemy.Type typeID = (Enemy.Type) monsterData.enemyType;

            if (monsterData.battlerAbilities.basicAbilityDataList != null) {
               foreach (BasicAbilityData basicAbility in monsterData.battlerAbilities.basicAbilityDataList) {
                  if (typeID == Enemy.Type.Humanoid) {
                     abilityManager.addNewAbility(basicAbility);
                  } else {
                     abilityManager.addNewAbility(basicAbility);
                  }
               }
            } else {
               Debug.LogError("There is no Basic List for: " + typeID);
            }

            // Save the monster data in the memory cache
            if (!_monsterDataDict.ContainsKey(typeID)) {
               _monsterDataDict.Add(typeID, monsterData);
            }

            if (battleManager != null) {
               battleManager.registerBattler(monsterData);
            }
         }

         _monsterDataList = getAllMonsterData();
      }
   }

   public override void loadAllXMLData () {
      base.loadAllXMLData();
      loadXMLData(MonsterToolManager.FOLDER_PATH);
   }

   public override void clearAllXMLData () {
      base.clearAllXMLData();
      textAssets = new List<TextAsset>();
   }

   #region Private Variables

   // The cached monster data 
   private Dictionary<Enemy.Type, BattlerData> _monsterDataDict = new Dictionary<Enemy.Type, BattlerData>();

   // Holds the list of the xml translated data
   [SerializeField] protected List<BattlerData> _monsterDataList;

   #endregion
}
