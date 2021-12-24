using System;
using System.Collections.Generic;
using MapCreationTool.Serialization;

public abstract class CustomMapManager
{
   // Area key, shared by all maps of this owned map group
   public abstract string mapTypeAreaKey { get; }

   // Display for this type of owned map, e.x. Farm, house
   public abstract string typeDisplayName { get; }

   // Checks if user can warp into a map of this map group, when using 'areaKey'.
   // Returns 'denyWarpHandler' actions which should be executed when denying the warp for the user
   public abstract bool canUserWarpInto (NetEntity user, string areaKey, out Action<NetEntity> denyWarpHandler);

   // Get the base map id, that is selected by the user
   public abstract int getBaseMapId (NetEntity user);
   public abstract int getBaseMapId (UserInfo userInfo);

   // Area key, that is set for a specific instance of a map of this map group
   public string getUserSpecificAreaKey (int userId) {
      return mapTypeAreaKey + "_user" + userId;
   }

   // Checks whether given area key is this map group's area key or this map group's user specific area key
   public bool associatedWithAreaKey (string areaKey) {
      return mapTypeAreaKey.Equals(areaKey) || mapTypeAreaKey.Equals(getMapTypeAreaKey(areaKey));
   }

   // Check if this area key is a user-specific area key
   public static bool isUserSpecificAreaKey (string areaKey) {
      return areaKey.Contains("_user");
   }

   // Extracts map type area key from a user specific area key
   public static string getMapTypeAreaKey (string userSpecificAreaKey) {
      return userSpecificAreaKey.Split(new string[] { "_user" }, StringSplitOptions.RemoveEmptyEntries)[0];
   }

   // Extracts user id from a user specific area key
   public static int getUserId (string userSpecificAreaKey) {
      if (userSpecificAreaKey.Contains("_user")) {
         return int.Parse(userSpecificAreaKey.Split(new string[] { "_user" }, StringSplitOptions.RemoveEmptyEntries)[1]);
      } else {
         return -1;
      }
   }

   public static bool isPrivateCustomArea (string areaKey) {
      return areaKey.Contains(CustomHouseManager.GROUP_AREA_KEY) || areaKey.Contains(CustomFarmManager.GROUP_AREA_KEY) || isUserSpecificAreaKey(areaKey);
   }

   // Gets the main placeholder map and the base maps for this type of custom map
   public Map[] getRelatedMaps () {
      Map mainMap = AreaManager.self.getMapInfo(mapTypeAreaKey);
      if (mainMap == null) {
         D.error($"Cound not find main map for custom map type: { mapTypeAreaKey }");
         return new Map[0];
      }

      List<Map> maps = new List<Map> { mainMap };
      maps.AddRange(AreaManager.self.getChildMaps(mainMap));
      return maps.ToArray();
   }
}
