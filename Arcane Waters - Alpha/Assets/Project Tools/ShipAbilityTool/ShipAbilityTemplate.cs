using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipAbilityTemplate : MonoBehaviour
{
   #region Public Variables

   // Name of the template
   public Text nameText;

   // Index of the template 
   public Text indexText;

   // Button for showing the panel in charge of editing the template
   public Button editButton;

   // Button for deleting an template
   public Button deleteButton;

   // Icon of the template
   public Image itemIcon;

   // Button for duplicating this template
   public Button duplicateButton;

   #endregion

   private void OnEnable () {
      if (!MasterToolAccountManager.canAlterData()) {
         duplicateButton.gameObject.SetActive(false);
         deleteButton.gameObject.SetActive(false);
      }

      if (Util.hasValidEntryName(nameText.text) && !ShipAbilityToolManager.self.didUserCreateData(nameText.text)) {
         deleteButton.gameObject.SetActive(false);
         editButton.gameObject.SetActive(false);
      }
   }

   #region Private Variables

   #endregion
}
