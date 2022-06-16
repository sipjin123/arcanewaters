﻿using UnityEngine;
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

namespace NubisDataHandling
{

   public class InventoryBundle
   {
      #region Public Variables
      // Reference to the user info.
      public UserInfo user;

      // The guild info.
      public GuildInfo guildInfo;

      // The equipped weapon.
      public Item equippedWeapon;

      // The equipped armor.
      public Item equippedArmor;

      // The equipped hat.
      public Item equippedHat;

      // The equipped gear.
      public Item equippedRing;
      public Item equippedNecklace;
      public Item equippedTrinket;

      // The item count.
      public int totalItemCount;

      // String containing the encoded inventory data.
      public string inventoryData;

      #endregion
   }

   public class XmlVersionEvent : UnityEvent<int>
   {
   }

   public class NubisDataFetcher : GenericGameManager
   {

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

      protected override void Awake () {
         base.Awake();
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
         string returnCode = await NubisClient.call<string>(nameof(DB_Main.fetchXmlVersion));

         try {
            int xmlVersion = int.Parse(returnCode);
            if (xmlVersion < 1) {
               D.error("Something went wrong with xml version fetching! Report: " + nameof(DB_Main.fetchXmlVersion));
            }
            xmlVersionEvent.Invoke(xmlVersion);
         } catch {
            xmlVersionEvent.Invoke(0);
            D.error("Something went wrong with xml version fetching! Report: " + nameof(DB_Main.fetchXmlVersion));
            D.error("Return code is: " + returnCode);
         }
      }

      #endregion

      #region Auction Features

      public async void getAuctionList (int pageNumber, int rowsPerPage, Item.Category[] categoryFilters,
         AuctionPanel.ListFilter auctionFilter) {
         AuctionPanel panel = (AuctionPanel) PanelManager.self.get(Panel.Type.Auction);
         panel.setLoadBlocker(true);

         // Call the nubis functions in parallel
         Task<string> listTask = NubisClient.call<string>(nameof(DB_Main.getAuctionList), pageNumber,
            rowsPerPage, categoryFilters, Global.player.userId, auctionFilter);
         Task<int> totalCountTask = NubisClient.call<int>(nameof(DB_Main.getAuctionListCount),
            Global.player.userId, categoryFilters, auctionFilter);
         Task<UserInfo> userInfoTask = NubisClient.call<UserInfo>(nameof(DB_Main.getUserInfoById), Global.player.userId);

         await Task.WhenAll(listTask, totalCountTask, userInfoTask);

         // Parse the received data
         List<AuctionItemData> auctionList = new List<AuctionItemData>();
         int totalAuctionCount = 0;
         UserInfo userInfo = new UserInfo();
         try {
            auctionList = Util.xmlLoad<List<AuctionItemData>>(listTask.Result);
            totalAuctionCount = totalCountTask.Result;
            userInfo = userInfoTask.Result;
         } catch (Exception e) {
            D.error("Something went wrong with nubis fetching: " + nameof(DB_Main.getAuctionList) + ".\n" + e.ToString());
         }

         // Update the panel with the results
         PanelManager.self.linkIfNotShowing(Panel.Type.Auction);
         panel.receiveAuctionsFromServer(auctionList, categoryFilters, auctionFilter, pageNumber, totalAuctionCount, userInfo.gold);
      }

      #endregion

      #region Crafting Features

      public void checkCraftingInfo (int bluePrintId, Item.Category category, int itemType) {
         processCraftingInfo(bluePrintId, category, itemType);
      }

