using UnityEngine;
using System.Collections.Generic;

public class InventoryCacheManager : MonoBehaviour
{
   #region Public Variables

   // Itemlist with stacking
   public List<Item> itemList;

   // Self reference
   public static InventoryCacheManager self;

   // If inventory has been cached
   public bool hasInitialized;

   #endregion

   private void Awake () {
      self = this;
   }

   public void fetchInventory () {
      Global.player.rpc.Cmd_RequestItemsFromServer(-1, 15);
   }

   public void receiveItemsFromServer (UserObjects userObjects, int pageNumber, int gold, int gems, int totalItemCount, int equippedArmorId, int equippedWeaponId, Item[] itemArray) {
      hasInitialized = true;
      itemList = new List<Item>();

      foreach (Item item in itemArray) {
         // Add non stacking Inventory to list
         if (item.category == Item.Category.CraftingIngredients) {
            Item findItem = itemList.Find(_ => _.category == Item.Category.CraftingIngredients &&
            ((CraftingIngredients.Type) _.itemTypeId == (CraftingIngredients.Type) item.itemTypeId));
            if (findItem != null) {
               // Add stack to existing inventory
               int index = itemList.IndexOf(findItem);
               itemList[index].count++;
            } else {
               // Add Non existent Crafting Material to inventory
               itemList.Add(item.getCastItem());
            }
         } else {
            // Add Non existent Non Crafting Material inventory
            itemList.Add(item.getCastItem());
         }
      }
   }
}