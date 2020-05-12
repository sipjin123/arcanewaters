﻿using System;
using MapCustomization;

public abstract class OwnedMapManager
{
   // Area key, shared by all maps of this owned map group
   public abstract string mapTypeAreaKey { get; }

   // Checks if user can warp into a map of this map group, when using 'areaKey'.
   // Returns 'denyWarpHandler' actions which should be executed when denying the warp for the user
   public abstract bool canUserWarpInto (NetEntity user, string areaKey, out Action<NetEntity> denyWarpHandler);

   // Given a user specific area key, get the area key for the base map of this map
   public abstract string getBaseMapAreaKey (string userSpecificAreaKey);

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
      return int.Parse(userSpecificAreaKey.Split(new string[] { "_user" }, StringSplitOptions.RemoveEmptyEntries)[1]);
   }
}