      private async void processCraftingInfo (int bluePrintId, Item.Category category, int itemType) {
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

         // Fetch xml of categories added for crafting ingredient
         List<string> itemsXml = new List<string>();
         foreach(Item.Category craftingCategory in CraftingItemCategories) {
            string xmlToAdd = await NubisClient.call<string>(nameof(DB_Main.fetchCategorizedItem), userId, (int) craftingCategory);
            itemsXml.Add(xmlToAdd);
         }

         string rawBlueprintData = await NubisClient.call<string>(nameof(DB_Main.fetchSingleBlueprint), bluePrintId, userId);
         string equippedItemContent = await NubisClient.call<string>(nameof(DB_Main.fetchEquippedItems), userId);
         
         // TODO: For other items that will be part of ingredients, add logic here
         // Process fetched xml and add it to crafting ingredient item list 
         foreach (string xml in itemsXml) {
            craftingIngredients.AddRange(CraftingIngredients.processCraftingIngredients(xml));
         }
         craftableItems = CraftableItem.processCraftableGroups(rawBlueprintData, craftingIngredients, Item.Category.None);
         equippedItemData = EquippedItems.processEquippedItemData(equippedItemContent);

         
         // Handle all crafting items that does not require blueprints here
         if (craftableItems.Count < 1 && CraftingItemCategories.Contains(category)) {
            CraftableItemRequirements craftableData = CraftingManager.self.getCraftableData(category, itemType);

            if (craftableData != null && craftableData.isAlwaysAvailable) {
               // Determine the status of this craftable Item depending on the available ingredients
               Blueprint.Status newStatus = getBlueprintStatus(craftableData, craftingIngredients);

               // Clone content from xml bundle
               Item newResultItem = craftableData.resultItem.Clone();
               List<Item> newItemRequirements = new List<Item>();
               foreach (Item requirement in craftableData.combinationRequirements) {
                  newItemRequirements.Add(requirement.Clone());
               }

               // Add the item
               CraftableItemData newCraftableItemData = new CraftableItemData {
                  craftableRequirements = new CraftableItemRequirements {
                     combinationRequirements = newItemRequirements.ToArray(),
                     isEnabled = true,
                     xmlId = craftableData.xmlId
                  },
                  craftableItem = newResultItem,
                  craftingStatus = newStatus
               };
               craftableItems.Add(newCraftableItemData);
            }
         }

         List<Item> equippedItems = new List<Item>();
         equippedItems.Add(equippedItemData.weaponItem);
         equippedItems.Add(equippedItemData.armorItem);
         equippedItems.Add(equippedItemData.hatItem);

         if (craftableItems.Count > 0) {
            craftingPanel.updatePanelWithSingleBlueprintWebRequest(craftableItems[0].craftableItem, equippedItems, craftingIngredients, craftableItems[0].craftableRequirements.combinationRequirements);
         } else {
            D.error("Error here! Craftable item count is empty!");
         }
      }

      public void fetchCraftableData (int pageIndex, int itemsPerPage, List<Item.Category> categoryList = null) {
         if (categoryList == null) {
            categoryList = new List<Item.Category>();
         }
         processCraftableData(pageIndex, itemsPerPage, categoryList);
      }

