using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Networking;
using static EquipmentToolManager;
using System.Linq;
namespace OldEquipmentFetcher {
   public class UserEquipmentFetcher : MonoBehaviour {
      #region Public Variables

      // Self
      public static UserEquipmentFetcher self;

      // Host and PHP File directories
      public const string WEB_DIRECTORY = "http://arcanewaters.com";//"http://localhost/arcane2";
      public const string WEAPON_DIRECTORY = "/user_equipment_weapon_v3.php?usrId=";
      public const string ARMOR_DIRECTORY = "/user_equipment_armor_v3.php?usrId=";
      public const string USER_DIRECTORY = "/user_data_v1.php?usrId=";

      public const string FETCH_CRAFTABLE_WEAPONS = "/fetch_craftable_weapons_v4.php?usrId=";
      public const string FETCH_CRAFTABLE_ARMORS = "/fetch_craftable_armors_v4.php?usrId=";
      public const string FETCH_CRAFTING_INGREDIENTS = "/fetch_crafting_ingredients_v3.php?usrId=";
      public const string FETCH_EQUIPPED_ITEMS = "/fetch_equipped_items_v3.php?usrId=";
      public const string FETCH_SINGLE_BP = "/fetch_single_blueprint_v4.php?";

      // The category types that are being fetched
      public Item.Category categoryFilter;

      // The current item cap per page
      public int itemsPerPage;

      // The current page of the item preview
      public int pageIndex;

      // The current data procedure being implemented
      public ItemDataType currentItemData;

      public enum ItemDataType
      {
         None = 0,
         Inventory = 1,
         Crafting = 2,
         SingleCrafting = 3
      }

      public enum RequestType
      {
         None = 0,
         EquippedItems = 1,
         CraftingIngredients = 2,
         SingleBlueprint = 3,
         CraftableWeapons = 4,
         CraftableArmors = 5,
      }

      #endregion

      private void Awake () {
         self = this;
      }

      private void initializeRequest (RequestType requestType, int id = 0) {
         switch (requestType) {
            case RequestType.CraftableWeapons:
               StartCoroutine(CO_DownloadCraftableWeapons());
               break;
            case RequestType.CraftableArmors:
               StartCoroutine(CO_DownloadCraftableArmors());
               break;
            case RequestType.EquippedItems:
               StartCoroutine(CO_DownloadEquippedItems());
               break;
            case RequestType.CraftingIngredients:
               StartCoroutine(CO_DownloadCraftingIngredients());
               break;
            case RequestType.SingleBlueprint:
               StartCoroutine(CO_FetchSingleBlueprint(id));
               break;
         }
         _requiredRequests.Add(requestType);
      }

      #region Craftable Item Fetching

      public void checkCraftingInfo (int bluePrintId) {
         resetValues();

         currentItemData = ItemDataType.SingleCrafting;

         initializeRequest(RequestType.EquippedItems);
         initializeRequest(RequestType.CraftingIngredients);
         initializeRequest(RequestType.SingleBlueprint, bluePrintId);
      }

      private IEnumerator CO_FetchSingleBlueprint (int bluePrintId) {
         int userID = Global.player == null ? 0 : Global.player.userId;

         // Request the user info from the web
         UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + FETCH_SINGLE_BP +
            "bpId=" + bluePrintId + "&usrId=" + userID);
         yield return www.SendWebRequest();

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            // Grab the map data from the request
            string rawData = www.downloadHandler.text;
            string splitter = "[next]";
            string[] xmlGroup = rawData.Split(new string[] { splitter }, StringSplitOptions.None);

            for (int i = 0; i < xmlGroup.Length; i++) {
               string xmlSubGroup = xmlGroup[i];
               processCraftableGroups(xmlSubGroup);
            }

