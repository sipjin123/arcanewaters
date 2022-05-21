using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MapCreationTool.Serialization;
using System;

public partial class SpawnManager : MonoBehaviour
{
   #region Public Variables

   // Self
   public static SpawnManager self;

   // List of spawns in loaded areas
   public Dictionary<string, List<Spawn>> instantiatedMapSpawns = new Dictionary<string, List<Spawn>>();

   #endregion

   void Awake () {
      self = this;
   }

   public void registerMapSpawn (Spawn spawnObj) {
      if (instantiatedMapSpawns.TryGetValue(spawnObj.AreaKey, out List<Spawn> mapSpawnList)) {
         if (mapSpawnList.Find(_ => _.spawnId == spawnObj.spawnId) == null) {
            mapSpawnList.Add(spawnObj);
         }
      } else {
         instantiatedMapSpawns.Add(spawnObj.AreaKey, new List<Spawn>() {spawnObj});
      }
   }

   public void storeSpawnPositions () {
      // Fetch all spawns from the database
      List<MapSpawn> spawnList = DB_Main.getMapSpawns();

      // Group spawns by map
      foreach (IGrouping<string, MapSpawn> mapSpawnGroup in spawnList.GroupBy(s => s.mapName)) {
         MapSpawnData mapSpawnData = new MapSpawnData {
            mapName = mapSpawnGroup.Key,
            spawns = new Dictionary<string, SpawnData>()
         };

         // Store all spawns for a map
         foreach (MapSpawn mapSpawn in mapSpawnGroup) {
            if (mapSpawnData.spawns.ContainsKey(mapSpawn.name)) continue;

            mapSpawnData.spawns.Add(mapSpawn.name, new SpawnData { name = mapSpawn.name, localPosition = new Vector2(mapSpawn.posX, mapSpawn.posY), spawnId = mapSpawn.spawnId, arriveFacing = mapSpawn.facingDirection });

#if UNITY_EDITOR
            mapSpawnData.spawnPreviewList.Add(new SpawnData { name = mapSpawn.name, localPosition = new Vector2(mapSpawn.posX, mapSpawn.posY) });
#endif
         }

         // Try to find a default spawn that has a 'defaultish' name
         foreach (SpawnData spawn in mapSpawnData.spawns.Values) {
            if (spawn.name.StartsWith("main", StringComparison.OrdinalIgnoreCase) ||
               spawn.name.StartsWith("init", StringComparison.OrdinalIgnoreCase) ||
               spawn.name.StartsWith("default", StringComparison.OrdinalIgnoreCase)) {
               mapSpawnData.defaultSpawn = spawn;
            }
         }

         // Otherwise, set it to anything
         if (mapSpawnData.defaultSpawn == null && mapSpawnData.spawns.Count > 0) {
            mapSpawnData.defaultSpawn = mapSpawnData.spawns.First().Value;
         }

         if (!_mapSpawns.ContainsKey(mapSpawnGroup.Key)) {
            _mapSpawns.Add(mapSpawnGroup.Key, mapSpawnData);
         }
#if UNITY_EDITOR
         _mapSpawnPreviewList.Add(mapSpawnData);
#endif
      }
   }

   public Vector2 getLocalPosition (string mapName, string spawnName, bool log = false) {
      if (_mapSpawns.TryGetValue(mapName, out MapSpawnData mapSpawnData)) {
         if (mapSpawnData.spawns.TryGetValue(spawnName, out SpawnData spawnData)) {
            Spawn spawn = null;
            if (instantiatedMapSpawns.TryGetValue(mapName, out List<Spawn> mapSpawnList)) {
               spawn = mapSpawnList.Find(_ => spawnData.spawnId == _.spawnId);
            }

            if (log) {
               D.editorLog("Random Position: {" + spawnData.localPosition.x + "} Map: {" + mapName + " : " + spawnName + "}", Color.yellow);
            }
            return spawnData.localPosition + (spawn ? spawn.getRandomPositionOffset(VoyageManager.isPvpArenaArea(mapName) ? .25f : 0.5f) : Vector2.zero);
         }
         
         // Try using any valid spawn point for the area
         if (mapSpawnData.spawns.Any()) {
            return mapSpawnData.spawns.First().Value.localPosition;
         }

         // Try using the default location
         return getDefaultLocalPosition(mapName, log);
      }

      D.warning($"Could not find position of spawn [{ mapName }:{ spawnName }]");
      return Vector2.zero;
   }

