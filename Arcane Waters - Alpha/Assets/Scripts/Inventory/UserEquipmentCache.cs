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

public class UserEquipmentCache : MonoBehaviour {
   #region Public Variables

   // Self
   public static UserEquipmentCache self;

   // Events to signal if loading is finished
   public UnityEvent finishedLoadingWeapons = new UnityEvent();
   public UnityEvent finishedLoadingArmors = new UnityEvent();

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

   public void fetchWeaponData (ItemDataType itemType) {
      StartCoroutine(CO_DownloadWeaponData(EquipmentType.Weapon, itemType));
   }

   public void fetchArmorData (ItemDataType itemType) {
      StartCoroutine(CO_DownloadWeaponData(EquipmentType.Armor, itemType));
   }

   private IEnumerator CO_DownloadWeaponData (EquipmentType equipmentType, ItemDataType itemType) {
      List<Item> itemList = new List<Item>();

      // Request the map from the web
      int userID = Global.player == null ? 0 : Global.player.userId;
      string site = "http://localhost/arcane";//"http://arcanewaters.com";
      switch (equipmentType) {
         case EquipmentType.Weapon:
            site += "/user_equipment_weapon_v1.php?usrId=" + userID;
            break;
         case EquipmentType.Armor:
            site += "/user_equipment_armor_v1.php?usrId=" + userID;
            break;
      }
      
      UnityWebRequest www = UnityWebRequest.Get(site);
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
            for (int i = 0; i < xmlGroup.Length - 2; i+= 2) {
               string xmlString = xmlGroup[i];
               try {
                  switch (equipmentType) {
                     case EquipmentType.Weapon:
                        WeaponStatData weaponData = Util.xmlLoad<WeaponStatData>(xmlString);
                        if (!_weaponDataList.ContainsKey(weaponData.equipmentID)) {
                           weaponData.itemSqlID = int.Parse(xmlGroup[i + 1]);
                           _weaponDataList.Add(weaponData.equipmentID, weaponData);
                        }
                        break;
                     case EquipmentType.Armor:
                        ArmorStatData armorData = Util.xmlLoad<ArmorStatData>(xmlString);
                        if (!_armorDataList.ContainsKey(armorData.equipmentID)) {
                           armorData.itemSqlID = int.Parse(xmlGroup[i + 1]);
                           _armorDataList.Add(armorData.equipmentID, armorData);
                        }
                        break;
                  }
               } catch {
                  Debug.LogWarning("Something went wrong with content: Index=" + index);
                  D.warning("Something went wrong with content: Index=" + index);
               }

               index++;

               // The last entry will be blank
               if (index >= xmlGroup.Length - 2) {
                  break;
               }
            }
            switch (itemType) {
               case ItemDataType.Inventory:
                  // Get the inventory panel
                  InventoryPanel panel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

                  // Make sure the inventory panel is showing
                  if (!panel.isShowing()) {
                     PanelManager.self.pushPanel(Panel.Type.Inventory);
                  }

                  foreach (WeaponStatData weaponData in _weaponDataList.Values) {
                     Item newItem = new Item {
                        category = Item.Category.Weapon,
                        itemTypeId = weaponData.equipmentID,
                        id = weaponData.itemSqlID,
                        itemDescription = weaponData.equipmentDescription,
                        itemName = weaponData.equipmentName,
                        iconPath = weaponData.equipmentIconPath
                     };
                     itemList.Add(newItem);
                  }

                  panel.receiveItemForDisplay(itemList.ToArray());
                  break;
               case ItemDataType.Crafting:
                  // TODO: Insert crafting logic here
                  break;
            }

            if (equipmentType == EquipmentType.Weapon) {
               finishedLoadingWeapons.Invoke();
            } else if (equipmentType == EquipmentType.Armor) {
               finishedLoadingArmors.Invoke();
            }
         }
      }
   }

   public WeaponStatData getWeaponDataByEquipmentID (int equipmentID) {
      WeaponStatData weaponData = _weaponDataList.Values.ToList().Find(_ => _.equipmentID == equipmentID);
      if (weaponData != null) {
         return weaponData;
      }

      return null;
   }

   public WeaponStatData getWeaponDataBySQLID (int weapontDataID) {
      WeaponStatData returnData = _weaponDataList.Values.ToList().Find(_ => _.itemSqlID == weapontDataID);
      if (returnData != null) {
         return returnData;
      }

      Debug.LogWarning("The weapon data does not exist!: " + weapontDataID);
      return null;
   }

   public ArmorStatData getArmorData (int armorTypeID) {
      if (_armorDataList.ContainsKey(armorTypeID)) {
         return _armorDataList[armorTypeID];
      }

      return null;
   }

   public ArmorStatData getArmorDataByID (int armorDataID) {
      ArmorStatData returnData = _armorDataList.Values.ToList().Find(_ => _.itemSqlID == armorDataID);
      if (returnData != null) {
         return returnData;
      }

      Debug.LogWarning("The armor data does not exist!: " + armorDataID);
      return null;
   }

   #region Private Variables

   // The cached weapon data collection for this local player
   protected Dictionary<int, WeaponStatData> _weaponDataList = new Dictionary<int, WeaponStatData>();

   // The cached armor data collection for this local player
   protected Dictionary<int, ArmorStatData> _armorDataList = new Dictionary<int, ArmorStatData>();

   #endregion
}
