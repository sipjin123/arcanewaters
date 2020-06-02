using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class POFarmManager : OwnedMapManager
{
   #region Public Variables

   // For testing, set if map was selected
   public bool testSelected;

   #endregion

   public override string mapTypeAreaKey => "pofarm";
   public override string typeDisplayName => "Farm";

   public override bool canUserWarpInto (NetEntity user, string areaKey, out Action<NetEntity> denyWarpHandler) {
      // Check if user is trying to warp into his farm
      if (areaKey.Equals(mapTypeAreaKey) || areaKey.Equals(getUserSpecificAreaKey(user.userId))) {
         // TODO: check if user has a farm, respond appropriately (buy a farm panel, etc.)

         if (!testSelected) {
            denyWarpHandler = (player) => PanelManager.self.get<OwnedMapsPanel>(Panel.Type.OwnedMaps).displayFor(this, true);
            return false;
         }

         denyWarpHandler = null;
         return true;
      } else {
         // User is trying to warp into someone else's farm
         // if user should be allowed to warp into someone else's farm, handle appropriately

         denyWarpHandler = null;
         return false;
      }
   }

   public override string getBaseMapAreaKey (int userId) {
      // TODO: find out on which base map is the user's farm based

      // For testing, return a hardcoded key
      return "pofarm_base some name 1";
   }

   #region Private Variables

   #endregion
}
