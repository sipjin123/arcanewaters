#define NUBIS
#if NUBIS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using UnityEngine;
using NubisDataHandling;
using UnityEngine.Analytics;

namespace Nubis.Controllers {
   public class NubisDataFetchTest : MonoBehaviour {
      #region Public Variables

      // The crafting ingredients
      public List<Item> ingredients = new List<Item>();

      // Test item Id
      public string itemId = "";

      // The events that are triggered after fetching
      public NubisUserInfoEvent nubisUserInfoEvent = new NubisUserInfoEvent();
      public NubisInventoryEvent nubisInventoryEvent = new NubisInventoryEvent();
      public NubisCraftingIngredientEvent nubisCraftingIngredientEvent = new NubisCraftingIngredientEvent();
      public NubisEquippedItemEvent nubisEquippedItemEvent = new NubisEquippedItemEvent();
      public NubisCraftableItemEvent nubisCraftableItemEvent = new NubisCraftableItemEvent();
      
      #endregion

      protected IEnumerator CO_FetchSingleBlueprint (int userId, int itmId) {
         string rawBlueprintData = "";

         yield return rawBlueprintData = Fetch_Single_Blueprint_v4Controller.fetchSingleBlueprint(userId, itmId);
         D.editorLog("Fetching raw data BP is: " + rawBlueprintData, Color.cyan);

         List<CraftableItemData> craftable = CraftableItem.processCraftableGroups(rawBlueprintData, ingredients);

         foreach (CraftableItemData data in craftable) {
            D.editorLog("Info is: " + data.craftableItem.id + " : " + data.craftingStatus + " : " + data.craftableItem.category + " : " + data.craftableItem.itemTypeId, Color.red);
         }
      }

      protected IEnumerator CO_FetchInventoryData (int userId) {
         string weaponRawData = "";
         string armorRawData = "";

         yield return weaponRawData = Fetch_Inventory_v1Controller.userInventory(userId, EquipmentType.Weapon);
         yield return armorRawData = Fetch_Inventory_v1Controller.userInventory(userId, EquipmentType.Armor);

         D.editorLog("Fetching raw data weapon is: " + weaponRawData, Color.cyan);
         D.editorLog("Fetching raw data armor is: " + armorRawData, Color.cyan);

         List<Item> compiledData = new List<Item>();
         List<Item> weaponList = UserInventory.processUserInventory(weaponRawData, EquipmentType.Weapon);
         List<Item> armorList = UserInventory.processUserInventory(armorRawData, EquipmentType.Armor);

         D.editorLog("================ Fetch Weapon: " + weaponList.Count, Color.blue);

         foreach (Item weapon in weaponList) {
            WeaponStatData weaponData = WeaponStatData.getStatData(weapon.data, 2);
            compiledData.Add(weapon);
            D.editorLog("Weapon: " + weapon.category + " : " + weapon.itemTypeId + " : " + weaponData.equipmentName, Color.yellow);
         }

         D.editorLog("================ Fetch Armor: " + armorList.Count, Color.blue);

         foreach (Item armor in armorList) {
            ArmorStatData armorData = ArmorStatData.getStatData(armor.data, 2);
            compiledData.Add(armor);
            D.editorLog("Weapon: " + armor.category + " : " + armor.itemTypeId + " : " + armorData.equipmentName, Color.yellow);
         }

         nubisInventoryEvent.Invoke(compiledData);
      }

      protected IEnumerator CO_FetchUserData (int userId) {
         string rawUserContent = "";
         yield return rawUserContent = User_Data_v1Controller.userData(userId);

         UserInfo userInfo = UserInfoData.processUserInfo(rawUserContent);
         nubisUserInfoEvent.Invoke(userInfo);

         D.editorLog("Fetch: " + rawUserContent, Color.blue);
         D.editorLog("UserInfo AccName: " + userInfo.username, Color.yellow);
         D.editorLog("UserInfo UsrGold: " + userInfo.gold, Color.yellow);
         D.editorLog("UserInfo WepId: " + userInfo.weaponId, Color.yellow);
         D.editorLog("UserInfo ArmId: " + userInfo.armorId, Color.yellow);
      }

      protected IEnumerator CO_FetchIngredients (int userId) {
         string stringContent = "";
         yield return stringContent = Fetch_Crafting_Ingredients_v3Controller.fetchCraftingIngredients(userId);

         D.editorLog("Fetch: " + stringContent, Color.blue);

         List<Item> craftingList = NubisDataHandling.CraftingIngredients.processCraftingIngredients(stringContent);
         D.editorLog("Here are the crafting list: " + craftingList.Count, Color.cyan);

         foreach (Item data in craftingList) {
            D.editorLog("Data: " + data.category + " : " + data.itemTypeId, Color.yellow);
         }

         ingredients = craftingList;
      }

      protected IEnumerator CO_FetchCraftableGroups (int userId) {
         string armorFetch = "";
         string weaponFetch = "";

         yield return armorFetch = Fetch_Craftable_Armors_v4Controller.fetchCraftableArmors(userId);
         yield return weaponFetch = Fetch_Craftable_Weapons_v4Controller.fetchCraftableWeapons(userId);

         D.editorLog("Weapon Fetch: " + weaponFetch, Color.blue);
         D.editorLog("Armor Fetch: " + armorFetch, Color.blue);

         List<CraftableItemData> weaponCraftables = CraftableItem.processCraftableGroups(weaponFetch, ingredients);
         List<CraftableItemData> armorCraftables = CraftableItem.processCraftableGroups(armorFetch, ingredients);

         List<CraftableItemData> compiledCraftables = new List<CraftableItemData>();
         foreach (CraftableItemData weaponData in weaponCraftables) {
            compiledCraftables.Add(weaponData);
            D.editorLog("Weapon: " + weaponData.craftingStatus + " : " + weaponData.craftableItem.category + " : " + weaponData.craftableItem.itemTypeId);
         }
         foreach (CraftableItemData armorData in armorCraftables) {
            compiledCraftables.Add(armorData);
            D.editorLog("Armor: " + armorData.craftingStatus + " : " + armorData.craftableItem.category + " : " + armorData.craftableItem.itemTypeId);
         }

         D.editorLog("Total craftable equipment are: " + compiledCraftables.Count, Color.cyan);
      }

      protected IEnumerator CO_FetchEquippedItems (int userId) {
         string equippedItemContent = "";
         yield return equippedItemContent = Fetch_Equipped_Items_v3Controller.fetchEquippedItems(userId);

         D.editorLog("Fetch: " + equippedItemContent, Color.blue);

         EquippedItemData equippedItemData = EquippedItems.processEquippedItemData(equippedItemContent);

         D.editorLog("The equippedItem weapon is: " + equippedItemData.weaponItem.id + " : " + equippedItemData.weaponItem.category + " : " + equippedItemData.weaponItem.itemTypeId, Color.yellow);
         D.editorLog("The equippedItem armor is: " + equippedItemData.armorItem.id + " : " +equippedItemData.armorItem.category + " : " + equippedItemData.armorItem.itemTypeId, Color.yellow);

      }

      #region Private Variables

      #endregion
   }
}
#endif