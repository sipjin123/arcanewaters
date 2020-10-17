using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Events;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

public enum XmlSlotIndex
{
   None = 0,
   Default = 3,
}

namespace NubisDataHandling {

   public class InventoryBundle
   {
      #region Public Variables
      // Reference to the user info.
      public UserInfo user;

      // The equipped weapon.
      public Item equippedWeapon;

      // The equipped armor.
      public Item equippedArmor;

      // The equipped hat.
      public Item equippedHat;

      // The item count.
      public int totalItemCount;

      // String containing the encoded inventory data.
      public string inventoryData;

      #endregion
   }

   public class XmlVersionEvent : UnityEvent<int> { 
   }

   public class NubisDataFetcher : MonoBehaviour {

      #region Public Variables

      // Self
      public static NubisDataFetcher self;

      // The web directory
      public string webDirectory = "";

      // The xml version event 
      public XmlVersionEvent xmlVersionEvent = new XmlVersionEvent();

      // Space syntax for data sending
      public static string SPACER = "_space_";

      // The valid length of an xml data
      public static int VALID_XML_LENGTH = 5;

      #endregion

      private void Awake () {
         self = this;
         webDirectory = "http://" + Global.getAddress(MyNetworkManager.ServerType.AmazonVPC) + ":7900/";
      }

      public static int getSlotIndex () {
         return (int) XmlSlotIndex.Default;
      }

      #region Xml Version Features

      public void fetchXmlVersion () {
         processXmlVersion();
      }

      private async void processXmlVersion () {
         string returnCode = await NubisClient.call(nameof(DB_Main.fetchXmlVersion), getSlotIndex());
         try {
            int xmlVersion = int.Parse(returnCode);
            xmlVersionEvent.Invoke(xmlVersion);
         } catch {
            xmlVersionEvent.Invoke(0);
            D.debug("Something went wrong with xml version fetching! Report: " + nameof(DB_Main.fetchXmlVersion));
            D.debug("Return code is: " + returnCode);
         }
      }

      #endregion

      #region Auction Features

      public async void getAuctionList (int pageNumber, int rowsPerPage, Item.Category[] categoryFilters,
         bool onlyHistory, bool onlyOwnAuctions) {
         AuctionPanel panel = (AuctionPanel) PanelManager.self.get(Panel.Type.Auction);
         panel.setLoadBlocker(true);

         // Prepare the category filter
         List<int> categoryFilterInt = Array.ConvertAll(categoryFilters.ToArray(), x => (int) x).ToList();
         string categoryFilterJSON = JsonConvert.SerializeObject(categoryFilterInt);

         // Call the list and list count in parallel
         Task<string> listTask = NubisClient.call(nameof(DB_Main.getAuctionList), pageNumber.ToString(),
            rowsPerPage.ToString(), categoryFilterJSON, Global.player.userId.ToString(),
            onlyHistory ? "1" : "0", onlyOwnAuctions ? "1" : "0");
         Task<string> totalCountTask = NubisClient.call(nameof(DB_Main.getAuctionListCount),
            Global.player.userId.ToString(), categoryFilterJSON,
            onlyHistory ? "1" : "0", onlyOwnAuctions ? "1" : "0");

         await Task.WhenAll(listTask, totalCountTask);

         // Parse the received data
         List<AuctionItemData> auctionList = new List<AuctionItemData>();
         int totalAuctionCount = 0;
         try {
            auctionList = Util.xmlLoad<List<AuctionItemData>>(listTask.Result);
            totalAuctionCount = int.Parse(totalCountTask.Result);
         } catch {
            D.debug("Something went wrong with nubis fetching: " + nameof(DB_Main.getAuctionList));
         }

         // Update the panel with the results
         PanelManager.self.linkIfNotShowing(Panel.Type.Auction);
         panel.receiveAuctionsFromServer(auctionList, categoryFilters, onlyHistory, onlyOwnAuctions, pageNumber, totalAuctionCount);
      }

      #endregion

      #region Crafting Features

      public void checkCraftingInfo (int bluePrintId) {
         processCraftingInfo(bluePrintId);
      }

