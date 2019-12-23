using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class SeaMonsterManager : XmlManager {
   #region Public Variables

   // Self
   public static SeaMonsterManager self;

   // Sea monsters to spawn on random maps
   public SeaMonsterEntity.Type[] randomSeaMonsters;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data
   public List<SeaMonsterEntityData> seaMonsterDataList;

   // Holds the info the monsters to spawn after fetching data from SQL database
   public List<SeaMonsterSpawnData> scheduledSpawnList = new List<SeaMonsterSpawnData>();

   public class SeaMonsterSpawnData
   {
      public SeaMonsterSpawner seaMonsterSpawner;
      public Instance instance;

      public void spawn () {
         // Create an Enemy in this instance
         SeaMonsterEntity seaMonster = Instantiate(PrefabsManager.self.seaMonsterPrefab);
         seaMonster.monsterType = seaMonsterSpawner.enemyType;
         seaMonster.areaKey = instance.areaKey;

         // Add it to the Instance
         InstanceManager.self.addSeaMonsterToInstance(seaMonster, instance);
         seaMonster.transform.position = seaMonsterSpawner.transform.position;
         NetworkServer.Spawn(seaMonster.gameObject);
      }
   }

   #endregion

   public void Awake () {
      self = this;

      // Create empty lists for each random sea map
      foreach (string areaKey in Area.getAllAreaKeys()) {
         if (Area.isRandom(areaKey)) {
            _spawners[areaKey] = new List<SeaMonsterSpawner>();
         }
      }
   }
   
   #region Spawn Features

   public void storeSpawner (SeaMonsterSpawner spawner, string areaKey) {
      if (Area.isRandom(areaKey)) {
         List<SeaMonsterSpawner> list = _spawners[areaKey];
         list.Add(spawner);
         _spawners[areaKey] = list;
      }
   }

   public void registerSeaMonstersOnServerForInstance (Instance instance) {
      // Registers the new monsters to spawn to a Queue which will trigger after the Seamonster data is fetched
      foreach (SeaMonsterSpawner spawner in _spawners[instance.areaKey]) {
         scheduledSpawnList.Add(new SeaMonsterSpawnData {
            instance = instance,
            seaMonsterSpawner = spawner
         });
      }
   }

   public void spawnSeamonstersOnServerForInstance  () {
      foreach (SeaMonsterSpawnData spawnSched in scheduledSpawnList) {
         // If we don't have any spawners defined for this Area, then we're done
         if (!_spawners.ContainsKey(spawnSched.instance.areaKey)) {
            D.log("No SeaMonster Spawners defined for Area Key: " + spawnSched.instance.areaKey);
            return;
         }

         spawnSched.spawn();
      }
   }

   #endregion

   #region XML Features

   public void initializeSeaMonsterCache () {
      if (!hasInitialized) {
         seaMonsterDataList = new List<SeaMonsterEntityData>();
         hasInitialized = true;

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<string> rawXMLData = DB_Main.getSeaMonsterXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (string rawText in rawXMLData) {
                  TextAsset newTextAsset = new TextAsset(rawText);
                  SeaMonsterEntityData newSeaData = Util.xmlLoad<SeaMonsterEntityData>(newTextAsset);
                  SeaMonsterEntity.Type typeID = (SeaMonsterEntity.Type) newSeaData.seaMonsterType;

                  if (!_seaMonsterData.ContainsKey(typeID)) {
                     _seaMonsterData.Add(typeID, newSeaData);
                     seaMonsterDataList.Add(newSeaData);
                  }
               }
               spawnSeamonstersOnServerForInstance ();
               RewardManager.self.initSeaMonsterLootList();
            });
         });
      }
   }
   
   public SeaMonsterEntityData getMonster (SeaMonsterEntity.Type enemyType, int itemType) {
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
      foreach (KeyValuePair<SeaMonsterEntity.Type, SeaMonsterEntityData> item in _seaMonsterData) {
         seaMonsterList.Add(item.Value);
      }
      return seaMonsterList;
   }

   #endregion

   #region Private Variables

   // Stores a list of SeaMonster Spawners for each random sea map
   protected Dictionary<string, List<SeaMonsterSpawner>> _spawners = new Dictionary<string, List<SeaMonsterSpawner>>();

   // The cached sea monster data 
   private Dictionary<SeaMonsterEntity.Type, SeaMonsterEntityData> _seaMonsterData = new Dictionary<SeaMonsterEntity.Type, SeaMonsterEntityData>();

   #endregion
}
