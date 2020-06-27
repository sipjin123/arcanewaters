using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using MapCreationTool;
using System.Linq;

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

   public void storeAreaInfo () {
      try {
         // Read the map data
         List<Map> maps = DB_Main.getMaps();

         foreach (Map map in maps) {
            if (map.publishedVersion >= 0) {
               storeAreaInfo(map);
            }
         }
      } catch {
         D.debug("Error in fetching map info");
      }
   }

   public void storeAreaInfo (Map map) {
      if (!_areaKeyToMapInfo.ContainsKey(map.name)) {
         _areaKeyToMapInfo.Add(map.name, map);
      }

      if (map.editorType == EditorType.Sea && !_seaAreaKeys.Contains(map.name)) {
         _seaAreaKeys.Add(map.name);
      }

      if (!_areaIdToName.ContainsKey(map.id)) {
         _areaIdToName.Add(map.id, map.name);
      }
   }

   public string getAreaName (int areaId) {
      if (_areaIdToName.TryGetValue(areaId, out string areaName)) {
         return areaName;
      }

      return areaId.ToString();
   }

   public int getAreaId (string areaKey) {
      if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
         return map.id;
      }

      return -1;
   }

   public Biome.Type getAreaBiome (string areaKey) {
      if (hasArea(areaKey)) {
         return getArea(areaKey).biome;
      } else if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
         return map.biome;
      } else {
         return Biome.Type.None;
      }
   }

   public int getAreaVersion (string areaKey) {
      if (hasArea(areaKey)) {
         return getArea(areaKey).version;
      } else if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
         return map.publishedVersion;
      } else {
         return 0;
      }
   }

   public Area.SpecialType getAreaSpecialType (string areaKey) {
      if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
         return map.specialType;
      }

      return Area.SpecialType.None;
   }

   public bool isSeaArea (string areaKey) {
      if (hasArea(areaKey)) {
         return getArea(areaKey).isSea;
      } else {
         return _seaAreaKeys.Contains(areaKey);
      }
   }

   public bool isInteriorArea (string areaKey) {
      if (hasArea(areaKey)) {
         return getArea(areaKey).isInterior;
      } else if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
         return map.editorType == EditorType.Interior;
      } else {
         return false;
      }
   }

   public EditorType? getAreaEditorType (string areaKey) {
      if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
         return map.editorType;
      }
      return null;
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

   public bool doesAreaExists (string areaKey) {
      return _areaKeyToMapInfo.ContainsKey(areaKey);
   }

   public List<Area> getAreas () {
      return new List<Area>(_areas.Values);
   }

   public List<string> getAreaKeys () {
      return new List<string>(_areaKeyToMapInfo.Keys);
   }

   public Map getMapInfo (string areaKey) {
      if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
         return map;
      }

      return null;
   }

   public List<string> getSeaAreaKeys () {
      return _seaAreaKeys;
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

   public void removeArea (string areaKey) {
      _areas.Remove(areaKey);

      EnemyManager.self.removeSpawners(areaKey);
   }

   public bool tryGetCustomMapManager (string areaKey, out CustomMapManager manager) {
      foreach (CustomMapManager cmm in _customMapManagers) {
         if (cmm.associatedWithAreaKey(areaKey)) {
            manager = cmm;
            return true;
         }
      }
      manager = null;
      return false;
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

   // The data of all areas accessible with the areaKey
   protected Dictionary<string, Map> _areaKeyToMapInfo = new Dictionary<string, Map>();

   // Map ID to map name dictionary, ideally would be removed once maps are tracked everywhere by their ID
   protected Dictionary<int, string> _areaIdToName = new Dictionary<int, string>();

   // The list of areas that are sea maps
   protected List<string> _seaAreaKeys = new List<string>();

   // Managers of owned maps
   protected CustomMapManager[] _customMapManagers = new CustomMapManager[] { new CustomHouseManager(), new CustomFarmManager() };

   #endregion
}
