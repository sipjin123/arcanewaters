using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class CustomHouseManager : CustomMapManager
{
   #region Public Variables

   #endregion

   public override string mapTypeAreaKey => "customhouse";
   public override string typeDisplayName => "House";

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
