﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CraftableItemTemplate : GenericEntryTemplate {
   #region Public Variables

   #endregion

   public void updateItemDisplay(Item resultItem) {
      string newName = "Undefined";
      newName = Util.getItemName(resultItem.category, resultItem.itemTypeId);
      updateDisplay(newName, resultItem.itemTypeId);
      setNameRestriction(newName);
   }

   #region Private Variables
      
   #endregion
}
