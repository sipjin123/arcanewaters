using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class CustomGuildMapManager : CustomMapManager
{
   #region Public Variables

   // The area key of this group of maps
   public static string GROUP_AREA_KEY = "customguildmap";

   #endregion

   public override string mapTypeAreaKey => GROUP_AREA_KEY;

   public override string typeDisplayName => "Guild Map";

   public override bool canUserWarpInto (NetEntity user, string areaKey, out Action<NetEntity> denyWarpHandler) {
      denyWarpHandler = null;

      // Check if the user is in a guild
      if (user.guildId <= 0) {
         return false;
      }

      if (areaKey.Equals(mapTypeAreaKey) || areaKey.Equals(getGuildSpecificAreaKey(user.guildId))) {
         if (user.guildMapBaseId <= 0) {
            D.debug("This guild hasn't chosen a map layout yet.");

            // Check if the user has priveleges to choose a map layout
            if (user.guildPermissions == int.MaxValue) {
               // If they do, show panel for selecting the layout
               denyWarpHandler = (player) => {
                  if (player.isServer) {
                     foreach (MapCreationTool.Serialization.Map relatedMap in getRelatedMaps()) {
                        D.adminLog("Server CustomGuildMapMngr: Related custom map is" + " : " + relatedMap.name + " : " + relatedMap.displayName + " : " + relatedMap.id, D.ADMIN_LOG_TYPE.CustomMap);
                     }
                     player.rpc.Target_ShowCustomMapPanel(mapTypeAreaKey, true, getRelatedMaps());
                  } else {
                     player.rpc.Cmd_RequestCustomMapPanelClient(mapTypeAreaKey, true);
                  }
               };
               return false;
            } else {
               denyWarpHandler = (player) => {
                  player.rpc.Target_DisplayServerMessage("Can't enter guild map until the guild leader chooses a layout.");
               };

               return false;
            }
            
         } else {
            return true;
         }
      } else {
         return false;
      }
   }

   public override int getBaseMapId (NetEntity user) {
      return user.guildMapBaseId;
   }

   public override int getBaseMapId (UserInfo userInfo) {
      return userInfo.guildMapBaseId;
   }

   public static bool canUserFarm (string areaKey, NetEntity user) {
      // Check if this is a guild map
      if (CustomMapManager.isGuildSpecificAreaKey(areaKey)) {
         
         // Check if the user's guild id matches the map's guild id
         if (CustomMapManager.getGuildId(areaKey) == user.guildId && user.guildId > 0) {
            // Add new guild permissions check here once implemented
            return true;
         }
      }

      return false;
   }

   public static string getGuildSpecificAreaKey (int guildId) {
      return GROUP_AREA_KEY + "_guild" + guildId;
   }

   public override int Bkg_GetBaseMapIdFromDB (int ownerId, int guildId) {
      return DB_Main.getCustomGuildMapBaseId(guildId);
   }

   #region Private Variables

   #endregion
}