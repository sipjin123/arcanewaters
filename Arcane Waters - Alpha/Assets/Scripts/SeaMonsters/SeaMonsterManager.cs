using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static SeaMonsterManager self;

   #endregion

   public void Awake () {
      self = this;

      // Create empty lists for each random sea map
      foreach (Area.Type areaType in System.Enum.GetValues(typeof(Area.Type))) {
         if (Area.isRandom(areaType)) {
            _spawners[areaType] = new List<SeaMonsterSpawner>();
         }
      }
   }

   public void storeSpawner (SeaMonsterSpawner spawner, Area.Type areaType) {
      if (Area.isRandom(areaType)) {
         List<SeaMonsterSpawner> list = _spawners[areaType];
         list.Add(spawner);
         _spawners[areaType] = list;
      }
   }

   public void spawnSeaMonstersOnServerForInstance (Instance instance) {
      // If we don't have any spawners defined for this Area, then we're done
      if (!_spawners.ContainsKey(instance.areaType)) {
         D.log("No SeaMonster Spawners defined for Area Type: " + instance.areaType);
         return;
      }

      foreach (SeaMonsterSpawner spawner in _spawners[instance.areaType]) {
         // Create an Enemy in this instance
         SeaMonsterEntity seaMonster = Instantiate(PrefabsManager.self.seaMonsterPrefab);
         seaMonster.monsterType = spawner.enemyType;
         seaMonster.areaType = instance.areaType;

         // Add it to the Instance
         InstanceManager.self.addSeaMonsterToInstance(seaMonster, instance);
         seaMonster.transform.position = spawner.transform.position;
         NetworkServer.Spawn(seaMonster.gameObject);
      }
   }

   #region Private Variables

   // Stores a list of SeaMonster Spawners for each random sea map
   protected Dictionary<Area.Type, List<SeaMonsterSpawner>> _spawners = new Dictionary<Area.Type, List<SeaMonsterSpawner>>();

   #endregion
}
