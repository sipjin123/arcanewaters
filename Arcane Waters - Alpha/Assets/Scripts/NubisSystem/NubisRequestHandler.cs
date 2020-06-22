using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class NubisRequestHandler : MonoBehaviour
{
   public enum XmlSlotIndex {
      None = 0,
      Default = 3,
   }

   public static string nubisFetchInventoryCount (string rawUserId) {
      string splitter = "_space_";
      string[] rawItemGroup = rawUserId.Split(new string[] { splitter }, StringSplitOptions.None);

      int userId = int.Parse(rawItemGroup[0]);
      int categoryFilter = int.Parse(rawItemGroup[1]);
      return NubisTranslator.Fetch_Inventory_v1Controller.userInventoryCount(userId, categoryFilter);
   }

   public static string nubisFetchUserData (string rawUserId) {
      int userId = int.Parse(rawUserId);
      return NubisTranslator.User_Data_v1Controller.userData(userId);
   }

   public static string nubisFetchCraftingIngredients (string rawUserId) {
      int userId = int.Parse(rawUserId);
      return NubisTranslator.Fetch_Crafting_Ingredients_v3Controller.fetchCraftingIngredients(userId);
   }

   public static string nubisFetchXmlZipBytes (string slotIndex) {
      int slot = int.Parse(slotIndex);
      return NubisTranslator.Fetch_XmlZip_Bytes_v1Controller.fetchZipRawData(slot);
   }

   public static string nubisFetchXmlVersion (string slotIndex) {
      int slot = int.Parse(slotIndex);
      return NubisTranslator.Fetch_Xml_Version_v1Controller.fetchXmlVersion(slot);
   }

   public static string nubisFetchSingleBlueprint (string rawContent) {
      string splitter = "_space_";
      string[] rawItemGroup = rawContent.Split(new string[] { splitter }, StringSplitOptions.None);

      int blueprintId = int.Parse(rawItemGroup[0]);
      int userId = int.Parse(rawItemGroup[1]);

      return NubisTranslator.Fetch_Single_Blueprint_v4Controller.fetchSingleBlueprint(blueprintId, userId);
   }

   public static string nubisFetchEquippedItems (string rawUserId) {
      int userId = int.Parse(rawUserId);
      return NubisTranslator.Fetch_Equipped_Items_v3Controller.fetchEquippedItems(userId);
   }

   public static string nubisFetchCraftableWeapons (string rawUserId) {
      int userId = int.Parse(rawUserId);
      return NubisTranslator.Fetch_Craftable_Weapons_v4Controller.fetchCraftableWeapons(userId);
   }

   public static string nubisTestFetch (string test1, string test2) {
      return test1 + " : " + test2;
   }

   public static string nubisFetchCraftableHats (string rawUserId) {
      int userId = int.Parse(rawUserId);
      return NubisTranslator.Fetch_Craftable_Hats_v1Controller.fetchCraftableHats(userId);
   }

   public static string nubisFetchCraftableArmors (string rawUserId) {
      int userId = int.Parse(rawUserId);
      return NubisTranslator.Fetch_Craftable_Armors_v4Controller.fetchCraftableArmors(userId);
   }

   public static string nubisFetchInventory (string rawContent) {
      string splitter = "_space_";
      string[] rawItemGroup = rawContent.Split(new string[] { splitter }, StringSplitOptions.None);

      int userId = int.Parse(rawItemGroup[0]);
      int inventoryPage = int.Parse(rawItemGroup[1]);
      int category = int.Parse(rawItemGroup[2]);
      int weaponId = 0;
      int armorId = 0;
      int hatId = 0;

      try {
         weaponId = int.Parse(rawItemGroup[3]);
      } catch {
         // No weapon equipped
      }
      try {
         armorId = int.Parse(rawItemGroup[4]);
      } catch {
         // No armor equipped
      }
      try {
         hatId = int.Parse(rawItemGroup[5]);
      } catch {
         // No hat equipped
      }

      return NubisTranslator.Fetch_Inventory_v1Controller.userInventory(userId, inventoryPage, category, weaponId, armorId, hatId);
   }

   public static string nubisFetchMapData (string rawMapName, string rawMapVersion) {
      rawMapName = rawMapName.Replace("+", " ");
      int mapVersion = int.Parse(rawMapVersion);
      return NubisTranslator.Fetch_Map_Data_v1Controller.fetchMapData(rawMapName, mapVersion);
   }

   public static string nubisFetchUserAbilities (string userId) {
      return NubisTranslator.Fetch_Abilities_v1Controller.userAbilities(int.Parse(userId));
   }

   public static int getSlotIndex () {
      return (int) XmlSlotIndex.Default;
   }
}