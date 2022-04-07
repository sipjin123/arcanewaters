using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class CustomGuildHouseManager : CustomMapManager
{
   #region Public Variables

   // The area key of this group of maps
   public static string GROUP_AREA_KEY = "customguildhouse";

   #endregion

   public override string mapTypeAreaKey => GROUP_AREA_KEY;
   public override string typeDisplayName => "Guild House";

   public override bool canUserWarpInto (NetEntity user, string areaKey, out Action<NetEntity> denyWarpHandler) {
      denyWarpHandler = null;

      // Check if the user is in a guild
      if (user.guildId <= 0) {
         return false;
      }

      if (areaKey.Equals(mapTypeAreaKey) || areaKey.Equals(getGuildSpecificAreaKey(user.guildId))) {
         if (user.guildHouseBaseId <= 0) {
            D.debug("This guild hasn't chosen a map layout yet.");

            // Check if the user has priveleges to choose a map layout
            if (user.guildPermissions == int.MaxValue) {
               // If they do, show panel for selecting the layout
               denyWarpHandler = (player) => {
                  if (player.isServer) {
                     foreach (MapCreationTool.Serialization.Map relatedMap in getRelatedMaps()) {
                        D.adminLog("Server CustomGuildHouseMngr: Related custom map is" + " : " + relatedMap.name + " : " + relatedMap.displayName + " : " + relatedMap.id, D.ADMIN_LOG_TYPE.CustomMap);
                     }
                     player.rpc.Target_ShowCustomMapPanel(mapTypeAreaKey, true, getRelatedMaps());
                  } else {
                     player.rpc.Cmd_RequestCustomMapPanelClient(mapTypeAreaKey, true);
                  }
               };
               return false;
            } else {
               denyWarpHandler = (player) => {
                  player.rpc.Target_DisplayServerMessage("Can't enter guild house until the guild leader chooses a layout.");
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
      return user.guildHouseBaseId;
   }

   public override int getBaseMapId (UserInfo userInfo) {
      return userInfo.guildHouseBaseId;
   }

   public static string getGuildSpecificAreaKey (int guildId) {
      return GROUP_AREA_KEY + "_guild" + guildId;
   }

   public override int Bkg_GetBaseMapIdFromDB (int ownerId, int guildId) {
      return DB_Main.getCustomGuildHouseBaseId(guildId);
   }

   #region Private Variables

   #endregion
}
