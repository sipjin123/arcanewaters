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

   public override string typeDisplayName => "Guild";

   public override bool canUserWarpInto (NetEntity user, string areaKey, out Action<NetEntity> denyWarpHandler) {
      denyWarpHandler = null;

      // Check if the user is in a guild
      if (user.guildId <= 0) {
         return false;
      }

      if (areaKey.Equals(mapTypeAreaKey) || areaKey.Equals(getGuildSpecificAreaKey(user.guildId))) {
         if (user.guildMapBaseId <= 0) {
            D.log("This guild hasn't chosen a map layout yet.");

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
                  ChatManager.self.addChat("Can't enter guild map until the guild leader chooses a layout.", ChatInfo.Type.System);
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

   #region Private Variables

   #endregion
}