      private async void processCraftingInfo (int bluePrintId) {
         // Get the panel
         CraftingPanel craftingPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

         // Show the crafting panel, except if the reward panel is showing
         if (!craftingPanel.isShowing()) {
            PanelManager.self.linkPanel(craftingPanel.type);
         }

         int userId = Global.player == null ? 0 : Global.player.userId;
         EquippedItemData equippedItemData = new EquippedItemData();
         List<Item> craftingIngredients = new List<Item>();
         List<CraftableItemData> craftableItems = new List<CraftableItemData>();

         string rawBlueprintData = await NubisClient.call(nameof(DB_Main.fetchSingleBlueprint), bluePrintId, userId);
         string craftingIngredientData = await NubisClient.call(nameof(DB_Main.fetchCraftingIngredients), userId);
         string equippedItemContent = await NubisClient.call(nameof(DB_Main.fetchEquippedItems), userId);

         craftingIngredients = CraftingIngredients.processCraftingIngredients(craftingIngredientData);
         craftableItems = CraftableItem.processCraftableGroups(rawBlueprintData, craftingIngredients, Item.Category.None);
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
            PanelManager.self.linkPanel(craftingPanel.type);
         }

         int userId = Global.player == null ? 0 : Global.player.userId;
         List<Item> craftingIngredients = new List<Item>();
         List<Item> craftableItems = new List<Item>();
         List<Blueprint.Status> blueprintStatus = new List<Blueprint.Status>();

         string craftingIngredientXml = await NubisClient.call(nameof(DB_Main.fetchCraftingIngredients), userId);
         string armorFetch = await NubisClient.call(nameof(DB_Main.fetchCraftableArmors), userId);
         string weaponFetch = await NubisClient.call(nameof(DB_Main.fetchCraftableWeapons), userId);
         string hatFetch = await NubisClient.call(nameof(DB_Main.fetchCraftableHats), userId);

         craftingIngredients = CraftingIngredients.processCraftingIngredients(craftingIngredientXml);
         List<CraftableItemData> weaponCraftables = CraftableItem.processCraftableGroups(weaponFetch, craftingIngredients, Item.Category.Weapon);
         List<CraftableItemData> armorCraftables = CraftableItem.processCraftableGroups(armorFetch, craftingIngredients, Item.Category.Armor);
         List<CraftableItemData> hatCraftables = CraftableItem.processCraftableGroups(hatFetch, craftingIngredients, Item.Category.Hats);

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

      #endregion

      #region Equipment Features

      public async void getUserInventory (List<Item.Category> categoryFilter, int pageIndex = 1, int itemsPerPage = 42) {
         if (Global.player == null) {
            return;
         }

         int userId = Global.player.userId;
         int[] categoryFilterInt = Array.ConvertAll(categoryFilter.ToArray(), x => (int) x);
         string categoryFilterJSON = JsonConvert.SerializeObject(categoryFilterInt);

         // Request the inventory to Nubis
         string inventoryBundleString = await NubisClient.callDirect("getUserInventoryPage",
            userId.ToString(), categoryFilterJSON, pageIndex.ToString(), itemsPerPage.ToString());

         var inventoryBundle = JsonConvert.DeserializeObject<InventoryBundle>(inventoryBundleString);

         List<Item> itemList = UserInventory.processUserInventory(inventoryBundle.inventoryData);

         // Get the inventory panel
         InventoryPanel inventoryPanel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

         // Make sure the inventory panel is showing
         if (!inventoryPanel.isShowing()) {
            PanelManager.self.linkPanel(Panel.Type.Inventory);
         }
         inventoryPanel.clearPanel();

         UserObjects userObjects = new UserObjects { userInfo = inventoryBundle.user, weapon = inventoryBundle.equippedWeapon, armor = inventoryBundle.equippedArmor, hat = inventoryBundle.equippedHat };
         inventoryPanel.receiveItemForDisplay(itemList, userObjects, categoryFilter, pageIndex, inventoryBundle.totalItemCount, true);
      }

