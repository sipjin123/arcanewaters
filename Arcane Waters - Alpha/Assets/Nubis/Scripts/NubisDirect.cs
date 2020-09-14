#if NUBIS
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
         D.editorLog("Requesting too many items per page.");
         return string.Empty;
      }

      // Process user info
      UserInfo newUserInfo = new UserInfo();
      newUserInfo = JsonConvert.DeserializeObject<UserInfo>(DB_Main.getUserInfoJSON(userIdStr));
      if (newUserInfo == null) {
         D.editorLog("Something went wrong with Nubis Data Fetch!", Color.red);
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
         totalItemCount = itemCount
      };
      return JsonConvert.SerializeObject(bundle,new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
   }
}
#endif
