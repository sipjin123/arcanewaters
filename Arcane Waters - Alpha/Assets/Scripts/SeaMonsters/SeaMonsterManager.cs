using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static SeaMonsterManager self;

   // Sea monsters to spawn on random maps
   public Enemy.Type[] randomSeaMonsters;

   // The files containing the crafting data
   public TextAsset[] monsterDataAssets;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data
   public List<SeaMonsterEntityDataCopy> monsterDataList;

   #endregion

   public void Awake () {
      self = this;
#if IS_SERVER_BUILD
      initializeCraftCache();
#else
      monsterDataAssets = null;
#endif

      // Create empty lists for each random sea map
      foreach (Area.Type areaType in System.Enum.GetValues(typeof(Area.Type))) {
         if (Area.isRandom(areaType)) {
            _spawners[areaType] = new List<SeaMonsterSpawner>();
         }
      }
   }

   #region Spawn Features

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

   #endregion

   #region XML Features

   private void initializeCraftCache () {
      if (!hasInitialized) {
         monsterDataList = new List<SeaMonsterEntityDataCopy>();
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in monsterDataAssets) {
            // Read and deserialize the file
            SeaMonsterEntityDataCopy monsterData = Util.xmlLoad<SeaMonsterEntityDataCopy>(textAsset);
            Enemy.Type typeID = (Enemy.Type) monsterData.seaMonsterType;
            
            // Save the monster data in the memory cache
            if (!_monsterData.ContainsKey(typeID)) {
               _monsterData.Add(typeID, monsterData);
               monsterDataList.Add(monsterData);
            }
         }
      }
   }
   
   public SeaMonsterEntityDataCopy getMonster (Enemy.Type enemyType, int itemType) {
      return _monsterData[enemyType];
   }

   public void receiveListFromServer (SeaMonsterEntityDataCopy[] battlerDataList) {
      if (!hasInitialized) {
         hasInitialized = true;
         monsterDataList = new List<SeaMonsterEntityDataCopy>();
         foreach (SeaMonsterEntityDataCopy battlerData in battlerDataList) {
            monsterDataList.Add(battlerData);
         }
      }
   }

   public List<SeaMonsterEntityDataCopy> getAllSeaMonsterData () {
      List<SeaMonsterEntityDataCopy> monsterList = new List<SeaMonsterEntityDataCopy>();
      foreach (KeyValuePair<Enemy.Type, SeaMonsterEntityDataCopy> item in _monsterData) {
         monsterList.Add(item.Value);
      }
      return monsterList;
   }

   #endregion

   #region Private Variables

   // Stores a list of SeaMonster Spawners for each random sea map
   protected Dictionary<Area.Type, List<SeaMonsterSpawner>> _spawners = new Dictionary<Area.Type, List<SeaMonsterSpawner>>();

   // The cached monster data 
   private Dictionary<Enemy.Type, SeaMonsterEntityDataCopy> _monsterData = new Dictionary<Enemy.Type, SeaMonsterEntityDataCopy>();

   #endregion
}
