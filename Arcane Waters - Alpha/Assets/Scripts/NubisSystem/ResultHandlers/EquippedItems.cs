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

      // The current armor  of the player
      public Item armorItem;
   }

   [Serializable]
   public class NubisEquippedItemEvent : UnityEvent<EquippedItemData> {
   }


   public static class EquippedItems {
      public static EquippedItemData processEquippedItemData (string contentData) {
         EquippedItemData equippedItemData = new EquippedItemData();
         Item weaponItem = new Item();
         Item armorItem = new Item();

         string splitter = "[next]";
         string[] rawItemGroup = contentData.Split(new string[] { splitter }, StringSplitOptions.None);

         for (int i = 0; i < rawItemGroup.Length; i++) {
            string itemGroup = rawItemGroup[i];
            string subSplitter = "[space]";
            string[] dataGroup = itemGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);

            if (dataGroup.Length > 0) {
               // Crafting ingredients have no crafting data
               if (dataGroup.Length == 5) {
                  int itemID = int.Parse(dataGroup[0]);
                  Item.Category itemCategory = (Item.Category) int.Parse(dataGroup[1]);
                  int itemTypeID = int.Parse(dataGroup[2]);

                  switch (itemCategory) {
                     case Item.Category.Weapon:
                        weaponItem = new Item {
                           id = itemID,
                           category = Item.Category.Weapon,
                           itemTypeId = itemTypeID,
                           data = dataGroup[3]
                        };
                        break;
                     case Item.Category.Armor:
                        armorItem = new Item {
                           id = itemID,
                           category = Item.Category.Armor,
                           itemTypeId = itemTypeID,
                           data = dataGroup[3]
                        };
                        break;
                  }
               }
            }
         }

         equippedItemData.weaponItem = weaponItem;
         equippedItemData.armorItem = armorItem;

         return equippedItemData;
      }
   }
}