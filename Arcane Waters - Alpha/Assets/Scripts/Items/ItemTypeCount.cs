using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public struct ItemTypeCount
{
   #region Public Variables

   // An item can be uniquely identified by a combination of it's category and 'itemTypeId'
   public Item.Category category;
   public int itemTypeId;

   // How much of this type of item is there
   public int count;

   #endregion

   public bool sameTypeAs (ItemTypeCount other) {
      return category == other.category && itemTypeId == other.itemTypeId;
   }

   #region Private Variables

   #endregion
}
