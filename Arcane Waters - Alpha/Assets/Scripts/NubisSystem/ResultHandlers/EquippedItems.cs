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

      // The gear items of the player
      public Item ringItem;
      public Item necklaceItem;
      public Item trinketItem;
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
         Item ringItem = new Item();
         Item necklaceItem = new Item();
         Item trinketItem = new Item();

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
                  int itemDurability = 0;
                  int itemCount = 1;
                  string itemData = "";

                  try {
                     itemDurability = int.Parse(dataGroup[4]);
                     itemCount = int.Parse(dataGroup[5]);
                     itemData = dataGroup[6];
                  } catch {
                     D.log("Failed to parse durability and item data");
                  }

                  switch (itemCategory) {
                     case Item.Category.Weapon:
                        weaponItem = new Item {
                           id = itemID,
                           category = Item.Category.Weapon,
                           itemTypeId = itemTypeID,
                           data = itemData,
                           paletteNames = paletteNames,
                           durability = itemDurability,
                           count = itemCount
                        };
                        break;
                     case Item.Category.Armor:
                        armorItem = new Item {
                           id = itemID,
                           category = Item.Category.Armor,
                           itemTypeId = itemTypeID,
                           data = itemData,
                           paletteNames = paletteNames,
                           durability = itemDurability,
                           count = itemCount
                        };
                        break;
                     case Item.Category.Hats:
                        hatItem = new Item {
                           id = itemID,
                           category = Item.Category.Hats,
                           itemTypeId = itemTypeID,
                           data = itemData,
                           paletteNames = paletteNames,
                           durability = itemDurability,
                           count = itemCount
                        };
                        break;
                     case Item.Category.Ring:
                        ringItem = new Item {
                           id = itemID,
                           category = Item.Category.Ring,
                           itemTypeId = itemTypeID,
                           data = itemData,
                           paletteNames = paletteNames,
                           durability = itemDurability,
                           count = itemCount
                        };
                        break;
                     case Item.Category.Necklace:
                        necklaceItem = new Item {
                           id = itemID,
                           category = Item.Category.Necklace,
                           itemTypeId = itemTypeID,
                           data = itemData,
                           paletteNames = paletteNames,
                           durability = itemDurability,
                           count = itemCount
                        };
                        break;
                     case Item.Category.Trinket:
                        trinketItem = new Item {
                           id = itemID,
                           category = Item.Category.Trinket,
                           itemTypeId = itemTypeID,
                           data = itemData,
                           paletteNames = paletteNames,
                           durability = itemDurability,
                           count = itemCount
                        };
                        break;
                  }
               }
            }
         }

         equippedItemData.weaponItem = weaponItem;
         equippedItemData.armorItem = armorItem;
         equippedItemData.hatItem = hatItem;
         equippedItemData.ringItem = ringItem;
         equippedItemData.necklaceItem = necklaceItem;
         equippedItemData.trinketItem = trinketItem;

         return equippedItemData;
      }
   }
}