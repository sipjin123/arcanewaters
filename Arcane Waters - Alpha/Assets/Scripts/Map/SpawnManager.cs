using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SpawnManager : MonoBehaviour {
   #region Public Variables

   // The starting spawn location for new characters
   public Spawn.Type startingSpawnLocation;

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

   public void store (Spawn.Type spawnType, Spawn spawn) {
      if (_spawns.ContainsKey(spawnType)) {
         D.warning("Storing multiple spawns of the same type: " + spawnType);
      }

      _spawns[spawnType] = spawn;
   }

   public Spawn getSpawn (Spawn.Type spawnType) {
      return _spawns[spawnType];
   }

   #region Private Variables

   // Keeps track of the Spawns that we know about
   protected Dictionary<Spawn.Type, Spawn> _spawns = new Dictionary<Spawn.Type, Spawn>();

   #endregion
}
