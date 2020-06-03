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
               string itmPalette1 = dataGroup[5];
               string itmPalette2 = dataGroup[6];
               Item.Category categoryType = (Item.Category) itmCategory;

               switch (categoryType) {
                  case Item.Category.Weapon:
                     WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(itmType);
                     try {
                        Item weaponItem = new Item {
                           category = Item.Category.Weapon,
                           itemTypeId = itmType,
                           id = itmId,
                           itemDescription = weaponData.equipmentDescription,
                           itemName = weaponData.equipmentName,
                           iconPath = weaponData.equipmentIconPath,
                           data = WeaponStatData.serializeWeaponStatData(weaponData),
                           paletteName1 = itmPalette1,
                           paletteName2 = itmPalette2
                        };

                        newItemList.Add(weaponItem);
                     } catch {
                        D.editorLog("Failed to gather data for weapon: " + itmType + " : " + weaponData, Color.red);
                     }
                     break;
                  case Item.Category.Armor:
                     ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(itmType);
                     try {
                        Item armorItem = new Item {
                           category = Item.Category.Armor,
                           itemTypeId = itmType,
                           id = itmId,
                           itemDescription = armorData.equipmentDescription,
                           itemName = armorData.equipmentName,
                           iconPath = armorData.equipmentIconPath,
                           data = ArmorStatData.serializeArmorStatData(armorData),
                           paletteName1 = itmPalette1,
                           paletteName2 = itmPalette2
                        };

                        newItemList.Add(armorItem);
                     } catch {
                        D.editorLog("Failed to gather data for armor: " + itmType + " : " + armorData, Color.red);
                     }
                     break;
                  case Item.Category.Hats:
                     HatStatData hatData = EquipmentXMLManager.self.getHatData(itmType);
                     if (hatData != null) {
                        Item hatItem = new Item {
                           category = Item.Category.Hats,
                           itemTypeId = itmType,
                           id = itmId,
                           itemDescription = hatData.equipmentDescription,
                           itemName = hatData.equipmentName,
                           iconPath = hatData.equipmentIconPath,
                           data = HatStatData.serializeHatStatData(hatData),
                           paletteName1 = itmPalette1,
                           paletteName2 = itmPalette2
                        };

                        newItemList.Add(hatItem);
                     } else {
                        D.editorLog("Failed to process data of Hat: " + itmType, Color.red);
                     }
                     break;
                  case Item.Category.CraftingIngredients:
                     Item craftingIngredient = new Item {
                        category = categoryType,
                        itemTypeId = itmType,
                        id = itmId,
                        data = "",
                        paletteName1 = itmPalette1,
                        paletteName2 = itmPalette2
                     };

                     Item castedItem = craftingIngredient.getCastItem();
                     craftingIngredient.itemDescription = castedItem.getDescription();
                     craftingIngredient.itemName = castedItem.getName();
                     craftingIngredient.iconPath = castedItem.getIconPath();
                     craftingIngredient.count = itmCount;

                     newItemList.Add(craftingIngredient);
                     break;
                  default:
                     D.editorLog("Unsupported category: " + itmCategory);
                     break;
               }
               index++;
            }
         }

         return newItemList;
      }
   }
}