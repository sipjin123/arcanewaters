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

      #endregion

      private void Awake () {
         self = this;
         webDirectory = "http://" + Global.getAddress(MyNetworkManager.ServerType.AmazonVPC) + ":7900/";
      }

      public void fetchXmlVersion () {
         StartCoroutine(CO_ProcessXmlVersion());
      }

      private IEnumerator CO_ProcessXmlVersion () {
         UnityWebRequest mapDataRequest = UnityWebRequest.Get(webDirectory + "fetch_xml_version_v1");
         yield return mapDataRequest.SendWebRequest();

         if (mapDataRequest.isNetworkError || mapDataRequest.isHttpError) {
            D.warning(mapDataRequest.error);
         } else {
            int xmlVersion = int.Parse(mapDataRequest.downloadHandler.text);
            xmlVersionEvent.Invoke(xmlVersion);
         }
      }

      private IEnumerator CO_ProcessMapData (string mapName) {
         string rawMapData = "";
         UnityWebRequest mapDataRequest = UnityWebRequest.Get(webDirectory + "fetch_map_data_v1?mapName=" + mapName);
         yield return mapDataRequest.SendWebRequest();

         if (mapDataRequest.isNetworkError || mapDataRequest.isHttpError) {
            D.warning(mapDataRequest.error);
         } else {
            rawMapData = mapDataRequest.downloadHandler.text;
         }
      }

      public void checkCraftingInfo (int bluePrintId) {
         StartCoroutine(CO_ProcessCraftingInfo(bluePrintId));
      }

      private IEnumerator CO_ProcessCraftingInfo (int bluePrintId) {
         int userId = Global.player == null ? 0 : Global.player.userId;
         EquippedItemData equippedItemData = new EquippedItemData();
         List<Item> craftingIngredients = new List<Item>();
         List<CraftableItemData> craftableItems = new List<CraftableItemData>();

         UnityWebRequest singleBpRequest = UnityWebRequest.Get(webDirectory + "fetch_single_blueprint_v4?usrId=" + userId + "&bpId=" + bluePrintId);
         yield return singleBpRequest.SendWebRequest();

         UnityWebRequest craftingIngredientRequest = UnityWebRequest.Get(webDirectory + "fetch_crafting_ingredients_v3?usrId=" + userId);
         yield return craftingIngredientRequest.SendWebRequest();

         UnityWebRequest equippedItemRequest = UnityWebRequest.Get(webDirectory + "fetch_equipped_items_v3?usrId=" + userId);
         yield return equippedItemRequest.SendWebRequest();

         string rawBlueprintData = "";
         string craftingIngredientData = "";
         string equippedItemContent = "";

         if (singleBpRequest.isNetworkError || singleBpRequest.isHttpError) {
            D.warning(singleBpRequest.error);
         } else {
            rawBlueprintData = singleBpRequest.downloadHandler.text;
         }

         if (craftingIngredientRequest.isNetworkError || craftingIngredientRequest.isHttpError) {
            D.warning(craftingIngredientRequest.error);
         } else {
            craftingIngredientData = craftingIngredientRequest.downloadHandler.text;
         }

         if (equippedItemRequest.isNetworkError || equippedItemRequest.isHttpError) {
            D.warning(equippedItemRequest.error);
         } else {
            equippedItemContent = equippedItemRequest.downloadHandler.text;
         }

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
      }

      public void fetchCraftableData (int pageIndex, int itemsPerPage) {
         StartCoroutine(CO_ProcessCraftableData(pageIndex, itemsPerPage));
      }

      private IEnumerator CO_ProcessCraftableData (int pageIndex, int itemsPerPage) {
         int userId = Global.player == null ? 0 : Global.player.userId;
         List<Item> craftingIngredients = new List<Item>();
         List<Item> craftableItems = new List<Item>();
         List<Blueprint.Status> blueprintStatus = new List<Blueprint.Status>();

         string craftingIngredientXml = "";
         string armorFetch = "";
         string weaponFetch = "";

         UnityWebRequest craftingIngredientRequest = UnityWebRequest.Get(webDirectory + "fetch_crafting_ingredients_v3?usrId=" + userId);
         yield return craftingIngredientRequest.SendWebRequest();

         UnityWebRequest craftableWeaponsRequest = UnityWebRequest.Get(webDirectory + "fetch_craftable_weapons_v4?usrId=" + userId);
         yield return craftableWeaponsRequest.SendWebRequest();

         UnityWebRequest craftableArmorsRequest = UnityWebRequest.Get(webDirectory + "fetch_craftable_armors_v4?usrId=" + userId);
         yield return craftableArmorsRequest.SendWebRequest();

         if (craftingIngredientRequest.isNetworkError || craftingIngredientRequest.isHttpError) {
            D.warning(craftingIngredientRequest.error);
         } else {
            craftingIngredientXml = craftingIngredientRequest.downloadHandler.text;
         }
         if (craftableWeaponsRequest.isNetworkError || craftableWeaponsRequest.isHttpError) {
            D.warning(craftableWeaponsRequest.error);
         } else {
            weaponFetch = craftableWeaponsRequest.downloadHandler.text;
         }
         if (craftableArmorsRequest.isNetworkError || craftableArmorsRequest.isHttpError) {
            D.warning(craftableArmorsRequest.error);
         } else {
            armorFetch = craftableArmorsRequest.downloadHandler.text;
         }

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
      }

      public void fetchEquipmentData (int pageIndex = 0, int itemsPerPage = 0, Item.Category[] categoryFilter = null) {
         if (itemsPerPage > 200) {
            D.warning("Requesting too many items per page.");
            return;
         }
         StartCoroutine(processUserInventory(pageIndex, itemsPerPage, categoryFilter));
      }

      private IEnumerator processUserInventory (int pageIndex = 0, int itemsPerPage = 0, Item.Category[] categoryFilter = null) {
         int userId = Global.player == null ? 0 : Global.player.userId;
         UserInfo newUserInfo = new UserInfo();
         List<Item> userInventory = new List<Item>();

         this.categoryFilter = categoryFilter == null ? Item.Category.None : categoryFilter[0];
         this.pageIndex = pageIndex;
         this.itemsPerPage = itemsPerPage;

         string weaponRawData = "";
         string armorRawData = "";
         string userRawData = "";
         UnityWebRequest weaponInventoryRequest = UnityWebRequest.Get(webDirectory + "fetch_user_inventory_v1?usrId=" + userId + "&itmType=" + (int) EquipmentType.Weapon);
         yield return weaponInventoryRequest.SendWebRequest();
         UnityWebRequest armorInventoryRequest = UnityWebRequest.Get(webDirectory + "fetch_user_inventory_v1?usrId=" + userId + "&itmType=" + (int) EquipmentType.Armor);
         yield return armorInventoryRequest.SendWebRequest();
         UnityWebRequest userDataRequest = UnityWebRequest.Get(webDirectory + "user_data_v1?usrId=" + userId);
         yield return userDataRequest.SendWebRequest();

         if (weaponInventoryRequest.isNetworkError || weaponInventoryRequest.isHttpError) {
            D.warning(weaponInventoryRequest.error);
         } else {
            weaponRawData = weaponInventoryRequest.downloadHandler.text;
         }
         if (armorInventoryRequest.isNetworkError || armorInventoryRequest.isHttpError) {
            D.warning(armorInventoryRequest.error);
         } else {
            armorRawData = armorInventoryRequest.downloadHandler.text;
         }
         if (userDataRequest.isNetworkError || userDataRequest.isHttpError) {
            D.warning(userDataRequest.error);
         } else {
            userRawData = userDataRequest.downloadHandler.text;
         }

         List<Item> weaponList = UserInventory.processUserInventory(weaponRawData, EquipmentType.Weapon);
         List<Item> armorList = UserInventory.processUserInventory(armorRawData, EquipmentType.Armor);
         newUserInfo = UserInfoData.processUserInfo(userRawData);

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
      }
   }
}