using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AreaManager : MonoBehaviour
{
   #region Public Variables

   // The area key of the place we spawn players after their ship sinks
   public string areaKeyForSunkenPlayers;

   // The spawn key of the place we spawn players after their ship sinks
   public string spawnKeyForSunkenPlayers;

   // Self
   public static AreaManager self;

   #endregion

   private void Awake () {
      self = this;

      if (areaKeyForSunkenPlayers == "" || spawnKeyForSunkenPlayers == "") {
         D.debug("Spawn area for sunken ships is not set properly");
      }
   }

   void Start () {
      // Store references to all of our areas
      foreach (Area area in FindObjectsOfType<Area>()) {
         storeArea(area);
      }

      // Routinely check if we can switch off the colliders for any of the areas
      InvokeRepeating("toggleAreaCollidersForPerformanceImprovement", 0f, .25f);
   }

   public Area getArea (string areaKey) {
      if (_areas.TryGetValue(areaKey, out Area area)) {
         return area;
      }
      return null;
   }

   public bool hasArea (string areaKey) {
      return _areas.ContainsKey(areaKey);
   }

   public List<Area> getAreas () {
      return new List<Area>(_areas.Values);
   }

   public List<string> getAreaKeys () {
      return new List<string>(_areas.Keys);
   }

   public void storeArea (Area area) {
      if (area == null || _areas.ContainsKey(area.areaKey)) {
         return;
      }

      _areas.Add(area.areaKey, area);

      // Store all of the Enemy Spawners in each area
      foreach (Enemy_Spawner spawner in area.GetComponentsInChildren<Enemy_Spawner>()) {
         EnemyManager.self.storeSpawner(spawner, area.areaKey);
      }
   }

   protected void toggleAreaCollidersForPerformanceImprovement () {
      foreach (Area area in _areas.Values) {
         // We only need colliders for the area that the player is in
         bool needColliders = InstanceManager.self.hasActiveInstanceForArea(area.areaKey);

         // Toggle the grid layers accordingly
         area.setColliders(needColliders);
      }
   }

   #region Private Variables

   // The Areas we know about
   protected Dictionary<string, Area> _areas = new Dictionary<string, Area>();

   #endregion
}
