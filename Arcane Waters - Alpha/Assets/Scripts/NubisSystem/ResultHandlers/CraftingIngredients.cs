using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace NubisDataHandling {
   [Serializable]
   public class NubisCraftingIngredientEvent : UnityEvent<List<Item>> {
   }

   public class CraftingIngredients {
      public static List<Item> processCraftingIngredients (string stringContent) {
         string rawData = stringContent;
         string splitter = "[next]";
         string[] rawItemGroup = rawData.Split(new string[] { splitter }, StringSplitOptions.None);

         List<Item> craftingIngredientList = new List<Item>();
         for (int i = 0; i < rawItemGroup.Length; i++) {
            string itemGroup = rawItemGroup[i];

            string subSplitter = "[space]";
            string[] dataGroup = itemGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);

            if (dataGroup.Length > 0) {
               if (dataGroup.Length == 4 || dataGroup.Length == 5) {
                  int itemID = int.Parse(dataGroup[0]);
                  Item.Category itemCategory = (Item.Category) int.Parse(dataGroup[1]);
                  int itemTypeID = int.Parse(dataGroup[2]);
                  int itemCount = 1;

                  if (dataGroup.Length == 5) {
                     itemCount = int.Parse(dataGroup[3]);
                  }

                  craftingIngredientList.Add(new Item {
                     id = itemID,
                     category = itemCategory,
                     itemTypeId = itemTypeID,
                     count = itemCount
                  });
               }
            }
         }

         return craftingIngredientList;
      } 
   }
}