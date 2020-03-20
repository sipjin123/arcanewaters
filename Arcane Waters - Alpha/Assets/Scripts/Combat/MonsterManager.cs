﻿using UnityEngine;
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

   // Determined if data setup is finished
   public bool isInitialized;

   #endregion

   public void Awake () {
      self = this;
   }

   public void translateRawDataToBattlerData (Enemy.Type enemyType, BattlerData mainData) {
      BattlerData rawData = _monsterDataList.Find(_=>_.battler.enemyType == enemyType).battler;
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

   public BattlerData getBattler (Enemy.Type enemyType) {
      if (!_monsterDataList.Exists(_=>_.battler.enemyType == enemyType)) {
         D.warning("Enemy type is not registered: " + enemyType);
         return null;
      }

      return _monsterDataList.Find(_=>_.battler.enemyType == enemyType).battler;
   }

   public BattlerData getBattler (int battlerId) {
      if (!_monsterDataList.Exists(_=>_.xmlId == battlerId)) {
         D.warning("Enemy type is not registered: " + battlerId);
         return null;
      }

      return _monsterDataList.Find(_=>_.xmlId == battlerId).battler;
   }

   public BattlerData getCopyOfMonster (Enemy.Type enemyType) {
      BattlerData newBattlerData = BattlerData.CreateInstance(_monsterDataList.Find(_=>_.battler.enemyType == enemyType).battler);
      newBattlerData.battlerAbilities = AbilityDataRecord.CreateInstance(newBattlerData.battlerAbilities);
      return newBattlerData;
   }

   public void receiveListFromServer (BattlerData[] battlerDataList) {
      if (!isInitialized) {
         foreach (BattlerData battlerData in battlerDataList) {
            if (!_monsterDataList.Exists(_=>_.battler.enemyType == battlerData.enemyType)) {
               BattlerXMLContent newContent = new BattlerXMLContent {
                  battler = battlerData,
                  xmlId = -1,
                  isEnabled = true
               };
               _monsterDataList.Add(newContent);
            } else {
               _monsterDataList.Find(_=>_.battler.enemyType == battlerData.enemyType).battler = battlerData;
            }
         }
      }
   }

   public List<int> getAllEnemyTypes () {
      List<int> typeList = new List<int>();
      foreach (BattlerXMLContent content in _monsterDataList) {
         typeList.Add((int) content.battler.enemyType);
      }
      return typeList;
   }

   public void initializeLandMonsterDataCache () {
      _monsterDataList = new List<BattlerXMLContent>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getLandMonsterXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               BattlerData monsterData = Util.xmlLoad<BattlerData>(newTextAsset);

               if (monsterData.battlerAbilities.basicAbilityDataList != null) {
                  foreach (BasicAbilityData basicAbility in monsterData.battlerAbilities.basicAbilityDataList) {
                     abilityManager.addNewAbility(basicAbility);
                  }
               }

               // Save the monster data in the memory cache
               if (xmlPair.isEnabled) {
                  BattlerXMLContent newXmlContent = new BattlerXMLContent { 
                     battler = monsterData, 
                     xmlId = xmlPair.xmlId, 
                     isEnabled = true
                  };
                  _monsterDataList.Add(newXmlContent);
               }

               if (battleManager != null) {
                  battleManager.registerBattler(monsterData);
               }
            }
            isInitialized = true;
            RewardManager.self.initLandMonsterLootList();
         });
      });
   }

   public List<BattlerData> getMonsterDataList() {
      List<BattlerData> battlerDataList = new List<BattlerData>();
      foreach (BattlerXMLContent content in _monsterDataList) {
         battlerDataList.Add(content.battler);
      }
      return battlerDataList;
   }

   #region Private Variables

   // The cached monster data 
   [SerializeField]
   private List<BattlerXMLContent> _monsterDataList = new List<BattlerXMLContent>();

   #endregion
}
