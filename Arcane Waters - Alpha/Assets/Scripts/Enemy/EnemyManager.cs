using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class EnemyManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static EnemyManager self;

   // The data of the seamonster by type
   public List<SeaMonsterDataPair> seaMonsterPair;

   #endregion

   public void Awake () {
      self = this;

      // Create empty lists for each Area Type
      foreach (Area.Type areaType in System.Enum.GetValues(typeof(Area.Type))) {
         _spawners[areaType] = new List<Enemy_Spawner>();
      }
   }

   public void storeSpawner (Enemy_Spawner spawner, Area.Type areaType) {
      List<Enemy_Spawner> list = _spawners[areaType];
      list.Add(spawner);
      _spawners[areaType] = list;
   }

   public void spawnEnemiesOnServerForInstance (Instance instance) {
      // If we don't have any spawners defined for this Area, then we're done
      if (!_spawners.ContainsKey(instance.areaType)) {
         D.log("No Enemy Spawners defined for Area Type: " + instance.areaType);
         return;
      }

      foreach (Enemy_Spawner spawner in _spawners[instance.areaType]) {
         // Create an Enemy in this instance
         Enemy enemy = Instantiate(PrefabsManager.self.enemyPrefab);
         enemy.enemyType = spawner.enemyType;

         // Add it to the Instance
         InstanceManager.self.addPlayerToInstance(enemy, instance.areaType);
         enemy.transform.position = spawner.transform.position;
         enemy.desiredPosition = enemy.transform.position;
         NetworkServer.Spawn(enemy.gameObject);
      }
   }

   #region Private Variables

   // Stores a list of Enemy Spawners for each type of Site
   protected Dictionary<Area.Type, List<Enemy_Spawner>> _spawners = new Dictionary<Area.Type, List<Enemy_Spawner>>();

   #endregion
}

[Serializable]
public class SeaMonsterDataPair
{
   // The type of seamonster
   public Enemy.Type seaMonsterType;
   
   // The data to assign to the seamonster
   public SeaMonsterEntityData seaMonsterData; 
}