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
   public static string getUserInventoryPage (string userId, string catFilter, string currentPageIndex, string defaultItemsPerPage) {
      var categoryFilter = Item.Category.None;
      if (!string.IsNullOrEmpty(catFilter)) {
         bool parsed = Enum.TryParse<Item.Category>(catFilter, true, out Item.Category categoryFilteredParsed);
         if (parsed) {
            categoryFilter = categoryFilteredParsed;
         }
      }

      // Process user info
      UserInfo newUserInfo = new UserInfo();
      newUserInfo = JsonConvert.DeserializeObject<UserInfo>(DB_Main.getUserInfoJSON(userId));
      if (newUserInfo == null) {
         D.editorLog("Something went wrong with Nubis Data Fetch!", Color.red);
      }

      // Process user equipped items
      string equippedItemContent = DB_Main.fetchEquippedItems(userId);
      EquippedItemData equippedItemData = EquippedItems.processEquippedItemData(equippedItemContent);
      Item equippedWeapon = equippedItemData.weaponItem;
      Item equippedArmor = equippedItemData.armorItem;
      Item equippedHat = equippedItemData.hatItem;

      List<Item.Category> newcategoryList = new List<Item.Category>();
      newcategoryList.Add(Item.Category.Weapon);
      newcategoryList.Add(Item.Category.Armor);
      newcategoryList.Add(Item.Category.CraftingIngredients);
      newcategoryList.Add(Item.Category.Blueprint);

      int[] categoryInt = Array.ConvertAll(newcategoryList.ToArray(), x => (int) x);
      string categoryJson = JsonConvert.SerializeObject(categoryInt);

      List<int> itemIdFilter = new List<int>();
      if (equippedWeapon.itemTypeId > 0) {
         itemIdFilter.Add(equippedWeapon.id);
      }
      if (equippedArmor.itemTypeId > 0) {
         itemIdFilter.Add(equippedArmor.id);
      }
      if (equippedHat.itemTypeId > 0) {
         itemIdFilter.Add(equippedHat.id);
      }

      string itemIdJson = JsonConvert.SerializeObject(itemIdFilter.ToArray());
      string itemCountResponse = DB_Main.getItemCount(userId.ToString(), categoryJson, itemIdJson, "0");
      int totalItemCount = int.Parse(defaultItemsPerPage);
      try {
         totalItemCount = int.Parse(itemCountResponse);
      } catch {
         D.editorLog("Failed to parse: " + itemCountResponse);
      }

      if (categoryFilter == Item.Category.Weapon ||
         categoryFilter == Item.Category.Armor ||
         categoryFilter == Item.Category.Hats ||
         categoryFilter == Item.Category.CraftingIngredients ||
         categoryFilter == Item.Category.None) {
         string inventoryData = DB_Main.userInventory(userId, currentPageIndex, ((int) categoryFilter).ToString(),
            equippedItemData.weaponItem.id.ToString(),
            equippedItemData.armorItem.id.ToString(),
            equippedItemData.hatItem.id.ToString(),
            defaultItemsPerPage.ToString());

         InventoryBundle bundle = new InventoryBundle {
            inventoryData = inventoryData,
            equippedHat = equippedHat,
            equippedArmor = equippedArmor,
            equippedWeapon = equippedWeapon,
            user = newUserInfo,
            totalItemCount = totalItemCount
         };
         return JsonConvert.SerializeObject(bundle,new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
      } else {
         return string.Empty;
      }
   }
}
#endif