      public async void getInventoryForItemSelection (List<Item.Category> categoryFilter, List<int> itemIdsToExclude,
         int pageIndex, int itemsPerPage) {
         if (Global.player == null) {
            return;
         }

         PanelManager.self.itemSelectionScreen.setLoadBlocker(true);

         int userId = Global.player.userId;
         int[] categoryFilterInt = Array.ConvertAll(categoryFilter.ToArray(), x => (int) x);
         string categoryFilterJSON = JsonConvert.SerializeObject(categoryFilterInt);
         string itemIdsToExcludeJSON = JsonConvert.SerializeObject(itemIdsToExclude);

         // Call the list and list count in parallel
         Task<string> listTask = NubisClient.call(nameof(DB_Main.userInventory), userId.ToString(), categoryFilterJSON,
            itemIdsToExcludeJSON, "1", pageIndex.ToString(), itemsPerPage.ToString());
         Task<string> totalCountTask = NubisClient.call(nameof(DB_Main.userInventoryCount), userId.ToString(), categoryFilterJSON,
            itemIdsToExcludeJSON, "1");

         await Task.WhenAll(listTask, totalCountTask);

         // Parse the received data
         if (!int.TryParse(totalCountTask.Result, out int totalCount)) {
            totalCount = 0;
         }

         List<Item> itemList = UserInventory.processUserInventory(listTask.Result);

         PanelManager.self.itemSelectionScreen.receiveItemsFromServer(itemList, pageIndex, totalCount);
      }

      //private async void processUserInventoryOld (int pageIndex = 1, int itemsPerPage = 10, Item.Category[] categoryFilter = null) {
      //   // Get the inventory panel
      //   InventoryPanel inventoryPanel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

      //   // Make sure the inventory panel is showing
      //   if (!inventoryPanel.isShowing()) {
      //      PanelManager.self.pushPanel(Panel.Type.Inventory);
      //   }
      //   inventoryPanel.clearPanel();

      //   int userId = Global.player == null ? 0 : Global.player.userId;
      //   List<Item> userInventory = new List<Item>();

      //   this.categoryFilter = categoryFilter == null ? Item.Category.None : categoryFilter[0];
      //   this.pageIndex = pageIndex;
      //   this.itemsPerPage = itemsPerPage;

      //   // Fetch the cached user info which was provided by the server upon login
      //   UserInfo newUserInfo = Global.getUserObjects().userInfo;
      //   EquippedItemData equippedItemData = new EquippedItemData();
      //   equippedItemData.weaponItem = Global.getUserObjects().weapon;
      //   equippedItemData.armorItem = Global.getUserObjects().armor;
      //   equippedItemData.hatItem = Global.getUserObjects().hat;

      //   List<Item.Category> newcategoryList = new List<Item.Category>();
      //   newcategoryList.Add(Item.Category.Weapon);
      //   newcategoryList.Add(Item.Category.Armor);
      //   newcategoryList.Add(Item.Category.CraftingIngredients);
      //   newcategoryList.Add(Item.Category.Blueprint);

      //   int[] categoryInt = Array.ConvertAll(newcategoryList.ToArray(), x => (int) x);
      //   string categoryJson = JsonConvert.SerializeObject(categoryInt);

      //   List<int> itemIdFilter = new List<int>();

      //   // Filter equipped item id so it will not be part of the inventory fetch
      //   UserObjects cachedUserObj = Global.getUserObjects();
      //   itemIdFilter.Add(cachedUserObj.weapon.id);
      //   itemIdFilter.Add(cachedUserObj.armor.id);
      //   itemIdFilter.Add(cachedUserObj.hat.id);

      //   userInventory.Add(cachedUserObj.weapon);
      //   userInventory.Add(cachedUserObj.armor);
      //   userInventory.Add(cachedUserObj.hat);

      //   // Fetch the total item count for pagination
      //   string itemIdJson = JsonConvert.SerializeObject(itemIdFilter.ToArray());
      //   string itemCountResponse = await NubisClient.call(nameof(DB_Main.getItemCount), userId.ToString(), categoryJson, itemIdJson, "0");
      //   int totalItemCount = InventoryPanel.ITEMS_PER_PAGE;

      //   try {
      //      totalItemCount = int.Parse(itemCountResponse);
      //   } catch {
      //      D.editorLog("Failed to parse: " + itemCountResponse);
      //   }

