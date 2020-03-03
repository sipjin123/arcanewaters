﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MapCreationTool.Serialization;

public partial class SpawnManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static SpawnManager self;

   #endregion

   void Awake () {
      self = this;
   }

   public void storeSpawnPositions () {
       List<MapSpawn> spawnList = DB_Main.getMapSpawns();

      foreach (MapSpawn mapSpawn in spawnList) {
         SpawnID spawnID = new SpawnID(mapSpawn.mapName, mapSpawn.name);
         _spawnLocalPositions[spawnID] = new Vector2(mapSpawn.posX, mapSpawn.posY);
      }
   }

   public static SpawnManager get () {
      // Check if we need to look it up
      if (self == null) {
         self = FindObjectOfType<SpawnManager>();
      }

      return self;
   }

   public void store (string areaKey, string spawnKey, Spawn spawn) {
      if (_spawns.ContainsKey(new SpawnID(areaKey, spawnKey))) {
         D.warning($"Storing multiple spawns of the same type: areaKey = { areaKey }, spawnKey = { spawnKey }");
      }

      _spawnIDList.Add(new SpawnID(areaKey, spawnKey));
      _spawns[new SpawnID(areaKey, spawnKey)] = spawn;

      // Add this spawn as the default for this area, if none exist yet
      if (!_defaultSpawn.ContainsKey(areaKey)) {
         _defaultSpawn.Add(areaKey, spawn);
      }
   }

   public Spawn getSpawn (string areaKey, string spawnKey) {
      SpawnID spawnID = new SpawnID(areaKey, spawnKey);
      if (_spawns.ContainsKey(spawnID)) {
         return _spawns[spawnID];
      } else {
         D.debug("Spawn ID is missing: " + areaKey+" - "+spawnKey);
         return _spawns.Values.ToList()[0];
      }
   }

   public Spawn getSpawn (string areaKey) {
      if (_defaultSpawn.ContainsKey(areaKey)) {
         return _defaultSpawn[areaKey];
      } else {
         D.warning("No default spawn is defined for area " + areaKey);
         return null;
      }
   }

   public Vector2 getSpawnLocalPosition (SpawnID spawnID) {
      if (_spawns.ContainsKey(spawnID)) {
         return _spawns[spawnID].transform.localPosition;
      }

      if (_spawnLocalPositions.ContainsKey(spawnID)) {
         return _spawnLocalPositions[spawnID];
      }

      D.warning($"Could not find position of spawn {spawnID}");

      return Vector2.zero;
   }

   public Vector2 getDefaultSpawnLocalPosition (string areaKey) {
      foreach (SpawnID spawnID in _spawnLocalPositions.Keys) {
         if (spawnID.areaKey == areaKey) {
            return _spawnLocalPositions[spawnID];
         }
      }

      D.warning("Could not find default spawn position for area key: " + areaKey);
      return Vector2.zero;
   }

   public List<SpawnID> getAllSpawnKeys () {
      return new List<SpawnID>(_spawns.Keys);
   }

   #region Private Variables

   // Keeps track of the Spawns that we know about
   protected Dictionary<SpawnID, Spawn> _spawns = new Dictionary<SpawnID, Spawn>();

   // Editor preview of data list
   [SerializeField]
   protected List<SpawnID> _spawnIDList = new List<SpawnID>();

   // Contains one spawn per area, accessible by area id
   protected Dictionary<string, Spawn> _defaultSpawn = new Dictionary<string, Spawn>();

   // The Server keeps track of all local spawn positions from the database
   protected Dictionary<SpawnID, Vector2> _spawnLocalPositions = new Dictionary<SpawnID, Vector2>();

   #endregion
}
