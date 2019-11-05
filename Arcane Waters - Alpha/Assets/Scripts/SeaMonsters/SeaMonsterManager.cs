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

   // The files containing the sea monster data
   public TextAsset[] monsterDataAssets;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data
   public List<SeaMonsterEntityData> seaMonsterDataList;

   #endregion

   public void Awake () {
      self = this;
      initializeSeaMonsterCache();

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

   private void initializeSeaMonsterCache () {
      if (!hasInitialized) {
         seaMonsterDataList = new List<SeaMonsterEntityData>();
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in monsterDataAssets) {
            // Read and deserialize the file
            SeaMonsterEntityData monsterData = Util.xmlLoad<SeaMonsterEntityData>(textAsset);
            Enemy.Type typeID = (Enemy.Type) monsterData.seaMonsterType;
            
            // Save the monster data in the memory cache
            if (!_seaMonsterData.ContainsKey(typeID)) {
               _seaMonsterData.Add(typeID, monsterData);
               seaMonsterDataList.Add(monsterData);
            }
         }
      }
   }
   
   public SeaMonsterEntityData getMonster (Enemy.Type enemyType, int itemType) {
      return _seaMonsterData[enemyType];
   }

   public void receiveListFromServer (SeaMonsterEntityData[] seamonsterDataList) {
      if (!hasInitialized) {
         hasInitialized = true;
         seaMonsterDataList = new List<SeaMonsterEntityData>();
         foreach (SeaMonsterEntityData seaMonsterData in seamonsterDataList) {
            seaMonsterDataList.Add(seaMonsterData);
         }
      }
   }

   public List<SeaMonsterEntityData> getAllSeaMonsterData () {
      List<SeaMonsterEntityData> seaMonsterList = new List<SeaMonsterEntityData>();
      foreach (KeyValuePair<Enemy.Type, SeaMonsterEntityData> item in _seaMonsterData) {
         seaMonsterList.Add(item.Value);
      }
      return seaMonsterList;
   }

   #endregion

   #region Private Variables

   // Stores a list of SeaMonster Spawners for each random sea map
   protected Dictionary<Area.Type, List<SeaMonsterSpawner>> _spawners = new Dictionary<Area.Type, List<SeaMonsterSpawner>>();

   // The cached seaa monster data 
   private Dictionary<Enemy.Type, SeaMonsterEntityData> _seaMonsterData = new Dictionary<Enemy.Type, SeaMonsterEntityData>();

   #endregion
}
