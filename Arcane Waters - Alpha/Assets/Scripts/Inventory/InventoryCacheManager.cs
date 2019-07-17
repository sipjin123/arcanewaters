using UnityEngine;
using System.Collections.Generic;

public class InventoryCacheManager : MonoBehaviour
{
   #region Public Variables
   public List<Item> itemList, rawItemList;

   public static InventoryCacheManager self;

   public bool hasInitialized;
   #endregion

   private void Awake () {
      self = this;
   }

   public void FetchInventory () {
      Global.player.rpc.Cmd_RequestItemsFromServer(1, 15);
   }

   public void receiveItemsFromServer (UserObjects userObjects, int pageNumber, int gold, int gems, int totalItemCount, int equippedArmorId, int equippedWeaponId, Item[] itemArray) {
      hasInitialized = true;
      itemList = new List<Item>();
      rawItemList = new List<Item>();
      foreach (Item item in itemArray) {
         rawItemList.Add(item.getCastItem());
         if (item.category == Item.Category.CraftingIngredients) {
            var findItem = itemList.Find(_ => _.category == Item.Category.CraftingIngredients &&
            ((CraftingIngredients.Type) _.itemTypeId == (CraftingIngredients.Type) item.itemTypeId));
            if (findItem != null) {
               int index = itemList.IndexOf(findItem);
               itemList[index].count++;
            } else {
               itemList.Add(item.getCastItem());
            }
         } else {
            itemList.Add(item.getCastItem());
         }
      }
   }
}