      private async void processCraftableData (int pageIndex, int itemsPerPage, List<Item.Category> categoryList) {
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

         // Fetch xml of item categories added for crafting ingredient
         List<string> itemsXml = new List<string>();
         foreach(Item.Category category in CraftingItemCategories) {
            string xmlToAdd = await NubisClient.call<string>(nameof(DB_Main.fetchCategorizedItem), userId, (int) category);
            itemsXml.Add(xmlToAdd);
         }

         string weaponFetch = "";
         string armorFetch = "";
         string hatFetch = "";
         string ringFetch = "";
         string necklaceFetch = "";
         string trinketFetch = "";
         string ingredientFetch = "";
         if (categoryList.Contains(Item.Category.None) || categoryList.Count < 1 || categoryList.Contains(Item.Category.Armor)) {
            armorFetch = await NubisClient.call<string>(nameof(DB_Main.fetchCraftableArmors), userId);
         }
         if (categoryList.Contains(Item.Category.None) || categoryList.Count < 1 || categoryList.Contains(Item.Category.Weapon)) {
            weaponFetch = await NubisClient.call<string>(nameof(DB_Main.fetchCraftableWeapons), userId);
         }
         if (categoryList.Contains(Item.Category.None) || categoryList.Count < 1 || categoryList.Contains(Item.Category.Hats)) {
            hatFetch = await NubisClient.call<string>(nameof(DB_Main.fetchCraftableHats), userId);
         }
         if (categoryList.Contains(Item.Category.None) || categoryList.Count < 1 || categoryList.Contains(Item.Category.Necklace)) {
            necklaceFetch = await NubisClient.call<string>(nameof(DB_Main.fetchCraftableNecklace), userId);
         }
         if (categoryList.Contains(Item.Category.None) || categoryList.Count < 1 || categoryList.Contains(Item.Category.Ring)) {
            ringFetch = await NubisClient.call<string>(nameof(DB_Main.fetchCraftableRings), userId);
         }
         if (categoryList.Contains(Item.Category.None) || categoryList.Count < 1 || categoryList.Contains(Item.Category.Trinket)) {
            trinketFetch = await NubisClient.call<string>(nameof(DB_Main.fetchCraftableTrinkets), userId);
         }
         if (categoryList.Contains(Item.Category.None) || categoryList.Count < 1 || categoryList.Contains(Item.Category.Ring) || categoryList.Contains(Item.Category.Necklace) || categoryList.Contains(Item.Category.Trinket)) {
            ingredientFetch = await NubisClient.call<string>(nameof(DB_Main.fetchCraftableIngredients), userId);
         }

         // Process fetched xml and and add item to crafting ingredient list
         foreach (string itemXml in itemsXml) {
            craftingIngredients.AddRange(CraftingIngredients.processCraftingIngredients(itemXml));
         }
         
         List<CraftableItemData> weaponCraftables = CraftableItem.processCraftableGroups(weaponFetch, craftingIngredients, Item.Category.Weapon);
         List<CraftableItemData> armorCraftables = CraftableItem.processCraftableGroups(armorFetch, craftingIngredients, Item.Category.Armor);
         List<CraftableItemData> hatCraftables = CraftableItem.processCraftableGroups(hatFetch, craftingIngredients, Item.Category.Hats);
         List<CraftableItemData> ringCraftables = CraftableItem.processCraftableGroups(ringFetch, craftingIngredients, Item.Category.Ring);
         List<CraftableItemData> necklaceCraftables = CraftableItem.processCraftableGroups(necklaceFetch, craftingIngredients, Item.Category.Necklace);
         List<CraftableItemData> trinketCraftables = CraftableItem.processCraftableGroups(trinketFetch, craftingIngredients, Item.Category.Trinket);
         List<CraftableItemData> ingredientCraftables = CraftableItem.processCraftableGroups(ingredientFetch, craftingIngredients, Item.Category.CraftingIngredients);

         if (categoryList.Contains(Item.Category.None) || categoryList.Count < 1 || categoryList.Any(item => CraftingItemCategories.Contains(item))) {
            // Craftable ingredients are hard coded for now, remove this block if it will be limited to unlocking
            List<CraftableItemRequirements> ingredientList = CraftingManager.self.getAllCraftableData();
            IEnumerable unlockedIngredientList = ingredientList.Where(_ => CraftingItemCategories.Contains(_.resultItem.category));

            foreach (CraftableItemRequirements unlockedRequirements in unlockedIngredientList) {
               if (unlockedRequirements.isEnabled && unlockedRequirements.isAlwaysAvailable) {
                  // Determine the status of this craftable Item depending on the available ingredients
                  Blueprint.Status status = getBlueprintStatus(unlockedRequirements, craftingIngredients);
                  CraftableItemData newCraftableData = new CraftableItemData {
                     craftableItem = unlockedRequirements.resultItem,
                     craftingStatus = status,
                     craftableRequirements = unlockedRequirements
                  };

                  craftableItems.Add(newCraftableData.craftableItem);
                  blueprintStatus.Add(status);
               }
            }
         }

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
         foreach (CraftableItemData ringData in ringCraftables) {
            craftableItems.Add(ringData.craftableItem);
            blueprintStatus.Add(ringData.craftingStatus);
         }
         foreach (CraftableItemData necklaceData in necklaceCraftables) {
            craftableItems.Add(necklaceData.craftableItem);
            blueprintStatus.Add(necklaceData.craftingStatus);
         }
         foreach (CraftableItemData trinketData in trinketCraftables) {
            craftableItems.Add(trinketData.craftableItem);
            blueprintStatus.Add(trinketData.craftingStatus);
         }
         /* Enable this if filtering of crafting ingredients are needed, right now they are hard coded to be available
         foreach (CraftableItemData ingredientData in ingredientCraftables) {
            craftableItems.Add(ingredientData.craftableItem);
            blueprintStatus.Add(ingredientData.craftingStatus);
         }*/
         craftingPanel.updatePanelWithBlueprintList(craftableItems.ToArray(), blueprintStatus.ToArray(), pageIndex, itemsPerPage);
      }

      private Blueprint.Status getBlueprintStatus (CraftableItemRequirements unlockedRequirements, List<Item> craftingIngredients) {
         Blueprint.Status blueprintStatus = Blueprint.Status.Craftable;
         if (unlockedRequirements == null) {
            blueprintStatus = Blueprint.Status.MissingRecipe;
         } else {
            // Compare the ingredients present in the inventory with the requirements
            foreach (Item requiredIngredient in unlockedRequirements.combinationRequirements) {
               // Get the inventory ingredient, if there is any
               Item inventoryIngredient = craftingIngredients.Find(s =>
                  s.itemTypeId == requiredIngredient.itemTypeId);

               // Verify that there are enough items in the inventory stack
               if (inventoryIngredient == null || inventoryIngredient.count < requiredIngredient.count) {
                  blueprintStatus = Blueprint.Status.NotCraftable;
                  break;
               }
            }
         }
         return blueprintStatus;
      }

