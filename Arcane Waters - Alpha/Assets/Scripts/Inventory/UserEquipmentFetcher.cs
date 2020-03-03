using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Networking;
using static EquipmentToolManager;
using System.Linq;
using UnityEngine.Events;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class UserEquipmentFetcher : MonoBehaviour {
   #region Public Variables

   // Self
   public static UserEquipmentFetcher self;

   // Events to signal if loading is finished
   public UnityEvent finishedLoadingEquipment = new UnityEvent();

   // Host and PHP File directories
   public const string WEB_DIRECTORY = "http://localhost/arcane";//"http://arcanewaters.com";
   public const string WEAPON_DIRECTORY = "/user_equipment_weapon_v1.php?usrId=";
   public const string ARMOR_DIRECTORY = "/user_equipment_armor_v1.php?usrId=";
   public const string USER_DIRECTORY = "/user_data_v1.php?usrId=";

   // The category types that are being fetched
   public Item.Category categoryFilter;

   // The current item cap per page
   public int itemsPerPage;

   // The current page of the item preview
   public int pageIndex;

   public enum ItemDataType
   {
      None = 0,
      Inventory = 1,
      Crafting = 2
   }

   #endregion

   private void Awake () {
      self = this;
   }

   public void fetchEquipmentData (ItemDataType itemType, int pageIndex = 0, int itemsPerPage = 0, Item.Category[] categoryFilter = null) {
      if (itemsPerPage > 200) {
         D.warning("Requesting too many items per page.");
         return;
      }

      this.categoryFilter = categoryFilter == null ? Item.Category.None : categoryFilter[0];
      this.pageIndex = pageIndex;
      this.itemsPerPage = itemsPerPage;

      _itemList = new List<Item>();
      _hasFinishedEquipmentData = false;
      _hasFinishedUserData = false;

      StartCoroutine(CO_DownloadUserData(itemType));
      StartCoroutine(CO_DownloadEquipmentData(EquipmentType.Weapon, itemType));
   }

   private IEnumerator CO_DownloadEquipmentData (EquipmentType equipmentType, ItemDataType itemType) {
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
               StartCoroutine(CO_DownloadEquipmentData(EquipmentType.Armor, itemType));
            } else if (equipmentType == EquipmentType.Armor) {
               _hasFinishedEquipmentData = true;
               finalizeItemList(itemType);
            }
         }
      }
   }

   private IEnumerator CO_DownloadUserData (ItemDataType itemType) {
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
            accountName = xmlPairCollection["usrName"],
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
         finalizeItemList(itemType);
      }
   }

   private void finalizeItemList (ItemDataType itemType) {
      if (_hasFinishedUserData && _hasFinishedEquipmentData) {
         switch (itemType) {
            case ItemDataType.Inventory:
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
               break;
            case ItemDataType.Crafting:
               // TODO: Insert crafting logic here
               break;
         }

         finishedLoadingEquipment.Invoke();
      }
   }

   #region Private Variables

   // The fetched Item list
   private List<Item> _itemList = new List<Item>();

   // The fetched user info
   private UserInfo _userInfo = null;

   // Determines if the data fetch is complete
   private bool _hasFinishedEquipmentData;
   private bool _hasFinishedUserData;

   #endregion
}
