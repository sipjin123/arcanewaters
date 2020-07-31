using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Events;
using Newtonsoft.Json;
using System;
using System.Linq;

public enum XmlSlotIndex
{
   None = 0,
   Default = 3,
}

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

      // The valid length of an xml data
      public static int VALID_XML_LENGTH = 10;

      #endregion

      private void Awake () {
         self = this;
         webDirectory = "http://" + Global.getAddress(MyNetworkManager.ServerType.AmazonVPC) + ":7900/";
      }

      private void Update () {
#if UNITY_EDITOR
         if (Input.GetKeyDown(KeyCode.Alpha8)) {
            D.editorLog("Fetching auction data for user", Color.green);
            List<Item.Category> categoryFilters = new List<Item.Category>();
            categoryFilters.Add(Item.Category.Weapon);
            categoryFilters.Add(Item.Category.Armor);
            categoryFilters.Add(Item.Category.Hats);

            List<int> categoryFilter = Array.ConvertAll(categoryFilters.ToArray(), x => (int) x).ToList();
            string jsonFilter = JsonConvert.SerializeObject(categoryFilter);
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               var returnData = DB_Main.fetchAuctionData(Global.player.userId.ToString(), "0", "5", jsonFilter, "1");
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Return data is: " + returnData, Color.green);
               });
            });
         }
         if (Input.GetKeyDown(KeyCode.Alpha0)) {
            D.editorLog("Fetching auction data history", Color.green);
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               var returnData = DB_Main.fetchAuctionPurchaseHistory(Global.player.userId.ToString(), "5", "0");
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.editorLog("Return data is: " + returnData, Color.green);
               });
            });
         }
