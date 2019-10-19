using UnityEngine;
using System.Collections.Generic;
using Mirror;

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
      if (!hasInitialized) {
         if (Global.player != null) {
            // Fetches all inventory for caching
            Global.player.rpc.Cmd_RequestItemsFromServer(1, 35, new Item.Category[] { Item.Category.None });
         }
      }
   }

   public void receiveItemsFromServer (UserObjects userObjects, int pageNumber, int gold, int gems, int totalItemCount, int equippedArmorId, int equippedWeaponId, Item[] itemArray) {
      hasInitialized = true;
      itemList = new List<Item>();

      foreach (Item item in itemArray) {
         itemList.Add(item.getCastItem());
      }
   }
}