﻿#if NUBIS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NubisDataHandling;
using UnityEngine;

/// <summary>
/// Allows Nubis to do some local computations before returning results.
/// </summary>
public class NubisDirect
{
   public static string getUserInventoryPage (string userIdStr, string categoryFilterJSON,
      string currentPageStr, string itemsPerPageStr) {

      if (int.TryParse(itemsPerPageStr, out int itemsPerPage) && itemsPerPage > 200) {
         D.debug("Requesting too many items per page.");
         return string.Empty;
      }

      // Process user info
      UserInfo newUserInfo = new UserInfo();
      try {
         newUserInfo = JsonConvert.DeserializeObject<UserInfo>(DB_Main.getUserInfoJSON(userIdStr));
         if (newUserInfo == null) {
            D.debug("Something went wrong with Nubis Data Fetch!");
         }
      } catch {
         D.debug("Failed to deserialize user info! {" + userIdStr + "}");
      }

      // Process guild info
      GuildInfo guildInfo = new GuildInfo();
      if (newUserInfo.guildId > 0) {
         string temp = DB_Main.getGuildInfoJSON(newUserInfo.guildId);
         try {
            guildInfo = JsonConvert.DeserializeObject<GuildInfo>(temp);
            if (guildInfo == null) {
               D.debug("Something went wrong with guild info data fetch for user {" + userIdStr + "}");
            }
         } catch {
            D.debug("Failed to deserialize guild info! {" + userIdStr + "}");
         }
      }

      // Process user equipped items
      string equippedItemContent = DB_Main.fetchEquippedItems(userIdStr);
      EquippedItemData equippedItemData = EquippedItems.processEquippedItemData(equippedItemContent);
      Item equippedWeapon = equippedItemData.weaponItem;
      Item equippedArmor = equippedItemData.armorItem;
      Item equippedHat = equippedItemData.hatItem;

      // Create an empty item id filter
      int[] itemIdsToExclude = new int[0];
      string itemIdsToExcludeJSON = JsonConvert.SerializeObject(itemIdsToExclude);

      string itemCountResponse = DB_Main.userInventoryCount(userIdStr, categoryFilterJSON, itemIdsToExcludeJSON, "1");
      if (!int.TryParse(itemCountResponse, out int itemCount)) {
         D.editorLog("Failed to parse: " + itemCountResponse);
         itemCount = 0;
      }

      string inventoryData = DB_Main.userInventory(userIdStr, categoryFilterJSON, itemIdsToExcludeJSON, "1",
         currentPageStr, itemsPerPageStr);

      InventoryBundle bundle = new InventoryBundle {
         inventoryData = inventoryData,
         equippedHat = equippedHat,
         equippedArmor = equippedArmor,
         equippedWeapon = equippedWeapon,
         user = newUserInfo,
         guildInfo = guildInfo,
         totalItemCount = itemCount
      };
      return JsonConvert.SerializeObject(bundle,new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
   }
}
#endif
