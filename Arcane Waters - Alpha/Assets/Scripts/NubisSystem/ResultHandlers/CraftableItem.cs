using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Events;

namespace NubisDataHandling {
   public class CraftableItemData {
      // The result of the crafting
      public Item craftableItem;

      // The crafting status
      public Blueprint.Status craftingStatus;

      // The crafting requirements
      public CraftableItemRequirements craftableRequirements;
   }

   [Serializable]
   public class NubisCraftableItemEvent : UnityEvent<List<CraftableItemData>> {
   }

   public static class CraftableItem {
      // This Function translates the raw text data into a craftable item group (In order to get crafting info, this class requires an initial fetch for the crafting ingredients the user currently has)
      public static List<CraftableItemData> processCraftableGroups (string contentData, List<Item> craftableIngredients, Item.Category category) {
         List<CraftableItemData> craftableItems = new List<CraftableItemData>();

         // Grab the crafting data from the request
         string rawData = contentData;
         string splitter = "[next]";
         string[] rawItemGroup = rawData.Split(new string[] { splitter }, StringSplitOptions.None);
         for (int i = 0; i < rawItemGroup.Length; i++) {
            string itemGroup = rawItemGroup[i];

            string subSplitter = "[space]";
            string[] dataGroup = itemGroup.Split(new string[] { subSplitter }, StringSplitOptions.None);
            if (dataGroup.Length > 0) {
               // Crafting ingredients have no crafting data
               if (dataGroup.Length >= 4) {
                  int itemID = int.Parse(dataGroup[0]);
                  Item.Category itemCategory = Item.Category.None;
                  if (category == Item.Category.None) {
                     itemCategory = (Item.Category) int.Parse(dataGroup[1]);
                  } else {
                     itemCategory = category;
                  }
                  int itemTypeID = int.Parse(dataGroup[2]);
                  string itemData = "";

                  CraftableItemRequirements craftableRequirements = CraftingManager.self.getCraftableData(itemCategory, itemTypeID);
                  if (craftableRequirements != null) {
                     string itemName = "";
                     string itemDesc = "";
                     string itemIconPath = "";

                     // Process the item as a weapon and extract the weapon data
                     if (craftableRequirements.resultItem.category == Item.Category.Weapon) {
                        WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(craftableRequirements.resultItem.itemTypeId);
                        itemDesc = weaponData.equipmentDescription;
                        itemIconPath = weaponData.equipmentIconPath;
                        itemData = WeaponStatData.serializeWeaponStatData(weaponData);
                     }

                     // Process the item as a armor and extract the armor data
                     if (craftableRequirements.resultItem.category == Item.Category.Armor) {
                        ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(craftableRequirements.resultItem.itemTypeId);
                        itemDesc = armorData.equipmentDescription;
                        itemIconPath = armorData.equipmentIconPath;
                        itemData = ArmorStatData.serializeArmorStatData(armorData);
                     }

                     // Process the item as a hat and extract the hat data
                     if (craftableRequirements.resultItem.category == Item.Category.Hats) {
                        HatStatData hatData = EquipmentXMLManager.self.getHatData(craftableRequirements.resultItem.itemTypeId);
                        itemName = hatData.equipmentName;
                        itemDesc = hatData.equipmentDescription;
                        itemIconPath = hatData.equipmentIconPath;
                        itemData = HatStatData.serializeHatStatData(hatData);
                     }

                     // Create the item
                     Item item = craftableRequirements.resultItem;
                     item.id = itemID;
                     item.data = itemData;
                     item.itemName = itemName;
                     item.itemDescription = itemDesc;
                     item.iconPath = itemIconPath;

                     // Determine the status of this craftable Item depending on the available ingredients
                     Blueprint.Status status = Blueprint.Status.Craftable;
                     if (craftableRequirements == null) {
                        status = Blueprint.Status.MissingRecipe;
                     } else {
                        // Compare the ingredients present in the inventory with the requirements
                        foreach (Item requiredIngredient in craftableRequirements.combinationRequirements) {
                           // Get the inventory ingredient, if there is any
                           Item inventoryIngredient = craftableIngredients.Find(s =>
                              s.itemTypeId == requiredIngredient.itemTypeId);

                           // Verify that there are enough items in the inventory stack
                           if (inventoryIngredient == null || inventoryIngredient.count < requiredIngredient.count) {
                              status = Blueprint.Status.NotCraftable;
                              break;
                           }
                        }
                     }

                     // Register the new data
                     CraftableItemData newCraftableData = new CraftableItemData {
                        craftableItem = item,
                        craftingStatus = status,
                        craftableRequirements = craftableRequirements
                     };
                     craftableItems.Add(newCraftableData);
                  }
               }
            }
         }

         return craftableItems;
      }
   }
}