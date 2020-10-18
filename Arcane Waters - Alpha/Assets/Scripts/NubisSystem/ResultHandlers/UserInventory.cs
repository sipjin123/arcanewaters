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
   Hat = 3,
}

namespace NubisDataHandling {
   [Serializable]
   public class NubisInventoryEvent : UnityEvent<List<Item>> {
   }

   public static class UserInventory {
      public static List<Item> processUserInventory (string contentData) {
         List<Item> newItemList = new List<Item>();
         string splitter = "[next]";
         string[] rawItemGroup = contentData.Split(new string[] { splitter }, StringSplitOptions.None);

         int index = 0;
         for (int i = 0; i < rawItemGroup.Length; i++) {
            string itemGroup = rawItemGroup[i];
            string subSplitter = "[space]";
            string[] dataGroup = itemGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);

            if (dataGroup.Length > 1) {
               int itmId = int.Parse(dataGroup[0]);
               int itmCategory = int.Parse(dataGroup[1]);
               int itmType = int.Parse(dataGroup[2]);
               int itmCount = int.Parse(dataGroup[3]);
               string itmData = dataGroup[4];
               string itmPalettes = dataGroup[5];
               //string itmPalette1 = dataGroup[5];
               //string itmPalette2 = dataGroup[6];
               //string itmPalettes = Item.parseItmPalette(new string[2] { itmPalette1, itmPalette2 });
               
               Item.Category categoryType = (Item.Category) itmCategory;

               Item otherItem = new Item {
                  category = categoryType,
                  itemTypeId = itmType,
                  id = itmId,
                  count = itmCount,
                  data = itmData,
                  paletteNames = itmPalettes
               }.getCastItem();

               newItemList.Add(otherItem);

               switch (categoryType) {
                  case Item.Category.Weapon:
                     WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(itmType);
                     if (weaponData != null) {
                        otherItem.setBasicInfo(weaponData.equipmentName, weaponData.equipmentDescription,
                           weaponData.equipmentIconPath);
                     } else {
                        D.editorLog("Failed to gather data for weapon: " + itmType + " : " + weaponData, Color.red);
                     }
                     break;
                  case Item.Category.Armor:
                     ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(itmType);
                     if (armorData != null) {
                        otherItem.setBasicInfo(armorData.equipmentName, armorData.equipmentDescription,
                           armorData.equipmentIconPath);
                     } else {
                        D.editorLog("Failed to gather data for armor: " + itmType + " : " + armorData + " : " + itmId, Color.red);
                     }
                     break;
                  case Item.Category.Hats:
                     HatStatData hatData = EquipmentXMLManager.self.getHatData(itmType);
                     if (hatData != null) {
                        otherItem.setBasicInfo(hatData.equipmentName, hatData.equipmentDescription,
                           hatData.equipmentIconPath);
                     } else {
                        D.editorLog("Failed to gather data for hat: " + itmType + " : " + hatData + " : " + itmId, Color.red);
                     }
                     break;
                  default:
                     otherItem.setBasicInfo(otherItem.getName(), otherItem.getDescription(), otherItem.getIconPath());
                     break;
               }
               index++;
            }
         }

         return newItemList;
      }
   }
}