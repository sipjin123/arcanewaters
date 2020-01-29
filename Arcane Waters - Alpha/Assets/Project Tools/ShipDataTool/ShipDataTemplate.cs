using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipDataTemplate : GenericEntryTemplate {
   #region Public Variables

   // The xml id of this template
   public int xml_id;

   // An object to determine if the sql data is enabled
   public GameObject enabledIndicator;

   #endregion

   public void updateItemDisplay (ShipData resultItem, bool isActive) {
      string newName = resultItem.shipName + " (" + ((Ship.Type) resultItem.shipType).ToString() + ")";

      updateDisplay(newName, (int) resultItem.shipID);
      setIDRestriction(xml_id);
      enabledIndicator.SetActive(isActive);
   }
}
