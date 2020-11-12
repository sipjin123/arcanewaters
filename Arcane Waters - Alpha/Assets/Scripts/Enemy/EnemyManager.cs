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

   public void spawnEnemiesOnServerForInstance (Instance instance) {
      // If we don't have any spawners defined for this Area, then we're done
      if (instance == null || !_spawners.ContainsKey(instance.areaKey)) {
         return;
      }

      foreach (Enemy_Spawner spawner in _spawners[instance.areaKey]) {
         // Create an Enemy in this instance
         Enemy enemy = Instantiate(PrefabsManager.self.enemyPrefab);
         enemy.enemyType = spawner.enemyType;

         // Add it to the Instance
         InstanceManager.self.addEnemyToInstance(enemy, instance);
         enemy.transform.position = spawner.transform.position;
         enemy.areaKey = instance.areaKey;
         NetworkServer.Spawn(enemy.gameObject);
      }
   }

   #region Private Variables

   // Stores a list of Enemy Spawners for each type of Site
   protected Dictionary<string, List<Enemy_Spawner>> _spawners = new Dictionary<string, List<Enemy_Spawner>>();

   #endregion
}