using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class SpawnManager : MonoBehaviour {
   #region Public Variables

   // New character starting location area key 
   public string startingLocationAreaKey;

   // New character starting location spawn key 
   public string startingLocationSpawnKey;

   // Self
   public static SpawnManager self;

   #endregion

   void Awake () {
      self = this;
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

      _spawns[new SpawnID(areaKey, spawnKey)] = spawn;
   }

   public Spawn getSpawn (string areaKey, string spawnKey) {
      return _spawns[new SpawnID(areaKey, spawnKey)];
   }

   public List<SpawnID> getAllSpawnKeys () {
      return new List<SpawnID>(_spawns.Keys);
   }

   #region Private Variables

   // Keeps track of the Spawns that we know about
   // Dictionary key - <areaKey, spawnKey>
   protected Dictionary<SpawnID, Spawn> _spawns = new Dictionary<SpawnID, Spawn>();

   #endregion

   /// <summary>
   /// Defines the identification of a spawn within the SpawnManager
   /// </summary>
   public struct SpawnID
   {
      // The key of the area that the spawn is placed in
      public string areaKey;

      // The key of a specific spawn
      public string spawnKey;

      public SpawnID (string areaKey, string spawnKey) {
         this.areaKey = areaKey;
         this.spawnKey = spawnKey;
      }
   }
}
