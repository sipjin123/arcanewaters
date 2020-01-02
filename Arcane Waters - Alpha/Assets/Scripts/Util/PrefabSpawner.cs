﻿using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;

public class PrefabSpawner : MonoBehaviour {
   #region Public Variables

   // A list of prefabs we want to auto-spawn at startup
   public List<GameObject> prefabs;

   [Space(5)]
   // A list of maps we want to auto-spawn at startup
   public List<Map> maps = new List<Map>();

   [Tooltip("The position of the first spawned map.")]
   public Vector2 mapSpawnStart = new Vector2(0, -100);

   [Tooltip("The distance between the middle points of the spawned maps.")]
   public Vector2 mapSpawnDelta = new Vector2(100, -100);

   [Tooltip("The amount of maps in a row of spawned maps.")]
   public int mapColumns = 5;

   #endregion

   void Awake () {
      spawnPrefabs();
      spawnMaps();
   }

   protected void spawnPrefabs () {
      foreach (GameObject prefab in prefabs) {
         if (prefab != null) {
            // If it's a map that's already in the scene, don't duplicate it
            Area area = prefab.GetComponent<Area>();

            if (area != null && isAreaAlreadyCreated(area.areaKey)) {
               continue;
            }

            Instantiate(prefab);
         }
      }
   }

   protected void spawnMaps () {
      int rowCounter = 0;
      Vector3 nextPos = mapSpawnStart;

      foreach (Map map in maps) {
         // If there is a name to override defined, use it
         string areaKey = string.IsNullOrWhiteSpace(map.overrideName) ? map.mapFile.name : map.overrideName;

         // If it's a map that's already in the scene, don't duplicate it
         if (isAreaAlreadyCreated(areaKey)) {
            Debug.LogWarning($"Area with a key { areaKey } already exists. Skipping spawning another one.");
            continue;
         }

         Area area = MapImporter.instantiateMapData(map.mapFile, areaKey, nextPos);

         // If there is an aditional prefab defined add it to the main instance
         if (map.includePrefab != null) {
            Instantiate(map.includePrefab, area.transform);
         }

         rowCounter++;
         if (rowCounter == mapColumns) {
            nextPos = new Vector3(mapSpawnStart.x, nextPos.y + mapSpawnDelta.y);
         } else {
            nextPos = new Vector3(nextPos.x + mapSpawnDelta.x, nextPos.y);
         }
      }
   }

   protected bool isAreaAlreadyCreated (string areaKey) {
      foreach (Area area in FindObjectsOfType<Area>()) {
         if (area.areaKey == areaKey) {
            return true;
         }
      }

      return false;
   }

   [System.Serializable]
   public class Map
   {
      [Tooltip("If this is not empty, the areaKey of the area will be set to this. Otherwise, the name of the file will be used.")]
      public string overrideName;

      [Tooltip("The map file from which to take the data. Should be a .arcane file.")]
      public TextAsset mapFile;

      [Tooltip("If this is set, the GameObject will be instantiated and added as a child to the spawned map.")]
      public GameObject includePrefab;
   }

   #region Private Variables

   #endregion
}
