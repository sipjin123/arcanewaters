using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SpawnManager : MonoBehaviour {
   #region Public Variables

   // The starting spawn location for new characters
   public string startingSpawnLocation;

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

   public void store (string spawnKey, Spawn spawn) {
      if (_spawns.ContainsKey(spawnKey)) {
         D.warning("Storing multiple spawns of the same type: " + spawnKey);
      }

      _spawns[spawnKey] = spawn;
   }

   public Spawn getSpawn (string spawnKey) {
      return _spawns[spawnKey];
   }

   public List<string> getAllSpawnKeys () {
      return new List<string>(_spawns.Keys);
   }

   #region Private Variables

   // Keeps track of the Spawns that we know about
   protected Dictionary<string, Spawn> _spawns = new Dictionary<string, Spawn>();

   #endregion
}
