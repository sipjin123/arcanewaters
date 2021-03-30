﻿using UnityEngine;
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

   #endregion

   void Awake () {
      D.adminLog("PrefabSpawner.Awake...", D.ADMIN_LOG_TYPE.Initialization);
      spawnPrefabs();
      D.adminLog("PrefabSpawner.Awake: OK", D.ADMIN_LOG_TYPE.Initialization);
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