   public Vector2 getDefaultLocalPosition (string areaKey, bool showLog = false) {
      if (_mapSpawns.TryGetValue(areaKey, out MapSpawnData mapSpawnData)) {
         if (mapSpawnData.defaultSpawn != null) {
            Spawn spawn = null;
            if (instantiatedMapSpawns.TryGetValue(areaKey, out List<Spawn> mapSpawnList)) {
               spawn = mapSpawnList.Find(_ => mapSpawnData.defaultSpawn.spawnId == _.spawnId);
            }

            return mapSpawnData.defaultSpawn.localPosition + (spawn ? spawn.getRandomPositionOffset() : Vector2.zero);
         }
      }

      D.warning("Could not find default spawn position for area key: " + areaKey);
      return Vector2.zero;
   }

   public SpawnData getMapSpawnData (string areaKey, string spawnName) {
      if (_mapSpawns.TryGetValue(areaKey, out MapSpawnData mapSpawnData)) {
         if (mapSpawnData.spawns.TryGetValue(spawnName, out SpawnData spawnData)) {
            return spawnData;
         }
      }

      D.warning("Could not find spawn data for area key: " + areaKey);
      return null;
   }

   public MapSpawnData getAllMapSpawnData (string areaKey) {
      if (_mapSpawns.TryGetValue(areaKey, out MapSpawnData mapSpawnData)) {
         return mapSpawnData;
      }

      D.warning("Could not find map spawn data for area key: " + areaKey);
      return null;
   }

   public List<SpawnData> getAllSpawnsInArea (string areaKey) {
      if (_mapSpawns.TryGetValue(areaKey, out MapSpawnData mapSpawnData)) {
         return mapSpawnData.spawns.Values.ToList();
      }

      return new List<SpawnData>();
   }

   public Spawn getFirstSpawnAround (string areaKey, Vector2 localCenterPosition, float radius) {
      if (instantiatedMapSpawns == null) {
         D.debug("Missing Instantiated Spawns!");
         return null;
      }

      if (instantiatedMapSpawns.TryGetValue(areaKey, out List<Spawn> mapSpawnList)) {
         foreach (Spawn spawn in mapSpawnList) {
            if (spawn == null) {
               D.debug("Null reference spawn");
               continue;
            }
            if (Vector2.Distance(localCenterPosition, spawn.transform.localPosition) < radius) {
               return spawn;
            }
         }
      }

      return null;
   }

   #region Private Variables

   // On server, track all the spawns from the database
   protected Dictionary<string, MapSpawnData> _mapSpawns = new Dictionary<string, MapSpawnData>();

   // Editor preview of data list
   [SerializeField]
   protected List<MapSpawnData> _mapSpawnPreviewList = new List<MapSpawnData>();

   [Serializable]
   public class MapSpawnData
   {
      // Name of the map
      public string mapName;

      // All of map's spawns
      [SerializeField]
      public Dictionary<string, SpawnData> spawns;

      // Editor preview of data list
      [SerializeField]
      public List<SpawnData> spawnPreviewList = new List<SpawnData>();

      // Default spawn for the map
      public SpawnData defaultSpawn;
   }

   [Serializable]
   public class SpawnData
   {
      // Name of the spawn
      public string name;

      // Local position in map of the spawn
      public Vector2 localPosition;

      // The map spawn id
      public int spawnId;

      // Arrive facing what direction
      public int arriveFacing;
   }

   #endregion
}