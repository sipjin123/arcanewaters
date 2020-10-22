using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Events;

namespace NubisDataHandling {
   public class EquippedItemData {
      // The current weapon of the player
      public Item weaponItem;

      // The current armor of the player
      public Item armorItem;

      // The current hat of the player
      public Item hatItem;
   }

   [Serializable]
   public class NubisEquippedItemEvent : UnityEvent<EquippedItemData> {
   }


   public static class EquippedItems {
      public static EquippedItemData processEquippedItemData (string contentData) {
         EquippedItemData equippedItemData = new EquippedItemData();
         Item weaponItem = new Item();
         Item armorItem = new Item();
         Item hatItem = new Item();

         string splitter = "[next]";
         string[] rawItemGroup = contentData.Split(new string[] { splitter }, StringSplitOptions.None);

         for (int i = 0; i < rawItemGroup.Length; i++) {
            string itemGroup = rawItemGroup[i];
            string subSplitter = "[space]";
            string[] dataGroup = itemGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);

            if (dataGroup.Length > 0) {
               // Crafting ingredients have no crafting data
               if (dataGroup.Length >= 4) {
                  int itemID = int.Parse(dataGroup[0]);
                  Item.Category itemCategory = (Item.Category) int.Parse(dataGroup[1]);
                  int itemTypeID = int.Parse(dataGroup[2]);
                  string paletteNames = "";
                  try {
                     paletteNames = dataGroup[3];
                  } catch {
                     paletteNames = "armor_one_white, armor_two_white, , ";
                  }

                  switch (itemCategory) {
                     case Item.Category.Weapon:
                        weaponItem = new Item {
                           id = itemID,
                           category = Item.Category.Weapon,
                           itemTypeId = itemTypeID,
                           data = "",
                           paletteNames = paletteNames
                        };
                        break;
                     case Item.Category.Armor:
                        armorItem = new Item {
                           id = itemID,
                           category = Item.Category.Armor,
                           itemTypeId = itemTypeID,
                           data = "",
                           paletteNames = paletteNames
                        };
                        break;
                     case Item.Category.Hats:
                        hatItem = new Item {
                           id = itemID,
                           category = Item.Category.Hats,
                           itemTypeId = itemTypeID,
                           data = "",
                           paletteNames = paletteNames
                        };
                        break;
                  }
               }
            }
         }

         equippedItemData.weaponItem = weaponItem;
         equippedItemData.armorItem = armorItem;
         equippedItemData.hatItem = hatItem;

         return equippedItemData;
      }
   }
}