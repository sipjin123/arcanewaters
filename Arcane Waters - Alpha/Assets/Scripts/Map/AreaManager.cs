using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class AreaManager : MonoBehaviour
{
   #region Public Variables

   // Self
   public static AreaManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   void Start () {
      // Store references to all of our areas
      foreach (Area area in FindObjectsOfType<Area>()) {
         storeArea(area);
      }

      // Routinely check if we can switch off the colliders for any of the areas
      InvokeRepeating("toggleAreaCollidersForPerformanceImprovement", 0f, .25f);
   }

   public void storeAreaKeys () {
      Dictionary<string, MapInfo> liveMaps = DB_Main.getLiveMaps();

      foreach (string areaKey in liveMaps.Keys) {
         _areaKeysFromDatabase.Add(areaKey);
      }
   }

   public void storeAreaIdsToNames () {
      foreach (Map map in DB_Main.getMaps()) {
         _areaIdToName.Add(map.id, map.name);
      }
      Debug.Log(_areaIdToName.Count);
   }

   public string getAreaName (int areaId) {
      if (_areaIdToName.TryGetValue(areaId, out string areaName)) {
         return areaName;
      }

      return areaId.ToString();
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
      return new List<string>(_areaKeysFromDatabase);
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

   // A set of all Area Keys from the database
   protected HashSet<string> _areaKeysFromDatabase = new HashSet<string>();

   // Map ID to map name dictionary, ideally would be removed once maps are tracked everywhere by their ID
   protected Dictionary<int, string> _areaIdToName = new Dictionary<int, string>();

   #endregion
}
