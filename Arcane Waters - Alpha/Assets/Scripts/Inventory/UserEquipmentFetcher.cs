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

   public void fetchEquipmentData (ItemDataType itemType) {
      _itemList = new List<Item>();
      StartCoroutine(CO_DownloadEquipmentData(EquipmentType.Weapon, itemType));
   }

   private IEnumerator CO_DownloadEquipmentData (EquipmentType equipmentType, ItemDataType itemType) {
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
                  Debug.LogWarning("Something went wrong with content: Index=" + index);
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
               finalizeItemList(itemType);
            }
         }
      }
   }

   private void finalizeItemList (ItemDataType itemType) {
      switch (itemType) {
         case ItemDataType.Inventory:
            // Get the inventory panel
            InventoryPanel panel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

            // Make sure the inventory panel is showing
            if (!panel.isShowing()) {
               PanelManager.self.pushPanel(Panel.Type.Inventory);
            }

            panel.receiveItemForDisplay(_itemList.ToArray());
            break;
         case ItemDataType.Crafting:
            // TODO: Insert crafting logic here
            break;
      }

      finishedLoadingEquipment.Invoke();
   }

   #region Private Variables

   // The fetched Item list
   private List<Item> _itemList = new List<Item>();

   #endregion
}