      //   if (this.categoryFilter == Item.Category.Weapon || this.categoryFilter == Item.Category.Armor || this.categoryFilter == Item.Category.Hats || this.categoryFilter == Item.Category.CraftingIngredients || this.categoryFilter == Item.Category.None) {
      //      string inventoryData = await NubisClient.call(nameof(DB_Main.userInventory), userId, pageIndex, (int) this.categoryFilter, equippedItemData.weaponItem.id, equippedItemData.armorItem.id, equippedItemData.hatItem.id, InventoryPanel.ITEMS_PER_PAGE);
      //      List<Item> itemList = UserInventory.processUserInventory(inventoryData);
      //      foreach (Item item in itemList) {
      //         if (item.category == Item.Category.Weapon && EquipmentXMLManager.self.getWeaponData(item.itemTypeId) != null) {
      //            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
      //            item.paletteNames = weaponData.palettes;
      //         }
      //         if (item.category == Item.Category.Armor && EquipmentXMLManager.self.getArmorData(item.itemTypeId) != null) {
      //            ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(item.itemTypeId);
      //            item.paletteNames = armorData.palettes;
      //         }
      //         if (item.category == Item.Category.Hats) {
      //            HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
      //            item.paletteNames = hatData.palettes;
      //         }
      //         if (item.id != newUserInfo.weaponId && item.id != newUserInfo.armorId && item.id != newUserInfo.hatId) {
      //            userInventory.Add(item);
      //         }
      //      }
      //   }

      //   // Provide the user with the Nubis fetched items and the cached version of the user info
      //   UserObjects userObjects = new UserObjects { userInfo = newUserInfo, weapon = equippedItemData.weaponItem, armor = equippedItemData.armorItem, hat = equippedItemData.hatItem };
      //   inventoryPanel.receiveItemForDisplay(userInventory.ToArray(), userObjects, this.categoryFilter, pageIndex, totalItemCount, false);

      //   // Refresh user info after the inventory has been loaded in the inventory panel, to make sure that user data is accurate and to improve the speed of the inventory loading reaction
      //   newUserInfo = await NubisClient.callJSONClass<UserInfo>(nameof(DB_Main.getUserInfoJSON), userId);
      //   if (newUserInfo == null) {
      //      D.editorLog("Something went wrong with Nubis Data Fetch!", Color.red);
      //   }

      //   // Process user equipped items
      //   string equippedItemContent = await NubisClient.call(nameof(DB_Main.fetchEquippedItems), userId);
      //   Item equippedWeapon = new Item();
      //   Item equippedArmor = new Item();
      //   Item equippedHat = new Item();

      //   equippedItemData = EquippedItems.processEquippedItemData(equippedItemContent);
      //   equippedWeapon = equippedItemData.weaponItem;
      //   equippedArmor = equippedItemData.armorItem;
      //   equippedHat = equippedItemData.hatItem;

      //   userInventory.Add(equippedWeapon);
      //   userInventory.Add(equippedArmor);
      //   userInventory.Add(equippedHat);

      //   // Provide the inventory panel with the updated equipped items and user info
      //   userObjects = new UserObjects { userInfo = newUserInfo, weapon = equippedItemData.weaponItem, armor = equippedItemData.armorItem, hat = equippedItemData.hatItem };
      //   inventoryPanel.receiveItemForDisplay(userInventory.ToArray(), userObjects, this.categoryFilter, pageIndex, totalItemCount, true);
      //}

      #endregion

      #region Ability Features

      public void fetchUserAbilities () {
         processUserAbilities();
      }

      private async void processUserAbilities () {
         int userId = Global.player == null ? 0 : Global.player.userId;
         AbilityPanel panel = (AbilityPanel) PanelManager.self.get(Panel.Type.Ability_Panel);
         panel.clearContent();

         // Make sure the panel is showing
         if (!panel.isShowing()) {
            PanelManager.self.linkPanel(Panel.Type.Ability_Panel);
         }

         // Fetch content from Nubis
         List<AbilitySQLData> abilityList = await NubisClient.callJSONList<List<AbilitySQLData>>(nameof(DB_Main.userAbilities), userId, (int) AbilityEquipStatus.ALL);

         // Update the Skill Panel with the abilities we received from the server
         panel.receiveDataFromServer(abilityList.ToArray());
      }

      #endregion
   }
}