using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using Nubis.Controllers;
using UnityEngine.Events;

namespace NubisDataHandling {
   public class NubisDataFetcher : MonoBehaviour {

      #region Public Variables

      // Self
      public static NubisDataFetcher self;

      // The category types that are being fetched
      public Item.Category categoryFilter;

      // The current item cap per page
      public int itemsPerPage;

      // The current page of the item preview
      public int pageIndex;

      #endregion

      private void Awake () {
         self = this;
      }

      public void checkCraftingInfo (int bluePrintId) {
         int userId = Global.player == null ? 0 : Global.player.userId;
         EquippedItemData equippedItemData = new EquippedItemData();
         List<Item> craftingIngredients = new List<Item>();
         List<CraftableItemData> craftableItems = new List<CraftableItemData>();

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string rawBlueprintData = Fetch_Single_Blueprint_v4Controller.fetchSingleBlueprint(userId, bluePrintId);
            string craftingIngredientData = Fetch_Crafting_Ingredients_v3Controller.fetchCraftingIngredients(userId);
            string equippedItemContent = Fetch_Equipped_Items_v3Controller.fetchEquippedItems(userId);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               craftingIngredients = CraftingIngredients.processCraftingIngredients(craftingIngredientData);
               craftableItems = CraftableItem.processCraftableGroups(rawBlueprintData, craftingIngredients);
               equippedItemData = EquippedItems.processEquippedItemData(equippedItemContent);

               // Get the panel
               CraftingPanel craftingPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

               // Show the crafting panel, except if the reward panel is showing
               if (!craftingPanel.isShowing()) {
                  PanelManager.self.pushPanel(craftingPanel.type);
               }

               List<Item> equippedItems = new List<Item>();
               equippedItems.Add(equippedItemData.weaponItem);
               equippedItems.Add(equippedItemData.armorItem);
               craftingPanel.updatePanelWithSingleBlueprintWebRequest(craftableItems[0].craftableItem, equippedItems, craftingIngredients, craftableItems[0].craftableRequirements.combinationRequirements);
            });
         });
      }

      public void fetchCraftableData (int pageIndex, int itemsPerPage) {
         int userId = Global.player == null ? 0 : Global.player.userId;
         List<Item> craftingIngredients = new List<Item>();
         List<Item> craftableItems = new List<Item>();
         List<Blueprint.Status> blueprintStatus = new List<Blueprint.Status>();

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string craftingIngredientXml = Fetch_Crafting_Ingredients_v3Controller.fetchCraftingIngredients(userId);
            string armorFetch = Fetch_Craftable_Armors_v4Controller.fetchCraftableArmors(userId);
            string weaponFetch = Fetch_Craftable_Weapons_v4Controller.fetchCraftableWeapons(userId);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               craftingIngredients = CraftingIngredients.processCraftingIngredients(craftingIngredientXml);
               List<CraftableItemData> weaponCraftables = CraftableItem.processCraftableGroups(weaponFetch, craftingIngredients);
               List<CraftableItemData> armorCraftables = CraftableItem.processCraftableGroups(armorFetch, craftingIngredients);

               foreach (CraftableItemData weaponData in weaponCraftables) {
                  craftableItems.Add(weaponData.craftableItem);
                  blueprintStatus.Add(weaponData.craftingStatus);
               }
               foreach (CraftableItemData armorData in armorCraftables) {
                  craftableItems.Add(armorData.craftableItem);
                  blueprintStatus.Add(armorData.craftingStatus);
               }

               // Get the panel
               CraftingPanel craftingPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

               // Show the crafting panel, except if the reward panel is showing
               if (!craftingPanel.isShowing()) {
                  PanelManager.self.pushPanel(craftingPanel.type);
               }

               craftingPanel.updatePanelWithBlueprintList(craftableItems.ToArray(), blueprintStatus.ToArray(), pageIndex, itemsPerPage);
            });
         });
      }

      public void fetchEquipmentData (int pageIndex = 0, int itemsPerPage = 0, Item.Category[] categoryFilter = null) {
         int userId = Global.player == null ? 0 : Global.player.userId;
         UserInfo newUserInfo = new UserInfo();
         List<Item> userInventory = new List<Item>();

         if (itemsPerPage > 200) {
            D.warning("Requesting too many items per page.");
            return;
         }

         this.categoryFilter = categoryFilter == null ? Item.Category.None : categoryFilter[0];
         this.pageIndex = pageIndex;
         this.itemsPerPage = itemsPerPage;

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string weaponRawData = Fetch_Inventory_v1Controller.userInventory(userId, EquipmentType.Weapon);
            string armorRawData = Fetch_Inventory_v1Controller.userInventory(userId, EquipmentType.Armor);
            string rawUserContent = User_Data_v1Controller.userData(userId);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               List<Item> weaponList = UserInventory.processUserInventory(weaponRawData, EquipmentType.Weapon);
               List<Item> armorList = UserInventory.processUserInventory(armorRawData, EquipmentType.Armor);
               newUserInfo = UserInfoData.processUserInfo(rawUserContent);

               foreach (Item weapon in weaponList) {
                  WeaponStatData weaponData = WeaponStatData.getStatData(weapon.data, 1);
                  userInventory.Add(weapon);
               }

               foreach (Item armor in armorList) {
                  ArmorStatData armorData = ArmorStatData.getStatData(armor.data, 2);
                  userInventory.Add(armor);
               }

               // Calculate the maximum page number
               int maxPage = Mathf.CeilToInt((float) userInventory.Count / itemsPerPage);
               if (maxPage == 0) {
                  maxPage = 1;
               }

               // Clamp the requested page number to the max page - the number of items could have changed
               pageIndex = Mathf.Clamp(pageIndex, 1, maxPage);

               Item equippedWeapon = userInventory.Find(_ => _.id == newUserInfo.weaponId);
               if (equippedWeapon == null) {
                  equippedWeapon = new Item { itemTypeId = 0, color1 = ColorType.None, color2 = ColorType.None };
               }
               Item equippedArmor = userInventory.Find(_ => _.id == newUserInfo.armorId);
               if (equippedArmor == null) {
                  equippedArmor = new Item { itemTypeId = 0, color1 = ColorType.None, color2 = ColorType.None };
               }

               // Get the inventory panel
               InventoryPanel inventoryPanel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

               // Show the inventory panel
               PanelManager.self.pushPanel(inventoryPanel.type);

               UserObjects userObjects = new UserObjects { userInfo = newUserInfo, weapon = equippedWeapon, armor = equippedArmor };
               inventoryPanel.receiveItemForDisplay(userInventory.ToArray(), userObjects, this.categoryFilter, pageIndex);
            });
         });
      }
   }
}