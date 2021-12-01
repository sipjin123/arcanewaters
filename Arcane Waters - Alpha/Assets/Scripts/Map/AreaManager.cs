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
         List<Map> maps = DB_Main.exec(DB_Main.getMaps);

         foreach (Map map in maps) {
            if (map.publishedVersion >= 0) {
               storeAreaInfo(map);
            }
         }
      } catch {
         D.debug("Error in fetching map info");
      }
   }

   public void receiveMapDataFromServerZip (List<Map> mapDataList) {
      // Servers and hosts get the complete map data from the DB
      if (NetworkServer.active) {
         return;
      }

      foreach (Map map in mapDataList) {
         storeAreaInfo(map);
      }
   }

   public void storeAreaInfo (Map map) {
      if (!_areaKeyToMapInfo.ContainsKey(map.name)) {
         _areaKeyToMapInfo.Add(map.name, map);
         _areaNames.Add(map.name);
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

   public Biome.Type getDefaultBiome (string areaKey) {
      if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
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

   public WeatherEffectType getAreaWeatherEffectType (string areaKey) {
      if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
         return map.weatherEffectType;
      }

      return WeatherEffectType.None;
   }

   public int getAreaMaxPlayerCount (string areaKey) {
      if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
         return map.maxPlayerCount;
      }

      return 0;
   }

   public PvpGameMode getAreaPvpGameMode (string areaKey) {
      if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
         return map.pvpGameMode;
      }

      return PvpGameMode.None;
   }

   public PvpArenaSize getAreaPvpArenaSize (string areaKey) {
      if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
         return map.pvpArenaSize;
      }

      return PvpArenaSize.None;
   }

   public static float getWidthForPvpArenaSize (PvpArenaSize size) {
      switch (size) {
         case PvpArenaSize.Medium:
            return 128.0f;
         case PvpArenaSize.Large:
            return 256.0f;
         default:
            return 64.0f;
      }
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

   public bool isPrivateArea (string areaKey) {
      return getAreaSpecialType(areaKey) == Area.SpecialType.Private;
   }

   public bool isTownArea (string areaKey) {
      return getAreaSpecialType(areaKey) == Area.SpecialType.Town;
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

   public List<string> getAllAreaNames () {
      return _areaNames;
   }

   public List<Map> getAllMapInfo () {
      return _areaKeyToMapInfo.Values.ToList();
   }

   public Map getMapInfo (string areaKey) {
      if (_areaKeyToMapInfo.TryGetValue(areaKey, out Map map)) {
         return map;
      }

      return null;
   }

   public IEnumerable<Map> getChildMaps (Map parentMap) {
      foreach (Map map in _areaKeyToMapInfo.Values) {
         if (map.sourceMapId == parentMap.id) {
            yield return map;
         }
      }
   }

   public List<string> getSeaAreaKeys () {
      return _seaAreaKeys;
   }

   public void storeArea (Area area) {
      if (area == null || _areas.ContainsKey(area.areaKey)) {
         return;
      }

      _areas.Add(area.areaKey, area);
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

   public bool isHouseOfUser (string areaKey, int userId) {
      // Check if the area is a custom house map
      if (tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         if (customMapManager is CustomHouseManager) {
            // Check if it belongs to the given user
            int areaUserId = CustomMapManager.getUserId(areaKey);
            return areaUserId == userId;
         }
      }

      return false;
   }

   public bool isFarmOfUser (string areaKey, int userId) {
      // Check if the area is a custom map
      if (tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         if (customMapManager is CustomFarmManager) {
            // Check if it belongs to the given user
            int areaUserId = CustomMapManager.getUserId(areaKey);
            return areaUserId == userId;
         }
      }

      return false;
   }

   public static bool isFarmingAllowed (string areaKey) {
      return (areaKey.Contains(CustomFarmManager.GROUP_AREA_KEY) || areaKey.Contains(CustomHouseManager.GROUP_AREA_KEY));
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

   // The area names
   protected List<string> _areaNames = new List<string>();

   // Managers of owned maps
   protected CustomMapManager[] _customMapManagers = new CustomMapManager[] { new CustomHouseManager(), new CustomFarmManager() };

   #endregion
}