      #endregion

      #region Equipment Features

      public async void getGuildInventory (Item.Category[] categoryFilter, int pageIndex = 1, int itemsPerPage = 42, Item.DurabilityFilter itemDurabilityFilter = Item.DurabilityFilter.None) {
         if (Global.player == null) {
            return;
         }

         int guildInventoryId = Global.player.guildInventoryId;
         int[] categoryFilterInt = Array.ConvertAll(categoryFilter.ToArray(), x => (int) x);
         string categoryFilterJSON = JsonConvert.SerializeObject(categoryFilterInt);

         List<Item> result = new List<Item>();
         int totalItems = 0;

         if (Global.player.guildId > 0 && Global.player.guildInventoryId > 0) {
            int[] itemIdsToExclude = new int[0];

            // Request the inventory to Nubis
            string inventoryData = await NubisClient.call<string>(nameof(DB_Main.userInventory), -guildInventoryId, categoryFilter, itemIdsToExclude, true,
               pageIndex, itemsPerPage, itemDurabilityFilter);

            if (!string.IsNullOrWhiteSpace(inventoryData)) {
               totalItems = await NubisClient.call<int>(nameof(DB_Main.userInventoryCount), -guildInventoryId, categoryFilter, itemIdsToExclude, true, itemDurabilityFilter);

               List<Item> itemList = UserInventory.processUserInventory(inventoryData);

               if (itemList == null) {
                  D.debug("Failed to fetch Nubis abilities");
               } else {
                  result = itemList;
               }
            }
         }

         GuildInventoryPanel.self.receiveItemForDisplay(result, categoryFilter, pageIndex, totalItems);
      }

