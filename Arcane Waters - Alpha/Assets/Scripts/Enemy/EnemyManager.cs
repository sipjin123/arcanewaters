using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using Random = UnityEngine.Random;

public class EnemyManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static EnemyManager self;
   
   #endregion

   public void Awake () {
      self = this;
   }

   public void Start () {
      // Create empty lists for each Area Type
      foreach (string areaKey in AreaManager.self.getAreaKeys()) {
         _spawners[areaKey] = new List<Enemy_Spawner>();
      }
   }

   public void storeSpawner (Enemy_Spawner spawner, string areaKey) {
      if (!_spawners.ContainsKey(areaKey)) {
         _spawners.Add(areaKey, new List<Enemy_Spawner>());
      }
      List<Enemy_Spawner> list = _spawners[areaKey];
      list.Add(spawner);
      _spawners[areaKey] = list;
   }

   public void removeSpawners (string areaKey) {
      List<Enemy_Spawner> list;
      if (_spawners.TryGetValue(areaKey, out list)) {
         list.Clear();
         _spawners[areaKey] = list;
      }
   }

    public void spawnEnemyAtLocation(Enemy.Type enemyType, Instance instance, Vector2 location) {
        // If we don't have any spawners defined for this Area, then we're done
        if (instance == null)
        {
            D.warning("Invalid instance received when trying to spawn enemy " + enemyType);
            return;
        }

        // Create an Enemy in this instance
        Enemy enemy = Instantiate(PrefabsManager.self.enemyPrefab);
        enemy.transform.localPosition = location;
        enemy.enemyType = enemyType;
        enemy.areaKey = instance.areaKey;
        enemy.setAreaParent(AreaManager.self.getArea(instance.areaKey), false);

        BattlerData battlerData = MonsterManager.self.getBattlerData(enemy.enemyType);
        if (battlerData != null)
        {
            enemy.isBossType = battlerData.isBossType;
            enemy.isSupportType = battlerData.isSupportType;
            enemy.animGroupType = battlerData.animGroup;
            enemy.facing = Direction.South;
            enemy.displayNameText.text = battlerData.enemyName;
        }

        // Add it to the Instance
        InstanceManager.self.addEnemyToInstance(enemy, instance);
        NetworkServer.Spawn(enemy.gameObject);
    }

    public void spawnEnemiesOnServerForInstance (Instance instance) {
       // If we don't have any spawners defined for this Area, then we're done
       if (instance == null || !_spawners.ContainsKey(instance.areaKey)) {
          return;
       }

       foreach (Enemy_Spawner spawner in _spawners[instance.areaKey]) {
          // Create an Enemy in this instance
          Enemy enemy = Instantiate(PrefabsManager.self.enemyPrefab);
          enemy.transform.localPosition = spawner.transform.localPosition;
          enemy.enemyType = spawner.getEnemyType(instance.biome);
          enemy.areaKey = instance.areaKey;
          enemy.setAreaParent(AreaManager.self.getArea(instance.areaKey), false);

          BattlerData battlerData = MonsterManager.self.getBattlerData(enemy.enemyType);
          if (battlerData != null) {
             enemy.isBossType = battlerData.isBossType;
             enemy.isSupportType = battlerData.isSupportType;
             enemy.animGroupType = battlerData.animGroup;
             enemy.facing = Direction.South;
             enemy.displayNameText.text = battlerData.enemyName;
          }

          // Add it to the Instance
          InstanceManager.self.addEnemyToInstance(enemy, instance);
          NetworkServer.Spawn(enemy.gameObject);
       }
    }

   public void spawnShipsOnServerForInstance (Instance instance) {
      // If we don't have any spawners defined for this Area, then we're done
      if (instance == null || !_spawners.ContainsKey(instance.areaKey)) {
         return;
      }

      int guildId = BotShipEntity.PIRATES_GUILD_ID;
      Area area = AreaManager.self.getArea(instance.areaKey);
      int randomEnemyTypeVal = Random.Range(0, 100);

      foreach (Enemy_Spawner spawner in _spawners[instance.areaKey]) {
         Ship.Type shipType = randomizeShipType(instance.biome);
         SeaMonsterEntityData seaEnemyData = SeaMonsterManager.self.getAllSeaMonsterData().Find(ent => ent.subVarietyTypeId == (int)shipType);

         if (seaEnemyData == null) {
            D.debug("Ship type {" + shipType + "} does not have matching data registered in sea enemy manager!");
            continue;
         }

         // Spawning ships has a 60% chance
         if (randomEnemyTypeVal < 60) {
            BotShipEntity botShip = Instantiate(PrefabsManager.self.botShipPrefab);
            botShip.areaKey = instance.areaKey;
            botShip.facing = Direction.South;
            botShip.setAreaParent(area, false);
            botShip.transform.localPosition = spawner.transform.localPosition;

            botShip.seaEntityData = seaEnemyData;
            botShip.maxHealth = seaEnemyData.maxHealth;
            botShip.currentHealth = seaEnemyData.maxHealth;

            botShip.shipType = shipType;
            if (seaEnemyData.skillIdList.Count > 0) {
               botShip.primaryAbilityId = seaEnemyData.skillIdList[0];
            }
            botShip.guildId = guildId;
            botShip.setShipData(seaEnemyData.xmlId, shipType, instance.difficulty);

            InstanceManager.self.addSeaMonsterToInstance(botShip, instance);
            NetworkServer.Spawn(botShip.gameObject);
         } else {
            // Spawn sea monster type based on biome
            SeaMonsterEntity.Type seaMonsterType = SeaMonsterEntity.Type.None;
            switch (instance.biome) {
               case Biome.Type.Forest:
               case Biome.Type.Pine:
                  seaMonsterType = SeaMonsterEntity.Type.Fishman;
                  break;
               case Biome.Type.Lava:
                  seaMonsterType = SeaMonsterEntity.Type.Reef_Giant;
                  break;
               case Biome.Type.Desert:
                  seaMonsterType = SeaMonsterEntity.Type.Worm;
                  break;
               default:
                  D.debug("No specific monster for biome: {" + instance.biome + "} Spawning default Fishman");
                  seaMonsterType = SeaMonsterEntity.Type.Fishman;
                  break;
            }

            SeaMonsterEntity seaEntity = Instantiate(PrefabsManager.self.seaMonsterPrefab);
            SeaMonsterEntityData data = SeaMonsterManager.self.getMonster(seaMonsterType);

            // Basic setup
            seaEntity.monsterType = data.seaMonsterType;
            seaEntity.areaKey = instance.areaKey;
            seaEntity.facing = Direction.South;

            // Transform setup
            seaEntity.setAreaParent(area, true);
            seaEntity.transform.localPosition = spawner.transform.localPosition;

            // Update stats based on difficulty
            seaEntity.difficultyLevel = instance.difficulty;

            // Network Setup
            InstanceManager.self.addSeaMonsterToInstance(seaEntity, instance);
            NetworkServer.Spawn(seaEntity.gameObject);
         }
      }
   }

   private Ship.Type randomizeShipType (Biome.Type biome) {
      Ship.Type shipType = Ship.Type.Type_1;

      switch (biome) {
         case Biome.Type.Forest:
            shipType = Ship.Type.Type_1;
            break;
         case Biome.Type.Desert:
            shipType = new List<Ship.Type> { Ship.Type.Type_1, Ship.Type.Type_2, Ship.Type.Type_3 }.ChooseRandom();
            break;
         case Biome.Type.Pine:
            shipType = new List<Ship.Type> { Ship.Type.Type_1, Ship.Type.Type_2 }.ChooseRandom();
            break;
         case Biome.Type.Snow:
            shipType = new List<Ship.Type> { Ship.Type.Type_2, Ship.Type.Type_3, Ship.Type.Type_4 }.ChooseRandom();
            break;
         case Biome.Type.Lava:
            shipType = new List<Ship.Type> { Ship.Type.Type_4, Ship.Type.Type_5, Ship.Type.Type_6 }.ChooseRandom();
            break;
         case Biome.Type.Mushroom:
            shipType = new List<Ship.Type> { Ship.Type.Type_6, Ship.Type.Type_7, Ship.Type.Type_8 }.ChooseRandom();
            break;
      }

      return shipType;
   }

   public int randomizeShipXmlId (Biome.Type biome) {
      int xmlId = 0;
      List<SeaMonsterEntityData> seaEnemyList = SeaMonsterManager.self.seaMonsterDataList.FindAll(_ => _.seaMonsterType == SeaMonsterEntity.Type.PirateShip);

      switch (biome) {
         case Biome.Type.Forest:
            xmlId = 17;
            break;
         case Biome.Type.Desert:
            xmlId = new List<int> { 
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_1).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_2).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_3).xmlId
            }.ChooseRandom();
            break;
         case Biome.Type.Pine:
            xmlId = new List<int> {
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_1).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_2).xmlId
            }.ChooseRandom();
            break;
         case Biome.Type.Snow:
            xmlId = new List<int> {
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_2).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_3).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_4).xmlId
            }.ChooseRandom();
            break;
         case Biome.Type.Lava:
            xmlId = new List<int> {
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_4).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_5).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_6).xmlId
            }.ChooseRandom();
            break;
         case Biome.Type.Mushroom:
            xmlId = new List<int> {
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_6).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_7).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_8).xmlId
            }.ChooseRandom();
            break;
      }

      return xmlId;
   }

   #region Private Variables

   // Stores a list of Enemy Spawners for each type of Site
   protected Dictionary<string, List<Enemy_Spawner>> _spawners = new Dictionary<string, List<Enemy_Spawner>>();

   #endregion
}