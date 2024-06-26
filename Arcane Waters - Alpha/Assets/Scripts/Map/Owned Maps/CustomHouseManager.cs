﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class CustomHouseManager : CustomMapManager
{
   #region Public Variables

   // The area key of this group of maps
   public static string GROUP_AREA_KEY = "customhouse";

   #endregion

   public override string mapTypeAreaKey => GROUP_AREA_KEY;
   public override string typeDisplayName => "House";

   public override int Bkg_GetBaseMapIdFromDB (int ownerId, int guildId) {
      return DB_Main.getCustomHouseBaseId(ownerId);
   }

   public override bool canUserWarpInto (NetEntity user, string areaKey, out Action<NetEntity> denyWarpHandler) {
      // Check if user is trying to warp into his house
      if (areaKey.Equals(mapTypeAreaKey) || areaKey.Equals(getUserSpecificAreaKey(user.userId))) {
         // If user has a layout for house, return it's id
         if (user.customHouseBaseId > 0) {
            denyWarpHandler = null;
            return true;
         }
         
         // Otherwise, show panel for selecting the layout
         denyWarpHandler = (player) => {
            if (player.isServer) {
               foreach (MapCreationTool.Serialization.Map relatedMap in getRelatedMaps()) {
                  D.adminLog("Server CustomHouseMngr: Related custom map is" + " : " + relatedMap.name + " : " + relatedMap.displayName + " : " + relatedMap.id, D.ADMIN_LOG_TYPE.CustomMap);
               }
               player.rpc.Target_ShowCustomMapPanel(mapTypeAreaKey, true, getRelatedMaps());
            } else {
               player.rpc.Cmd_RequestCustomMapPanelClient(mapTypeAreaKey, true);
            }
         };
         return false;
      } else {
         // User is trying to warp into someone else's house
         // if user should be allowed to warp into someone else's house, handle appropriately

         denyWarpHandler = null;
         return false;
      }
   }

   public override int getBaseMapId (NetEntity user) {
      return user.customHouseBaseId;
   }

   public override int getBaseMapId (UserInfo userInfo) {
      return userInfo.customHouseBaseId;
   }

   #region Private Variables

   #endregion
}
