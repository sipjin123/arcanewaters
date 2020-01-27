using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;
using UnityThreading;
using System;

public class PrefabSpawner : MonoBehaviour
{
   #region Public Variables

   // A list of prefabs we want to auto-spawn at startup
   public List<GameObject> prefabs;

   [Space(5)]
   // A list of maps we want to auto-spawn at startup
   public string[] maps = new string[0];

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

      string[] mapDatas = downloadMapData(maps).ToArray();

      for (int i = 0; i < mapDatas.Length; i++) {
         if (mapDatas[i] == null) {
            D.warning($"Failed to download map {maps[i]}");
            continue;
         }

         try {
            // If it's a map that's already in the scene, don't duplicate it
            if (isAreaAlreadyCreated(maps[i])) {
               Debug.LogWarning($"Area with a key { maps[i] } already exists. Skipping spawning another one.");
               continue;
            }

            Area area = MapImporter.instantiateMapData(mapDatas[i], maps[i], nextPos);

         } catch (Exception ex) {
            D.error($"Unable to spawn map of key {maps[i]}. Error message: {ex.Message}");
         }

         rowCounter++;
         if (rowCounter == mapColumns) {
            nextPos = new Vector3(mapSpawnStart.x, nextPos.y + mapSpawnDelta.y);
            rowCounter = 0;
         } else {
            nextPos = new Vector3(nextPos.x + mapSpawnDelta.x, nextPos.y);
         }
      }
   }

   protected string[] downloadMapData (params string[] names) {
      string[] result = new string[names.Length];
      Task[] tasks = new Task[result.Length];

      for (int i = 0; i < names.Length; i++) {
         // Save index to avoid problems with scope of i
         int index = i;

         tasks[index] = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            try {
               result[index] = DB_Main.getLiveMapData(names[index])?.serializedData;
            } catch (Exception ex) {
               D.error($"Caught an exception while downloading map ${names[index]}: {ex.Message}");
            }
         });
      }

      foreach (Task task in tasks) {
         task.Wait();
      }

      return result;
   }

   protected bool isAreaAlreadyCreated (string areaKey) {
      foreach (Area area in FindObjectsOfType<Area>()) {
         if (area.areaKey == areaKey) {
            return true;
         }
      }

      return false;
   }
   #region Private Variables

   #endregion
}
