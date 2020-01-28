using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CraftableItemTemplate : MonoBehaviour {
   #region Public Variables

   // Name of the craftable item
   public Text nameText;

   // Index of the craftable item
   public Text indexText;

   // Button for showing the panel in charge of editing the ingredients
   public Button editButton;

   // Button for deleting a craftable item
   public Button deleteButton;

   // Button for duplicating a craftable item
   public Button duplicateButton;

   // Icon of the item
   public Image itemIcon;

   #endregion

   public void updateItemDisplay(Item resultItem) {
      string newName = "Undefined";
      try {
         newName = Util.getItemName(resultItem.category, resultItem.itemTypeId);
      } catch {
      }

      nameText.text = newName;
      indexText.text = "ID# "+resultItem.itemTypeId.ToString();

      if (!MasterToolAccountManager.canAlterData()) {
         duplicateButton.gameObject.SetActive(false);
         deleteButton.gameObject.SetActive(false);
      }

      if (MasterToolAccountManager.PERMISSION_LEVEL == AdminManager.Type.ContentWriter) {
         if (Util.hasValidEntryName(nameText.text) && !CraftingToolManager.self.didUserCreateData(nameText.text)) {
            deleteButton.gameObject.SetActive(false);
            editButton.gameObject.SetActive(false);
         }
      }
   }

   #region Private Variables
      
   #endregion
}
