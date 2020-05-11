using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Events;

public enum EquipmentType
{
   None = 0,
   Weapon = 1,
   Armor = 2,
   Helm = 3,
}

namespace NubisDataHandling {
   [Serializable]
   public class NubisInventoryEvent : UnityEvent<List<Item>> {
   }

   public static class UserInventory {
      public static List<Item> processUserInventory (string contentData, EquipmentType equipmentType) {
         List<Item> newItemList = new List<Item>();
         string splitter = "[next]";
         string[] rawItemGroup = contentData.Split(new string[] { splitter }, StringSplitOptions.None);

         int index = 0;
         for (int i = 0; i < rawItemGroup.Length; i++) {
            string itemGroup = rawItemGroup[i];
            string subSplitter = "[space]";
            string[] dataGroup = itemGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);

            if (dataGroup.Length > 1) {
               string xmlString = dataGroup[0];
               int itemId = int.Parse(dataGroup[1]);

               try {
                  switch (equipmentType) {
                     case EquipmentType.Weapon:
                        WeaponStatData weaponData = Util.xmlLoad<WeaponStatData>(xmlString);
                        Item weaponItem = new Item {
                           category = Item.Category.Weapon,
                           itemTypeId = weaponData.equipmentID,
                           id = itemId,
                           itemDescription = weaponData.equipmentDescription,
                           itemName = weaponData.equipmentName,
                           iconPath = weaponData.equipmentIconPath,
                           data = xmlString
                        };

                        newItemList.Add(weaponItem);

                        break;
                     case EquipmentType.Armor:
                        ArmorStatData armorData = Util.xmlLoad<ArmorStatData>(xmlString);
                        Item armorItem = new Item {
                           category = Item.Category.Armor,
                           itemTypeId = armorData.equipmentID,
                           id = itemId,
                           itemDescription = armorData.equipmentDescription,
                           itemName = armorData.equipmentName,
                           iconPath = armorData.equipmentIconPath,
                           data = xmlString
                        };

                        newItemList.Add(armorItem);
                        break;
                  }
               } catch {
                  D.warning("Something went wrong with content: Index=" + index);
               }
               index++;
            }
         }

         return newItemList;
      }
   }
}