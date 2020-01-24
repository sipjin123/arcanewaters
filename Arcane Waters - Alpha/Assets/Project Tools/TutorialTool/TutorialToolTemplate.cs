using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialToolTemplate : MonoBehaviour {
   #region Public Variables

   // Name of the data 
   public Text nameText;

   // Index of the data 
   public Text indexText;

   // Button for showing the panel in charge of editing the data
   public Button editButton;

   // Button for deleting a data
   public Button deleteButton;

   // Icon of the item
   public Image itemIcon;

   // Button for duplicating this template
   public Button duplicateButton;

   #endregion

   private void OnEnable () {
      if (!MasterToolAccountManager.canAlterData()) {
         duplicateButton.gameObject.SetActive(false);
         deleteButton.gameObject.SetActive(false);
      }

      if (Util.hasValidEntryName(nameText.text) && !TutorialToolManager.self.didUserCreateData(nameText.text)) {
         deleteButton.gameObject.SetActive(false);
         editButton.gameObject.SetActive(false);
      }
   }

   #region Private Variables

   #endregion
}
