﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class CustomFarmManager : CustomMapManager
{
   #region Public Variables

   // The area key of this group of maps
   public static string GROUP_AREA_KEY = "customfarm";

   #endregion

   public override string mapTypeAreaKey => GROUP_AREA_KEY;
   public override string typeDisplayName => "Farm";

   public override int Bkg_GetBaseMapIdFromDB (int ownerId, int guildId) {
      return DB_Main.getCustomFarmBaseId(ownerId);
   }

   public override bool canUserWarpInto (NetEntity user, string areaKey, out Action<NetEntity> denyWarpHandler) {
      // Check if user is trying to warp into his farm
      if (areaKey.Equals(mapTypeAreaKey) || areaKey.Equals(getUserSpecificAreaKey(user.userId))) {
         // If user has a layout for farm, return it's id
         if (user.customFarmBaseId > 0) {
            denyWarpHandler = null;
            return true;
         }

         // Otherwise, show panel for selecting the layout
         denyWarpHandler = (player) => {
            if (player.isServer) {
               foreach (MapCreationTool.Serialization.Map relatedMap in getRelatedMaps()) {
                  D.adminLog("Server CustomFarmMngr: Related custom map is" + " : " + relatedMap.name + " : " + relatedMap.displayName + " : " + relatedMap.id, D.ADMIN_LOG_TYPE.CustomMap);
               }
               player.rpc.Target_ShowCustomMapPanel(mapTypeAreaKey, true, getRelatedMaps());
            } else {
               player.rpc.Cmd_RequestCustomMapPanelClient(mapTypeAreaKey, true);
            }
         };
         return false;
      } else {
         // User is trying to warp into someone else's farm
         // if user should be allowed to warp into someone else's farm, handle appropriately

         denyWarpHandler = null;
         return false;
      }
   }

   public override int getBaseMapId (NetEntity user) {
      return user.customFarmBaseId;
   }

   public override int getBaseMapId (UserInfo userInfo) {
      return userInfo.customFarmBaseId;
   }

   #region Private Variables

   #endregion
}
