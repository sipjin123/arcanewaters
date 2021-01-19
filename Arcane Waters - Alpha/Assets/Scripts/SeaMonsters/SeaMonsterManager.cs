using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Linq;

public class SeaMonsterManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static SeaMonsterManager self;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data
   public List<SeaMonsterEntityData> seaMonsterDataList = new List<SeaMonsterEntityData>();

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
   }
   
   #region Spawn Features

   public void storeSpawner (SeaMonsterSpawner spawner, string areaKey) {

   }

   #endregion

   #region XML Features

   public void initializeSeaMonsterCache () {
      if (!hasInitialized) {
         seaMonsterDataList = new List<SeaMonsterEntityData>();
         hasInitialized = true;

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> rawXMLData = DB_Main.getSeaMonsterXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (XMLPair xmlPair in rawXMLData) {
                  TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
                  try {
                     SeaMonsterEntityData newSeaData = Util.xmlLoad<SeaMonsterEntityData>(newTextAsset);

                     if (!_seaMonsterData.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
                        newSeaData.xmlId = xmlPair.xmlId;
                        _seaMonsterData.Add(xmlPair.xmlId, newSeaData);
                        seaMonsterDataList.Add(newSeaData);
                     }
                  } catch {
                     D.debug("Failed to load sea monster: " + xmlPair.xmlId);
                  }
               }
            });
         });
      }
   }
   
   public SeaMonsterEntityData getMonster (SeaMonsterEntity.Type enemyType) {
      SeaMonsterEntityData fetchedData = _seaMonsterData.Values.ToList().Find(_ => _.seaMonsterType == enemyType);
      if (fetchedData != null) {
         return fetchedData;
      }

      D.editorLog("This sea monster does not exist in the database: " + enemyType, Color.red);
      return null;
   }

   public SeaMonsterEntityData getMonster (int xmlId) {
      SeaMonsterEntityData dafetchedData = _seaMonsterData.Values.ToList().Find(_ => _.xmlId == xmlId);
      if (dafetchedData != null) {
         return dafetchedData;
      }

      D.editorLog("This sea monster does not exist in the database xml id: " + xmlId, Color.red);
      return null;
   }

   public void receiveListFromZipData (SeaMonsterEntityData[] seamonsterDataList) {
      if (!hasInitialized) {
         hasInitialized = true;
         seaMonsterDataList = new List<SeaMonsterEntityData>();
         foreach (SeaMonsterEntityData seaMonsterData in seamonsterDataList) {
            if (!_seaMonsterData.ContainsKey(seaMonsterData.xmlId) && seaMonsterData.isXmlEnabled) {
               seaMonsterDataList.Add(seaMonsterData);
               _seaMonsterData.Add(seaMonsterData.xmlId, seaMonsterData);
            } else {
               D.editorLog("Ignoring Disabled Entry: " + seaMonsterData.xmlId + " : " + seaMonsterData.seaMonsterType, Color.red);
            }
         }
      }
   }

   public List<SeaMonsterEntityData> getAllSeaMonsterData () {
      List<SeaMonsterEntityData> seaMonsterList = new List<SeaMonsterEntityData>();
      foreach (KeyValuePair<int, SeaMonsterEntityData> item in _seaMonsterData) {
         seaMonsterList.Add(item.Value);
      }
      return seaMonsterList;
   }

   #endregion

   #region Private Variables

   // Stores a list of SeaMonster Spawners for each sea map
   protected Dictionary<string, List<SeaMonsterSpawner>> _spawners = new Dictionary<string, List<SeaMonsterSpawner>>();

   // The cached sea monster data 
   private Dictionary<int, SeaMonsterEntityData> _seaMonsterData = new Dictionary<int, SeaMonsterEntityData>();

   #endregion
}
