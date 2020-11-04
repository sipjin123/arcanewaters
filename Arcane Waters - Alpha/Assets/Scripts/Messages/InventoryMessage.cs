using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class InventoryMessage : NetworkMessage
{
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The User Objects for this player
   public UserObjects userObjects;

   // The selected categories
   public Item.Category[] categories;

   // The page number that was requested
   public int pageNumber;

   // The total gold amount we have on hand
   public int gold;

   // The total gems amount we have on hand
   public int gems;

   // The total item count we have in our inventory
   public int totalItemCount;

   // The currently equipped armor and weapon IDs, if any
   public int equippedArmorId;
   public int equippedWeaponId;

   // The array of items included for the page we requested
   public Item[] itemArray;

   #endregion

   public InventoryMessage () { }

   public InventoryMessage (uint netId, UserObjects userObjects, Item.Category[] categories, int pageNumber, int gold, int gems, int totalItemCount, int equippedArmorId, int equippedWeaponId, Item[] itemArray) {
      this.netId = netId;
      this.userObjects = userObjects;
      this.categories = categories;
      this.pageNumber = pageNumber;
      this.gold = gold;
      this.gems = gems;
      this.totalItemCount = totalItemCount;
      this.equippedArmorId = equippedArmorId;
      this.equippedWeaponId = equippedWeaponId;
      this.itemArray = itemArray;
   }
}