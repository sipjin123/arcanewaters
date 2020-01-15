using UnityEngine;
using System.Collections.Generic;


public partial class SpawnManager : MonoBehaviour {
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
         D.warning("Spawin ID is missing: " + areaKey+" - "+spawnKey);
         return null;
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

   public List<SpawnID> getAllSpawnKeys () {
      return new List<SpawnID>(_spawns.Keys);
   }

   #region Private Variables

   // Keeps track of the Spawns that we know about
   // Dictionary key - <areaKey, spawnKey>
   protected Dictionary<SpawnID, Spawn> _spawns = new Dictionary<SpawnID, Spawn>();

   // Editor preview of data list
   [SerializeField]
   protected List<SpawnID> _spawnIDList = new List<SpawnID>();

   // Contains one spawn per area, accessible by area id
   protected Dictionary<string, Spawn> _defaultSpawn = new Dictionary<string, Spawn>();

   #endregion
}