      public async void getUserInventory (List<Item.Category> categoryFilter, int pageIndex = 1, int itemsPerPage = 42, Item.DurabilityFilter itemDurabilityFilter = Item.DurabilityFilter.None, Panel.Type panelType = Panel.Type.Inventory) {
         if (Global.player == null) {
            return;
         }

         int userId = Global.player.userId;
         int[] categoryFilterInt = Array.ConvertAll(categoryFilter.ToArray(), x => (int) x);
         string categoryFilterJSON = JsonConvert.SerializeObject(categoryFilterInt);

         InventoryBundle inventoryBundle = null;

         // Request the inventory from Nubis
         string inventoryBundleString = await NubisClient.callDirect<string>("getUserInventoryPage",
            userId,
            categoryFilter.ToArray(),
            pageIndex,
            itemsPerPage,
            itemDurabilityFilter);

         // Request the jobs from Nubis
         string jobsBundleString = await NubisClient.call<string>(nameof(DB_Main.getJobXPString), userId);
         Jobs jobsData = new Jobs();
         if (jobsBundleString != null && jobsBundleString.Length > 0) {
            string splitter = "[space]";
            string[] xmlGroup = jobsBundleString.Split(new string[] { splitter }, StringSplitOptions.None);
            if (xmlGroup.Length >= 6) {
               try {
                  jobsData.farmerXP = int.Parse(xmlGroup[0]);
                  jobsData.explorerXP = int.Parse(xmlGroup[1]);
                  jobsData.sailorXP = int.Parse(xmlGroup[2]);
                  jobsData.traderXP = int.Parse(xmlGroup[3]);
                  jobsData.crafterXP = int.Parse(xmlGroup[4]);
                  jobsData.minerXP = int.Parse(xmlGroup[5]);
               } catch {
                  D.debug("Error! Something went wrong translating jobs data!");
               }
            }
         }
         
         try {
            inventoryBundle = JsonConvert.DeserializeObject<InventoryBundle>(inventoryBundleString);
         } catch {
            D.debug("Inventory bundle failed to fetch! {" + inventoryBundleString + "} :: {" + categoryFilterJSON + "} :: {" + pageIndex + "} :: {" + itemsPerPage + "}");
         }

         if (inventoryBundle == null) {
            D.debug("Inventory bundle is not existing!" + " : " + inventoryBundleString);
            return;
         }

         // Update sql id of each equipment cache loaded
         if (inventoryBundle.equippedWeapon.itemTypeId != 0) {
            WeaponStatData xmlWeaponData = EquipmentXMLManager.self.getWeaponData(inventoryBundle.equippedWeapon.itemTypeId);
            if (xmlWeaponData == null) {
               D.debug("NUBIS DATA FETCHER :: Weapon data missing! ID:" + inventoryBundle.equippedWeapon.itemTypeId + " WeaponDataContent: " + EquipmentXMLManager.self.weaponStatList.Count);
               inventoryBundle.equippedWeapon.itemTypeId = 0;
            }
         }
         if (inventoryBundle.equippedArmor.itemTypeId != 0) {
            ArmorStatData xmlArmorData = EquipmentXMLManager.self.getArmorDataBySqlId(inventoryBundle.equippedArmor.itemTypeId);
            if (xmlArmorData == null) {
               D.debug("NUBIS DATA FETCHER :: Armor data missing! ID:" + inventoryBundle.equippedArmor.itemTypeId + " ArmorDataContent: " + EquipmentXMLManager.self.armorStatList.Count);
               inventoryBundle.equippedArmor.itemTypeId = 0;
            }
         }
         if (inventoryBundle.equippedHat.itemTypeId != 0) {
            HatStatData xmlHatData = EquipmentXMLManager.self.getHatData(inventoryBundle.equippedHat.itemTypeId);
            if (xmlHatData == null) {
               D.debug("NUBIS DATA FETCHER :: Hat data missing! ID:" + inventoryBundle.equippedHat.itemTypeId + " HatDataContent: " + EquipmentXMLManager.self.hatStatList.Count);
               inventoryBundle.equippedHat.itemTypeId = 0;
            }
         }
         if (inventoryBundle.equippedRing.itemTypeId != 0) {
            RingStatData xmlRingData = EquipmentXMLManager.self.getRingData(inventoryBundle.equippedRing.itemTypeId);
            if (xmlRingData == null) {
               D.debug("NUBIS DATA FETCHER :: Ring data missing! ID:" + inventoryBundle.equippedRing.itemTypeId + " RingDataContent: " + EquipmentXMLManager.self.ringStatList.Count);
               inventoryBundle.equippedRing.itemTypeId = 0;
            }
         }
         if (inventoryBundle.equippedNecklace.itemTypeId != 0) {
            NecklaceStatData xmlNecklaceData = EquipmentXMLManager.self.getNecklaceData(inventoryBundle.equippedNecklace.itemTypeId);
            if (xmlNecklaceData == null) {
               D.debug("NUBIS DATA FETCHER :: Necklace data missing! ID:" + inventoryBundle.equippedNecklace.itemTypeId + " NecklaceDataContent: " + EquipmentXMLManager.self.necklaceStatList.Count);
               inventoryBundle.equippedNecklace.itemTypeId = 0;
            }
         }
         if (inventoryBundle.equippedTrinket.itemTypeId != 0) {
            TrinketStatData xmlTrinketData = EquipmentXMLManager.self.getTrinketData(inventoryBundle.equippedTrinket.itemTypeId);
            if (xmlTrinketData == null) {
               D.debug("NUBIS DATA FETCHER :: Trinket data missing! ID:" + inventoryBundle.equippedTrinket.itemTypeId + " TrinketDataContent: " + EquipmentXMLManager.self.trinketStatList.Count);
               inventoryBundle.equippedTrinket.itemTypeId = 0;
            }
         }

         List<Item> itemList = UserInventory.processUserInventory(inventoryBundle.inventoryData);
         
         // Filter inventory items here
         itemList.RemoveAll(_ => _.category == Item.Category.Usable);

         if (panelType == Panel.Type.Inventory) {
            // Get the inventory panel
            InventoryPanel inventoryPanel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

            // Make sure the inventory panel is showing
            if (!inventoryPanel.isShowing()) {
               PanelManager.self.linkPanel(Panel.Type.Inventory);

               // When inventory panel is opened, we should always start at the first page
               pageIndex = 0;
            }
            inventoryPanel.clearPanel();

            UserObjects userObjects = new UserObjects { userInfo = inventoryBundle.user, weapon = inventoryBundle.equippedWeapon, armor = inventoryBundle.equippedArmor, hat = inventoryBundle.equippedHat,
            ring = inventoryBundle.equippedRing, necklace = inventoryBundle.equippedNecklace, trinket = inventoryBundle.equippedTrinket };
            inventoryPanel.receiveItemForDisplay(itemList, userObjects, inventoryBundle.guildInfo, categoryFilter, pageIndex, inventoryBundle.totalItemCount, true, jobsData);
         } else if (panelType == Panel.Type.Craft) {
            // Get the crafting panel
            CraftingPanel craftingPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);
            craftingPanel.receiveRefineableItems(itemList, pageIndex);
         }
      }

