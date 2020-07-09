using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace NubisDataHandling {

   public class XmlVersionEvent : UnityEvent<int> { 
   }

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

      // The web directory
      public string webDirectory = "";

      // The xml version event 
      public XmlVersionEvent xmlVersionEvent = new XmlVersionEvent();

      // Space syntax for data sending
      public static string SPACER = "_space_";

      #endregion

      private void Awake () {
         self = this;
         webDirectory = "http://" + Global.getAddress(MyNetworkManager.ServerType.AmazonVPC) + ":7900/";
      }

      public void fetchXmlVersion () {
         processXmlVersion();
      }

      private async void processXmlVersion () {
         string returnCode = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchXmlVersion), NubisRequestHandler.getSlotIndex().ToString());
         try {
            int xmlVersion = int.Parse(returnCode);
            xmlVersionEvent.Invoke(xmlVersion);
         } catch {
            xmlVersionEvent.Invoke(0);
            D.debug("Something went wrong with xml version fetching! Report: " + nameof(NubisRequestHandler.nubisFetchXmlVersion));
            D.debug("Return code is: " + returnCode);
         }
      }

      public void checkCraftingInfo (int bluePrintId) {
         processCraftingInfo(bluePrintId);
      }

      private async void processCraftingInfo (int bluePrintId) {
         // Get the panel
         CraftingPanel craftingPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

         // Show the crafting panel, except if the reward panel is showing
         if (!craftingPanel.isShowing()) {
            PanelManager.self.pushPanel(craftingPanel.type);
         }

         int userId = Global.player == null ? 0 : Global.player.userId;
         EquippedItemData equippedItemData = new EquippedItemData();
         List<Item> craftingIngredients = new List<Item>();
         List<CraftableItemData> craftableItems = new List<CraftableItemData>();

         string rawBlueprintData = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchSingleBlueprint), bluePrintId + "_space_" + userId.ToString());
         string craftingIngredientData = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchCraftingIngredients), userId.ToString());
         string equippedItemContent = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchEquippedItems), userId.ToString());

         craftingIngredients = CraftingIngredients.processCraftingIngredients(craftingIngredientData);
         craftableItems = CraftableItem.processCraftableGroups(rawBlueprintData, craftingIngredients);
         equippedItemData = EquippedItems.processEquippedItemData(equippedItemContent);

         List<Item> equippedItems = new List<Item>();
         equippedItems.Add(equippedItemData.weaponItem);
         equippedItems.Add(equippedItemData.armorItem);
         equippedItems.Add(equippedItemData.hatItem);
         craftingPanel.updatePanelWithSingleBlueprintWebRequest(craftableItems[0].craftableItem, equippedItems, craftingIngredients, craftableItems[0].craftableRequirements.combinationRequirements);
      }

      public void fetchCraftableData (int pageIndex, int itemsPerPage) {
         processCraftableData(pageIndex, itemsPerPage);
      }

      private async void processCraftableData (int pageIndex, int itemsPerPage) {
         // Get the panel
         CraftingPanel craftingPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);
         craftingPanel.clearContent();

         // Show the crafting panel, except if the reward panel is showing
         if (!craftingPanel.isShowing()) {
            PanelManager.self.pushPanel(craftingPanel.type);
         }

         int userId = Global.player == null ? 0 : Global.player.userId;
         List<Item> craftingIngredients = new List<Item>();
         List<Item> craftableItems = new List<Item>();
         List<Blueprint.Status> blueprintStatus = new List<Blueprint.Status>();

         string craftingIngredientXml = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchCraftingIngredients), userId.ToString());
         string armorFetch = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchCraftableArmors), userId.ToString());
         string weaponFetch = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchCraftableWeapons), userId.ToString());
         string hatFetch = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchCraftableHats), userId.ToString());

         craftingIngredients = CraftingIngredients.processCraftingIngredients(craftingIngredientXml);
         List<CraftableItemData> weaponCraftables = CraftableItem.processCraftableGroups(weaponFetch, craftingIngredients);
         List<CraftableItemData> armorCraftables = CraftableItem.processCraftableGroups(armorFetch, craftingIngredients);
         List<CraftableItemData> hatCraftables = CraftableItem.processCraftableGroups(hatFetch, craftingIngredients);

         foreach (CraftableItemData weaponData in weaponCraftables) {
            craftableItems.Add(weaponData.craftableItem);
            blueprintStatus.Add(weaponData.craftingStatus);
         }
         foreach (CraftableItemData armorData in armorCraftables) {
            craftableItems.Add(armorData.craftableItem);
            blueprintStatus.Add(armorData.craftingStatus);
         }
         foreach (CraftableItemData hatData in hatCraftables) {
            craftableItems.Add(hatData.craftableItem);
            blueprintStatus.Add(hatData.craftingStatus);
         }
         craftingPanel.updatePanelWithBlueprintList(craftableItems.ToArray(), blueprintStatus.ToArray(), pageIndex, itemsPerPage);
      }

      public void fetchEquipmentData (int pageIndex = 1, int itemsPerPage = 42, Item.Category[] categoryFilter = null) {
         if (itemsPerPage > 200) {
            D.debug("Requesting too many items per page.");
            return;
         }

         processUserInventory(pageIndex, itemsPerPage, categoryFilter);
      }

      private async void processUserInventory (int pageIndex = 1, int itemsPerPage = 10, Item.Category[] categoryFilter = null) {
         // Get the inventory panel
         InventoryPanel inventoryPanel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

         // Make sure the inventory panel is showing
         if (!inventoryPanel.isShowing()) {
            PanelManager.self.pushPanel(Panel.Type.Inventory);
         }
         inventoryPanel.clearPanel();

         int userId = Global.player == null ? 0 : Global.player.userId;
         UserInfo newUserInfo = new UserInfo();
         List<Item> userInventory = new List<Item>();

         this.categoryFilter = categoryFilter == null ? Item.Category.None : categoryFilter[0];
         this.pageIndex = pageIndex;
         this.itemsPerPage = itemsPerPage;

         string itemCountResponse = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchInventoryCount), userId.ToString() + SPACER + (int)this.categoryFilter);
         int totalItemCount = InventoryPanel.ITEMS_PER_PAGE;

         try {
            totalItemCount = int.Parse(itemCountResponse);
         } catch {
            D.editorLog("Failed to parse: " + itemCountResponse);
         }

         // Process user info
         string userRawData = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchUserData), userId.ToString());
         if (userRawData.Length < 10) {
            D.editorLog("Something went wrong with Nubis Data Fetch!", Color.red);
            D.editorLog("Content: " + userRawData, Color.red);
         }
         newUserInfo = UserInfoData.processUserInfo(userRawData);

         // Process user equipped items
         string equippedItemContent = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchEquippedItems), userId.ToString());
         Item equippedWeapon = new Item();
         Item equippedArmor = new Item();
         Item equippedHat = new Item(); 

         EquippedItemData equippedItemData = EquippedItems.processEquippedItemData(equippedItemContent);
         equippedWeapon = equippedItemData.weaponItem;
         equippedArmor = equippedItemData.armorItem;
         equippedHat = equippedItemData.hatItem;
         userInventory.Add(equippedWeapon);
         userInventory.Add(equippedArmor);
         userInventory.Add(equippedHat);

         if (this.categoryFilter == Item.Category.Weapon || this.categoryFilter == Item.Category.Armor || this.categoryFilter == Item.Category.Hats || this.categoryFilter == Item.Category.CraftingIngredients || this.categoryFilter == Item.Category.None) {
            string inventoryData = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchInventory), userId.ToString() + SPACER + pageIndex + SPACER + ((int) this.categoryFilter).ToString() + SPACER + equippedItemData.weaponItem.id + SPACER + equippedItemData.armorItem.id + SPACER + equippedItemData.hatItem.id);
            List<Item> itemList = UserInventory.processUserInventory(inventoryData);
            foreach (Item item in itemList) {
               if (item.category == Item.Category.Weapon && EquipmentXMLManager.self.getWeaponData(item.itemTypeId) != null) {
                  WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
                  item.paletteName1 = weaponData.palette1;
                  item.paletteName2 = weaponData.palette2;
               }
               if (item.category == Item.Category.Armor && EquipmentXMLManager.self.getArmorData(item.itemTypeId) != null) {
                  ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(item.itemTypeId);
                  item.paletteName1 = armorData.palette1;
                  item.paletteName2 = armorData.palette2;
               }
               if (item.category == Item.Category.Hats) {
                  HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
                  item.paletteName1 = hatData.palette1;
                  item.paletteName2 = hatData.palette2;
               }
               if (item.id != newUserInfo.weaponId && item.id != newUserInfo.armorId && item.id != newUserInfo.hatId) {
                  userInventory.Add(item);
               }
            }
         }

         UserObjects userObjects = new UserObjects { userInfo = newUserInfo, weapon = equippedWeapon, armor = equippedArmor, hat = equippedHat };
         inventoryPanel.receiveItemForDisplay(userInventory.ToArray(), userObjects, this.categoryFilter, pageIndex, totalItemCount);
      }

      public void fetchUserAbilities () {
         processUserAbilities();
      }

      private async void processUserAbilities () {
         int userId = Global.player == null ? 0 : Global.player.userId;
         AbilityPanel panel = (AbilityPanel) PanelManager.self.get(Panel.Type.Ability_Panel);
         panel.clearContent();

         // Make sure the panel is showing
         if (!panel.isShowing()) {
            PanelManager.self.pushPanel(Panel.Type.Ability_Panel);
         }

         // Fetch content from Nubis
         string abilityRawData = await NubisClient.call(nameof(NubisRequestHandler.nubisFetchUserAbilities), userId.ToString());
         List<AbilitySQLData> abilityList = UserAbilities.processUserAbilities(abilityRawData);

         // Update the Skill Panel with the abilities we received from the server
         panel.receiveDataFromServer(abilityList.ToArray());
      }
   }
}