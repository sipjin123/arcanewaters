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
            Global.player.rpc.Cmd_RequestItemsFromServer(1, 15);
         }
      }
   }

   public void receiveItemsFromServer (UserObjects userObjects, int pageNumber, int gold, int gems, int totalItemCount, int equippedArmorId, int equippedWeaponId, Item[] itemArray) {
      hasInitialized = true;
      itemList = new List<Item>();
      List<Item> craftableList = new List<Item>();

      foreach (Item item in itemArray) {
         itemList.Add(item.getCastItem());
         if (item.category == Item.Category.Blueprint) {
            Item convertedItem = Blueprint.getItemData(item.itemTypeId);
            craftableList.Add(convertedItem);
         }
      }

      // Only the clients will need to request the crafting data from the server
      if (!NetworkServer.active) {
         Global.player.rpc.Cmd_RequestCraftingItems(craftableList.ToArray());
      }
   }
}