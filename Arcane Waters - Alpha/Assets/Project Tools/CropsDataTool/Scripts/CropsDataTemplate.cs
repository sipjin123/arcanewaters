using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CropsDataTemplate : GenericEntryTemplate {
   #region Public Variables

   // The xml id of this template
   public int xmlId;

   // An object to determine if the sql data is enabled
   public GameObject enabledIndicator;

   #endregion

   public void updateItemDisplay (CropsData resultItem, bool isActive) {
      string newName = resultItem.xmlName + " (" + ((Crop.Type) resultItem.cropsType).ToString() + ")";

      updateDisplay(newName, (int) resultItem.xmlId);
      setIdRestriction(xmlId);
      enabledIndicator.SetActive(!isActive);
   }
}
