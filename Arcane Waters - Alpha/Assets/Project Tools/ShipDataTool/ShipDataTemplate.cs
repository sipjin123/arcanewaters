using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipDataTemplate : MonoBehaviour {
   #region Public Variables

   // Name of the ship 
   public Text nameText;

   // Index of the ship 
   public Text indexText;

   // Button for showing the panel in charge of editing the ship data
   public Button editButton;

   // Button for deleting a ship data
   public Button deleteButton;

   // Icon of the ship
   public Image itemIcon;

   // Button for duplicating this template
   public Button duplicateButton;

   // The xml id of this template
   public int xml_id;

   // An object to determine if the sql data is enabled
   public GameObject enabledIndicator;

   #endregion

   public void updateItemDisplay (ShipData resultItem, bool isActive) {
      string newName = "Undefined";
      try {
         newName = resultItem.shipName + " (" + ((Ship.Type) resultItem.shipType).ToString() + ")";
      } catch {
      }

      nameText.text = newName;
      indexText.text = "ID# " + ((int) resultItem.shipID).ToString();
      enabledIndicator.SetActive(isActive);

      if (!MasterToolAccountManager.canAlterData()) {
         deleteButton.gameObject.SetActive(false);
         duplicateButton.gameObject.SetActive(false);
      }

      if (MasterToolAccountManager.PERMISSION_LEVEL == AdminManager.Type.ContentWriter) {
         if (resultItem.shipType != Ship.Type.None && !ShipDataToolManager.self.didUserCreateData((int) resultItem.shipType)) {
            deleteButton.gameObject.SetActive(false);
            editButton.gameObject.SetActive(false);
         }
      }
   }
}
