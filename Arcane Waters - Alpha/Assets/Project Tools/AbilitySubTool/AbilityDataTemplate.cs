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

   // Determines if the template needs attention
   public GameObject warningIndicator;

   #endregion

   public void updateItemDisplay (BasicAbilityData resultItem) {
      actualName = resultItem.itemName;
      updateDisplay(resultItem.itemName, resultItem.itemID);
      setNameRestriction(actualName);
   }

   #region Private Variables

   #endregion
}