#endif
      }

      public static int getSlotIndex () {
         return (int) XmlSlotIndex.Default;
      }

      #region Debug Function

      public void testNubisFunction () {
         processTestNubisFunction();
      }

      private async void processTestNubisFunction () {
         UserObjects returnCode = await NubisClient.call<UserObjects>(nameof(DB_Main.getUserInfoJSON), Global.player.userId.ToString());
         try {
            D.editorLog("The return code is: "
               + returnCode.accountId
               + " : " + returnCode.weapon.itemTypeId
               + " : " + returnCode.armor.itemTypeId
               + " : " + returnCode.userInfo.username, Color.magenta);
         } catch {
            D.debug("Something went wrong with xml version fetching! Report: " + nameof(DB_Main.getUserInfoJSON));
            D.debug("Return code is: " + returnCode);
         }
      }

      #endregion

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

      public void fetchAuctionHistory (int page) {
         processFetchAuctionHistory(page);
      }

      private async void processFetchAuctionHistory (int page) {
         string result = await NubisClient.call(nameof(DB_Main.fetchAuctionPurchaseHistory), Global.player.userId.ToString(), AuctionMarketPanel.MAX_PAGE_COUNT.ToString(), page.ToString());
         if (result.Length > VALID_XML_LENGTH) {
            List<AuctionItemData> auctionItemList = Util.xmlLoad<List<AuctionItemData>>(result);

            AuctionRootPanel panel = (AuctionRootPanel) PanelManager.self.get(Panel.Type.Auction);
            panel.marketPanel.loadAuctionHistory(auctionItemList);
         } else {
            D.debug("Something went wrong with nubis fetching: " + nameof(DB_Main.fetchAuctionPurchaseHistory));
         }
      }

      public void requestUserItemsForAuction (int pageIndex, Item.Category categoryFilters) {
         processUserItemsForAuction(pageIndex, categoryFilters);
      }

      private async void processUserItemsForAuction (int pageIndex, Item.Category categoryFilters) {
         // Fetched data from db_main
         string userItemData = await NubisClient.call(nameof(DB_Main.fetchEquippedItems), Global.player.userId.ToString());
         if (userItemData.Length > VALID_XML_LENGTH) {
            Item equippedWeapon = new Item();
            Item equippedArmor = new Item();
            Item equippedHat = new Item();

            EquippedItemData equippedItemData = EquippedItems.processEquippedItemData(userItemData);
            equippedWeapon = equippedItemData.weaponItem;
            equippedArmor = equippedItemData.armorItem;
            equippedHat = equippedItemData.hatItem;

            string returnCode = await NubisClient.call(nameof(DB_Main.userInventory), Global.player.userId.ToString(), pageIndex.ToString(), ((int) categoryFilters).ToString(), equippedWeapon.id.ToString(), equippedArmor.id.ToString(), equippedHat.id.ToString());
            if (returnCode.Length > VALID_XML_LENGTH) {
               AuctionRootPanel panel = (AuctionRootPanel) PanelManager.self.get(Panel.Type.Auction);
               if (panel.isShowing()) {
                  List<Item> itemList = UserInventory.processUserInventory(returnCode);
                  panel.userPanel.gameObject.SetActive(true);
                  panel.userPanel.loadUserItemList(itemList);
                  panel.show();
               }
            } else {
               D.debug("Something went wrong with nubis fetching: " + nameof(DB_Main.userInventory));
            }
         } else {
            D.debug("Something went wrong with nubis fetching: " + nameof(DB_Main.fetchEquippedItems));
         }
      }

      public void checkAuctionMarket (int pageIndex, Item.Category[] categoryFilters, bool checkOwnItems) {
         AuctionRootPanel panel = (AuctionRootPanel) PanelManager.self.get(Panel.Type.Auction);
         if (checkOwnItems) {
            panel.marketPanel.toggleAuctionedUserItemLoader(true);
         } else {
            panel.setBlockers(true);
         }

         processAuctionMarket (pageIndex, categoryFilters, checkOwnItems);
      }

      private async void processAuctionMarket (int pageIndex, Item.Category[] categoryFilters, bool checkOwnItems) {
         List<int> categoryFilter = Array.ConvertAll(categoryFilters.ToArray(), x => (int) x).ToList();
         string jsonFilter = JsonConvert.SerializeObject(categoryFilter);

         string result = await NubisClient.call(nameof(DB_Main.fetchAuctionData), Global.player.userId.ToString(), pageIndex.ToString(), AuctionMarketPanel.MAX_PAGE_COUNT.ToString(), jsonFilter, checkOwnItems ? "1" : "0");
         if (result.Length > VALID_XML_LENGTH) {
            AuctionRootPanel panel = (AuctionRootPanel) PanelManager.self.get(Panel.Type.Auction);

            if (panel.isShowing()) {
               List<AuctionItemData> actualData = Util.xmlLoad<List<AuctionItemData>>(result);

               panel.marketPanel.loadAuctionItems(actualData, checkOwnItems);
               panel.setBlockers(false);
               panel.show();
            }
         } else {
            D.debug("Something went wrong with nubis fetching: " + nameof(DB_Main.fetchAuctionData));
         }
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
            PanelManager.self.pushPanel(craftingPanel.type);
         }

         int userId = Global.player == null ? 0 : Global.player.userId;
         EquippedItemData equippedItemData = new EquippedItemData();
         List<Item> craftingIngredients = new List<Item>();
         List<CraftableItemData> craftableItems = new List<CraftableItemData>();

         string rawBlueprintData = await NubisClient.call(nameof(DB_Main.fetchSingleBlueprint), bluePrintId, userId);
         string craftingIngredientData = await NubisClient.call(nameof(DB_Main.fetchCraftingIngredients), userId);
         string equippedItemContent = await NubisClient.call(nameof(DB_Main.fetchEquippedItems), userId);

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

         string craftingIngredientXml = await NubisClient.call(nameof(DB_Main.fetchCraftingIngredients), userId);
         string armorFetch = await NubisClient.call(nameof(DB_Main.fetchCraftableArmors), userId);
         string weaponFetch = await NubisClient.call(nameof(DB_Main.fetchCraftableWeapons), userId);
         string hatFetch = await NubisClient.call(nameof(DB_Main.fetchCraftableHats), userId);

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

      #endregion

      #region Equipment Features

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

         // Process user info
         string userRawData = await NubisClient.call(nameof(DB_Main.getUserInfoJSON), userId);
         if (userRawData.Length < 10) {
            D.editorLog("Something went wrong with Nubis Data Fetch!", Color.red);
            D.editorLog("Content: " + userRawData, Color.red);
         } 
         newUserInfo = JsonUtility.FromJson<UserInfo>(userRawData);

         // Process user equipped items
         string equippedItemContent = await NubisClient.call(nameof(DB_Main.fetchEquippedItems), userId);
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
         string itemCountResponse = await NubisClient.call(nameof(DB_Main.getItemCount), userId, categoryJson, itemIdJson, "");
         int totalItemCount = InventoryPanel.ITEMS_PER_PAGE;

         try {
            totalItemCount = int.Parse(itemCountResponse);
         } catch {
            D.editorLog("Failed to parse: " + itemCountResponse);
         }

         if (this.categoryFilter == Item.Category.Weapon || this.categoryFilter == Item.Category.Armor || this.categoryFilter == Item.Category.Hats || this.categoryFilter == Item.Category.CraftingIngredients || this.categoryFilter == Item.Category.None) {
            string inventoryData = await NubisClient.call(nameof(DB_Main.userInventory), userId, pageIndex, (int) this.categoryFilter, equippedItemData.weaponItem.id, equippedItemData.armorItem.id, equippedItemData.hatItem.id);
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
            PanelManager.self.pushPanel(Panel.Type.Ability_Panel);
         }

         // Fetch content from Nubis
         string abilityRawData = await NubisClient.call(nameof(DB_Main.userAbilities), userId);
         List<AbilitySQLData> abilityList = UserAbilities.processUserAbilities(abilityRawData);

         // Update the Skill Panel with the abilities we received from the server
         panel.receiveDataFromServer(abilityList.ToArray());
      }

      #endregion
   }
}