using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class WorldMapManager : MonoBehaviour
{
   #region Public Variables

   // The x coordinates for the Open World Map
   public const string WORLD_MAP_COORDS_X = "ABCDEFGHIJKLMNO";

   // The y coordinates for the Open World Map
   public const string WORLD_MAP_COORDS_Y = "012345678";

   // Prefix for the areas in the Open World Map
   public const string WORLD_MAP_PREFIX = "world_map_";

   // X coordinate of the Origin of the Open World Map
   public const int WORLD_MAP_GEO_COORDS_X = 21;

   // Y coordinate of the Origin of the Open World Map
   public const int WORLD_MAP_GEO_COORDS_Y = 55;

   // Self
   public static WorldMapManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public void Update () {
      if (Global.player != null) {
         updatePlayerSpot();
      }
   }

   private void updatePlayerSpot () {
      WorldMapSpot spot = getSpotFromPosition(Global.player.areaKey, Global.player.transform.localPosition);

      if (spot == null) {
         return;
      }

      _playerSpotCache[Global.player.userId] = spot;
   }

   private static bool areValidOpenWorldMapAreaCoords (WorldMapAreaCoords areaCoords) {
      if (areaCoords.x < 0 || areaCoords.x >= WORLD_MAP_COORDS_X.Length || areaCoords.y < 0 || areaCoords.y >= WORLD_MAP_COORDS_Y.Length) {
         return false;
      }

      return true;
   }

   public bool getNextArea (string areaKey, Direction direction, out string nextAreaKey) {
      WorldMapAreaCoords mapAreaCoords = getAreaCoords(areaKey);
      nextAreaKey = "";

      if (!areValidOpenWorldMapAreaCoords(mapAreaCoords)) {
         return false;
      }

      switch (direction) {
         case Direction.North:
            mapAreaCoords.y += 1;
            break;
         case Direction.East:
            mapAreaCoords.x += 1;
            break;
         case Direction.South:
            mapAreaCoords.y -= 1;
            break;
         case Direction.West:
            mapAreaCoords.x -= 1;
            break;
      }

      if (!areValidOpenWorldMapAreaCoords(mapAreaCoords)) {
         return false;
      }

      nextAreaKey = getAreaKey(mapAreaCoords);
      return true;
   }

   public static string getAreaSuffix (WorldMapAreaCoords areaCoords) {
      if (!areValidOpenWorldMapAreaCoords(areaCoords)) {
         return "";
      }

      char cx = WORLD_MAP_COORDS_X[areaCoords.x];
      char cy = WORLD_MAP_COORDS_Y[areaCoords.y];

      return $"{cx}{cy}".ToUpper();
   }

   public static WorldMapAreaCoords getAreaCoords (string areaKey) {
      if (Util.isEmpty(areaKey) || areaKey.Length < 2 || !areaKey.StartsWith(WORLD_MAP_PREFIX)) {
         return new WorldMapAreaCoords(-1, -1);
      }

      char cx = areaKey[areaKey.Length - 2];
      char cy = areaKey[areaKey.Length - 1];

      int x = WORLD_MAP_COORDS_X.IndexOf(cx.ToString().ToUpper());
      int y = WORLD_MAP_COORDS_Y.IndexOf(cy);

      return new WorldMapAreaCoords(x, y);
   }

   public static string getAreaKey (WorldMapAreaCoords areaCoords) {
      string suffix = getAreaSuffix(areaCoords);
      return $"{WORLD_MAP_PREFIX}{suffix}";
   }

   public WorldMapAreaCoords getAreaCoordsForBiome (Biome.Type biome) {
      switch (biome) {
         case Biome.Type.Forest:
            return new WorldMapAreaCoords(1, 6);
         case Biome.Type.Desert:
            return new WorldMapAreaCoords(2, 2);
         case Biome.Type.Pine:
            return new WorldMapAreaCoords(7, 0);
         case Biome.Type.Snow:
            return new WorldMapAreaCoords(8, 6);
         case Biome.Type.Lava:
            return new WorldMapAreaCoords(12, 2);
         case Biome.Type.Mushroom:
            return new WorldMapAreaCoords(13, 7);
         default:
            return new WorldMapAreaCoords(0, 0);
      }
   }

   public List<WorldMapAreaCoords> getAreaCoordsList (List<string> areaKeys) {
      List<WorldMapAreaCoords> areaCoordsList = new List<WorldMapAreaCoords>();

      foreach (string areaKey in areaKeys) {
         if (isWorldMapArea(areaKey)) {
            areaCoordsList.Add(getAreaCoords(areaKey));
         }
      }

      return areaCoordsList;
   }

   public List<string> getAreaKeysList (IEnumerable<WorldMapAreaCoords> areaCoordsList) {
      List<string> areaKeys = new List<string>();

      foreach (WorldMapAreaCoords areaCoords in areaCoordsList) {
         areaKeys.Add(getAreaKey(areaCoords));
      }

      return areaKeys;
   }

   public List<WorldMapAreaCoords> getAreaCoordsForBiomesList (List<Biome.Type> biomes) {
      List<WorldMapAreaCoords> areaCoordsList = new List<WorldMapAreaCoords>();

      foreach (Biome.Type biome in biomes) {
         areaCoordsList.Add(getAreaCoordsForBiome(biome));
      }

      return areaCoordsList;
   }

   public static List<string> getAllAreasList () {
      List<string> maps = new List<string>();

      for (int row = 0; row < WORLD_MAP_COORDS_Y.Length; row++) {
         for (int col = 0; col < WORLD_MAP_COORDS_X.Length; col++) {
            maps.Add(getAreaKey(new WorldMapAreaCoords(col, row)));
         }
      }

      return maps;
   }

   public Vector2Int getMapSize () {
      return new Vector2Int(WORLD_MAP_COORDS_X.Length, WORLD_MAP_COORDS_Y.Length);
   }

   public static bool isWorldMapArea (string areaKey) {
      return !Util.isEmpty(areaKey) && areaKey.StartsWith(WORLD_MAP_PREFIX);
   }

   public Vector3 getPositionFromSpot (string areaKey, WorldMapSpot spot) {
      try {
         Area area = AreaManager.self.getArea(areaKey);
         Vector2Int areaTileSize = area.mapTileSize;
         Vector2 halfAreaSize = area.getAreaHalfSizeWorld();
         Vector2 relativePositionUnscaled = isWorldMapArea(areaKey) ? new Vector2(spot.areaX, spot.areaY) : new Vector2(spot.subAreaX, spot.subAreaY);
         Vector2 relativePositionScaled = relativePositionUnscaled * 0.16f;
         return new Vector3(relativePositionScaled.x - halfAreaSize.x, relativePositionScaled.y + halfAreaSize.y);
      } catch {
      }

      return Vector3.zero;
   }

   public WorldMapSpot getSpotFromPosition (string areaKey, Vector3 localPosition) {
      try {
         Area area = AreaManager.self.getArea(areaKey);
         Vector2Int areaTileSize = area.mapTileSize;
         Vector2 halfAreaSize = area.getAreaHalfSizeWorld();
         Vector2 entityPositionFromAreaTopLeftCornerScaled = new Vector2(localPosition.x + halfAreaSize.x, localPosition.y - halfAreaSize.y);
         Vector2 entityPositionFromAreaTopLeftCornerUnscaled = entityPositionFromAreaTopLeftCornerScaled / 0.16f;

         if (isWorldMapArea(areaKey)) {
            WorldMapAreaCoords areaCoords = getAreaCoords(areaKey);
            return new WorldMapSpot {
               areaWidth = areaTileSize.x,
               areaHeight = areaTileSize.y,
               worldX = areaCoords.x,
               worldY = areaCoords.y,
               areaX = entityPositionFromAreaTopLeftCornerUnscaled.x,
               areaY = entityPositionFromAreaTopLeftCornerUnscaled.y
            };
         } else {
            IEnumerable<WorldMapSpot> warpSpots = _spots.Where(_ => _.type == WorldMapSpot.SpotType.Warp);
            WorldMapSpot warpToCurrentArea = warpSpots.FirstOrDefault(_ => _.target == areaKey);

            if (warpToCurrentArea != null) {
               return new WorldMapSpot {
                  areaWidth = warpToCurrentArea.areaWidth,
                  areaHeight = warpToCurrentArea.areaHeight,
                  worldX = warpToCurrentArea.worldX,
                  worldY = warpToCurrentArea.worldY,
                  areaX = warpToCurrentArea.areaX,
                  areaY = warpToCurrentArea.areaY,
                  subAreaX = entityPositionFromAreaTopLeftCornerUnscaled.x,
                  subAreaY = entityPositionFromAreaTopLeftCornerUnscaled.y,
                  subAreaKey = areaKey
               };
            } else {
               // A spot for the current position couldn't be computed,
               // so instead use the latest known position of the player as a fallback
               if (_playerSpotCache.TryGetValue(Global.player.userId, out WorldMapSpot value)) {
                  return value;
               }
            }
         }
      } catch {
      }

      return null;
   }

   public List<WorldMapSpot> getSpots () {
      return _spots;
   }

   public List<WorldMapAreaCoords> getVisitedAreasCoordsList () {
      return _visitedAreasCoords;
   }

   public void setWorldMapData (List<WorldMapAreaCoords> visitedAreasCoords, List<WorldMapSpot> spots) {
      _visitedAreasCoords = visitedAreasCoords;
      _spots = spots;
   }

   public WorldMapGeoCoords getGeoCoordsFromSpot (WorldMapSpot spot) {
      Vector2Int worldSize = getMapSize();
      int worldX = WORLD_MAP_GEO_COORDS_X + spot.worldX;
      int worldY = WORLD_MAP_GEO_COORDS_Y + (worldSize.y - 1 - spot.worldY);
      float areaX = spot.areaX / spot.areaWidth * 255;
      float areaY = -spot.areaY / spot.areaHeight * 255;
      float targetX = spot.subAreaX;
      float targetY = -spot.subAreaY;
      string subAreaKey = spot.subAreaKey;

      return new WorldMapGeoCoords {
         worldX = worldX,
         worldY = worldY,
         areaX = areaX,
         areaY = areaY,
         subAreaX = targetX,
         subAreaY = targetY,
         subAreaKey = subAreaKey
      };
   }

   public WorldMapSpot getSpotFromGeoCoords (WorldMapGeoCoords geoCoords) {
      Vector2Int worldSize = getMapSize();
      int areaWidth = 256;
      int areaHeight = 256;

      return new WorldMapSpot {
         areaWidth = areaWidth,
         areaHeight = areaHeight,
         worldX = geoCoords.worldX - WORLD_MAP_GEO_COORDS_X,
         worldY = -geoCoords.worldY + WORLD_MAP_GEO_COORDS_Y + worldSize.y - 1,
         areaX = geoCoords.areaX / 255 * areaWidth,
         areaY = -geoCoords.areaY / 255 * areaHeight,
         subAreaX = geoCoords.subAreaX,
         subAreaY = -geoCoords.subAreaY,
         subAreaKey = geoCoords.subAreaKey
      };
   }

   public string encodeGeoCoords (WorldMapGeoCoords geoCoords) {
      try {
         return JsonUtility.ToJson(geoCoords);
      } catch {
         return string.Empty;
      }
   }

   public WorldMapGeoCoords decodeGeoCoords (string geoCoordsStr) {
      try {
         return JsonUtility.FromJson<WorldMapGeoCoords>(geoCoordsStr);
      } catch {
         return null;
      }
   }

   public string encodeSpot (WorldMapSpot spot) {
      try {
         return JsonUtility.ToJson(spot);
      } catch {
         return string.Empty;
      }
   }

   public WorldMapSpot decodeSpot (string geoCoordsStr) {
      try {
         return JsonUtility.FromJson<WorldMapSpot>(geoCoordsStr);
      } catch {
         return null;
      }
   }

   public string getStringFromGeoCoords (WorldMapGeoCoords geoCoords) {
      float adjustedAreaY = geoCoords.areaY / 255 * 999;
      float adjustedAreaX = geoCoords.areaX / 255 * 999;

      if (!Util.isEmpty(geoCoords.subAreaKey)) {
         int subAreaId = AreaManager.self.getAreaId(geoCoords.subAreaKey);

         if (subAreaId >= 0) {
            string subAreaIdStr = subAreaId.ToString("D3");
            string subAreaXStr = geoCoords.subAreaX.ToString("F0");
            string subAreaYStr = geoCoords.subAreaY.ToString("F0");
            return $"<{geoCoords.worldY}.{Mathf.FloorToInt(adjustedAreaY)}.{subAreaIdStr}{subAreaYStr}, {geoCoords.worldX}.{Mathf.FloorToInt(adjustedAreaX)}.{subAreaIdStr}{subAreaXStr}>";
         }
      }

      return $"<{geoCoords.worldY}.{Mathf.FloorToInt(adjustedAreaY)}, {geoCoords.worldX}.{Mathf.FloorToInt(adjustedAreaX)}>";
   }

   public string getDisplayStringFromGeoCoords (WorldMapGeoCoords geoCoords) {
      float adjustedAreaY = geoCoords.areaY / 255 * 999;
      float adjustedAreaX = geoCoords.areaX / 255 * 999;

      if (!Util.isEmpty(geoCoords.subAreaKey)) {
         int subAreaId = AreaManager.self.getAreaId(geoCoords.subAreaKey);

         if (subAreaId >= 0) {
            string subAreaIdStr = subAreaId.ToString("D3");
            string subAreaXStr = geoCoords.subAreaX.ToString("F0");
            string subAreaYStr = geoCoords.subAreaY.ToString("F0");
            return $"<{geoCoords.worldY}.{Mathf.FloorToInt(adjustedAreaY)}.*, {geoCoords.worldX}.{Mathf.FloorToInt(adjustedAreaX)}.*>";
         }
      }

      return $"<{geoCoords.worldY}.{Mathf.FloorToInt(adjustedAreaY)}, {geoCoords.worldX}.{Mathf.FloorToInt(adjustedAreaX)}>";
   }

   public string getLatitudeFromGeoCoords (WorldMapGeoCoords geoCoords) {
      float adjustedAreaY = geoCoords.areaY / 255 * 999;

      if (!Util.isEmpty(geoCoords.subAreaKey)) {
         int subAreaId = AreaManager.self.getAreaId(geoCoords.subAreaKey);

         if (subAreaId >= 0) {
            string subAreaIdStr = subAreaId.ToString("D3");
            string subAreaYStr = geoCoords.subAreaY.ToString("F0");
            return $"{geoCoords.worldY}.{Mathf.FloorToInt(adjustedAreaY)}.{subAreaIdStr}{subAreaYStr}";
         }
      }

      return $"{geoCoords.worldY}.{Mathf.FloorToInt(adjustedAreaY)}";
   }

   public string getLongitudeFromGeoCoords (WorldMapGeoCoords geoCoords) {
      float adjustedAreaX = geoCoords.areaX / 255 * 999;

      if (!Util.isEmpty(geoCoords.subAreaKey)) {
         int subAreaId = AreaManager.self.getAreaId(geoCoords.subAreaKey);

         if (subAreaId >= 0) {
            string subAreaIdStr = subAreaId.ToString("D3");
            string subAreaXStr = geoCoords.subAreaX.ToString("F0");
            return $"{geoCoords.worldX}.{Mathf.FloorToInt(adjustedAreaX)}.{subAreaIdStr}{subAreaXStr}";
         }
      }

      return $"{geoCoords.worldX}.{Mathf.FloorToInt(adjustedAreaX)}";
   }

   public WorldMapGeoCoords getGeoCoordsFromString (string geoCoordsStr, out int startIndex, out int strLength) {
      startIndex = -1;
      strLength = 0;

      if (Util.isEmpty(geoCoordsStr)) {
         return null;
      }

      int prefixIndex = geoCoordsStr.IndexOf("<");
      if (prefixIndex < 0) {
         return null;
      }

      int suffixIndex = geoCoordsStr.IndexOf(">");
      if (suffixIndex < 0) {
         return null;
      }

      int commaIndex = geoCoordsStr.IndexOf(",");
      if (prefixIndex >= commaIndex || commaIndex >= suffixIndex) {
         return null;
      }

      startIndex = prefixIndex;
      strLength = suffixIndex + 1 - prefixIndex;

      // Latitude is the first coordinate
      string latitudeString = geoCoordsStr.Substring(prefixIndex + 1, commaIndex - prefixIndex - 1);
      string longitudeString = geoCoordsStr.Substring(commaIndex + 1, suffixIndex - commaIndex - 1);

      string[] latitudeStringTokens = latitudeString.Split(new[] { '.' }, System.StringSplitOptions.RemoveEmptyEntries);
      string[] longitudeStringTokens = longitudeString.Split(new[] { '.' }, System.StringSplitOptions.RemoveEmptyEntries);

      WorldMapGeoCoords newGeoCoords = new WorldMapGeoCoords();

      if (longitudeStringTokens.Length > 1) {
         newGeoCoords.worldX = int.Parse(longitudeStringTokens[0]);
         newGeoCoords.areaX = (int) (float.Parse(longitudeStringTokens[1]) / 999.0f * 255.0f);
      }

      if (latitudeStringTokens.Length > 1) {
         newGeoCoords.worldY = int.Parse(latitudeStringTokens[0]);
         newGeoCoords.areaY = (int) (float.Parse(latitudeStringTokens[1]) / 999.0f * 255.0f);
      }

      if (longitudeStringTokens.Length > 2) {
         string subAreaIdStr = longitudeStringTokens[2].Substring(0, 3);
         string subAreaXStr = longitudeStringTokens[2].Substring(3);
         float subAreaX = float.Parse(subAreaXStr);
         string subAreaKey = AreaManager.self.getAreaName(int.Parse(subAreaIdStr));
         newGeoCoords.subAreaX = subAreaX;
         newGeoCoords.subAreaKey = subAreaKey;
      }

      if (latitudeStringTokens.Length > 2) {
         string subAreaIdStr = latitudeStringTokens[2].Substring(0, 3);
         string subAreaYStr = latitudeStringTokens[2].Substring(3);
         float subAreaY = float.Parse(subAreaYStr);
         string subAreaKey = AreaManager.self.getAreaName(int.Parse(subAreaIdStr));
         newGeoCoords.subAreaY = subAreaY;
         newGeoCoords.subAreaKey = subAreaKey;
      }

      return newGeoCoords;
   }

   public WorldMapGeoCoords getGeoCoordsFromWorldMapAreaCoords (WorldMapAreaCoords areaCoords) {
      return new WorldMapGeoCoords {
         worldX = WORLD_MAP_GEO_COORDS_X + areaCoords.x,
         worldY = WORLD_MAP_GEO_COORDS_Y + (getMapSize().y - 1 - areaCoords.y)
      };
   }

   public WorldMapAreaCoords getWorldMapAreaCoordsFromGeoCoords (WorldMapGeoCoords geoCoords) {
      return new WorldMapAreaCoords {
         x = geoCoords.worldX - WORLD_MAP_GEO_COORDS_X,
         y = -geoCoords.worldY + WORLD_MAP_GEO_COORDS_Y + getMapSize().y - 1,
      };
   }

   public bool areGeoCoordsValid (WorldMapGeoCoords geoCoords) {
      if (geoCoords.worldX < WORLD_MAP_GEO_COORDS_X ||
         geoCoords.worldY < WORLD_MAP_GEO_COORDS_Y ||
         geoCoords.worldX >= WORLD_MAP_GEO_COORDS_X + getMapSize().x ||
         geoCoords.worldY >= WORLD_MAP_GEO_COORDS_Y + getMapSize().y ||
         geoCoords.areaX < 0 || geoCoords.areaX > 255 ||
         geoCoords.areaY < 0 || geoCoords.areaY > 255) {
         return false;
      }

      return true;
   }

   public bool areSpotsInTheSamePosition (WorldMapSpot spotA, WorldMapSpot spotB) {
      if (spotA.worldX == spotB.worldX &&
         spotA.worldY == spotB.worldY &&
         spotA.areaX == spotB.areaX &&
         spotA.areaY == spotB.areaY &&
         spotA.subAreaKey == spotB.subAreaKey &&
         spotA.subAreaX == spotB.subAreaX &&
         spotA.subAreaY == spotB.subAreaY
         ) {
         return true;
      }

      return false;
   }

   public static bool doesNewAreaExtendWorldVisitStreak (List<string> visitLog, string newArea) {
      // Checks if visiting an area extends world map area visiting streak
      // Checks if newArea is unique in the chain of visited world map areas
      // Assumes newArea was already added to the visit log, visit log ordered from newest to oldest

      // If new area is not open-world, it won't
      if (!isWorldMapArea(newArea)) {
         return false;
      }

      HashSet<string> visited = new HashSet<string>();
      for (int i = 1; i < visitLog.Count; i++) {
         // If this is not a world map area, this is the end of the streak
         if (!isWorldMapArea(visitLog[i])) {
            break;
         }

         visited.Add(visitLog[i]);
      }

      // If newArea is not in visit streak, then it will extend the streak
      return !visited.Contains(newArea);
   }

   #region Private Variables

   // Client cache for the world spots
   private List<WorldMapSpot> _spots = new List<WorldMapSpot>();

   // Client cache for the visited open world map areas
   private List<WorldMapAreaCoords> _visitedAreasCoords = new List<WorldMapAreaCoords>();

   // Client cache to store the current spot for the player
   private Dictionary<int, WorldMapSpot> _playerSpotCache = new Dictionary<int, WorldMapSpot>();

   #endregion
}
