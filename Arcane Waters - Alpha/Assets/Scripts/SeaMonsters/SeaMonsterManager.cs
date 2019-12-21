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
   
   private void Start () {
      initializeSeaMonsterCache();
   }

   #region Spawn Features

   public void storeSpawner (SeaMonsterSpawner spawner, string areaKey) {
      if (Area.isRandom(areaKey)) {
         List<SeaMonsterSpawner> list = _spawners[areaKey];
         list.Add(spawner);
         _spawners[areaKey] = list;
      }
   }

   public void spawnSeaMonstersOnServerForInstance (Instance instance) {
      // If we don't have any spawners defined for this Area, then we're done
      if (!_spawners.ContainsKey(instance.areaKey)) {
         D.log("No SeaMonster Spawners defined for Area Key: " + instance.areaKey);
         return;
      }

      foreach (SeaMonsterSpawner spawner in _spawners[instance.areaKey]) {
         // Create an Enemy in this instance
         SeaMonsterEntity seaMonster = Instantiate(PrefabsManager.self.seaMonsterPrefab);
         seaMonster.monsterType = spawner.enemyType;
         seaMonster.areaKey = instance.areaKey;

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

   public override void loadAllXMLData () {
      base.loadAllXMLData();
      loadXMLData(SeaMonsterToolManager.FOLDER_PATH);
   }

   public override void clearAllXMLData () {
      base.clearAllXMLData();
      textAssets = new List<TextAsset>();
   }

   #endregion

   #region Private Variables

   // Stores a list of SeaMonster Spawners for each random sea map
   protected Dictionary<string, List<SeaMonsterSpawner>> _spawners = new Dictionary<string, List<SeaMonsterSpawner>>();

   // The cached sea monster data 
   private Dictionary<SeaMonsterEntity.Type, SeaMonsterEntityData> _seaMonsterData = new Dictionary<SeaMonsterEntity.Type, SeaMonsterEntityData>();

   #endregion
}
