using UnityEngine;
using System.Collections.Generic;
using System;

public class WorldMapManager : MonoBehaviour
{
   #region Public Variables

   // The x coordinates for the Open World
   public const string OPEN_WORLD_MAP_COORDS_X = "ABCDEFGHIJKLMNO";

   // The y coordinates for the Opoen World
   public const string OPEN_WORLD_MAP_COORDS_Y = "012345678";

   // Open World Map Prefix
   public const string WORLD_MAP_PREFIX = "world_map_";

   // Self
   public static WorldMapManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public static void setCachedWorldMapPins (List<WorldMapPanelPinInfo> pins) {
      _worldPinsCache = pins;
   }

   public static List<WorldMapPanelPinInfo> getCachedWorldMapPins () {
      return _worldPinsCache;
   }

   private static bool areValidOpenWorldAreaCoords (Vector2Int mapCoords) {
      if (mapCoords.x < 0 || mapCoords.x >= OPEN_WORLD_MAP_COORDS_X.Length || mapCoords.y < 0 || mapCoords.y >= OPEN_WORLD_MAP_COORDS_Y.Length) {
         return false;
      }

      return true;
   }

   public static bool computeNextOpenWorldArea (string areaKey, Direction direction, out string nextAreaKey) {
      Vector2Int mapCoords = computeOpenWorldAreaCoords(areaKey);
      nextAreaKey = "";

      if (!areValidOpenWorldAreaCoords(mapCoords)) {
         return false;
      }

      switch (direction) {
         case Direction.North:
            mapCoords.y += 1;
            break;
         case Direction.East:
            mapCoords.x += 1;
            break;
         case Direction.South:
            mapCoords.y -= 1;
            break;
         case Direction.West:
            mapCoords.x -= 1;
            break;
      }

      if (!areValidOpenWorldAreaCoords(mapCoords)) {
         return false;
      }

      nextAreaKey = computeOpenWorldAreaKey(mapCoords);
      return true;
   }

   public static string computeOpenWorldAreaSuffix (Vector2Int areaCoords) {
      if (!areValidOpenWorldAreaCoords(areaCoords)) {
         return "";
      }

      char cx = OPEN_WORLD_MAP_COORDS_X[areaCoords.x];
      char cy = OPEN_WORLD_MAP_COORDS_Y[areaCoords.y];

      return $"{cx}{cy}".ToUpper();
   }

   public static Vector2Int computeOpenWorldAreaCoords (string areaKey) {
      if (Util.isEmpty(areaKey) || areaKey.Length < 2 || !areaKey.StartsWith(WORLD_MAP_PREFIX)) {
         return new Vector2Int(-1, -1);
      }

      char cx = areaKey[areaKey.Length - 2];
      char cy = areaKey[areaKey.Length - 1];

      int x = OPEN_WORLD_MAP_COORDS_X.IndexOf(cx.ToString().ToUpper());
      int y = OPEN_WORLD_MAP_COORDS_Y.IndexOf(cy);

      return new Vector2Int(x, y);
   }

   public static string computeOpenWorldAreaKey (Vector2Int areaCoords) {
      string suffix = computeOpenWorldAreaSuffix(areaCoords);
      return $"{WORLD_MAP_PREFIX}{suffix}";
   }

   public static Vector2Int computeOpenWorldAreaCoordsForBiome (Biome.Type biome) {
      switch (biome) {
         case Biome.Type.Forest:
            return new Vector2Int(1, 6);
         case Biome.Type.Desert:
            return new Vector2Int(2, 2);
         case Biome.Type.Pine:
            return new Vector2Int(7, 0);
         case Biome.Type.Snow:
            return new Vector2Int(8, 6);
         case Biome.Type.Lava:
            return new Vector2Int(12, 2);
         case Biome.Type.Mushroom:
            return new Vector2Int(13, 7);
         default:
            return new Vector2Int(0, 0);
      }
   }

   public static List<Vector2Int> computeOpenWorldAreaCoordsList (List<string> areaKeys) {
      List<Vector2Int> areaCoordsList = new List<Vector2Int>();

      foreach (string areaKey in areaKeys) {
         if (VoyageManager.isWorldMap(areaKey)) {
            areaCoordsList.Add(computeOpenWorldAreaCoords(areaKey));
         }
      }

      return areaCoordsList;
   }

   public static List<string> computeOpenWorldAreaKeysList (IEnumerable<Vector2Int> areaCoordsList) {
      List<string> areaKeys = new List<string>();
      
      foreach (Vector2Int areaCoords in areaCoordsList) {
         areaKeys.Add(computeOpenWorldAreaKey(areaCoords));
      }

      return areaKeys;
   }

   public static List<Vector2Int> computeAreaCoordsForBiomesList (List<Biome.Type> biomes) {
      List<Vector2Int> areaCoordsList = new List<Vector2Int>();

      foreach (Biome.Type biome in biomes) {
         areaCoordsList.Add(computeOpenWorldAreaCoordsForBiome(biome));
      }

      return areaCoordsList;
   }

   public static List<string> getOpenWorldAreasList () {
      List<string> maps = new List<string>();

      for (int row = 0; row < OPEN_WORLD_MAP_COORDS_Y.Length; row++) {
         for (int col = 0; col < OPEN_WORLD_MAP_COORDS_X.Length; col++) {
            maps.Add(computeOpenWorldAreaKey(new Vector2Int(col, row)));
         }
      }

      return maps;
   }

   #region Private Variables

   // Global cache for the World pins
   private static List<WorldMapPanelPinInfo> _worldPinsCache = new List<WorldMapPanelPinInfo>();

   #endregion
}
