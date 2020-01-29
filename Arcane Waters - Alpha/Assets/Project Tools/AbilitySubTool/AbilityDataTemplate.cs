using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AbilityDataTemplate : GenericEntryTemplate
{
   #region Public Variables

   // The raw name of the item
   public string actualName;

   #endregion

   public void updateItemDisplay (BasicAbilityData resultItem) {
      actualName = resultItem.itemName;
      modifyDisplay(resultItem.itemName, resultItem.itemID);
   }

   #region Private Variables

   #endregion
}