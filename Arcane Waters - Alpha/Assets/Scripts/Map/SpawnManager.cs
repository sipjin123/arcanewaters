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

   #endregion

   void Awake () {
      self = this;
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

            mapSpawnData.spawns.Add(mapSpawn.name, new SpawnData { name = mapSpawn.name, localPosition = new Vector2(mapSpawn.posX, mapSpawn.posY) });

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

   public Vector2 getLocalPosition (string mapName, string spawnName) {
      if (_mapSpawns.TryGetValue(mapName, out MapSpawnData mapSpawnData)) {
         if (mapSpawnData.spawns.TryGetValue(spawnName, out SpawnData spawnData)) {
            return spawnData.localPosition;
         }
      }

      D.warning($"Could not find position of spawn [{ mapName }:{ spawnName }]");
      return Vector2.zero;
   }

   public Vector2 getDefaultLocalPosition (string areaKey) {
      if (_mapSpawns.TryGetValue(areaKey, out MapSpawnData mapSpawnData)) {
         if (mapSpawnData.defaultSpawn != null) {
            return mapSpawnData.defaultSpawn.localPosition;
         }
      }

      D.warning("Could not find default spawn position for area key: " + areaKey);
      return Vector2.zero;
   }

   #region Private Variables

   // On server, track all the spawns from the database
   protected Dictionary<string, MapSpawnData> _mapSpawns = new Dictionary<string, MapSpawnData>();

   // Editor preview of data list
   [SerializeField]
   protected List<MapSpawnData> _mapSpawnPreviewList = new List<MapSpawnData>();

   [Serializable]
   protected class MapSpawnData
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
   protected class SpawnData
   {
      // Name of the spawn
      public string name;

      // Local position in map of the spawn
      public Vector2 localPosition;
   }

   #endregion
}