            _finishedRequests.Add(RequestType.SingleBlueprint);
            finalizeSingleCraftingProcedure();
         }
      }

      private void finalizeSingleCraftingProcedure () {
         bool hasCompletedRequests = true;

         foreach (RequestType request in _requiredRequests) {
            if (!_finishedRequests.Contains(request)) {
               hasCompletedRequests = false;
            }
         }

         if (hasCompletedRequests) {
            processCraftableItems();
  }
      }

      #endregion

      #region Populate crafting panel

      public void fetchCraftableData (int pageIndex, int itemsPerPage) {
         if (itemsPerPage > 200) {
            D.warning("Requesting too many items per page.");
            return;
         }

         resetValues();

         currentItemData = ItemDataType.Crafting;

         this.pageIndex = pageIndex;
         this.itemsPerPage = itemsPerPage;

         initializeRequest(RequestType.CraftableWeapons);
         initializeRequest(RequestType.CraftableArmors);
         initializeRequest(RequestType.EquippedItems);
         initializeRequest(RequestType.CraftingIngredients);
      }

      private IEnumerator CO_DownloadCraftingIngredients () {
         int userID = Global.player == null ? 0 : Global.player.userId;

         // Request the user info from the web
         UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + FETCH_CRAFTING_INGREDIENTS + userID);
         yield return www.SendWebRequest();

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            // Grab the crafting data from the request
            string rawData = www.downloadHandler.text;
            string splitter = "[next]";
            string[] xmlGroup = rawData.Split(new string[] { splitter }, StringSplitOptions.None);

            for (int i = 0; i < xmlGroup.Length; i++) {
               string xmlSubGroup = xmlGroup[i];
               processCraftingIngredientGroups(xmlSubGroup);
            }

            _finishedRequests.Add(RequestType.CraftingIngredients);
            if (currentItemData == ItemDataType.Crafting) {
               finalizeCraftingProcedure();
            } else if (currentItemData == ItemDataType.SingleCrafting) {
               finalizeSingleCraftingProcedure();
            }
         }
      }

      private IEnumerator CO_DownloadEquippedItems () {
         int userID = Global.player == null ? 0 : Global.player.userId;

         // Request the user info from the web
         UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + FETCH_EQUIPPED_ITEMS + userID);
         yield return www.SendWebRequest();

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            // Grab the crafting data from the request
            string rawData = www.downloadHandler.text;
            string splitter = "[next]";
            string[] xmlGroup = rawData.Split(new string[] { splitter }, StringSplitOptions.None);

            for (int i = 0; i < xmlGroup.Length; i++) {
               string xmlSubGroup = xmlGroup[i];
               processEquippedItemGroups(xmlSubGroup);
            }

            _finishedRequests.Add(RequestType.EquippedItems);
            if (currentItemData == ItemDataType.Crafting) {
               finalizeCraftingProcedure();
            } else if (currentItemData == ItemDataType.SingleCrafting) {
               finalizeSingleCraftingProcedure();
            }
         }
      }

      private IEnumerator CO_DownloadCraftableWeapons () {
         int userID = Global.player == null ? 0 : Global.player.userId;

         // Request the user info from the web
         UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + FETCH_CRAFTABLE_WEAPONS + userID);
         yield return www.SendWebRequest();

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            // Grab the crafting data from the request
            string rawData = www.downloadHandler.text;
            string splitter = "[next]";
            string[] xmlGroup = rawData.Split(new string[] { splitter }, StringSplitOptions.None);

            for (int i = 0; i < xmlGroup.Length; i++) {
               string xmlSubGroup = xmlGroup[i];
               processCraftableGroups(xmlSubGroup);
            }

            _finishedRequests.Add(RequestType.CraftableWeapons);
            finalizeCraftingProcedure();
         }
      }

      private IEnumerator CO_DownloadCraftableArmors () {
         int userID = Global.player == null ? 0 : Global.player.userId;

         // Request the user info from the web
         UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + FETCH_CRAFTABLE_ARMORS + userID);
         yield return www.SendWebRequest();

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            // Grab the crafting data from the request
            string rawData = www.downloadHandler.text;
            string splitter = "[next]";
            string[] xmlGroup = rawData.Split(new string[] { splitter }, StringSplitOptions.None);

            for (int i = 0; i < xmlGroup.Length; i++) {
               string xmlSubGroup = xmlGroup[i];
               processCraftableGroups(xmlSubGroup);
            }

            _finishedRequests.Add(RequestType.CraftableArmors);
            finalizeCraftingProcedure();
         }
      }

      private void finalizeCraftingProcedure () {
         bool hasCompletedRequests = true;

         foreach (RequestType request in _requiredRequests) {
            if (!_finishedRequests.Contains(request)) {
               hasCompletedRequests = false;
            }
         }

         if (hasCompletedRequests) {
            processCraftableItems();

            // Get the panel
            CraftingPanel craftingPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

            // Show the crafting panel, except if the reward panel is showing
            if (!craftingPanel.isShowing()) {
               PanelManager.self.pushPanel(craftingPanel.type);
            }

            craftingPanel.updatePanelWithBlueprintList(_itemList.ToArray(), _bluePrintStatuses.ToArray(), pageIndex, itemsPerPage);
         }
      }

      #endregion

      #region Subgroup translations

      private void processCraftableGroups (string xmlSubGroup) {
         string subSplitter = "[space]";
         string[] xmlPair = xmlSubGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);

         if (xmlPair.Length > 0) {
            // Crafting ingredients have no crafting data
            if (xmlPair.Length == 6) {
               int itemID = int.Parse(xmlPair[0]);
               Item.Category itemCategory = (Item.Category) int.Parse(xmlPair[1]);
               int itemTypeID = int.Parse(xmlPair[2]);

               CraftableItemRequirements craftableRequirements = xmlPair[3] == "" ? null : Util.xmlLoad<CraftableItemRequirements>(xmlPair[3]);
               if (craftableRequirements != null) {
                  string itemName = "";
                  string itemDesc = "";
                  string itemIconPath = "";
                  if (craftableRequirements.resultItem.category == Item.Category.Weapon) {
                     WeaponStatData weaponData = Util.xmlLoad<WeaponStatData>(xmlPair[4]);
                     itemName = weaponData.equipmentName;
                     itemDesc = weaponData.equipmentDescription;
                     itemIconPath = weaponData.equipmentIconPath;
                  }
                  if (craftableRequirements.resultItem.category == Item.Category.Armor) {
                     ArmorStatData armorData = Util.xmlLoad<ArmorStatData>(xmlPair[4]);
                     itemName = armorData.equipmentName;
                     itemDesc = armorData.equipmentDescription;
                     itemIconPath = armorData.equipmentIconPath;
                  }

                  Item item = craftableRequirements.resultItem;
                  item.id = itemID;
                  item.data = xmlPair[4];
                  item.itemName = itemName;
                  item.itemDescription = itemDesc;
                  item.iconPath = itemIconPath;

                  _craftableList.Add(craftableRequirements);
                  this._itemList.Add(item);
               }
            }
         }
      }

      private void processEquippedItemGroups (string xmlSubGroup) {
         string subSplitter = "[space]";
         string[] xmlPair = xmlSubGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);

         if (xmlPair.Length > 0) {
            // Crafting ingredients have no crafting data
            if (xmlPair.Length == 5) {
               int itemID = int.Parse(xmlPair[0]);
               Item.Category itemCategory = (Item.Category) int.Parse(xmlPair[1]);
               int itemTypeID = int.Parse(xmlPair[2]);

               switch (itemCategory) {
                  case Item.Category.Weapon:
                     _equippedWeaponItem = new Item {
                        category = Item.Category.Weapon,
                        itemTypeId = itemTypeID,
                        data = xmlPair[3]
                     };
                     break;
                  case Item.Category.Armor:
                     _equippedArmorItem = new Item {
                        category = Item.Category.Armor,
                        itemTypeId = itemTypeID,
                        data = xmlPair[3]
                     };
                     break;
               }
            }
         }
      }

      private void processCraftingIngredientGroups (string xmlSubGroup) {
         string subSplitter = "[space]";
         string[] xmlPair = xmlSubGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);

         if (xmlPair.Length > 0) {
            // Crafting ingredients have no crafting data
            if (xmlPair.Length == 4) {
               int itemID = int.Parse(xmlPair[0]);
               Item.Category itemCategory = (Item.Category) int.Parse(xmlPair[1]);
               int itemTypeID = int.Parse(xmlPair[2]);

               _ingredientItems.Add(new Item {
                  id = itemID,
                  category = itemCategory,
                  itemTypeId = itemTypeID
               });
            }
         }
      }

      private void processCraftableItems () {
         foreach (CraftableItemRequirements craftable in _craftableList) {
            Blueprint.Status status = Blueprint.Status.Craftable;
            if (craftable == null) {
               status = Blueprint.Status.MissingRecipe;
            } else {
               // Compare the ingredients present in the inventory with the requisites
               foreach (Item requiredIngredient in craftable.combinationRequirements) {
                  // Get the inventory ingredient, if there is any
                  Item inventoryIngredient = _ingredientItems.Find(s =>
                     s.itemTypeId == requiredIngredient.itemTypeId);

                  // Verify that there are enough items in the inventory stack
                  if (inventoryIngredient == null || inventoryIngredient.count < requiredIngredient.count) {
                     status = Blueprint.Status.NotCraftable;
                     break;
                  }
               }
            }
            _bluePrintStatuses.Add(status);
         }
      }

      #endregion

      #region Inventory Fetching

      public void fetchEquipmentData (int pageIndex = 0, int itemsPerPage = 0, Item.Category[] categoryFilter = null) {
         if (itemsPerPage > 200) {
            D.warning("Requesting too many items per page.");
            return;
         }

         this.categoryFilter = categoryFilter == null ? Item.Category.None : categoryFilter[0];
         this.pageIndex = pageIndex;
         this.itemsPerPage = itemsPerPage;

         currentItemData = ItemDataType.Inventory;

         _itemList = new List<Item>();
         _hasFinishedEquipmentData = false;
         _hasFinishedUserData = false;

         StartCoroutine(CO_DownloadUserData());
         StartCoroutine(CO_DownloadEquipmentData(EquipmentType.Weapon));
      }

      private IEnumerator CO_DownloadEquipmentData (EquipmentType equipmentType) {
         // Request the map from the web
         int userID = Global.player == null ? 0 : Global.player.userId;
         string newPath = WEB_DIRECTORY;

         switch (equipmentType) {
            case EquipmentType.Weapon:
               newPath += WEAPON_DIRECTORY + userID;
               break;
            case EquipmentType.Armor:
               newPath += ARMOR_DIRECTORY + userID;
               break;
         }

         UnityWebRequest www = UnityWebRequest.Get(newPath);
         yield return www.SendWebRequest();

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            // Grab the map data from the request
            string mapData = www.downloadHandler.text;
            string splitter = ":::";

            string[] xmlGroup = mapData.Split(new string[] { splitter }, StringSplitOptions.None);
            int index = 0;
            if (xmlGroup.Length > 0) {
               for (int i = 0; i < xmlGroup.Length - 2; i += 2) {
                  string xmlString = xmlGroup[i];
                  try {
                     switch (equipmentType) {
                        case EquipmentType.Weapon:
                           WeaponStatData weaponData = Util.xmlLoad<WeaponStatData>(xmlString);
                           int weaponID = int.Parse(xmlGroup[i + 1]);

                           Item weaponItem = new Item {
                              category = Item.Category.Weapon,
                              itemTypeId = weaponData.equipmentID,
                              id = weaponID,
                              itemDescription = weaponData.equipmentDescription,
                              itemName = weaponData.equipmentName,
                              iconPath = weaponData.equipmentIconPath,
                              data = xmlString
                           };

                           _itemList.Add(weaponItem);

                           break;
                        case EquipmentType.Armor:
                           ArmorStatData armorData = Util.xmlLoad<ArmorStatData>(xmlString);
                           int armorID = int.Parse(xmlGroup[i + 1]);

                           Item armorItem = new Item {
                              category = Item.Category.Armor,
                              itemTypeId = armorData.equipmentID,
                              id = armorID,
                              itemDescription = armorData.equipmentDescription,
                              itemName = armorData.equipmentName,
                              iconPath = armorData.equipmentIconPath,
                              data = xmlString
                           };

                           _itemList.Add(armorItem);
                           break;
                     }
                  } catch {
                     D.warning("Something went wrong with content: Index=" + index);
                  }

                  index++;

                  // The last entry will be blank
                  if (index >= xmlGroup.Length - 2) {
                     break;
                  }
               }

               if (equipmentType == EquipmentType.Weapon) {
                  StartCoroutine(CO_DownloadEquipmentData(EquipmentType.Armor));
               } else if (equipmentType == EquipmentType.Armor) {
                  _hasFinishedEquipmentData = true;
                  finalizeInventory();
               }
            }
         }
      }

      private IEnumerator CO_DownloadUserData () {
         int userID = Global.player == null ? 0 : Global.player.userId;

         // Request the user info from the web
         UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + USER_DIRECTORY + userID);
         yield return www.SendWebRequest();

         Dictionary<string, string> xmlPairCollection = new Dictionary<string, string>();

         if (www.isNetworkError || www.isHttpError) {
            D.warning(www.error);
         } else {
            // Grab the map data from the request
            string rawData = www.downloadHandler.text;
            string splitter = "[space]";
            string[] xmlGroup = rawData.Split(new string[] { splitter }, StringSplitOptions.None);

            for (int i = 0; i < xmlGroup.Length; i++) {
               string xmlSubGroup = xmlGroup[i];
               string subSplitter = ":";
               string[] xmlPair = xmlSubGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);
               if (xmlPair.Length > 1) {
                  xmlPairCollection.Add(xmlPair[0], xmlPair[1]);
               } else {
                  xmlPairCollection.Add(xmlPair[0], "0");
               }
            }

            _userInfo = new UserInfo {
               gold = int.Parse(xmlPairCollection["usrGold"]),
               gems = int.Parse(xmlPairCollection["accGems"]),
               gender = (Gender.Type) int.Parse(xmlPairCollection["usrGender"]),
               username = xmlPairCollection["usrName"],
               bodyType = (BodyLayer.Type) int.Parse(xmlPairCollection["bodyType"]),
               hairType = (HairLayer.Type) int.Parse(xmlPairCollection["hairType"]),
               hairColor1 = (ColorType) int.Parse(xmlPairCollection["hairColor1"]),
               hairColor2 = (ColorType) int.Parse(xmlPairCollection["hairColor2"]),
               eyesType = (EyesLayer.Type) int.Parse(xmlPairCollection["eyesType"]),
               eyesColor1 = (ColorType) int.Parse(xmlPairCollection["eyesColor1"]),
               eyesColor2 = (ColorType) int.Parse(xmlPairCollection["eyesColor2"]),
               weaponId = int.Parse(xmlPairCollection["wpnId"]),
               armorId = int.Parse(xmlPairCollection["armId"])
            };
            _hasFinishedUserData = true;
            finalizeInventory();
         }
      }

      private void finalizeInventory () {
         if (_hasFinishedUserData && _hasFinishedEquipmentData) {
            // Get the inventory panel
            InventoryPanel panel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

            // Make sure the inventory panel is showing
            if (!panel.isShowing()) {
               PanelManager.self.pushPanel(Panel.Type.Inventory);
            }

            Item equippedWeapon = _itemList.Find(_ => _.id == _userInfo.weaponId);
            if (equippedWeapon == null) {
               equippedWeapon = new Item { itemTypeId = 0, color1 = ColorType.None, color2 = ColorType.None };
            }
            Item equippedArmor = _itemList.Find(_ => _.id == _userInfo.armorId);
            if (equippedArmor == null) {
               equippedArmor = new Item { itemTypeId = 0, color1 = ColorType.None, color2 = ColorType.None };
            }
            UserObjects userObjects = new UserObjects { userInfo = _userInfo, weapon = equippedWeapon, armor = equippedArmor };

            // Calculate the maximum page number
            int maxPage = Mathf.CeilToInt((float) _itemList.Count / itemsPerPage);
            if (maxPage == 0) {
               maxPage = 1;
            }

            // Clamp the requested page number to the max page - the number of items could have changed
            pageIndex = Mathf.Clamp(pageIndex, 1, maxPage);

            panel.receiveItemForDisplay(_itemList.ToArray(), userObjects, categoryFilter, pageIndex);
         }
      }

      #endregion

      private void resetValues () {
         _itemList = new List<Item>();
         _bluePrintStatuses = new List<Blueprint.Status>();
         _craftableList = new List<CraftableItemRequirements>();
         _ingredientItems = new List<Item>();
         _requiredRequests.Clear();
         _finishedRequests.Clear();
      }

      #region Private Variables

      // The fetched Item list
      private List<Item> _itemList = new List<Item>();

      // Crafting data lists
      private List<Blueprint.Status> _bluePrintStatuses = new List<Blueprint.Status>();
      private List<CraftableItemRequirements> _craftableList = new List<CraftableItemRequirements>();
      private List<Item> _ingredientItems = new List<Item>();

      // Equipped items
      private Item _equippedWeaponItem, _equippedArmorItem;

      // The fetched user info
      private UserInfo _userInfo = null;

      // Determines if the data fetch is complete
      private bool _hasFinishedEquipmentData;
      private bool _hasFinishedUserData;

      // Request handling hashsets
      private HashSet<RequestType> _finishedRequests = new HashSet<RequestType>();
      private HashSet<RequestType> _requiredRequests = new HashSet<RequestType>();

      #endregion
   }
}