      public async void getInventoryForItemSelection (List<Item.Category> categoryFilter, List<int> itemIdsToExclude,
         int pageIndex, int itemsPerPage) {
         if (Global.player == null) {
            return;
         }

         PanelManager.self.itemSelectionScreen.setLoadBlocker(true);

         int userId = Global.player.userId;

         // Call the list and list count in parallel
         Task<string> listTask = NubisClient.call<string>(nameof(DB_Main.userInventory), userId, categoryFilter.ToArray(),
            itemIdsToExclude.ToArray(), true, pageIndex, itemsPerPage, Item.DurabilityFilter.None);
         Task<int> totalCountTask = NubisClient.call<int>(nameof(DB_Main.userInventoryCount), userId, categoryFilter.ToArray(),
            itemIdsToExclude.ToArray(), true, Item.DurabilityFilter.None);

         await Task.WhenAll(listTask, totalCountTask);

         // Parse the received data
         int totalCount = totalCountTask.Result;

         List<Item> itemList = UserInventory.processUserInventory(listTask.Result);

         // Filter inventory items here
         itemList.RemoveAll(_ => _.category == Item.Category.Usable);
         itemList.RemoveAll(_ => _.category == Item.Category.Quest_Item);

         PanelManager.self.itemSelectionScreen.receiveItemsFromServer(itemList, pageIndex, totalCount);
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
            PanelManager.self.linkPanel(Panel.Type.Ability_Panel);
         }

         // Fetch content from Nubis
         List<AbilitySQLData> abilityList = await NubisClient.call<List<AbilitySQLData>>(nameof(DB_Main.userAbilities), userId, AbilityEquipStatus.ALL);

         if (abilityList == null || abilityList.Count < 1) {
            D.debug("Failed to fetch Nubis abilities");
            return;
         }

         // Update the Skill Panel with the abilities we received from the server
         panel.receiveDataFromServer(abilityList.ToArray());
      }

      #endregion

      #region Server History

      public async void getServerHistory (int maxRows, DateTime startDate) {
         // Serialize input parameters
         long startDateBinary = startDate.ToBinary();

         // Call the functions
         Task<bool> isServerOnlineTask = NubisClient.call<bool>(nameof(DB_Main.isMasterServerOnline));
         Task<List<ServerHistoryInfo>> serverHistoryListTask = NubisClient.call<List<ServerHistoryInfo>>(nameof(DB_Main.getServerHistoryList), Global.MASTER_SERVER_PORT, startDateBinary, maxRows);

         await Task.WhenAll(isServerOnlineTask, serverHistoryListTask);

         //Parse the received data
         bool isServerOnline = isServerOnlineTask.Result;
         List<ServerHistoryInfo> serverHistoryList = null;
         try {
            serverHistoryList = serverHistoryListTask.Result;
         } catch (Exception e) {
            D.debug("Failed to fetch the server history.\n" + e.ToString());
         }

         if (serverHistoryList == null) {
            serverHistoryList = new List<ServerHistoryInfo>();
         }

         ServerStatusPanel.self.updatePanelWithServerHistory(isServerOnline, serverHistoryList);
      }

      public async void getOnlineServersListForClientLogin (bool isSteam) {
         List<int> serverPortList = await NubisClient.call<List<int>>(nameof(DB_Main.getOnlineServerList));

         if (serverPortList == null) {
            serverPortList = new List<int>();
         }

         if (serverPortList.Count > 0) {
            TitleScreen.self.startUpNetworkClient(isSteam, serverPortList);
         } else {
            // If the request failed, try logging in to the master server anyway
            TitleScreen.self.startUpNetworkClient(isSteam, new List<int>() { Global.MASTER_SERVER_PORT });
         }
      }

      #endregion
      
      #region Private Variables
      
      // Reference to item categories included for crafting requirements 
      private List<Item.Category> CraftingItemCategories = new List<Item.Category>() {
            Item.Category.CraftingIngredients,
            Item.Category.Quest_Item,
            Item.Category.Crop
      };
      
      #endregion
   }
}