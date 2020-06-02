using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class PlayerOwnedHouseManager : OwnedMapManager
{
   #region Public Variables

   #endregion

   public override string mapTypeAreaKey => "poh";
   public override string typeDisplayName => "House";

   public override bool canUserWarpInto (NetEntity user, string areaKey, out Action<NetEntity> denyWarpHandler) {
      // Check if user is trying to warp into his house
      if (areaKey.Equals(mapTypeAreaKey) || areaKey.Equals(getUserSpecificAreaKey(user.userId))) {
         // TODO: check if user has a house, respond appropriately (buy a house panel, etc.)

         // For testing, allow user to access his house
         denyWarpHandler = null;
         return true;
      } else {
         // User is trying to warp into someone else's house
         // TODO: check if user is allowed to access some else's house

         denyWarpHandler = null;
         return true;
      }
   }

   public override string getBaseMapAreaKey (int userId) {
      // TODO: find out on which base map is the user's house based

      // For testing, return a hard-coded map
      return "pineward_adventure";
   }

   #region Private Variables

   #endregion
}
